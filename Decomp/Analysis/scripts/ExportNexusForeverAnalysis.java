// Export native-client reverse-engineering artifacts from a Ghidra headless run.
//
// Usage from analyzeHeadless:
//   -postScript ExportNexusForeverAnalysis.java <output-dir> [max-decompiled-functions]
//       [decompile-mode] [ghidra-version] [binary-fingerprint] [label-fingerprint]
//       [labels-applied] [cache-warm-mode] [max-warm-functions-per-run]
//
// The script writes a per-program folder containing:
//   summary.txt
//   strings.csv
//   string_xrefs.csv
//   imports.csv
//   functions.csv
//   selected_xrefs.csv
//   selected_reasons_summary.csv
//   selected_call_edges.csv
//   function_pointer_families.csv
//   function_pointer_family_slots.csv
//   selected_decompiled.c
//
// Selection is intentionally biased toward networking, packet, auth, and game-data
// strings/imports so the first pass is useful for NexusForever protocol/data mapping.
//@category NexusForever

import java.io.File;
import java.io.IOException;
import java.io.FileWriter;
import java.io.PrintWriter;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Locale;
import java.util.Map;
import java.util.Properties;
import java.util.Set;

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileOptions;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.decompiler.component.DecompilerUtils;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.DataIterator;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;
import ghidra.program.model.symbol.FlowType;
import ghidra.program.model.symbol.RefType;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolIterator;
import ghidra.program.model.symbol.SymbolTable;
import ghidra.program.model.symbol.SymbolType;

public class ExportNexusForeverAnalysis extends GhidraScript {
	private static final int DEFAULT_MAX_DECOMPILED = 200;
	private static final int DEFAULT_MAX_WARM_FUNCTIONS = 100;
	private static final int DECOMPILE_TIMEOUT_SECONDS = 45;
	private static final int MIN_FUNCTION_POINTER_FAMILY_SLOTS = 4;
	private static final int MAX_CELL_LENGTH = 8192;
	private static final String EXPORT_SCRIPT_VERSION = "8";
	private static final String DECOMPILE_MANIFEST_VERSION = "1";
	private static final String DECOMPILE_FRAGMENT_VERSION = "1";
	private static final String DECOMPILE_MODE_AUTO = "auto";
	private static final String DECOMPILE_MODE_FORCE = "force";
	private static final String DECOMPILE_MODE_SKIP = "skip";
	private static final String CACHE_WARM_MODE_OFF = "off";
	private static final String CACHE_WARM_MODE_INCREMENTAL = "incremental";
	private static final String CACHE_WARM_MODE_COMPLETE = "complete";
	private static final String DECOMPILE_MANIFEST_NAME = "selected_decompiled.manifest";
	private static final String DECOMPILE_CACHE_DIR_NAME = "selected_decompiled_cache";
	private static final String CANONICAL_CACHE_DIR_NAME = "functions";
	private static final String CACHE_WARM_SUMMARY_NAME = "decompile_cache_summary.properties";

	private static final String[] INTERESTING_STRING_KEYWORDS = {
		"packet", "opcode", "message", "client", "server", "auth", "login", "realm",
		"world", "network", "sts", "socket", "connect", "send", "recv", "encrypt", "decrypt",
		"crypt", "compress", "zlib", "archive", ".tbl", ".bin", ".xml", "lua",
		"spell", "ability", "telegraph", "cooldown", "proc", "aura", "buff", "debuff",
		"quest", "objective", "challenge", "achievement", "pathmission", "tutorial",
		"entity", "unit", "creature", "vehicle", "mount", "publicevent", "combat",
		"position", "movement", "velocity", "spline", "teleport", "item", "vendor", "loot",
		"craft", "trade", "auction", "chat", "mail", "whisper", "channel", "guild",
		"circle", "friend", "iccomm", "duel", "warparty", "matching", "queue", "raid",
		"dungeon", "instance", "housing", "property", "plug", "neighbor", "taxi", "flight",
		"transport", "portal", "storefront", "catalog", "currency", "reward", "datacube",
		"hoverboard", "path", "map", "houston", "wildstar"
	};

	private static final String[] HIGH_VALUE_STRING_PATTERNS = {
		"db\\worldsocket.tbl", "worldsocket", "network_sendmessagebyid",
		"db\\realmdatacenter.tbl", "realmdatacenter", "realmdatacenterid",
		"login.realm", "onloginstart", "onloginfinish", "onrequestgametoken",
		"requestgametoken", "consumegametoken", "tokenkeydata", "listmyaccounts",
		"loginstart", "loginfinish", "keydata", "netaddress", "clientnetaddress",
		"conntype", "connproducttype", "connappindex", "conndeployment", "connepoch",
		"producttype", "appindex", "appid", "appids", "notifyflags", "versionflags", "authprovidercode",
		"accessmask", "aliases", "alias", "userstatus", "servicetimeschedule",
		"externalaccount", "pccafe", "licenses", "loginname", "gameaccountid",
		"premastersecret", "authntoken", "serverrand", "serverpublickey", "serversignature",
		"sessionkey", "sessionticket", "sessiontoken", "gamecommand", "charactercreate",
		"characterselect", "toonhandle", "accountcurrency", "entitlement",
		"spellcastwithservicetoken", "servicetokencastresult",
		"spellcastfailed", "prereqfailuremessage", "spellcooldown", "spelltelegraph",
		"spell4base", "spell4", "procchance", "procspell", "actionset", "innatespell",
		"sapvital", "vitalmodifier", "combatlogvitalmodifier", "cmbtlog.disablevitalmodifier",
		"monservicetokencost", "monrezservicetokencost", "bwakehereservicetoken", "wakeherecooldown",
		"monaltcostrapidtransport", "moncostrapidtransport", "brapidtransportallowed",
		"getrapidtransportcooldown", "rapidtransportresult", "rapidtransport_invalid",
		"clientrapidtransport", "rapid transport price:", "rapid transport to $1n?", "rapidtransport",
		"gettaxisforworld", "getplayertaxiunit", "purchaseflightpath", "invoketaxiwindow",
		"flightpath", "instanceportal", "selectedportal", "portalteleport",
		"setsendmessageresultfunction", "btaxiallowed", "btransportallowed",
		"stsinetsocket", "socketcrypt", "publiceventobjectivetype",
		"publiceventobjectivenotificationmode", "publiceventobjectivecategory", "publiceventstatus",
		"defendobjectiveunits", "game.publicevent", "game.publiceventobjective", "tspell4idability",
		"questobjective", "questtracker", "questdirection", "queststate", "pathmission",
		"pathexplorer", "db\\pathexploreractivate.tbl", "db\\pathexplorernode.tbl",
		"db\\pathexplorerpowermap.tbl", "db\\pathexplorerscavengerclue.tbl",
		"db\\pathexplorerscavengerhunt.tbl", "explorernode", "explorerhunt",
		"explorerpowermap", "castpathexplorersearching", "game.pathmission.getexplorer",
		"pathscientist", "db\\pathscientistcreatureinfo.tbl",
		"db\\pathscientistdatacubediscovery.tbl", "db\\pathscientistexperimentation.tbl",
		"db\\pathscientistexperimentationpattern.tbl", "db\\pathscientistfieldstudy.tbl",
		"db\\pathscientistspecimensurvey.tbl", "db\\pathscientistscanbotprofile.tbl",
		"scientistmission", "scientistdatacubediscovery", "scientistfieldstudy",
		"scientistspecimensurvey", "scientistexperimentation", "scanbotprofile",
		"setscannername", "dismissscanbot", "game.pathmission.getscientist",
		"pathsettler", "db\\pathsettlerhub.tbl", "db\\pathsettlerimprovement.tbl",
		"db\\pathsettlerimprovementgroup.tbl", "db\\pathsettlerinfrastructure.tbl",
		"db\\pathsettlermayor.tbl", "db\\pathsettlersheriff.tbl", "settlermayor",
		"settlersheriff", "pathsettlerhub", "pathsettlerimprovement",
		"pathsettlerimprovementgroup", "settlerbuildstatus",
		"game.pathmission.getsettler", "pathsoldier", "db\\pathsoldieractivate.tbl",
		"db\\pathsoldierassassinate.tbl", "db\\pathsoldierevent.tbl",
		"db\\pathsoldiereventwave.tbl", "db\\pathsoldierswat.tbl",
		"db\\pathsoldiertowerdefense.tbl", "soldierholdout", "soldierevent",
		"pathsoldierassassinate", "pathsoldiereventwave", "pathsoldierswat",
		"pathsoldiertowerdefense", "game.pathmission.getsoldierholdout",
		"game.soldierevent", "challengecompleted", "challengetracker",
		"achievementchecklist", "achievementtitle",
		"galacticarchive", "tutorialprompt", "tutorial", "hoverboard", "ridersreef",
		"showinstancegamemodedialog", "hideinstancegamemodedialog", "raidinforesponse",
		"strsavedinstanceid", "bremovessingleinstance", "matchjoined", "matchingpenalty",
		"matchingqueue", "matchingrole", "queuedplayers", "readycheck", "warpartyinvite",
		"guildinvite", "guildrank", "circleinvite", "friendrequest", "ignorelist",
		"mailattachment", "duelrequest", "duelcancel", "duelstart", "iccomm",
		"housingneighbor", "housingplug", "decorplug", "propertymodifier", "neighborhood",
		"storefront", "catalogoffer", "giftrecipient", "giftmessage",
		"tunitproperty", "publiceventunitpropertymodifier",
		"modifyinterruptarmor", "combatlogmodifyinterruptarmor",
		"cmbtlog.disablemodifyinterruptarmor", "interruptarmor",
		"combatlogccstatebreak", "ccstatebreak", "unitstateset", "setbusy",
		"unitcaster", "unitcasterowner",
		"channelupdate_loot", "floatermultiheal", "floatertransference",
		"combatlogbuildswitch", "combatlogdatacube"
	};

