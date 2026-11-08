import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

// For every call to ClientEvent_DispatchNamedEvent, find the event-name string loaded just before the call and report it with the calling function.
public class EventCallers extends GhidraScript {
    private String str(Address a) {
        Data d = currentProgram.getListing().getDataAt(a);
        if (d != null && d.hasStringValue()) {
            Object v = d.getValue();
            return v == null ? null : v.toString();
        }
        return null;
    }

    @Override
    public void run() throws Exception {
        FunctionManager fm = currentProgram.getFunctionManager();
        Function helper = null;
        for (Function f : fm.getFunctions(true)) if (f.getName().equals("ClientEvent_DispatchNamedEvent")) { helper = f; break; }
        if (helper == null) { printerr("helper not found"); return; }
        Listing listing = currentProgram.getListing();
        ReferenceManager rm = currentProgram.getReferenceManager();
        try (PrintWriter out = new PrintWriter(new FileWriter("I:/tools/event_callers.tsv"))) {
            out.println("# helper " + helper.getEntryPoint());
            for (Reference r : rm.getReferencesTo(helper.getEntryPoint())) {
                if (!r.getReferenceType().isCall()) continue;
                Function caller = fm.getFunctionContaining(r.getFromAddress());
                if (caller == null) continue;
                String name = null;
                Instruction ins = listing.getInstructionAt(r.getFromAddress());
                for (int i = 0; i < 14 && ins != null; i++) {
                    ins = ins.getPrevious();
                    if (ins == null) break;
                    for (Reference r2 : ins.getReferencesFrom()) {
                        String s = str(r2.getToAddress());
                        if (s != null && s.length() < 80) { name = s; break; }
                    }
                    if (name != null) break;
                }
                out.println(caller.getEntryPoint() + "\t" + caller.getName() + "\t" + caller.getBody().getNumAddresses() + "\t" + r.getFromAddress() + "\t" + (name == null ? "" : name.replace("\t", " ")));
            }
        }
    }
}
