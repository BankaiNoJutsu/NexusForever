// Apply source-controlled NexusForever reverse-engineering labels to a Ghidra program.
//
// Usage from analyzeHeadless:
//   -preScript ApplyNexusForeverLabels.java <function-label-csv>
//
// CSV columns:
//   program,address,name,comment
//@category NexusForever

import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.CodeUnit;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolTable;
import ghidra.program.model.symbol.SourceType;

public class ApplyNexusForeverLabels extends GhidraScript {
	@Override
	public void run() throws Exception {
		if (currentProgram == null) {
			printerr("No current program.");
			return;
		}

		String[] args = getScriptArgs();
		if (args.length == 0) {
			printerr("Missing label map path.");
			return;
		}

		File labelMap = new File(args[0]);
		if (!labelMap.exists()) {
			printerr("Label map does not exist: " + labelMap.getAbsolutePath());
			return;
		}

		String programName = currentProgram.getName();
		FunctionManager functionManager = currentProgram.getFunctionManager();
		SymbolTable symbolTable = currentProgram.getSymbolTable();
		int applied = 0;
		int created = 0;
		int localLabels = 0;
		int skipped = 0;
		int missing = 0;
		int duplicates = 0;
		Map<String, String> labelsByAddress = new HashMap<>();

		try (BufferedReader reader = new BufferedReader(new FileReader(labelMap))) {
			String line;
			int lineNumber = 0;
			while ((line = reader.readLine()) != null && !monitor.isCancelled()) {
				lineNumber++;
				line = line.trim();
				if (line.isEmpty() || line.startsWith("#")) {
					continue;
				}

				String[] parts = splitCsv(line);
				if (parts.length < 3) {
					printerr("Skipping invalid label row " + lineNumber + ": " + line);
					skipped++;
					continue;
				}

				if ("program".equalsIgnoreCase(parts[0])) {
					continue;
				}

				if (!"*".equals(parts[0]) && !programName.equalsIgnoreCase(parts[0])) {
					continue;
				}

				Address address = toAddr(parts[1]);
				String name = parts[2];
				String key = programName.toLowerCase(Locale.ROOT) + ":" +
					address.toString().toLowerCase(Locale.ROOT);
				String previousName = labelsByAddress.put(key, name);
				if (previousName != null) {
					printerr("Duplicate label row " + lineNumber + " for " + programName + " " +
						parts[1] + ": " + previousName + " -> " + name);
					duplicates++;
				}

				Function function = functionManager.getFunctionAt(address);
				boolean isFunctionEntry = function != null;
				if (function == null) {
					function = functionManager.getFunctionContaining(address);
				}
				if (function == null && currentProgram.getListing().getInstructionAt(address) != null) {
					function = createFunction(address, name);
					if (function != null) {
						created++;
						isFunctionEntry = true;
					}
				}

				if (function == null) {
					printerr("No function at " + parts[1] + " for " + programName + " label " + name);
					missing++;
					continue;
				}

				if (isFunctionEntry) {
					if (!function.getName().equals(name)) {
						function.setName(name, SourceType.USER_DEFINED);
					}
				}
				else {
					Symbol existingSymbol = symbolTable.getPrimarySymbol(address);
					if (existingSymbol == null || !existingSymbol.getName().equals(name)) {
						symbolTable.createLabel(address, name, SourceType.USER_DEFINED);
					}
					localLabels++;
				}

				String commentText = joinCsvRemainder(parts, 3);
				if (!commentText.isEmpty()) {
					String comment = "NexusForever: " + commentText;
					if (isFunctionEntry) {
						String existingComment = function.getComment();
						if (existingComment == null ||
							existingComment.trim().isEmpty() ||
							existingComment.startsWith("NexusForever: ")) {
							function.setComment(comment);
						}
					}
					else {
						CodeUnit codeUnit = currentProgram.getListing().getCodeUnitAt(address);
						if (codeUnit != null) {
							String existingComment = codeUnit.getComment(CodeUnit.PRE_COMMENT);
							if (existingComment == null ||
								existingComment.trim().isEmpty() ||
								existingComment.startsWith("NexusForever: ")) {
								codeUnit.setComment(CodeUnit.PRE_COMMENT, comment);
							}
						}
					}
				}

				applied++;
			}
		}

		println("Applied NexusForever labels for " + programName + ": applied=" + applied +
			", created=" + created + ", localLabels=" + localLabels + ", skipped=" + skipped + ", missing=" + missing +
			", duplicateRows=" + duplicates);
	}

	private String[] splitCsv(String line) {
		List<String> fields = new ArrayList<>();
		StringBuilder field = new StringBuilder();
		boolean quoted = false;

		for (int i = 0; i < line.length(); i++) {
			char c = line.charAt(i);
			if (c == '"') {
				if (quoted && i + 1 < line.length() && line.charAt(i + 1) == '"') {
					field.append('"');
					i++;
				}
				else {
					quoted = !quoted;
				}
			}
			else if (c == ',' && !quoted) {
				fields.add(field.toString().trim());
				field.setLength(0);
			}
			else {
				field.append(c);
			}
		}

		fields.add(field.toString().trim());
		return fields.toArray(new String[0]);
	}

	private String joinCsvRemainder(String[] parts, int startIndex) {
		if (parts.length <= startIndex) {
			return "";
		}

		StringBuilder builder = new StringBuilder();
		for (int i = startIndex; i < parts.length; i++) {
			if (i > startIndex) {
				builder.append(", ");
			}
			builder.append(parts[i]);
		}
		return builder.toString().trim();
	}
}