	private static final String[] INTERESTING_IMPORT_KEYWORDS = {
		"socket", "connect", "send", "recv", "select", "getaddrinfo", "gethost",
		"wsastartup", "wsasend", "wsarecv", "ioctlsocket", "closesocket",
		"crypt", "bcrypt", "winhttp", "wininet", "internet", "http", "readfile",
		"writefile", "createfile"
	};

	@Override
	public void run() throws Exception {
		if (currentProgram == null) {
			printerr("No current program.");
			return;
		}

		String[] args = getScriptArgs();
		File outputRoot = args.length > 0
			? new File(args[0])
			: askDirectory("Select NexusForever export output directory", "Choose");
		int maxDecompiled = args.length > 1 ? Integer.parseInt(args[1]) : DEFAULT_MAX_DECOMPILED;
		DecompileSettings decompileSettings = new DecompileSettings(
			args.length > 2 ? args[2] : DECOMPILE_MODE_AUTO,
			args.length > 3 ? args[3] : "",
			args.length > 4 ? args[4] : "",
			args.length > 5 ? args[5] : "",
			args.length > 6 && Boolean.parseBoolean(args[6]),
			args.length > 7 ? args[7] : CACHE_WARM_MODE_INCREMENTAL,
			args.length > 8 ? Integer.parseInt(args[8]) : DEFAULT_MAX_WARM_FUNCTIONS);

		File programDir = new File(outputRoot, sanitizePathPart(currentProgram.getName()));
		if (!programDir.exists() && !programDir.mkdirs()) {
			throw new RuntimeException("Could not create output directory: " + programDir);
		}

		Listing listing = currentProgram.getListing();
		FunctionManager functionManager = currentProgram.getFunctionManager();
		ReferenceManager referenceManager = currentProgram.getReferenceManager();
		Selection selection = new Selection();

		writeSummary(programDir, listing);
		writeFunctions(programDir, listing);
		writeImports(programDir, functionManager, selection);
		writeStringsAndXrefs(programDir, listing, referenceManager, functionManager, selection);
		selectLabeledFunctions(listing, selection);
		writeSelectedXrefs(programDir, selection, functionManager);
		writeSelectedReasonsReport(programDir, selection, functionManager, maxDecompiled);
		writeSelectedCallEdges(programDir, selection, functionManager, listing, referenceManager,
			maxDecompiled);
		writeFunctionPointerFamilies(programDir, functionManager, referenceManager, selection,
			maxDecompiled);
		DecompileCache decompileCache = new DecompileCache(programDir, decompileSettings);
		CacheWarmSummary cacheWarmSummary = warmFullProgramCache(programDir, functionManager,
			decompileSettings, decompileCache);
		writeSelectedDecompiled(programDir, selection, functionManager, maxDecompiled,
			decompileSettings, decompileCache);
		refreshCacheWarmSummary(functionManager, decompileCache, cacheWarmSummary);
		writeCacheWarmSummary(programDir, cacheWarmSummary);

		println("NexusForever export complete: " + programDir.getAbsolutePath());
	}

