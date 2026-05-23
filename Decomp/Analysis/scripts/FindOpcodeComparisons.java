// Find instructions comparing against one or more opcode immediates.
//
// Usage:
//   -postScript FindOpcodeComparisons.java <opcode> [opcode...] [scanStart] [scanEnd] [maxMatches]
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.scalar.Scalar;

public class FindOpcodeComparisons extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 1) {
            printerr("Usage: FindOpcodeComparisons.java <opcode> [opcode...] [scanStart] [scanEnd] [maxMatches]");
            return;
        }

        long scanStart = 0x140001000L;
        long scanEnd = 0x140957000L;
        int maxMatches = 64;
        int opcodeCount = args.length;

        if (args.length >= 2 && isHex(args[args.length - 1])) {
            long last = parseHex(args[args.length - 1]);
            if (last <= 0x10000L) {
                maxMatches = (int) last;
                opcodeCount = args.length - 1;
            }
        }

        if (opcodeCount >= 3 && isHex(args[opcodeCount - 2]) && isHex(args[opcodeCount - 1])) {
            long rangeStart = parseHex(args[opcodeCount - 2]);
            long rangeEnd = parseHex(args[opcodeCount - 1]);
            if (rangeStart >= 0x140000000L && rangeEnd > rangeStart) {
                scanStart = rangeStart;
                scanEnd = rangeEnd;
                opcodeCount -= 2;
            }
        }

        long[] opcodes = new long[opcodeCount];
        for (int i = 0; i < opcodeCount; i++) {
            opcodes[i] = parseHex(args[i]);
            println("opcode[" + i + "]=" + formatHex(opcodes[i]));
        }
        println(String.format("scan=%s..%s max=%d", formatHex(scanStart), formatHex(scanEnd), maxMatches));

        Listing listing = currentProgram.getListing();
        InstructionIterator iter = listing.getInstructions(toAddr(scanStart), true);

        int matches = 0;
        while (iter.hasNext()) {
            Instruction instruction = iter.next();
            if (instruction.getAddress().getOffset() > scanEnd) {
                break;
            }

            for (int operandIndex = 0; operandIndex < instruction.getNumOperands(); operandIndex++) {
                Object[] objects = instruction.getOpObjects(operandIndex);
                for (Object object : objects) {
                    if (!(object instanceof Scalar)) {
                        continue;
                    }
                    long value = ((Scalar) object).getUnsignedValue();
                    for (long opcode : opcodes) {
                        if (value != opcode) {
                            continue;
                        }
                        Function function = getFunctionContaining(instruction.getAddress());
                        String functionText = function == null
                            ? "<no_fn>"
                            : function.getName() + "@" + function.getEntryPoint();
                        println(String.format(
                            "match=%s fn=%s insn=%s",
                            instruction.getAddress(),
                            functionText,
                            instruction));
                        matches++;
                        if (matches >= maxMatches) {
                            println("total_matches=" + matches);
                            return;
                        }
                    }
                }
            }
        }

        println("total_matches=" + matches);
    }

    private boolean isHex(String input) {
        String normalised = stripHexPrefix(input);
        return !normalised.isEmpty() && normalised.matches("[0-9A-Fa-f]+");
    }

    private long parseHex(String input) {
        return Long.parseUnsignedLong(stripHexPrefix(input), 16);
    }

    private String stripHexPrefix(String input) {
        if (input.startsWith("0x") || input.startsWith("0X")) {
            return input.substring(2);
        }
        return input;
    }

    private String formatHex(long value) {
        return "0x" + Long.toHexString(value);
    }
}
