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
import java.util.List;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
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
		int applied = 0;
		int skipped = 0;
		int missing = 0;

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
				Function function = functionManager.getFunctionAt(address);
				if (function == null) {
					function = functionManager.getFunctionContaining(address);
				}

				if (function == null) {
					printerr("No function at " + parts[1] + " for " + programName + " label " + parts[2]);
					missing++;
					continue;
				}

				String name = parts[2];
				if (!function.getName().equals(name)) {
					function.setName(name, SourceType.USER_DEFINED);
				}

				if (parts.length >= 4 && !parts[3].isEmpty()) {
					String existingComment = function.getComment();
					String comment = "NexusForever: " + parts[3];
					if (existingComment == null ||
						existingComment.trim().isEmpty() ||
						existingComment.startsWith("NexusForever: ")) {
						function.setComment(comment);
					}
				}

				applied++;
			}
		}

		println("Applied NexusForever labels for " + programName + ": applied=" + applied +
			", skipped=" + skipped + ", missing=" + missing);
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
}
