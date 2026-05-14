// Usage from analyzeHeadless:
//   -postScript FindImmediateInstructions.java <value> [instruction-context] [filter]
//
// Example:
//   -postScript FindImmediateInstructions.java 0x141 6 R8D
//@category NexusForever

import java.math.BigInteger;
import java.util.ArrayList;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.AddressSetView;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.scalar.Scalar;

public class FindImmediateInstructions extends GhidraScript {

    @Override
    protected void run() throws Exception {
        if (currentProgram == null) {
            printerr("No open program.");
            return;
        }

        String[] scriptArgs = getScriptArgs();

        if (scriptArgs.length < 1) {
            printerr("Usage: FindImmediateInstructions.java <value> [instruction-context] [filter]");
            return;
        }

        long value = parseValue(scriptArgs[0]);
        int context = scriptArgs.length > 1 ? Integer.parseInt(scriptArgs[1]) : 6;
        String filter = scriptArgs.length > 2 ? scriptArgs[2].toUpperCase() : null;

        Listing listing = currentProgram.getListing();
        AddressSetView body = currentProgram.getMemory();
        InstructionIterator iterator = listing.getInstructions(body, true);
        int matches = 0;

        println(String.format("value=%s (%d)", scriptArgs[0], value));

        while (iterator.hasNext()) {
            Instruction instruction = iterator.next();
            if (!hasImmediate(instruction, value)) {
                continue;
            }

            ArrayList<Instruction> window = buildWindow(instruction, context);
            if (!matchesFilter(window, filter)) {
                continue;
            }

            Function function = getFunctionContaining(instruction.getAddress());
            println("match=" + instruction.getAddress() + " function=" + describe(function));
            for (Instruction item : window) {
                println("  " + item.getAddress() + ": " + item);
            }
            println("");
            matches++;
        }

        println("totalMatches=" + matches);
    }

    private long parseValue(String valueText) {
        if (valueText.startsWith("0x") || valueText.startsWith("0X")) {
            return new BigInteger(valueText.substring(2), 16).longValue();
        }
        return new BigInteger(valueText, 10).longValue();
    }

    private boolean hasImmediate(Instruction instruction, long value) {
        for (int operandIndex = 0; operandIndex < instruction.getNumOperands(); operandIndex++) {
            Object[] objects = instruction.getOpObjects(operandIndex);
            for (Object object : objects) {
                if (object instanceof Scalar) {
                    Scalar scalar = (Scalar) object;
                    if (scalar.getUnsignedValue() == value || scalar.getSignedValue() == value) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private ArrayList<Instruction> buildWindow(Instruction center, int context) {
        ArrayList<Instruction> window = new ArrayList<>();

        Instruction cursor = center;
        for (int i = 0; i < context && cursor != null; i++) {
            cursor = cursor.getPrevious();
        }

        if (cursor == null) {
            cursor = center;
            for (int i = 0; i < context && cursor.getPrevious() != null; i++) {
                cursor = cursor.getPrevious();
            }
        }

        int total = context * 2 + 1;
        for (int i = 0; i < total && cursor != null; i++) {
            window.add(cursor);
            cursor = cursor.getNext();
        }

        return window;
    }

    private boolean matchesFilter(ArrayList<Instruction> window, String filter) {
        if (filter == null || filter.isEmpty()) {
            return true;
        }

        StringBuilder builder = new StringBuilder();
        for (Instruction instruction : window) {
            builder.append(instruction.toString().toUpperCase()).append('\n');
        }

        return builder.toString().contains(filter);
    }

    private String describe(Function function) {
        return function == null ? "<none>" : function.getName() + "@" + function.getEntryPoint();
    }
}