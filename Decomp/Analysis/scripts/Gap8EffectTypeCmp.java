// Read-only: find CMP/SUB/LEA instructions comparing against effect-type constants whose preceding 5 instructions read a [reg+0x10] or [reg+0xc] dword.
// args: outPath value...
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import ghidra.program.model.scalar.Scalar;
import java.io.*;
import java.util.*;

public class Gap8EffectTypeCmp extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        Set<Long> vals = new HashSet<>();
        for (int i = 1; i < a.length; i++) vals.add(Long.decode(a[i]));
        InstructionIterator it = currentProgram.getListing().getInstructions(true);
        Instruction[] win = new Instruction[6];
        int n = 0;
        while (it.hasNext() && !monitor.isCancelled()) {
            Instruction ins = it.next();
            String mn = ins.getMnemonicString();
            boolean hit = false;
            if (mn.equals("CMP") || mn.equals("SUB") || mn.equals("LEA")) {
                for (int k = 0; k < ins.getNumOperands(); k++) {
                    for (Object o : ins.getOpObjects(k)) {
                        if (o instanceof Scalar) {
                            long v = ((Scalar) o).getSignedValue();
                            if (mn.equals("SUB")) { if (ins.getOperandRefType(k) != null && !ins.toString().contains("[") && (vals.contains(v) || vals.contains(-v))) hit = true; }
                            else if (mn.equals("LEA")) { if (vals.contains(-v) && v < 0) hit = true; }
                            else if (vals.contains(v)) hit = true;
                        }
                    }
                }
            }
            if (hit) {
                boolean ctx = false;
                String s = ins.toString();
                if (s.contains("+ 0x10]")) ctx = true;
                for (int j = 3; j < 6; j++) { if (win[j] != null) { String t = win[j].toString(); if (t.startsWith("MOV") && (t.contains("+ 0x10]"))) ctx = true; } }
                if (ctx) {
                    Function f = getFunctionContaining(ins.getAddress());
                    out.println(ins.getAddress() + " " + s + "  in " + (f == null ? "<none>" : f.getName() + "@" + f.getEntryPoint()));
                }
            }
            for (int j = 0; j < 5; j++) win[j] = win[j + 1];
            win[5] = ins;
        }
        out.close();
    }
}
