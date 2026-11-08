// Read-only: decompile functions at/containing given addresses to an output file. args: outPath addr [addr...]
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

public class Gap8Decomp extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        DecompInterface di = new DecompInterface(); di.openProgram(currentProgram);
        for (int i = 1; i < a.length; i++) {
            String s = a[i];
            Function f = getFunctionAt(toAddr(s));
            if (f == null) f = getFunctionContaining(toAddr(s));
            out.println("=== " + s + " -> " + (f == null ? "<none>" : f.getName() + " " + f.getEntryPoint() + " size=" + f.getBody().getNumAddresses()));
            if (f != null) {
                DecompileResults r = di.decompileFunction(f, 120, monitor);
                out.println(r.decompileCompleted() ? r.getDecompiledFunction().getC() : "decompile failed");
            }
        }
        out.close();
    }
}
