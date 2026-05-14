// Export native-client reverse-engineering artifacts from a Ghidra headless run.
//
// Usage from analyzeHeadless:
//   -postScript ExportNexusForeverAnalysis.java <output-dir> [max-decompiled-functions]
//
// The script writes a per-program folder containing:
//   summary.txt
//   strings.csv
//   imports.csv
//   functions.csv
//   selected_xrefs.csv
//   selected_decompiled.c
//
// Selection is intentionally biased toward networking, packet, auth, and game-data
// strings/imports so the first pass is useful for NexusForever protocol/data mapping.
//@category NexusForever

import java.io.File;
import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Locale;
import java.util.Map;
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
import ghidra.program.model.listing.Listing;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolIterator;
import ghidra.program.model.symbol.SymbolTable;
import ghidra.program.model.symbol.SymbolType;

public class ExportNexusForeverAnalysis extends GhidraScript {
	private static final int DEFAULT_MAX_DECOMPILED = 200;
	private static final int DECOMPILE_TIMEOUT_SECONDS = 45;
	private static final int MAX_CELL_LENGTH = 8192;

	private static final String[] INTERESTING_STRING_KEYWORDS = {
		"packet", "opcode", "message", "client", "server", "auth", "login", "realm",
		"world", "network", "sts", "socket", "connect", "send", "recv", "encrypt", "decrypt",
		"crypt", "compress", "zlib", "archive", ".tbl", ".bin", ".xml", "lua",
		"spell", "quest", "entity", "unit", "creature", "publicevent", "combat",
		"position", "movement", "item", "vendor", "loot", "chat", "path", "map",
		"houston", "wildstar"
	};

	private static final String[] HIGH_VALUE_STRING_PATTERNS = {
		"db\\worldsocket.tbl", "worldsocket", "network_sendmessagebyid",
		"db\\realmdatacenter.tbl", "realmdatacenter", "realmdatacenterid",
		"login.realm", "onloginstart", "onloginfinish", "onrequestgametoken",
		"requestgametoken", "loginstart", "loginfinish", "keydata",
		"stsinetsocket", "socketcrypt", "publiceventobjectivetype",
		"game.publicevent", "game.publiceventobjective", "tspell4idability",
		"tunitproperty", "publiceventunitpropertymodifier"
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
		writeSelectedXrefs(programDir, selection, functionManager);
		writeSelectedDecompiled(programDir, selection, functionManager, maxDecompiled);

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
		File hitsOut = new File(programDir, "interesting_strings.csv");
		try (
			PrintWriter stringsWriter = new PrintWriter(new FileWriter(stringsOut));
			PrintWriter hitsWriter = new PrintWriter(new FileWriter(hitsOut))) {
			stringsWriter.println("address,type,ref_count,interesting,value");
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
				boolean interesting = keyword != null;
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
						csv(keyword),
						csv(limit(value)));
					for (Reference ref : refs) {
						Function containing =
							functionManager.getFunctionContaining(ref.getFromAddress());
						if (containing != null) {
							if (highValue != null) {
								selection.add(containing, "target:" + highValue + "@" +
									data.getMinAddress(), ref.getFromAddress());
							}
							selection.add(containing, "string:" + keyword + "@" +
								data.getMinAddress(), ref.getFromAddress());
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

	private void writeSelectedDecompiled(File programDir, Selection selection,
			FunctionManager functionManager, int maxDecompiled) throws Exception {
		File out = new File(programDir, "selected_decompiled.c");
		ArrayList<SelectedFunction> selected = new ArrayList<>(selection.byEntry.values());
		selected.sort(Comparator.comparing((SelectedFunction item) -> item.priorityBucket())
			.thenComparing(item -> item.entry.toString()));

		DecompInterface decompiler = setUpDecompiler();
		try (PrintWriter writer = new PrintWriter(new FileWriter(out))) {
			writer.println("/*");
			writer.println(" * Selected Ghidra decompiler output for " + currentProgram.getName());
			writer.println(" * Functions are selected by interesting imports/strings, not by proof of correctness.");
			writer.println(" */");
			writer.println();

			if (!decompiler.openProgram(currentProgram)) {
				writer.println("/* Could not open program in decompiler: " +
					decompiler.getLastMessage() + " */");
				return;
			}

			int count = 0;
			for (SelectedFunction selectedFunction : selected) {
				if (monitor.isCancelled() || count >= maxDecompiled) {
					break;
				}
				Function function = functionManager.getFunctionAt(selectedFunction.entry);
				if (function == null || function.isExternal()) {
					continue;
				}

				writer.println("/*");
				writer.println(" * Function: " + function.getName() + " @ " +
					function.getEntryPoint());
				writer.println(" * Reasons: " + String.join("; ", selectedFunction.reasons));
				writer.println(" */");
				DecompileResults results =
					decompiler.decompileFunction(function, DECOMPILE_TIMEOUT_SECONDS, monitor);
				if (results.decompileCompleted() && results.getDecompiledFunction() != null) {
					writer.println(results.getDecompiledFunction().getC());
				}
				else {
					writer.println("/* Decompile failed: " + results.getErrorMessage() + " */");
				}
				writer.println();
				count++;
			}
		}
		finally {
			decompiler.dispose();
		}
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

	private int countFunctions(Listing listing) {
		int count = 0;
		FunctionIterator iterator = listing.getFunctions(true);
		while (iterator.hasNext()) {
			iterator.next();
			count++;
		}
		return count;
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
}
