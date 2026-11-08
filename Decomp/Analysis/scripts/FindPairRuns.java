import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.mem.*;
import java.io.*;
import java.util.*;

// Finds runs of 16-byte records (pointer to a C string, pointer to a function entry) in initialised data.
public class FindPairRuns extends GhidraScript {
    private String readCString(Address a) {
        try {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 80; i++) {
                int b = currentProgram.getMemory().getByte(a.add(i)) & 0xff;
                if (b == 0) return sb.length() >= 2 ? sb.toString() : null;
                if (b < 0x20 || b > 0x7e) return null;
                sb.append((char) b);
            }
        } catch (Exception e) { }
        return null;
    }

    @Override
    public void run() throws Exception {
        Memory mem = currentProgram.getMemory();
        FunctionManager fm = currentProgram.getFunctionManager();
        PrintWriter out = new PrintWriter(new FileWriter("I:/tools/pair_runs.txt"));
        int minRun = 3;
        for (MemoryBlock b : mem.getBlocks()) {
            if (!b.isInitialized() || b.isExecute()) continue;
            Address a = b.getStart();
            long end = b.getEnd().getOffset();
            while (a.getOffset() + 16 <= end) {
                Address runStart = a;
                List<String> names = new ArrayList<>();
                List<String> funcs = new ArrayList<>();
                Address cur = a;
                while (cur.getOffset() + 16 <= end) {
                    long sp, fp;
                    try { sp = mem.getLong(cur); fp = mem.getLong(cur.add(8)); } catch (Exception e) { break; }
                    Address sa, fa;
                    try { sa = toAddr(sp); fa = toAddr(fp); } catch (Exception e) { break; }
                    if (sa == null || fa == null || !mem.contains(sa) || !mem.contains(fa)) break;
                    Function f = fm.getFunctionAt(fa);
                    if (f == null) break;
                    String s = readCString(sa);
                    if (s == null) break;
                    names.add(s);
                    funcs.add(f.getEntryPoint().toString() + (f.getName().startsWith("FUN_") ? "" : "=" + f.getName()));
                    cur = cur.add(16);
                }
                if (names.size() >= minRun) {
                    out.println("RUN " + runStart + " n=" + names.size() + " first=" + names.get(0) + " last=" + names.get(names.size() - 1));
                    for (int i = 0; i < names.size(); i++) out.println("  " + names.get(i) + " " + funcs.get(i));
                    a = cur;
                } else {
                    a = a.add(8);
                }
            }
        }
        out.close();
    }
}