	private void writeSummary(File programDir, Listing listing) throws Exception {
		File out = new File(programDir, "summary.txt");
		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println("Program: " + currentProgram.getName());
			writer.println("Executable path: " + currentProgram.getExecutablePath());
			writer.println("Executable format: " + currentProgram.getExecutableFormat());
			writer.println("Language: " + currentProgram.getLanguageID());
			writer.println("Compiler spec: " + currentProgram.getCompilerSpec().getCompilerSpecID());
			writer.println("Image base: " + currentProgram.getImageBase());
			writer.println("Min address: " + currentProgram.getMinAddress());
			writer.println("Max address: " + currentProgram.getMaxAddress());
			writer.println();
			writer.println("Memory blocks:");
			for (MemoryBlock block : currentProgram.getMemory().getBlocks()) {
				writer.printf("  %s,%s,%s,%d,%s,%s,%s%n",
					block.getName(),
					block.getStart(),
					block.getEnd(),
					block.getSize(),
					block.isRead(),
					block.isWrite(),
					block.isExecute());
			}
			writer.println();
			writer.println("Functions: " + countFunctions(listing));
		}
	}

	private void writeFunctions(File programDir, Listing listing) throws Exception {
		File out = new File(programDir, "functions.csv");
		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println("entry,name,namespace,body_address_count,thunk,external");
			FunctionIterator iterator = listing.getFunctions(true);
			while (iterator.hasNext() && !monitor.isCancelled()) {
				Function function = iterator.next();
				writer.printf("%s,%s,%s,%d,%s,%s%n",
					csv(function.getEntryPoint().toString()),
					csv(function.getName()),
					csv(function.getParentNamespace().getName(true)),
					function.getBody().getNumAddresses(),
					Boolean.toString(function.isThunk()),
					Boolean.toString(function.isExternal()));
			}
		}
	}

	private void writeImports(File programDir, FunctionManager functionManager, Selection selection)
			throws Exception {
		File out = new File(programDir, "imports.csv");
		SymbolTable symbolTable = currentProgram.getSymbolTable();
		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println("address,name,namespace,type,ref_count,interesting");
			SymbolIterator iterator = symbolTable.getExternalSymbols();
			while (iterator.hasNext() && !monitor.isCancelled()) {
				Symbol symbol = iterator.next();
				String fullName = symbol.getName(true);
				String lower = fullName.toLowerCase(Locale.ROOT);
				boolean interesting = containsAny(lower, INTERESTING_IMPORT_KEYWORDS);
				Reference[] refs = symbol.getReferences();
				writer.printf("%s,%s,%s,%s,%d,%s%n",
					csv(symbol.getAddress().toString()),
					csv(symbol.getName()),
					csv(symbol.getParentNamespace().getName(true)),
					csv(symbol.getSymbolType().toString()),
					refs.length,
					Boolean.toString(interesting));
				if (interesting && symbol.getSymbolType() == SymbolType.FUNCTION) {
					for (Reference ref : refs) {
						Function containing =
							functionManager.getFunctionContaining(ref.getFromAddress());
						if (containing != null) {
							selection.add(containing, "import:" + fullName, ref.getFromAddress());
						}
					}
				}
			}
		}
	}

	private void writeStringsAndXrefs(File programDir, Listing listing, ReferenceManager referenceManager,
			FunctionManager functionManager, Selection selection) throws Exception {
		File stringsOut = new File(programDir, "strings.csv");
		File refsOut = new File(programDir, "string_xrefs.csv");
		File hitsOut = new File(programDir, "interesting_strings.csv");
		try (
			PrintWriter stringsWriter = new PrintWriter(new FileWriter(stringsOut));
			PrintWriter refsWriter = new PrintWriter(new FileWriter(refsOut));
			PrintWriter hitsWriter = new PrintWriter(new FileWriter(hitsOut))) {
			stringsWriter.println("address,type,ref_count,interesting,value");
			refsWriter.println("string_address,xref_address,function_entry,function_name,value");
			hitsWriter.println("address,type,ref_count,matched_keyword,value");

			DataIterator iterator = listing.getDefinedData(true);
			while (iterator.hasNext() && !monitor.isCancelled()) {
				Data data = iterator.next();
				if (!data.hasStringValue()) {
					continue;
				}
				Object rawValue = data.getValue();
				if (rawValue == null) {
					continue;
				}
				String value = rawValue.toString();
				if (value.trim().isEmpty()) {
					continue;
				}
				String lowerValue = value.toLowerCase(Locale.ROOT);
				String keyword = firstMatch(lowerValue, INTERESTING_STRING_KEYWORDS);
				String highValue = firstMatch(lowerValue, HIGH_VALUE_STRING_PATTERNS);
				boolean interesting = keyword != null || highValue != null;
				String matchedKeyword = keyword != null ? keyword : highValue;
				ArrayList<Reference> refs = referencesTo(referenceManager, data.getMinAddress());

				stringsWriter.printf("%s,%s,%d,%s,%s%n",
					csv(data.getMinAddress().toString()),
					csv(data.getDataType().getDisplayName()),
					refs.size(),
					Boolean.toString(interesting),
					csv(limit(value)));

				if (interesting) {
					hitsWriter.printf("%s,%s,%d,%s,%s%n",
						csv(data.getMinAddress().toString()),
						csv(data.getDataType().getDisplayName()),
						refs.size(),
						csv(matchedKeyword),
						csv(limit(value)));
					for (Reference ref : refs) {
						Function containing =
							functionManager.getFunctionContaining(ref.getFromAddress());
						Function resolvedFunction = containing != null
							? containing
							: resolveNearbyDataFunction(listing, functionManager, ref.getFromAddress(), value);
						refsWriter.printf("%s,%s,%s,%s,%s%n",
							csv(data.getMinAddress().toString()),
							csv(ref.getFromAddress().toString()),
							csv(resolvedFunction == null ? "" : resolvedFunction.getEntryPoint().toString()),
							csv(resolvedFunction == null ? "" : resolvedFunction.getName()),
							csv(limit(value)));
						if (resolvedFunction != null) {
							if (highValue != null) {
								selection.add(resolvedFunction, "target:" + highValue + "@" +
									data.getMinAddress(), ref.getFromAddress());
							}
							if (keyword != null) {
								selection.add(resolvedFunction, "string:" + keyword + "@" +
									data.getMinAddress(), ref.getFromAddress());
							}
						}
					}
				}
			}
		}
	}

	private void writeSelectedXrefs(File programDir, Selection selection,
			FunctionManager functionManager) throws Exception {
		File out = new File(programDir, "selected_xrefs.csv");
		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println("function_entry,function_name,xref_address,reason");
			for (Map.Entry<Address, SelectedFunction> entry : selection.byEntry.entrySet()) {
				Function function = functionManager.getFunctionAt(entry.getKey());
				String functionName = function == null ? "" : function.getName();
				SelectedFunction selected = entry.getValue();
				for (String reason : selected.reasons) {
					writer.printf("%s,%s,%s,%s%n",
						csv(entry.getKey().toString()),
						csv(functionName),
						csv(selected.firstXref == null ? "" : selected.firstXref.toString()),
						csv(reason));
				}
			}
		}
	}

	private void writeSelectedReasonsReport(File programDir, Selection selection,
			FunctionManager functionManager, int maxDecompiled) throws Exception {
		File out = new File(programDir, "selected_reasons_summary.csv");
		ArrayList<SelectedFunction> ordered = new ArrayList<>(selection.byEntry.values());
		ordered.sort(Comparator.comparing((SelectedFunction item) -> item.priorityBucket())
			.thenComparing(item -> item.entry.toString()));
		ArrayList<SelectedFunction> selectedForDecompile =
			getSelectedFunctionsForDecompilation(selection, functionManager, maxDecompiled);
		Set<Address> selectedEntries = new LinkedHashSet<>();
		for (SelectedFunction selectedFunction : selectedForDecompile) {
			selectedEntries.add(selectedFunction.entry);
		}

		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println(
				"entry,name,priority_bucket,reason_count,selected_for_decompile,first_xref,reasons");
			for (SelectedFunction selectedFunction : ordered) {
				Function function = functionManager.getFunctionAt(selectedFunction.entry);
				String functionName = function == null ? "" : function.getName();
				writer.printf("%s,%s,%d,%d,%s,%s,%s%n",
					csv(selectedFunction.entry.toString()),
					csv(functionName),
					selectedFunction.priorityBucket(),
					selectedFunction.reasons.size(),
					Boolean.toString(selectedEntries.contains(selectedFunction.entry)),
					csv(selectedFunction.firstXref == null ? "" : selectedFunction.firstXref.toString()),
					csv(joinReasons(selectedFunction)));
			}
		}
	}

	private void writeSelectedCallEdges(File programDir, Selection selection,
			FunctionManager functionManager, Listing listing, ReferenceManager referenceManager,
			int maxDecompiled) throws Exception {
		File out = new File(programDir, "selected_call_edges.csv");
		ArrayList<SelectedFunction> ordered = new ArrayList<>(selection.byEntry.values());
		ordered.sort(Comparator.comparing((SelectedFunction item) -> item.priorityBucket())
			.thenComparing(item -> item.entry.toString()));
		ArrayList<SelectedFunction> selectedForDecompile =
			getSelectedFunctionsForDecompilation(selection, functionManager, maxDecompiled);
		Set<Address> selectedForDecompileEntries = new LinkedHashSet<>();
		for (SelectedFunction selectedFunction : selectedForDecompile) {
			selectedForDecompileEntries.add(selectedFunction.entry);
		}

		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println(
				"seed_entry,seed_name,seed_priority_bucket,seed_selected_for_decompile,relation," +
				"edge_type,site_address,neighbor_entry,neighbor_name,neighbor_namespace," +
				"neighbor_thunk,neighbor_external,neighbor_selected,neighbor_priority_bucket," +
				"neighbor_selected_for_decompile,seed_reasons");

			for (SelectedFunction selectedFunction : ordered) {
				Function seed = functionManager.getFunctionAt(selectedFunction.entry);
				if (seed == null || seed.isExternal()) {
					continue;
				}

				ArrayList<SelectedCallEdge> edges = getSelectedCallEdges(seed, functionManager, listing,
					referenceManager);
				edges.sort(Comparator.comparing((SelectedCallEdge edge) -> edge.relation)
					.thenComparing(edge -> edge.siteAddress.toString())
					.thenComparing(edge -> edge.neighbor.getEntryPoint().toString()));

				for (SelectedCallEdge edge : edges) {
					SelectedFunction neighborSelected =
						selection.byEntry.get(edge.neighbor.getEntryPoint());
					writer.printf("%s,%s,%d,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s%n",
						csv(seed.getEntryPoint().toString()),
						csv(seed.getName()),
						selectedFunction.priorityBucket(),
						Boolean.toString(selectedForDecompileEntries.contains(seed.getEntryPoint())),
						csv(edge.relation),
						csv(edge.edgeType),
						csv(edge.siteAddress.toString()),
						csv(edge.neighbor.getEntryPoint().toString()),
						csv(edge.neighbor.getName()),
						csv(edge.neighbor.getParentNamespace().getName(true)),
						Boolean.toString(edge.neighbor.isThunk()),
						Boolean.toString(edge.neighbor.isExternal()),
						Boolean.toString(neighborSelected != null),
						csv(neighborSelected == null
							? ""
							: Integer.toString(neighborSelected.priorityBucket())),
						Boolean.toString(selectedForDecompileEntries.contains(
							edge.neighbor.getEntryPoint())),
						csv(joinReasons(selectedFunction)));
				}
			}
		}
	}

	private void writeFunctionPointerFamilies(File programDir, FunctionManager functionManager,
			ReferenceManager referenceManager, Selection selection, int maxDecompiled)
			throws Exception {
		File familiesOut = new File(programDir, "function_pointer_families.csv");
		File slotsOut = new File(programDir, "function_pointer_family_slots.csv");
		ArrayList<SelectedFunction> selectedForDecompile =
			getSelectedFunctionsForDecompilation(selection, functionManager, maxDecompiled);
		Set<Address> selectedForDecompileEntries = new LinkedHashSet<>();
		for (SelectedFunction selectedFunction : selectedForDecompile) {
			selectedForDecompileEntries.add(selectedFunction.entry);
		}

		ArrayList<FunctionPointerFamily> families =
			getFunctionPointerFamilies(functionManager, referenceManager);
		try (
			PrintWriter familiesWriter = new PrintWriter(new FileWriter(familiesOut));
			PrintWriter slotsWriter = new PrintWriter(new FileWriter(slotsOut))) {
			familiesWriter.println(
				"family_address,block_name,block_permissions,entry_count,named_entry_count," +
				"thunk_entry_count,selected_entry_count,selected_for_decompile_count," +
				"start_ref_count,slot0_entry,slot0_name,slot1_entry,slot1_name," +
				"slot2_entry,slot2_name,slot3_entry,slot3_name");
			slotsWriter.println(
				"family_address,slot_index,slot_address,slot_ref_count,function_entry," +
				"function_name,function_namespace,named,thunk,selected,selected_for_decompile");

			for (FunctionPointerFamily family : families) {
				int namedCount = 0;
				int thunkCount = 0;
				int selectedCount = 0;
				int selectedForDecompileCount = 0;
				for (FunctionPointerFamilySlot slot : family.slots) {
					if (slot.named) {
						namedCount++;
					}
					if (slot.function.isThunk()) {
						thunkCount++;
					}
					if (selection.byEntry.containsKey(slot.function.getEntryPoint())) {
						selectedCount++;
					}
					if (selectedForDecompileEntries.contains(slot.function.getEntryPoint())) {
						selectedForDecompileCount++;
					}
				}

				familiesWriter.printf("%s,%s,%s,%d,%d,%d,%d,%d,%d,%s,%s,%s,%s,%s,%s,%s,%s%n",
					csv(family.address.toString()),
					csv(family.blockName),
					csv(family.blockPermissions),
					family.slots.size(),
					namedCount,
					thunkCount,
					selectedCount,
					selectedForDecompileCount,
					family.slots.get(0).referenceCount,
					csv(getFamilyPreviewEntry(family, 0)),
					csv(getFamilyPreviewName(family, 0)),
					csv(getFamilyPreviewEntry(family, 1)),
					csv(getFamilyPreviewName(family, 1)),
					csv(getFamilyPreviewEntry(family, 2)),
					csv(getFamilyPreviewName(family, 2)),
					csv(getFamilyPreviewEntry(family, 3)),
					csv(getFamilyPreviewName(family, 3)));

				for (int slotIndex = 0; slotIndex < family.slots.size(); slotIndex++) {
					FunctionPointerFamilySlot slot = family.slots.get(slotIndex);
					slotsWriter.printf("%s,%d,%s,%d,%s,%s,%s,%s,%s,%s,%s%n",
						csv(family.address.toString()),
						slotIndex,
						csv(slot.slotAddress.toString()),
						slot.referenceCount,
						csv(slot.function.getEntryPoint().toString()),
						csv(slot.function.getName()),
						csv(slot.function.getParentNamespace().getName(true)),
						Boolean.toString(slot.named),
						Boolean.toString(slot.function.isThunk()),
						Boolean.toString(selection.byEntry.containsKey(slot.function.getEntryPoint())),
						Boolean.toString(selectedForDecompileEntries.contains(
							slot.function.getEntryPoint())));
				}
			}
		}
	}

	private void selectLabeledFunctions(Listing listing, Selection selection) {
		FunctionIterator iterator = listing.getFunctions(true);
		while (iterator.hasNext() && !monitor.isCancelled()) {
			Function function = iterator.next();
			String comment = function.getComment();
			if (comment != null && comment.contains("NexusForever:")) {
				selection.add(function, "label:" + function.getName(), function.getEntryPoint());
			}
		}
	}

	private void writeSelectedDecompiled(File programDir, Selection selection,
			FunctionManager functionManager, int maxDecompiled, DecompileSettings settings,
			DecompileCache decompileCache)
			throws Exception {
		File out = new File(programDir, "selected_decompiled.c");
		File manifestFile = new File(programDir, DECOMPILE_MANIFEST_NAME);
		ArrayList<SelectedFunction> selected = getSelectedFunctionsForDecompilation(
			selection, functionManager, maxDecompiled);

		if (settings.isSkip()) {
			println("Skipping selected_decompiled.c for " + currentProgram.getName() +
				" (mode=skip). Existing artifact left untouched.");
			return;
		}

		String contextFingerprint = buildDecompileContextFingerprint(settings);
		String fingerprint = buildDecompileFingerprint(selected, functionManager, maxDecompiled,
			settings, contextFingerprint);
		if (settings.isAuto() && out.isFile() && manifestMatches(manifestFile, fingerprint)) {
			println("Reusing selected_decompiled.c for " + currentProgram.getName() +
				" (fingerprint unchanged).");
			return;
		}

		LinkedHashMap<Address, DecompileFragment> fragmentsByEntry = new LinkedHashMap<>();
		ArrayList<SelectedFunction> pendingDecompilation = new ArrayList<>();
		int reusedFragments = 0;
		for (SelectedFunction selectedFunction : selected) {
			Function function = functionManager.getFunctionAt(selectedFunction.entry);
			if (function == null || function.isExternal()) {
				continue;
			}

			DecompileFragment cachedFragment = decompileCache.tryReadOrImport(function);
			if (cachedFragment != null) {
				fragmentsByEntry.put(selectedFunction.entry, cachedFragment);
				reusedFragments++;
			}
			else {
				pendingDecompilation.add(selectedFunction);
			}
		}

		DecompInterface decompiler = pendingDecompilation.isEmpty() ? null : setUpDecompiler();
		boolean decompilerOpened = false;
		String decompilerOpenError = null;
		if (decompiler != null) {
			decompilerOpened = decompiler.openProgram(currentProgram);
			if (!decompilerOpened) {
				decompilerOpenError = decompiler.getLastMessage();
				println("Could not open program in decompiler for " + currentProgram.getName() +
					": " + decompilerOpenError);
			}
		}

		int decompiledFragments = 0;
		boolean completedAll = true;
		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println("/*");
			writer.println(" * Selected Ghidra decompiler output for " + currentProgram.getName());
			writer.println(" * Functions are selected by interesting imports/strings, not by proof of correctness.");
			writer.println(" */");
			writer.println();

			if (decompiler != null && !decompilerOpened) {
				writer.println("/* Could not open program in decompiler: " +
					decompilerOpenError + " */");
				writer.println();
			}

			for (SelectedFunction selectedFunction : selected) {
				if (monitor.isCancelled()) {
					completedAll = false;
					break;
				}
				Function function = functionManager.getFunctionAt(selectedFunction.entry);
				if (function == null || function.isExternal()) {
					continue;
				}

				DecompileFragment fragment = fragmentsByEntry.get(selectedFunction.entry);
				if (fragment == null) {
					if (decompilerOpened) {
						DecompileResults results =
							decompiler.decompileFunction(function, DECOMPILE_TIMEOUT_SECONDS, monitor);
						String body = results.decompileCompleted() && results.getDecompiledFunction() != null
							? results.getDecompiledFunction().getC()
							: "/* Decompile failed: " + results.getErrorMessage() + " */";
						fragment = new DecompileFragment(body, false);
						fragmentsByEntry.put(selectedFunction.entry, fragment);
						decompileCache.writeCanonicalFragment(function, body);
						decompiledFragments++;
					}
					else {
						completedAll = false;
						fragment = new DecompileFragment(
							"/* Could not open program in decompiler: " + decompilerOpenError + " */",
							false);
						fragmentsByEntry.put(selectedFunction.entry, fragment);
					}
				}

				writer.println("/*");
				writer.println(" * Function: " + function.getName() + " @ " +
					function.getEntryPoint());
				writer.println(" * Reasons: " + String.join("; ", selectedFunction.reasons));
				writer.println(" */");
				writer.println(fragment.body);
				writer.println();
			}
		}
		finally {
			if (decompiler != null) {
				decompiler.dispose();
			}
		}

		println("Rendered selected_decompiled.c for " + currentProgram.getName() +
			": reused=" + reusedFragments + ", decompiled=" + decompiledFragments + ".");

		if (completedAll && (pendingDecompilation.isEmpty() || decompilerOpened)) {
			writeDecompileManifest(manifestFile, out, fingerprint, selected, functionManager,
				maxDecompiled, settings, contextFingerprint, reusedFragments, decompiledFragments);
		}
	}

	private CacheWarmSummary warmFullProgramCache(File programDir, FunctionManager functionManager,
			DecompileSettings settings, DecompileCache decompileCache) throws Exception {
		ArrayList<Function> functions = getInternalFunctions(functionManager);
		CacheWarmSummary summary = new CacheWarmSummary();
		summary.mode = settings.cacheWarmMode;
		summary.totalInternalFunctions = functions.size();
		summary.canonicalCacheDir = decompileCache.canonicalCacheDir.getAbsolutePath();
		summary.binaryFingerprint = settings.binaryFingerprint;

		if (settings.isSkip() || settings.isCacheWarmOff()) {
			for (Function function : functions) {
				if (decompileCache.tryReadCanonical(function) != null) {
					summary.canonicalCachedFragments++;
				}
			}
			summary.remainingUncached = summary.totalInternalFunctions - summary.canonicalCachedFragments;
			writeCacheWarmSummary(programDir, summary);
			println("Full-program cache warm skipped for " + currentProgram.getName() +
				" (mode=" + summary.mode + "). cached=" + summary.canonicalCachedFragments +
				", remaining=" + summary.remainingUncached + ".");
			return summary;
		}

		DecompInterface decompiler = null;
		boolean decompilerOpened = false;
		try {
			for (Function function : functions) {
				if (monitor.isCancelled()) {
					break;
				}

				DecompileFragment cached = decompileCache.tryReadCanonical(function);
				if (cached != null) {
					summary.canonicalCachedFragments++;
					summary.skippedAlreadyExported++;
					continue;
				}

				if (settings.isCacheWarmIncremental() &&
						summary.warmedThisRun >= settings.maxWarmFunctionsPerRun) {
					summary.remainingUncached++;
					continue;
				}

				if (decompiler == null) {
					decompiler = setUpDecompiler();
					decompilerOpened = decompiler.openProgram(currentProgram);
					if (!decompilerOpened) {
						println("Could not open program in decompiler for cache warm " +
							currentProgram.getName() + ": " + decompiler.getLastMessage());
						break;
					}
				}

				DecompileResults results =
					decompiler.decompileFunction(function, DECOMPILE_TIMEOUT_SECONDS, monitor);
				String body = results.decompileCompleted() && results.getDecompiledFunction() != null
					? results.getDecompiledFunction().getC()
					: "/* Decompile failed: " + results.getErrorMessage() + " */";
				decompileCache.writeCanonicalFragment(function, body);
				summary.warmedThisRun++;
				summary.canonicalCachedFragments++;
			}
		}
		finally {
			if (decompiler != null) {
				decompiler.dispose();
			}
		}

		summary.remainingUncached += Math.max(0,
			summary.totalInternalFunctions - summary.canonicalCachedFragments - summary.remainingUncached);
		writeCacheWarmSummary(programDir, summary);
		println("Full-program cache warm for " + currentProgram.getName() +
			": cached=" + summary.canonicalCachedFragments +
			", warmed=" + summary.warmedThisRun +
			", skippedExisting=" + summary.skippedAlreadyExported +
			", remaining=" + summary.remainingUncached + ".");
		return summary;
	}

	private void refreshCacheWarmSummary(FunctionManager functionManager,
			DecompileCache decompileCache, CacheWarmSummary summary) {
		summary.totalInternalFunctions = getInternalFunctions(functionManager).size();
		summary.canonicalCachedFragments = decompileCache.countCanonicalFragments();
		summary.remainingUncached = Math.max(0,
			summary.totalInternalFunctions - summary.canonicalCachedFragments);
	}

	private ArrayList<Function> getInternalFunctions(FunctionManager functionManager) {
		ArrayList<Function> functions = new ArrayList<>();
		FunctionIterator iterator = currentProgram.getListing().getFunctions(true);
		while (iterator.hasNext() && !monitor.isCancelled()) {
			Function function = iterator.next();
			if (!function.isExternal()) {
				functions.add(function);
			}
		}
		functions.sort(Comparator.comparing(function -> function.getEntryPoint().toString()));
		return functions;
	}

	private void writeCacheWarmSummary(File programDir, CacheWarmSummary summary)
			throws Exception {
		Properties properties = new Properties();
		properties.setProperty("summary.version", "1");
		properties.setProperty("script.version", EXPORT_SCRIPT_VERSION);
		properties.setProperty("timestampUtc", java.time.Instant.now().toString());
		properties.setProperty("program.name", safeString(currentProgram.getName()));
		properties.setProperty("binary.fingerprint", safeString(summary.binaryFingerprint));
		properties.setProperty("cache.mode", safeString(summary.mode));
		properties.setProperty("cache.directory", safeString(summary.canonicalCacheDir));
		properties.setProperty("functions.totalInternal", Integer.toString(summary.totalInternalFunctions));
		properties.setProperty("cache.canonicalCachedFragments", Integer.toString(summary.canonicalCachedFragments));
		properties.setProperty("cache.warmedThisRun", Integer.toString(summary.warmedThisRun));
		properties.setProperty("cache.reusedExistingFragments", Integer.toString(summary.reusedExistingFragments));
		properties.setProperty("cache.remainingUncached", Integer.toString(summary.remainingUncached));
		properties.setProperty("cache.skippedAlreadyExported", Integer.toString(summary.skippedAlreadyExported));

		File summaryFile = new File(programDir, CACHE_WARM_SUMMARY_NAME);
		try (var writer = Files.newBufferedWriter(summaryFile.toPath(), StandardCharsets.UTF_8)) {
			properties.store(writer, "Full-program decompile cache summary");
		}
	}

	private ArrayList<SelectedFunction> getSelectedFunctionsForDecompilation(Selection selection,
			FunctionManager functionManager, int maxDecompiled) {
		ArrayList<SelectedFunction> ordered = new ArrayList<>(selection.byEntry.values());
		ordered.sort(Comparator.comparing((SelectedFunction item) -> item.priorityBucket())
			.thenComparing(item -> item.entry.toString()));

		ArrayList<SelectedFunction> selected = new ArrayList<>();
		if (maxDecompiled <= 0) {
			return selected;
		}

		for (SelectedFunction selectedFunction : ordered) {
			if (selected.size() >= maxDecompiled) {
				break;
			}
			Function function = functionManager.getFunctionAt(selectedFunction.entry);
			if (function == null || function.isExternal()) {
				continue;
			}
			selected.add(selectedFunction);
		}
		return selected;
	}

	private boolean manifestMatches(File manifestFile, String expectedFingerprint) {
		if (!manifestFile.isFile()) {
			return false;
		}

		Properties properties = new Properties();
		try (var reader = Files.newBufferedReader(manifestFile.toPath(), StandardCharsets.UTF_8)) {
			properties.load(reader);
		}
		catch (IOException ex) {
			println("Ignoring unreadable decompile manifest " + manifestFile.getAbsolutePath() +
				": " + ex.getMessage());
			return false;
		}

		return DECOMPILE_MANIFEST_VERSION.equals(properties.getProperty("manifest.version"))
			&& EXPORT_SCRIPT_VERSION.equals(properties.getProperty("script.version"))
			&& expectedFingerprint.equals(properties.getProperty("fingerprint"));
	}

	private void writeDecompileManifest(File manifestFile, File outputFile, String fingerprint,
			ArrayList<SelectedFunction> selected, FunctionManager functionManager,
			int maxDecompiled, DecompileSettings settings, String contextFingerprint,
			int reusedFragments, int decompiledFragments) throws Exception {
		Properties properties = new Properties();
		properties.setProperty("manifest.version", DECOMPILE_MANIFEST_VERSION);
		properties.setProperty("script.version", EXPORT_SCRIPT_VERSION);
		properties.setProperty("fingerprint", fingerprint);
		properties.setProperty("context.fingerprint", contextFingerprint);
		properties.setProperty("program.name", safeString(currentProgram.getName()));
		properties.setProperty("program.executablePath", safeString(currentProgram.getExecutablePath()));
		properties.setProperty("program.executableFormat", safeString(currentProgram.getExecutableFormat()));
		properties.setProperty("program.language", safeString(currentProgram.getLanguageID()));
		properties.setProperty("program.imageBase", safeString(currentProgram.getImageBase()));
		properties.setProperty("ghidra.version", settings.ghidraVersion);
		properties.setProperty("binary.fingerprint", settings.binaryFingerprint);
		properties.setProperty("labels.applied", Boolean.toString(settings.labelsApplied));
		properties.setProperty("labels.fingerprint", settings.labelFingerprint);
		properties.setProperty("max.decompiledFunctions", Integer.toString(maxDecompiled));
		properties.setProperty("decompile.timeoutSeconds",
			Integer.toString(DECOMPILE_TIMEOUT_SECONDS));
		properties.setProperty("output.file", outputFile.getName());
		properties.setProperty("output.exists", Boolean.toString(outputFile.isFile()));
		properties.setProperty("selected.count", Integer.toString(selected.size()));
		properties.setProperty("cache.reusedFragments", Integer.toString(reusedFragments));
		properties.setProperty("cache.decompiledFragments",
			Integer.toString(decompiledFragments));

		for (int index = 0; index < selected.size(); index++) {
			SelectedFunction selectedFunction = selected.get(index);
			Function function = functionManager.getFunctionAt(selectedFunction.entry);
			String keyPrefix = "selected." + index + ".";
			properties.setProperty(keyPrefix + "entry", selectedFunction.entry.toString());
			properties.setProperty(keyPrefix + "name", safeFunctionName(function));
			properties.setProperty(keyPrefix + "priority",
				Integer.toString(selectedFunction.priorityBucket()));
			properties.setProperty(keyPrefix + "reasons", joinReasons(selectedFunction));
		}

		try (var writer = Files.newBufferedWriter(manifestFile.toPath(), StandardCharsets.UTF_8)) {
			properties.store(writer, "Selected decompiler manifest");
		}
	}

	private String buildDecompileContextFingerprint(DecompileSettings settings) throws Exception {
		StringBuilder builder = new StringBuilder();
		appendFingerprintValue(builder, "fragment.version", DECOMPILE_FRAGMENT_VERSION);
		appendFingerprintValue(builder, "script.version", EXPORT_SCRIPT_VERSION);
		appendFingerprintValue(builder, "program.name", currentProgram.getName());
		appendFingerprintValue(builder, "program.executablePath", currentProgram.getExecutablePath());
		appendFingerprintValue(builder, "program.executableFormat", currentProgram.getExecutableFormat());
		appendFingerprintValue(builder, "program.language", safeString(currentProgram.getLanguageID()));
		appendFingerprintValue(builder, "program.imageBase", safeString(currentProgram.getImageBase()));
		appendFingerprintValue(builder, "ghidra.version", settings.ghidraVersion);
		appendFingerprintValue(builder, "binary.fingerprint", settings.binaryFingerprint);
		appendFingerprintValue(builder, "labels.applied",
			Boolean.toString(settings.labelsApplied));
		appendFingerprintValue(builder, "labels.fingerprint", settings.labelFingerprint);
		appendFingerprintValue(builder, "decompile.timeoutSeconds",
			Integer.toString(DECOMPILE_TIMEOUT_SECONDS));
		return sha256Hex(builder.toString());
	}

	private String buildDecompileFingerprint(ArrayList<SelectedFunction> selected,
			FunctionManager functionManager, int maxDecompiled, DecompileSettings settings,
			String contextFingerprint)
			throws Exception {
		StringBuilder builder = new StringBuilder();
		appendFingerprintValue(builder, "manifest.version", DECOMPILE_MANIFEST_VERSION);
		appendFingerprintValue(builder, "script.version", EXPORT_SCRIPT_VERSION);
		appendFingerprintValue(builder, "context.fingerprint", contextFingerprint);
		appendFingerprintValue(builder, "max.decompiledFunctions",
			Integer.toString(maxDecompiled));
		appendFingerprintValue(builder, "selected.count", Integer.toString(selected.size()));

		for (int index = 0; index < selected.size(); index++) {
			SelectedFunction selectedFunction = selected.get(index);
			Function function = functionManager.getFunctionAt(selectedFunction.entry);
			appendFingerprintValue(builder, "selected." + index + ".entry",
				selectedFunction.entry.toString());
			appendFingerprintValue(builder, "selected." + index + ".name",
				safeFunctionName(function));
			appendFingerprintValue(builder, "selected." + index + ".priority",
				Integer.toString(selectedFunction.priorityBucket()));
			appendFingerprintValue(builder, "selected." + index + ".reasons",
				joinReasons(selectedFunction));
		}

		return sha256Hex(builder.toString());
	}

	private String buildDecompileFunctionFingerprint(Function function, DecompileSettings settings)
			throws Exception {
		StringBuilder builder = new StringBuilder();
		appendFingerprintValue(builder, "fragment.version", DECOMPILE_FRAGMENT_VERSION);
		appendFingerprintValue(builder, "binary.fingerprint", settings.binaryFingerprint);
		appendFingerprintValue(builder, "entry", function.getEntryPoint().toString());
		appendFingerprintValue(builder, "body.addressCount",
			Long.toString(function.getBody().getNumAddresses()));
		return sha256Hex(builder.toString());
	}

	private DecompileFragment tryReadDecompileFragment(File metadataFile, Function function,
			boolean canonical, String expectedProgramCacheToken) {
		File bodyFile = getDecompileFragmentBodyFile(metadataFile);
		if (!metadataFile.isFile() || !bodyFile.isFile()) {
			return null;
		}

		Properties properties = new Properties();
		try (var reader = Files.newBufferedReader(metadataFile.toPath(), StandardCharsets.UTF_8)) {
			properties.load(reader);
		}
		catch (IOException ex) {
			println("Ignoring unreadable decompile fragment metadata " + metadataFile.getAbsolutePath() +
				": " + ex.getMessage());
			return null;
		}

		if (!DECOMPILE_FRAGMENT_VERSION.equals(properties.getProperty("fragment.version")) ||
			!function.getEntryPoint().toString().equals(properties.getProperty("entry"))) {
			return null;
		}

		if (canonical && !safeString(expectedProgramCacheToken).equals(
				properties.getProperty("program.cacheToken"))) {
			return null;
		}

		try {
			String body = new String(Files.readAllBytes(bodyFile.toPath()), StandardCharsets.UTF_8);
			String expectedBodyHash = properties.getProperty("body.sha256");
			if (expectedBodyHash != null && !expectedBodyHash.equals(sha256Hex(body))) {
				println("Ignoring mismatched decompile fragment body " + bodyFile.getAbsolutePath());
				return null;
			}
			return new DecompileFragment(body, true);
		}
		catch (Exception ex) {
			println("Ignoring unreadable decompile fragment body " + bodyFile.getAbsolutePath() +
				": " + ex.getMessage());
			return null;
		}
	}

	private void writeDecompileFragment(File fragmentCacheDir, Function function,
			DecompileSettings settings, String programCacheToken, String fingerprint, String body)
			throws Exception {
		if (!fragmentCacheDir.exists() && !fragmentCacheDir.mkdirs()) {
			throw new IOException("Could not create decompile fragment cache directory: " +
				fragmentCacheDir.getAbsolutePath());
		}

		File metadataFile = getDecompileFragmentMetadataFile(fragmentCacheDir, function);
		File bodyFile = getDecompileFragmentBodyFile(metadataFile);
		Properties properties = new Properties();
		properties.setProperty("fragment.version", DECOMPILE_FRAGMENT_VERSION);
		properties.setProperty("script.version", EXPORT_SCRIPT_VERSION);
		properties.setProperty("fingerprint", fingerprint);
		properties.setProperty("program.cacheToken", programCacheToken);
		properties.setProperty("binary.fingerprint", settings.binaryFingerprint);
		properties.setProperty("entry", function.getEntryPoint().toString());
		properties.setProperty("name", safeFunctionName(function));
		properties.setProperty("body.addressCount", Long.toString(function.getBody().getNumAddresses()));
		properties.setProperty("body.file", bodyFile.getName());
		properties.setProperty("body.sha256", sha256Hex(body));
		Files.write(bodyFile.toPath(), body.getBytes(StandardCharsets.UTF_8));
		try (var writer = Files.newBufferedWriter(metadataFile.toPath(), StandardCharsets.UTF_8)) {
			properties.store(writer, "Selected decompiler fragment");
		}
	}

	private File getDecompileFragmentMetadataFile(File fragmentCacheDir, Function function) {
		return new File(fragmentCacheDir,
			sanitizePathPart(function.getEntryPoint().toString()) + ".fragment.properties");
	}

	private File getDecompileFragmentBodyFile(File metadataFile) {
		String metadataName = metadataFile.getName();
		String bodyName = metadataName.endsWith(".fragment.properties")
			? metadataName.substring(0, metadataName.length() - ".fragment.properties".length()) + ".fragment.c"
			: metadataName + ".c";
		return new File(metadataFile.getParentFile(), bodyName);
	}

	private String buildCanonicalProgramToken(DecompileSettings settings) throws Exception {
		if (!settings.binaryFingerprint.trim().isEmpty()) {
			return sanitizePathPart(settings.binaryFingerprint.toLowerCase(Locale.ROOT));
		}

		StringBuilder builder = new StringBuilder();
		appendFingerprintValue(builder, "program.name", currentProgram.getName());
		appendFingerprintValue(builder, "program.executablePath", currentProgram.getExecutablePath());
		appendFingerprintValue(builder, "program.imageBase", safeString(currentProgram.getImageBase()));
		return sanitizePathPart("program_" + sha256Hex(builder.toString()));
	}

	private void appendFingerprintValue(StringBuilder builder, String key, String value) {
		builder.append(key)
			.append('=')
			.append(value == null ? "" : value)
			.append('\n');
	}

	private String joinReasons(SelectedFunction selectedFunction) {
		return String.join(" || ", selectedFunction.reasons);
	}

	private String safeFunctionName(Function function) {
		return function == null ? "" : safeString(function.getName());
	}

	private String safeString(Object value) {
		return value == null ? "" : value.toString();
	}

	private String sha256Hex(String value) throws Exception {
		MessageDigest digest = MessageDigest.getInstance("SHA-256");
		byte[] hash = digest.digest(value.getBytes(StandardCharsets.UTF_8));
		StringBuilder builder = new StringBuilder(hash.length * 2);
		for (byte item : hash) {
			builder.append(Character.forDigit((item >> 4) & 0xf, 16));
			builder.append(Character.forDigit(item & 0xf, 16));
		}
		return builder.toString();
	}

	private DecompInterface setUpDecompiler() {
		DecompileOptions options = DecompilerUtils.getDecompileOptions(state.getTool(), currentProgram);
		DecompInterface decompiler = new DecompInterface();
		decompiler.setOptions(options);
		decompiler.toggleCCode(true);
		decompiler.toggleSyntaxTree(true);
		decompiler.setSimplificationStyle("decompile");
		return decompiler;
	}

	private ArrayList<FunctionPointerFamily> getFunctionPointerFamilies(
			FunctionManager functionManager, ReferenceManager referenceManager) {
		ArrayList<FunctionPointerFamily> families = new ArrayList<>();
		Memory memory = currentProgram.getMemory();
		int pointerSize = currentProgram.getDefaultPointerSize();

		for (MemoryBlock block : memory.getBlocks()) {
			if (!block.isInitialized() || block.isExecute()) {
				continue;
			}
			long minimumBytes = (long)pointerSize * MIN_FUNCTION_POINTER_FAMILY_SLOTS;
			if (block.getSize() < minimumBytes) {
				continue;
			}

			long offset = 0;
			long maximumFamilyStart = block.getSize() - minimumBytes;
			while (offset <= maximumFamilyStart && !monitor.isCancelled()) {
				Address familyAddress = addAddressOffset(block.getStart(), offset);
				if (familyAddress == null) {
					break;
				}

				if (getFunctionPointerFamilySlot(memory, functionManager, referenceManager,
						familyAddress) == null) {
					offset += pointerSize;
					continue;
				}

				ArrayList<FunctionPointerFamilySlot> slots = new ArrayList<>();
				long scanOffset = offset;
				while (scanOffset <= block.getSize() - pointerSize && !monitor.isCancelled()) {
					Address slotAddress = addAddressOffset(block.getStart(), scanOffset);
					if (slotAddress == null) {
						break;
					}

					FunctionPointerFamilySlot slot = getFunctionPointerFamilySlot(memory,
						functionManager, referenceManager, slotAddress);
					if (slot == null) {
						break;
					}

					slots.add(slot);
					scanOffset += pointerSize;
				}

				if (slots.size() >= MIN_FUNCTION_POINTER_FAMILY_SLOTS) {
					families.add(new FunctionPointerFamily(familyAddress, block.getName(),
						getBlockPermissions(block), slots));
					offset = scanOffset;
					continue;
				}

				offset += pointerSize;
			}
		}

		return families;
	}

	private ArrayList<SelectedCallEdge> getSelectedCallEdges(Function function,
			FunctionManager functionManager, Listing listing, ReferenceManager referenceManager) {
		ArrayList<SelectedCallEdge> edges = new ArrayList<>();
		Set<String> seen = new LinkedHashSet<>();
		InstructionIterator instructions = listing.getInstructions(function.getBody(), true);
		while (instructions.hasNext() && !monitor.isCancelled()) {
			Instruction instruction = instructions.next();
			if (!isCallOrJump(instruction)) {
				continue;
			}

			for (Address flow : instruction.getFlows()) {
				Function callee = resolveFunction(functionManager, flow);
				if (callee == null) {
					continue;
				}

				addSelectedCallEdge(edges, seen, "callee", classifyEdgeType(instruction),
					instruction.getAddress(), callee);
			}
		}

		ReferenceIterator references = referenceManager.getReferencesTo(function.getEntryPoint());
		while (references.hasNext() && !monitor.isCancelled()) {
			Reference reference = references.next();
			if (!isCallOrJump(reference)) {
				continue;
			}

			Function caller = functionManager.getFunctionContaining(reference.getFromAddress());
			if (caller == null) {
				caller = resolveFunction(functionManager, reference.getFromAddress());
			}
			if (caller == null) {
				continue;
			}

			addSelectedCallEdge(edges, seen, "caller", classifyEdgeType(reference),
				reference.getFromAddress(), caller);
		}

		return edges;
	}

	private FunctionPointerFamilySlot getFunctionPointerFamilySlot(Memory memory,
			FunctionManager functionManager, ReferenceManager referenceManager, Address slotAddress) {
		Long pointerValue = readPointer(memory, slotAddress);
		if (pointerValue == null || pointerValue.longValue() == 0L) {
			return null;
		}

		Address targetAddress = toAbsoluteAddress(pointerValue.longValue());
		if (targetAddress == null) {
			return null;
		}

		Function function = functionManager.getFunctionAt(targetAddress);
		if (function == null || function.isExternal()) {
			return null;
		}

		return new FunctionPointerFamilySlot(slotAddress, function,
			countReferences(referenceManager, slotAddress), hasMeaningfulFunctionName(function));
	}

	private void addSelectedCallEdge(ArrayList<SelectedCallEdge> edges, Set<String> seen,
			String relation, String edgeType, Address siteAddress, Function neighbor) {
		String key = relation + "|" + edgeType + "|" + siteAddress + "|" +
			neighbor.getEntryPoint();
		if (!seen.add(key)) {
			return;
		}

		edges.add(new SelectedCallEdge(relation, edgeType, siteAddress, neighbor));
	}

	private boolean isCallOrJump(Instruction instruction) {
		if (instruction == null) {
			return false;
		}

		FlowType flowType = instruction.getFlowType();
		return flowType != null && (flowType.isCall() || flowType.isJump());
	}

	private boolean isCallOrJump(Reference reference) {
		if (reference == null || reference.getReferenceType() == null) {
			return false;
		}

		RefType referenceType = reference.getReferenceType();
		return referenceType.isCall() || referenceType.isJump();
	}

	private String classifyEdgeType(Instruction instruction) {
		if (instruction == null || instruction.getFlowType() == null) {
			return "";
		}

		FlowType flowType = instruction.getFlowType();
		if (flowType.isCall()) {
			return "call";
		}
		if (flowType.isJump()) {
			return "jump";
		}
		return flowType.toString().toLowerCase(Locale.ROOT);
	}

	private String classifyEdgeType(Reference reference) {
		if (reference == null || reference.getReferenceType() == null) {
			return "";
		}

		RefType referenceType = reference.getReferenceType();
		if (referenceType.isCall()) {
			return "call";
		}
		if (referenceType.isJump()) {
			return "jump";
		}
		return referenceType.toString().toLowerCase(Locale.ROOT);
	}

	private Function resolveFunction(FunctionManager functionManager, Address address) {
		if (address == null) {
			return null;
		}

		Function function = functionManager.getFunctionAt(address);
		if (function == null) {
			function = functionManager.getFunctionContaining(address);
		}
		return function;
	}

	private Address addAddressOffset(Address address, long offset) {
		try {
			return address.add(offset);
		}
		catch (Exception ex) {
			return null;
		}
	}

	private Long readPointer(Memory memory, Address address) {
		try {
			int pointerSize = currentProgram.getDefaultPointerSize();
			if (pointerSize == 8) {
				return Long.valueOf(memory.getLong(address));
			}
			if (pointerSize == 4) {
				return Long.valueOf(Integer.toUnsignedLong(memory.getInt(address)));
			}
		}
		catch (Exception ex) {
			return null;
		}

		return null;
	}

	private Address toAbsoluteAddress(long value) {
		try {
			return toAddr(value);
		}
		catch (Exception ex) {
			return null;
		}
	}

	private int countReferences(ReferenceManager referenceManager, Address address) {
		int count = 0;
		ReferenceIterator iterator = referenceManager.getReferencesTo(address);
		while (iterator.hasNext()) {
			iterator.next();
			count++;
		}
		return count;
	}

	private boolean hasMeaningfulFunctionName(Function function) {
		if (function == null || function.getName() == null) {
			return false;
		}

		String lowerName = function.getName().toLowerCase(Locale.ROOT);
		return !lowerName.startsWith("fun_") &&
			!lowerName.startsWith("thunk_fun_") &&
			!lowerName.startsWith("nullsub_");
	}

	private String getBlockPermissions(MemoryBlock block) {
		StringBuilder builder = new StringBuilder(3);
		builder.append(block.isRead() ? 'r' : '-');
		builder.append(block.isWrite() ? 'w' : '-');
		builder.append(block.isExecute() ? 'x' : '-');
		return builder.toString();
	}

	private String getFamilyPreviewEntry(FunctionPointerFamily family, int slotIndex) {
		return slotIndex < family.slots.size()
			? family.slots.get(slotIndex).function.getEntryPoint().toString()
			: "";
	}

	private String getFamilyPreviewName(FunctionPointerFamily family, int slotIndex) {
		return slotIndex < family.slots.size()
			? family.slots.get(slotIndex).function.getName()
			: "";
	}

	private int countFunctions(Listing listing) {
		int count = 0;
		FunctionIterator iterator = listing.getFunctions(true);
		while (iterator.hasNext()) {
			iterator.next();
			count++;
		}
		return count;
	}

	private Function resolveNearbyDataFunction(Listing listing, FunctionManager functionManager,
			Address fromAddress, String hintName) {
		Data containingData = listing.getDefinedDataContaining(fromAddress);
		if (containingData == null) {
			return null;
		}

		int pointerSize = currentProgram.getDefaultPointerSize();
		ArrayList<Address> candidates = new ArrayList<>();
		Set<Address> seen = new LinkedHashSet<>();
		Map<Address, Function> nearbyFunctions = new LinkedHashMap<>();
		Address dataBase = containingData.getMinAddress();

		for (int i = -4; i <= 4; i++) {
			addCandidate(candidates, seen, fromAddress, pointerSize * (long)i);
		}
		for (int i = 0; i <= 4; i++) {
			addCandidate(candidates, seen, dataBase, pointerSize * (long)i);
		}

		for (Address candidate : candidates) {
			Function function = resolveFunctionPointerAt(
				listing.getDefinedDataContaining(candidate), candidate, functionManager);
			if (function != null) {
				nearbyFunctions.putIfAbsent(function.getEntryPoint(), function);
			}
		}

		if (nearbyFunctions.isEmpty()) {
			return null;
		}

		if (nearbyFunctions.size() == 1) {
			return nearbyFunctions.values().iterator().next();
		}

		String normalizedHint = normalizeIdentifier(hintName);
		Function bestFunction = null;
		int bestScore = 0;
		boolean tiedBest = false;
		for (Function function : nearbyFunctions.values()) {
			int score = scoreFunctionName(normalizedHint, function.getName());
			if (score > bestScore) {
				bestFunction = function;
				bestScore = score;
				tiedBest = false;
			}
			else if (score > 0 && score == bestScore) {
				tiedBest = true;
			}
		}

		return bestScore > 0 && !tiedBest ? bestFunction : null;
	}

	private void addCandidate(ArrayList<Address> candidates, Set<Address> seen, Address seed,
			long offset) {
		try {
			Address candidate = seed.getAddressSpace().getAddress(seed.getOffset() + offset);
			if (seen.add(candidate)) {
				candidates.add(candidate);
			}
		}
		catch (Exception ignored) {
		}
	}

	private Function resolveFunctionPointerAt(Data data, Address candidate,
			FunctionManager functionManager) {
		if (data == null) {
			return null;
		}

		if (data.getMinAddress().equals(candidate)) {
			Function function = functionFromValue(data.getValue(), functionManager);
			if (function != null) {
				return function;
			}
		}

		for (int i = 0; i < data.getNumComponents(); i++) {
			Function function = resolveFunctionPointerAt(data.getComponent(i), candidate, functionManager);
			if (function != null) {
				return function;
			}
		}

		return null;
	}

	private Function functionFromValue(Object value, FunctionManager functionManager) {
		if (!(value instanceof Address)) {
			return null;
		}

		Address target = (Address)value;
		Function function = functionManager.getFunctionAt(target);
		if (function == null) {
			function = functionManager.getFunctionContaining(target);
		}
		return function;
	}

	private int scoreFunctionName(String normalizedHint, String functionName) {
		if (normalizedHint == null || normalizedHint.isEmpty() || functionName == null) {
			return 0;
		}

		String normalizedFunction = normalizeIdentifier(functionName);
		if (normalizedFunction.isEmpty() || normalizedFunction.startsWith("fun")) {
			return 0;
		}

		if (normalizedFunction.contains(normalizedHint) || normalizedHint.contains(normalizedFunction)) {
			return 1000 + Math.min(normalizedHint.length(), normalizedFunction.length());
		}

		int commonPrefix = 0;
		int limit = Math.min(normalizedHint.length(), normalizedFunction.length());
		while (commonPrefix < limit && normalizedHint.charAt(commonPrefix) == normalizedFunction.charAt(commonPrefix)) {
			commonPrefix++;
		}

		return commonPrefix >= 8 ? commonPrefix : 0;
	}

	private String normalizeIdentifier(String value) {
		return value == null
			? ""
			: value.replaceAll("[^A-Za-z0-9]", "").toLowerCase(Locale.ROOT);
	}

	private ArrayList<Reference> referencesTo(ReferenceManager referenceManager, Address address) {
		ArrayList<Reference> refs = new ArrayList<>();
		ReferenceIterator iterator = referenceManager.getReferencesTo(address);
		while (iterator.hasNext()) {
			refs.add(iterator.next());
		}
		return refs;
	}

	private boolean containsAny(String value, String[] needles) {
		return firstMatch(value, needles) != null;
	}

	private String firstMatch(String value, String[] needles) {
		for (String needle : needles) {
			if (value.contains(needle)) {
				return needle;
			}
		}
		return null;
	}

	private String sanitizePathPart(String value) {
		return value.replaceAll("[^A-Za-z0-9._-]", "_");
	}

	private String limit(String value) {
		if (value.length() <= MAX_CELL_LENGTH) {
			return value;
		}
		return value.substring(0, MAX_CELL_LENGTH) + "...<truncated>";
	}

	private String csv(String value) {
		if (value == null) {
			return "\"\"";
		}
		String escaped = value.replace("\r", "\\r").replace("\n", "\\n").replace("\"", "\"\"");
		return "\"" + escaped + "\"";
	}

	private static class Selection {
		private final LinkedHashMap<Address, SelectedFunction> byEntry = new LinkedHashMap<>();

		void add(Function function, String reason, Address xrefAddress) {
			SelectedFunction selected = byEntry.get(function.getEntryPoint());
			if (selected == null) {
				selected = new SelectedFunction(function.getEntryPoint());
				byEntry.put(function.getEntryPoint(), selected);
			}
			selected.reasons.add(reason);
			if (selected.firstXref == null) {
				selected.firstXref = xrefAddress;
			}
		}
	}

	private static class SelectedFunction {
		private final Address entry;
		private final Set<String> reasons = new LinkedHashSet<>();
		private Address firstXref;

		SelectedFunction(Address entry) {
			this.entry = entry;
		}

		int priorityBucket() {
			for (String reason : reasons) {
				if (reason.startsWith("label:")) {
					return -2;
				}
			}
			for (String reason : reasons) {
				if (reason.startsWith("target:")) {
					return -1;
				}
			}
			for (String reason : reasons) {
				if (reason.startsWith("import:")) {
					return 0;
				}
			}
			return 1;
		}
	}

	private static class SelectedCallEdge {
		private final String relation;
		private final String edgeType;
		private final Address siteAddress;
		private final Function neighbor;

		SelectedCallEdge(String relation, String edgeType, Address siteAddress,
				Function neighbor) {
			this.relation = relation;
			this.edgeType = edgeType;
			this.siteAddress = siteAddress;
			this.neighbor = neighbor;
		}
	}

	private static class FunctionPointerFamily {
		private final Address address;
		private final String blockName;
		private final String blockPermissions;
		private final ArrayList<FunctionPointerFamilySlot> slots;

		FunctionPointerFamily(Address address, String blockName, String blockPermissions,
				ArrayList<FunctionPointerFamilySlot> slots) {
			this.address = address;
			this.blockName = blockName;
			this.blockPermissions = blockPermissions;
			this.slots = slots;
		}
	}

	private static class FunctionPointerFamilySlot {
		private final Address slotAddress;
		private final Function function;
		private final int referenceCount;
		private final boolean named;

		FunctionPointerFamilySlot(Address slotAddress, Function function, int referenceCount,
				boolean named) {
			this.slotAddress = slotAddress;
			this.function = function;
			this.referenceCount = referenceCount;
			this.named = named;
		}
	}

	private static class CacheWarmSummary {
		private String mode = "";
		private String canonicalCacheDir = "";
		private String binaryFingerprint = "";
		private int totalInternalFunctions;
		private int canonicalCachedFragments;
		private int warmedThisRun;
		private int reusedExistingFragments;
		private int remainingUncached;
		private int skippedAlreadyExported;
	}

	private class DecompileCache {
		private final DecompileSettings settings;
		private final File cacheRoot;
		private final File canonicalCacheDir;
		private final String programCacheToken;

		DecompileCache(File programDir, DecompileSettings settings) throws Exception {
			this.settings = settings;
			this.cacheRoot = new File(programDir, DECOMPILE_CACHE_DIR_NAME);
			this.programCacheToken = buildCanonicalProgramToken(settings);
			this.canonicalCacheDir = new File(new File(cacheRoot, CANONICAL_CACHE_DIR_NAME),
				programCacheToken);
			purgeLegacyCacheFolders();
		}

		DecompileFragment tryReadOrImport(Function function) {
			return tryReadCanonical(function);
		}

		DecompileFragment tryReadCanonical(Function function) {
			File metadataFile = getDecompileFragmentMetadataFile(canonicalCacheDir, function);
			return tryReadDecompileFragment(metadataFile, function, true, programCacheToken);
		}

		int countCanonicalFragments() {
			if (!canonicalCacheDir.isDirectory()) {
				return 0;
			}

			File[] files = canonicalCacheDir.listFiles((dir, name) ->
				name.endsWith(".fragment.properties"));
			return files == null ? 0 : files.length;
		}

		void writeCanonicalFragment(Function function, String body) throws Exception {
			if (tryReadCanonical(function) != null) {
				return;
			}

			writeDecompileFragment(canonicalCacheDir, function, settings, programCacheToken,
				buildDecompileFunctionFingerprint(function, settings), body);
		}

		private void purgeLegacyCacheFolders() throws IOException {
			if (!cacheRoot.isDirectory()) {
				return;
			}

			File[] children = cacheRoot.listFiles();
			if (children == null) {
				return;
			}

			for (File child : children) {
				if (CANONICAL_CACHE_DIR_NAME.equals(child.getName())) {
					continue;
				}
				deleteRecursively(child);
			}
		}

		private void deleteRecursively(File file) throws IOException {
			if (file.isDirectory()) {
				File[] children = file.listFiles();
				if (children != null) {
					for (File child : children) {
						deleteRecursively(child);
					}
				}
			}

			if (file.exists() && !file.delete()) {
				throw new IOException("Could not delete legacy decompile cache path: " +
					file.getAbsolutePath());
			}
		}
	}

	private static class DecompileFragment {
		private final String body;
		private final boolean reusedFromCache;

		DecompileFragment(String body, boolean reusedFromCache) {
			this.body = body;
			this.reusedFromCache = reusedFromCache;
		}
	}

	private static class DecompileSettings {
		private final String mode;
		private final String ghidraVersion;
		private final String binaryFingerprint;
		private final String labelFingerprint;
		private final boolean labelsApplied;
		private final String cacheWarmMode;
		private final int maxWarmFunctionsPerRun;

		DecompileSettings(String mode, String ghidraVersion, String binaryFingerprint,
				String labelFingerprint, boolean labelsApplied, String cacheWarmMode,
				int maxWarmFunctionsPerRun) {
			this.mode = normalizeMode(mode);
			this.ghidraVersion = ghidraVersion == null ? "" : ghidraVersion;
			this.binaryFingerprint = binaryFingerprint == null ? "" : binaryFingerprint;
			this.labelFingerprint = labelFingerprint == null ? "" : labelFingerprint;
			this.labelsApplied = labelsApplied;
			this.cacheWarmMode = normalizeCacheWarmMode(cacheWarmMode);
			this.maxWarmFunctionsPerRun = Math.max(0, maxWarmFunctionsPerRun);
		}

		boolean isAuto() {
			return DECOMPILE_MODE_AUTO.equals(mode);
		}

		boolean isSkip() {
			return DECOMPILE_MODE_SKIP.equals(mode);
		}

		boolean isCacheWarmOff() {
			return CACHE_WARM_MODE_OFF.equals(cacheWarmMode);
		}

		boolean isCacheWarmIncremental() {
			return CACHE_WARM_MODE_INCREMENTAL.equals(cacheWarmMode);
		}

		private static String normalizeMode(String mode) {
			if (mode == null) {
				return DECOMPILE_MODE_AUTO;
			}

			String normalized = mode.trim().toLowerCase(Locale.ROOT);
			if (DECOMPILE_MODE_FORCE.equals(normalized) || DECOMPILE_MODE_SKIP.equals(normalized)) {
				return normalized;
			}
			return DECOMPILE_MODE_AUTO;
		}

		private static String normalizeCacheWarmMode(String mode) {
			if (mode == null) {
				return CACHE_WARM_MODE_INCREMENTAL;
			}

			String normalized = mode.trim().toLowerCase(Locale.ROOT);
			if (CACHE_WARM_MODE_OFF.equals(normalized) ||
					CACHE_WARM_MODE_COMPLETE.equals(normalized)) {
				return normalized;
			}
			return CACHE_WARM_MODE_INCREMENTAL;
		}
	}
}
