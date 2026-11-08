import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

// For every run (I:/tools/all_runs.txt: "<start> <n>"), find references to addresses inside [start, start+16n); for each, report the identifier-like strings referenced
// earlier in the same function (instruction order), nearest first.
public class RunOwners4 extends GhidraScript {
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
        Listing listing = currentProgram.getListing();
        PrintWriter out = new PrintWriter(new FileWriter("I:/tools/run_owners4.txt"));
        try (BufferedReader r = new BufferedReader(new FileReader("I:/tools/all_runs.txt"))) {
            String l;
            while ((l = r.readLine()) != null) {
                if (l.isBlank()) continue;
                String[] p = l.trim().split(" ");
                Address start = toAddr(p[0]);
                int n = Integer.parseInt(p[1]);
                out.println("RUN " + p[0] + " n=" + n);
                Set<String> done = new HashSet<>();
                for (long off = 0; off < 16L * n; off++) {
                    Address a = start.add(off);
                    for (Reference ref : rm.getReferencesTo(a)) {
                        Function f = fm.getFunctionContaining(ref.getFromAddress());
                        if (f == null) continue;
                        String key = ref.getFromAddress().toString();
                        if (!done.add(key)) continue;
                        // collect strings referenced earlier in f
                        List<String> prior = new ArrayList<>();
                        InstructionIterator it = listing.getInstructions(f.getEntryPoint(), true);
                        while (it.hasNext()) {
                            Instruction ins = it.next();
                            if (ins.getAddress().compareTo(ref.getFromAddress()) >= 0) break;
                            for (Reference r2 : ins.getReferencesFrom()) {
                                String t = str(r2.getToAddress());
                                if (t != null && t.length() < 60) prior.add(t.replace("\n", "\\n"));
                            }
                        }
                        Collections.reverse(prior);
                        int k = Math.min(4, prior.size());
                        out.println("  ref +" + off + " from " + ref.getFromAddress() + " in " + f.getEntryPoint() + " prior=" + prior.subList(0, k));
                    }
                }
            }
        }
        out.close();
    }
}
