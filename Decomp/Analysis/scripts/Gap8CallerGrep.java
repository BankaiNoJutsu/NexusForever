// Read-only: decompile every caller of the given functions and print only lines matching a regex (with function header).
// args: outPath regex addr[,addr...] (addresses comma separated in 3rd arg)
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;
import java.util.regex.*;

public class Gap8CallerGrep extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        String rx = a[1].startsWith("@") ? new String(java.nio.file.Files.readAllBytes(java.nio.file.Paths.get(a[1].substring(1)))).trim() : a[1]; Pattern p = Pattern.compile(rx);
        DecompInterface di = new DecompInterface(); di.openProgram(currentProgram);
        Set<Address> done = new TreeSet<>();
        for (int i = 2; i < a.length; i++) {
            for (Reference r : getReferencesTo(toAddr(a[i]))) {
                Function f = getFunctionContaining(r.getFromAddress());
                if (f != null) done.add(f.getEntryPoint());
            }
        }
        out.println("callers: " + done.size());
        for (Address e : done) {
            Function f = getFunctionAt(e);
            DecompileResults r = di.decompileFunction(f, 60, monitor);
            if (!r.decompileCompleted()) { out.println("## " + e + " " + f.getName() + " decompile failed"); continue; }
            String c = r.getDecompiledFunction().getC();
            List<String> hits = new ArrayList<>();
            for (String line : c.split("\n")) if (p.matcher(line).find()) hits.add(line);
            if (!hits.isEmpty()) {
                out.println("## " + e + " " + f.getName() + " size=" + f.getBody().getNumAddresses());
                for (String h : hits) out.println("   " + h.trim());
            }
        }
        out.close();
    }
}
