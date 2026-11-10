// Read-only: print instructions whose text matches a regex, with the N instructions before each, plus the containing function.
// Use to tell call sites of one virtual slot apart by how their arguments are set up (for example a float in XMM1).
// args: outPath regex|@regexFile contextBefore
//@category NexusForever
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import java.io.*;
import java.util.regex.*;

public class InsScanContext extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        String rx = a[1].startsWith("@") ? new String(java.nio.file.Files.readAllBytes(java.nio.file.Paths.get(a[1].substring(1)))).trim() : a[1];
        Pattern p = Pattern.compile(rx);
        int before = a.length > 2 ? Integer.parseInt(a[2]) : 6;
        Listing listing = currentProgram.getListing();
        InstructionIterator it = listing.getInstructions(true);
        while (it.hasNext() && !monitor.isCancelled()) {
            Instruction ins = it.next();
            if (!p.matcher(ins.toString()).find())
                continue;
            Function f = getFunctionContaining(ins.getAddress());
            out.println("### " + ins.getAddress() + " in " + (f == null ? "<none>" : f.getName() + "@" + f.getEntryPoint()));
            java.util.ArrayDeque<String> lines = new java.util.ArrayDeque<>();
            Instruction prev = ins;
            for (int i = 0; i < before; i++) {
                prev = listing.getInstructionBefore(prev.getAddress());
                if (prev == null)
                    break;
                lines.addFirst(prev.getAddress() + "  " + prev);
            }
            for (String l : lines)
                out.println(l);
            out.println(ins.getAddress() + "  " + ins);
        }
        out.close();
    }
}
