import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;

// FindCallsToTargets.java - finds CALL instructions to target addresses within a scan range
public class FindCallsToTargets extends ghidra.app.script.GhidraScript {
    @Override
    public void run() throws Exception {
        // Scan range: start of handlers to end; targets: the three consumer functions
        String[] args = getScriptArgs();
        long scanStart = Long.parseUnsignedLong(args[0], 16);
        long scanEnd   = Long.parseUnsignedLong(args[1], 16);
        long[] targets = new long[]{
            0x1406368d0L, // RewardRotation_UpsertEntryState
            0x1406369c0L, // RewardRotation_UpdateEntryState
            0x140636ac0L, // RewardRotation_RemoveEntryState
            0x140636840L  // RewardRotation_LoadEntryStateArray
        };
        String[] names = {"UpsertEntryState","UpdateEntryState","RemoveEntryState","LoadEntryStateArray"};
        Address start = toAddr(scanStart);
        Address end   = toAddr(scanEnd);
        Listing listing = currentProgram.getListing();
        InstructionIterator iter = listing.getInstructions(start, true);
        int found = 0;
        while (iter.hasNext()) {
            Instruction instr = iter.next();
            if (instr.getAddress().getOffset() > scanEnd) break;
            String mn = instr.getMnemonicString();
            if (!mn.equals("CALL")) continue;
            Address[] flows = instr.getFlows();
            if (flows == null) continue;
            for (Address flow : flows) {
                long foff = flow.getOffset();
                for (int i = 0; i < targets.length; i++) {
                    if (foff == targets[i]) {
                        println("CALL@" + instr.getAddress() + " -> " + names[i] + "(0x" + Long.toHexString(targets[i]) + ")");
                        found++;
                    }
                }
            }
        }
        println("total_calls_found=" + found);
    }
}
