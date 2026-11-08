// Read-only: list reference targets from within functions. args: outPath funcAddr...
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;
public class Gap8RefsFrom extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        for (int i = 1; i < a.length; i++) {
            Function f = getFunctionAt(toAddr(a[i]));
            out.println("=== " + a[i] + " " + (f == null ? "<none>" : f.getName()));
            if (f == null) continue;
            InstructionIterator it = currentProgram.getListing().getInstructions(f.getBody(), true);
            while (it.hasNext()) {
                Instruction ins = it.next();
                for (Reference r : ins.getReferencesFrom()) {
                    if (r.getReferenceType().isData()) {
                        Data d = getDataAt(r.getToAddress());
                        out.println("  " + ins.getAddress() + " -> " + r.getToAddress() + " " + (d == null ? "" : d.getDataType().getName() + " " + d.getLabel()));
                    }
                }
            }
        }
        out.close();
    }
}
