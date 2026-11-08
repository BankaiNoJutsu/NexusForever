// Read-only: list calling functions (and call-site addresses) of given addresses. args: outPath addr...
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

public class Gap8Callers extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        for (int i = 1; i < a.length; i++) {
            out.println("=== references to " + a[i]);
            Map<Address, List<String>> seen = new TreeMap<>();
            for (Reference r : getReferencesTo(toAddr(a[i]))) {
                Function f = getFunctionContaining(r.getFromAddress());
                Address k = f == null ? r.getFromAddress() : f.getEntryPoint();
                seen.computeIfAbsent(k, x -> new ArrayList<>()).add((f == null ? "<nofunc>" : f.getName()) + " @" + r.getFromAddress() + " " + r.getReferenceType());
            }
            for (Map.Entry<Address, List<String>> e : seen.entrySet()) out.println("  " + e.getKey() + " " + e.getValue().get(0) + (e.getValue().size() > 1 ? " (+" + (e.getValue().size()-1) + ")" : ""));
        }
        out.close();
    }
}
