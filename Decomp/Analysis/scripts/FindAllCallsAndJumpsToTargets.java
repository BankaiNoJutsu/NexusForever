import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;

// FindAllCallsAndJumpsToTargets.java
public class FindAllCallsAndJumpsToTargets extends ghidra.app.script.GhidraScript {
    @Override
    public void run() throws Exception {
        long[] targets = new long[]{
            0x1406368d0L, // RewardRotation_UpsertEntryState
            0x1406369c0L, // RewardRotation_UpdateEntryState
            0x140636ac0L, // RewardRotation_RemoveEntryState
        };
        String[] names = {"Upsert","Update","Remove"};
        Listing listing = currentProgram.getListing();
        // Scan entire .text section 0x140001000 to 0x140957000
        Address start = toAddr(0x140001000L);
        Address end   = toAddr(0x140957000L);
        InstructionIterator iter = listing.getInstructions(start, true);
        int found = 0;
        while (iter.hasNext()) {
            Instruction instr = iter.next();
            if (instr.getAddress().compareTo(end) > 0) break;
            String mn = instr.getMnemonicString();
            if (!mn.equals("CALL") && !mn.equals("JMP")) continue;
            Address[] flows = instr.getFlows();
            if (flows == null) continue;
            for (Address flow : flows) {
                long foff = flow.getOffset();
                for (int i = 0; i < targets.length; i++) {
                    if (foff == targets[i]) {
                        Function caller = getFunctionContaining(instr.getAddress());
                        String callerName = (caller != null) ? caller.getName() + "@" + caller.getEntryPoint() : "<no_fn>";
                        println(mn + "@" + instr.getAddress() + " caller=" + callerName + " -> " + names[i]);
                        found++;
                    }
                }
            }
        }
        println("total=" + found);
    }
}
