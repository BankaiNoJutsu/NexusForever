// Read-only: print instructions whose text matches a regex, plus function. args: outPath regex
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import java.io.*;
import java.util.regex.*;
public class Gap8InsScan extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        String rx = a[1].startsWith("@") ? new String(java.nio.file.Files.readAllBytes(java.nio.file.Paths.get(a[1].substring(1)))).trim() : a[1]; Pattern p = Pattern.compile(rx);
        InstructionIterator it = currentProgram.getListing().getInstructions(true);
        while (it.hasNext() && !monitor.isCancelled()) {
            Instruction ins = it.next();
            String s = ins.toString();
            if (p.matcher(s).find()) {
                Function f = getFunctionContaining(ins.getAddress());
                out.println(ins.getAddress() + " " + s + "  in " + (f == null ? "<none>" : f.getName() + "@" + f.getEntryPoint()));
            }
        }
        out.close();
    }
}
