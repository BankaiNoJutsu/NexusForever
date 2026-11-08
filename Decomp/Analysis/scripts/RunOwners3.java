import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

// For unresolved runs (start and entry count in I:/tools/unresolved_runs2.txt: "<start> <n>"), report every reference to any address inside the table range (+-16 bytes),
// and the strings referenced by the referencing function.
public class RunOwners3 extends GhidraScript {
    private String str(Address a) {
        Data d = currentProgram.getListing().getDataAt(a);
        if (d != null && d.hasStringValue()) {
            Object v = d.getValue();
            return v == null ? null : v.toString();
        }
        return null;
    }

    @Override
    public void run() throws Exception {
        FunctionManager fm = currentProgram.getFunctionManager();
        ReferenceManager rm = currentProgram.getReferenceManager();
        PrintWriter out = new PrintWriter(new FileWriter("I:/tools/run_owners3.txt"));
        try (BufferedReader r = new BufferedReader(new FileReader("I:/tools/unresolved_runs2.txt"))) {
            String l;
            while ((l = r.readLine()) != null) {
                if (l.isBlank()) continue;
                String[] p = l.trim().split(" ");
                Address start = toAddr(p[0]);
                int n = Integer.parseInt(p[1]);
                out.println("RUN " + p[0] + " n=" + n);
                Address lo = start.subtract(16);
                Address hi = start.add(16L * n + 16);
                int shown = 0;
                for (Address a = lo; a.compareTo(hi) <= 0; a = a.add(1)) {
                    for (Reference ref : rm.getReferencesTo(a)) {
                        Function f = fm.getFunctionContaining(ref.getFromAddress());
                        if (f == null) continue;
                        out.println("  ref to +" + a.subtract(start) + " from " + ref.getFromAddress() + " in " + f.getEntryPoint());
                        if (shown++ > 2) continue;
                        Set<String> seen = new LinkedHashSet<>();
                        InstructionIterator it = currentProgram.getListing().getInstructions(f.getBody(), true);
                        while (it.hasNext() && seen.size() < 8) {
                            Instruction ins = it.next();
                            for (Reference r2 : ins.getReferencesFrom()) {
                                String t = str(r2.getToAddress());
                                if (t != null && t.length() < 60) seen.add(t.replace("\n", "\\n"));
                            }
                        }
                        out.println("    strings: " + seen);
                    }
                }
            }
        }
        out.close();
    }
}
