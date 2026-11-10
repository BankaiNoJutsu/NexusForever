// Search initialised memory for 8-byte little-endian pointers equal to each given address and report where they are stored.
// Read-only. Useful for finding the vtable slot of a virtual function that has no direct callers.
//
// Usage from analyzeHeadless:
//   -postScript FindPointerReferences.java <outPath> <targetAddress> [targetAddress...]
//
// Output lines: target,storedAt,containingSymbol
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.symbol.Symbol;

public class FindPointerReferences extends GhidraScript {
	@Override
	public void run() throws Exception {
		String[] args = getScriptArgs();
		if (args.length < 2) {
			printerr("Usage: FindPointerReferences.java <outPath> <targetAddress> [targetAddress...]");
			return;
		}

		Memory memory = currentProgram.getMemory();
		try (PrintWriter out = new PrintWriter(new FileWriter(args[0]))) {
			out.println("target,storedAt,containingSymbol");
			for (int i = 1; i < args.length; i++) {
				long value = toAddr(args[i]).getOffset();
				byte[] pattern = new byte[8];
				for (int b = 0; b < 8; b++) {
					pattern[b] = (byte) ((value >> (8 * b)) & 0xff);
				}

				Address at = memory.findBytes(memory.getMinAddress(), pattern, null, true, monitor);
				while (at != null && !monitor.isCancelled()) {
					Symbol symbol = getSymbolBefore(at);
					out.println(args[i] + "," + at + "," + (symbol == null ? "" : symbol.getName() + "@" + symbol.getAddress()));
					at = memory.findBytes(at.add(1), pattern, null, true, monitor);
				}
			}
		}
	}
}
