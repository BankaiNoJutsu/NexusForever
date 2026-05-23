// Find direct CALL instructions to one or more target addresses.
//
// Usage:
//   -postScript FindCallsToTarget.java <target> [target...] [scanStart] [scanEnd]
//
// Defaults scan .text from 0x140001000 through 0x140957000.
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import ghidra.program.model.listing.Listing;

public class FindCallsToTarget extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 1) {
            printerr("Usage: FindCallsToTarget.java <target> [target...] [scanStart] [scanEnd]");
            return;
        }

        long scanStart = 0x140001000L;
        long scanEnd = 0x140957000L;
        int targetCount = args.length;
        if (args.length >= 3 && isHex(args[args.length - 2]) && isHex(args[args.length - 1])) {
            scanStart = parseHex(args[args.length - 2]);
            scanEnd = parseHex(args[args.length - 1]);
            targetCount = args.length - 2;
        }

        long[] targets = new long[targetCount];
        for (int i = 0; i < targetCount; i++) {
            targets[i] = parseHex(args[i]);
            println("target[" + i + "]=" + formatHex(targets[i]));
        }
        println(String.format("scan=%s..%s", formatHex(scanStart), formatHex(scanEnd)));

        Listing listing = currentProgram.getListing();
        Address start = toAddr(scanStart);
        Address end = toAddr(scanEnd);
        InstructionIterator iter = listing.getInstructions(start, true);

        int found = 0;
        while (iter.hasNext()) {
            Instruction instr = iter.next();
            if (instr.getAddress().getOffset() > scanEnd) {
                break;
            }
            if (!instr.getMnemonicString().equals("CALL")) {
                continue;
            }

            Address[] flows = instr.getFlows();
            if (flows == null) {
                continue;
            }

            for (Address flow : flows) {
                long flowOffset = flow.getOffset();
                for (long target : targets) {
                    if (flowOffset != target) {
                        continue;
                    }
                    Function caller = getFunctionContaining(instr.getAddress());
                    String callerName = caller == null
                        ? "<no_fn>"
                        : caller.getName() + "@" + caller.getEntryPoint();
                    println("CALL@" + instr.getAddress() + " caller=" + callerName + " -> " + formatHex(target));
                    found++;
                }
            }
        }

        println("total_calls_found=" + found);
    }

    private boolean isHex(String input) {
        String normalised = stripHexPrefix(input);
        if (normalised.isEmpty()) {
            return false;
        }
        return normalised.matches("[0-9A-Fa-f]+");
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
