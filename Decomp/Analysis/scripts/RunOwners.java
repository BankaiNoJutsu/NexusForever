import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

// For each run start listed in I:/tools/pair_runs.txt, print the functions referencing it and the strings those functions reference.
public class RunOwners extends GhidraScript {
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
        List<String> starts = new ArrayList<>();
        try (BufferedReader r = new BufferedReader(new FileReader("I:/tools/pair_runs.txt"))) {
            String l;
            while ((l = r.readLine()) != null) if (l.startsWith("RUN ")) starts.add(l.split(" ")[1]);
        }
        FunctionManager fm = currentProgram.getFunctionManager();
        ReferenceManager rm = currentProgram.getReferenceManager();
        PrintWriter out = new PrintWriter(new FileWriter("I:/tools/run_owners.txt"));
        for (String s : starts) {
            Address a = toAddr(s);
            out.println("RUN " + s);
            int shown = 0;
            for (Reference ref : rm.getReferencesTo(a)) {
                Function f = fm.getFunctionContaining(ref.getFromAddress());
                out.println("  ref from " + ref.getFromAddress() + " in " + (f == null ? "<none>" : f.getEntryPoint().toString()));
                if (f == null || shown++ > 2) continue;
                Set<String> seen = new LinkedHashSet<>();
                InstructionIterator it = currentProgram.getListing().getInstructions(f.getBody(), true);
                while (it.hasNext()) {
                    Instruction ins = it.next();
                    for (Reference r2 : ins.getReferencesFrom()) {
                        String t = str(r2.getToAddress());
                        if (t != null && t.length() < 60) seen.add(t.replace("\n", "\\n"));
                    }
                    if (seen.size() >= 8) break;
                }
                out.println("    strings: " + seen);
            }
        }
        out.close();
    }
}
