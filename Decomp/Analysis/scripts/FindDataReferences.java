// Print code references to one or more data addresses.
//
// Usage:
//   -postScript FindDataReferences.java [--maxRefs <count>] <dataAddress> [dataAddress...]
//@category NexusForever

import java.util.ArrayList;
import java.util.List;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class FindDataReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        ParsedArgs parsedArgs = parseArgs(args);
        if (parsedArgs.addresses.isEmpty()) {
            printerr("Usage: FindDataReferences.java [--maxRefs <count>] <dataAddress> [dataAddress...]");
            return;
        }

        ReferenceManager referenceManager = currentProgram.getReferenceManager();
        int scannedTargets = 0;
        for (String addressText : parsedArgs.addresses) {
            if (scannedTargets > 0) {
                println("");
            }

            Address target = toAddr(parseHex(addressText));
            ReferenceIterator iterator = referenceManager.getReferencesTo(target);
            println("target=" + target);

            int count = 0;
            while (iterator.hasNext() && count < parsedArgs.maxRefs) {
                Reference reference = iterator.next();
                Address from = reference.getFromAddress();
                Function function = getFunctionContaining(from);
                String functionText = function == null
                    ? "<no_fn>"
                    : function.getName() + "@" + function.getEntryPoint();
                Instruction instruction = currentProgram.getListing().getInstructionAt(from);
                String instructionText = instruction == null ? "<no_insn>" : instruction.toString();
                println(String.format(
                    "ref from=%s type=%s function=%s insn=%s",
                    from,
                    reference.getReferenceType(),
                    functionText,
                    instructionText));
                count++;
            }

            println("total_refs=" + count);
            scannedTargets++;
        }

        println("");
        println("scanned_targets=" + scannedTargets);
    }

    private ParsedArgs parseArgs(String[] args) {
        ParsedArgs parsedArgs = new ParsedArgs();
        boolean explicitMaxRefs = false;
        for (int index = 0; index < args.length; index++) {
            String arg = args[index];
            if ("--maxRefs".equals(arg)) {
                if (index + 1 >= args.length) {
                    throw new IllegalArgumentException("Missing value after --maxRefs");
                }

                parsedArgs.maxRefs = Integer.parseInt(args[++index]);
                explicitMaxRefs = true;
                continue;
            }

            parsedArgs.addresses.add(arg);
        }

        if (!explicitMaxRefs && parsedArgs.addresses.size() > 1) {
            String lastArg = parsedArgs.addresses.get(parsedArgs.addresses.size() - 1);
            if (isDecimalInteger(lastArg)) {
                parsedArgs.maxRefs = Integer.parseInt(lastArg);
                parsedArgs.addresses.remove(parsedArgs.addresses.size() - 1);
            }
        }

        return parsedArgs;
    }

    private long parseHex(String input) {
        String normalised = input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
        return Long.parseUnsignedLong(normalised, 16);
    }

    private boolean isDecimalInteger(String input) {
        if (input == null || input.isEmpty()) {
            return false;
        }

        for (int index = 0; index < input.length(); index++) {
            if (!Character.isDigit(input.charAt(index))) {
                return false;
            }
        }

        return true;
    }

    private static class ParsedArgs {
        private final List<String> addresses = new ArrayList<>();
        private int maxRefs = 64;
    }
}
