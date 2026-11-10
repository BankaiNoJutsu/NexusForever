// Read-only: print the instructions of the functions at the given addresses. args: outPath addr...
// Use when the decompiler drops call arguments and the registers at a call site have to be read directly.
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.InstructionIterator;
import java.io.FileWriter;
import java.io.PrintWriter;

public class DumpDisassembly extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] a = getScriptArgs();
        PrintWriter out = new PrintWriter(new FileWriter(a[0]));
        for (int i = 1; i < a.length; i++) {
            Address addr = toAddr(a[i]);
            Function f = getFunctionContaining(addr);
            if (f == null) {
                out.println("=== " + a[i] + " no function");
                continue;
            }
            out.println("=== " + f.getName() + " @ " + f.getEntryPoint());
            InstructionIterator it = currentProgram.getListing().getInstructions(f.getBody(), true);
            while (it.hasNext() && !monitor.isCancelled()) {
                Instruction ins = it.next();
                out.println(ins.getAddress() + "  " + ins);
            }
        }
        out.close();
    }
}
