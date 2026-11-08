import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.mem.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

// For run starts that have no code reference: find 8-byte pointers to the run in memory, then references to those locations.
public class RunOwners2 extends GhidraScript {
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
        Set<String> unresolved = new LinkedHashSet<>();
        try (BufferedReader r = new BufferedReader(new FileReader("I:/tools/unresolved_runs.txt"))) {
            String l;
            while ((l = r.readLine()) != null) if (!l.isBlank()) unresolved.add(l.trim());
        }
        Memory mem = currentProgram.getMemory();
        FunctionManager fm = currentProgram.getFunctionManager();
        ReferenceManager rm = currentProgram.getReferenceManager();
        PrintWriter out = new PrintWriter(new FileWriter("I:/tools/run_owners2.txt"));
        for (String s : unresolved) {
            Address a = toAddr(s);
            long v = a.getOffset();
            byte[] pat = new byte[8];
            for (int i = 0; i < 8; i++) pat[i] = (byte) ((v >> (8 * i)) & 0xff);
            out.println("RUN " + s);
            Address from = mem.getMinAddress();
            int hits = 0;
            while (hits < 6) {
                Address f = mem.findBytes(from, pat, null, true, monitor);
                if (f == null) break;
                hits++;
                out.println("  pointer at " + f);
                int shown = 0;
                for (Reference ref : rm.getReferencesTo(f)) {
                    Function fn = fm.getFunctionContaining(ref.getFromAddress());
                    out.println("    ref from " + ref.getFromAddress() + " in " + (fn == null ? "<none>" : fn.getEntryPoint().toString()));
                    if (fn == null || shown++ > 1) continue;
                    Set<String> seen = new LinkedHashSet<>();
                    InstructionIterator it = currentProgram.getListing().getInstructions(fn.getBody(), true);
                    while (it.hasNext() && seen.size() < 8) {
                        Instruction ins = it.next();
                        for (Reference r2 : ins.getReferencesFrom()) {
                            String t = str(r2.getToAddress());
                            if (t != null && t.length() < 60) seen.add(t.replace("\n", "\\n"));
                        }
                    }
                    out.println("      strings: " + seen);
                }
                from = f.add(1);
            }
        }
        out.close();
    }
}
