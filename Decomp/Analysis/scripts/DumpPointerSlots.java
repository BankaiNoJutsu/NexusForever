// Read the 8-byte pointer stored at each given address and report what it points at.
// Read-only. Useful for vtable slots whose target has no function defined yet, which the
// function-pointer family export therefore omits.
//
// Usage from analyzeHeadless:
//   -postScript DumpPointerSlots.java <outPath> <slotAddress> [slotAddress...]
//
// Output lines: slot,target,functionAtTarget,hasInstruction
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;

public class DumpPointerSlots extends GhidraScript {
	@Override
	public void run() throws Exception {
		String[] args = getScriptArgs();
		if (args.length < 2) {
			printerr("Usage: DumpPointerSlots.java <outPath> <slotAddress> [slotAddress...]");
			return;
		}

		try (PrintWriter out = new PrintWriter(new FileWriter(args[0]))) {
			out.println("slot,target,functionAtTarget,hasInstruction");
			for (int i = 1; i < args.length; i++) {
				Address slot = toAddr(args[i]);
				long value = currentProgram.getMemory().getLong(slot);
				Address target = toAddr(value);
				Function function = getFunctionAt(target);
				boolean hasInstruction = currentProgram.getListing().getInstructionAt(target) != null;
				out.println(slot + "," + target + "," + (function == null ? "" : function.getName()) + "," + hasInstruction);
			}
		}
	}
}
