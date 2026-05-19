import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;

public class InspectTwoAddresses extends ghidra.app.script.GhidraScript {
    @Override
    public void run() throws Exception {
        long[] addrs = new long[]{ 0x1400a1e20L, 0x1400a1f80L };
        DecompInterface decomp = new DecompInterface();
        decomp.openProgram(currentProgram);
        for (long a : addrs) {
            ghidra.program.model.listing.Function fn = getFunctionAt(toAddr(a));
            if (fn == null) fn = getFunctionContaining(toAddr(a));
            if (fn == null) { println("no fn at " + Long.toHexString(a)); continue; }
            DecompileResults res = decomp.decompileFunction(fn, 120, monitor);
            println("=== " + fn.getName() + " @ " + fn.getEntryPoint());
            if (res.decompileCompleted()) println(res.getDecompiledFunction().getC());
            else println("FAILED: " + res.getErrorMessage());
        }
        decomp.dispose();
    }
}
