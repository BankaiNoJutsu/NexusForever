// Read-only: find reads of [R+disp] where R was loaded from [x+0x18] within 6 prior instructions (entity->creature info); print following 3 instructions.
// args: outPath disp[,disp...] (hex like 0x90,0x94)
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import java.io.*;
import java.util.*;
import java.util.regex.*;

public class Gap8CreatureInfoScan extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        String[] disps = a[1].split(",");
        Pattern ld = Pattern.compile("^MOV ([A-Z0-9]+),qword ptr \\[[A-Z0-9]+ \\+ 0x18\\]$");
        List<Pattern> dp = new ArrayList<>();
        for (String d : disps) dp.add(Pattern.compile("\\[([A-Z0-9]+) \\+ " + d + "\\]"));
        Deque<Instruction> prev = new ArrayDeque<>();
        int tail = 0;
        InstructionIterator it = currentProgram.getListing().getInstructions(true);
        while (it.hasNext() && !monitor.isCancelled()) {
            Instruction ins = it.next();
            String s = ins.toString();
            if (tail > 0) { out.println("      " + ins.getAddress() + " " + s); tail--; }
            for (Pattern p : dp) {
                Matcher m = p.matcher(s);
                if (!m.find()) continue;
                String reg = m.group(1);
                boolean found = false;
                for (Instruction pi : prev) {
                    Matcher lm = ld.matcher(pi.toString());
                    if (lm.find() && lm.group(1).equals(reg)) found = true;
                }
                if (!found) continue;
                Function f = getFunctionContaining(ins.getAddress());
                out.println(ins.getAddress() + " " + s + "  in " + (f == null ? "<none>" : f.getName() + "@" + f.getEntryPoint()));
                tail = 3;
                break;
            }
            prev.addLast(ins);
            if (prev.size() > 6) prev.removeFirst();
        }
        out.close();
    }
}
