// Read-only: dump bytes/dwords/floats at addresses. args: outPath addr:count(dwords)...
import ghidra.app.script.GhidraScript;
import java.io.*;
public class Gap8Bytes extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        for (int i = 1; i < a.length; i++) {
            String[] p = a[i].split(":");
            int n = p.length > 1 ? Integer.parseInt(p[1]) : 1;
            for (int k = 0; k < n; k++) {
                long v = currentProgram.getMemory().getInt(toAddr(p[0]).add(4L * k)) & 0xffffffffL;
                out.println(toAddr(p[0]).add(4L * k) + " 0x" + Long.toHexString(v) + " float=" + Float.intBitsToFloat((int) v));
            }
        }
        out.close();
    }
}
