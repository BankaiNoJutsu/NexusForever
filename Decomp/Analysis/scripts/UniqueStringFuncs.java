import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

// For every function that still has the default name, list the strings it references that no other function references.
public class UniqueStringFuncs extends GhidraScript {
    @Override
    public void run() throws Exception {
        FunctionManager fm = currentProgram.getFunctionManager();
        ReferenceManager rm = currentProgram.getReferenceManager();
        Listing listing = currentProgram.getListing();
        Map<Function, List<String>> result = new TreeMap<>(Comparator.comparing(f -> f.getEntryPoint()));
        DataIterator it = listing.getDefinedData(true);
        while (it.hasNext() && !monitor.isCancelled()) {
            Data d = it.next();
            if (!d.hasStringValue()) continue;
            Object v = d.getValue();
            String s = v == null ? "" : v.toString();
            if (s.length() < 4) continue;
            Set<Function> users = new HashSet<>();
            for (Reference r : rm.getReferencesTo(d.getAddress())) {
                Function f = fm.getFunctionContaining(r.getFromAddress());
                if (f != null) users.add(f);
            }
            if (users.size() != 1) continue;
            Function f = users.iterator().next();
            if (!f.getName().startsWith("FUN_")) continue;
            result.computeIfAbsent(f, k -> new ArrayList<>()).add(s.replace("\r", "\\r").replace("\n", "\\n").replace("\t", "\\t"));
        }
        try (PrintWriter out = new PrintWriter(new FileWriter("I:/tools/unique_strings.tsv"))) {
            for (Map.Entry<Function, List<String>> e : result.entrySet()) {
                Function f = e.getKey();
                StringBuilder sb = new StringBuilder();
                sb.append(f.getEntryPoint()).append('\t').append(f.getBody().getNumAddresses());
                for (String s : e.getValue()) sb.append('\t').append(s.length() > 120 ? s.substring(0, 120) : s);
                out.println(sb);
            }
        }
    }
}
