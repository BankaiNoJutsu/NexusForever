// Read-only: list functions in an address range. args: outPath startAddr endAddr
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.*;
import java.io.*;
public class Gap8ListFuncs extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        FunctionIterator it = currentProgram.getFunctionManager().getFunctions(toAddr(a[1]), true);
        while (it.hasNext()) {
            Function f = it.next();
            if (f.getEntryPoint().compareTo(toAddr(a[2])) > 0) break;
            out.println(f.getEntryPoint() + " " + f.getName() + " size=" + f.getBody().getNumAddresses());
        }
        out.close();
    }
}
