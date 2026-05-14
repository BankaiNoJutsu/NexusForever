// Trace direct references to a target function and print each call site with a
// small instruction window. This is useful for recovering register-passed
// arguments such as the Windows x64 fourth argument in R9/R9D.
//
// Usage from analyzeHeadless:
//   -postScript TraceFunctionCallers.java <address> [instruction-context] [filter1] [filter2]
//
// When filter strings are provided, only call sites whose printed instruction
// window contains all filters will be reported. Matching is case-insensitive.
//@category NexusForever

import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.Set;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class TraceFunctionCallers extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        if (args.length == 0) {
            printerr("Missing target address.");
            return;
        }

        Address target = toAddr(stripHexPrefix(args[0]));
        if (target == null) {
            printerr("Could not resolve target address: " + args[0]);
            return;
        }

        int context = args.length > 1 ? Integer.parseInt(args[1]) : 6;
        String filter1 = args.length > 2 ? args[2].toUpperCase() : null;
        String filter2 = args.length > 3 ? args[3].toUpperCase() : null;
        Listing listing = currentProgram.getListing();
        FunctionManager functionManager = currentProgram.getFunctionManager();
        ReferenceManager referenceManager = currentProgram.getReferenceManager();

        Function targetFunction = functionManager.getFunctionAt(target);
        println("target=" + target + " function=" + describe(targetFunction));

        ReferenceIterator iterator = referenceManager.getReferencesTo(target);
        ArrayList<Reference> refs = new ArrayList<>();
        while (iterator.hasNext()) {
            refs.add(iterator.next());
        }

        println("references=" + refs.size());
        Set<Address> seenCallSites = new LinkedHashSet<>();
        for (Reference ref : refs) {
            Address from = ref.getFromAddress();
            if (!seenCallSites.add(from)) {
                continue;
            }

            Function caller = functionManager.getFunctionContaining(from);
            Instruction callInstruction = listing.getInstructionContaining(from);

            println("caller=" + describe(caller) + " ref=" + from + " type=" + ref.getReferenceType());
            if (callInstruction == null) {
                println("  <no instruction at reference>");
                continue;
            }

            ArrayList<Instruction> window = new ArrayList<>();
            Instruction cursor = callInstruction;
            for (int i = 0; i < context && cursor != null; i++) {
                cursor = cursor.getPrevious();
            }
            if (cursor == null) {
                cursor = listing.getInstructionAt(callInstruction.getAddress());
                while (cursor != null && cursor.getNext() != callInstruction) {
                    cursor = cursor.getNext();
                }
                if (cursor == null) {
                    cursor = callInstruction;
                }
            }
            else {
                cursor = cursor.getNext();
            }

            while (cursor != null) {
                window.add(cursor);
                if (cursor.equals(callInstruction)) {
                    break;
                }
                cursor = cursor.getNext();
            }

            if (!matchesFilters(window, filter1, filter2)) {
                continue;
            }

            for (Instruction instruction : window) {
                println("  " + instruction.getAddress() + ": " + instruction);
            }
            println("");
        }
    }

    private boolean matchesFilters(ArrayList<Instruction> window, String filter1, String filter2) {
        if (filter1 == null && filter2 == null) {
            return true;
        }

        StringBuilder builder = new StringBuilder();
        for (Instruction instruction : window) {
            builder.append(instruction.toString().toUpperCase()).append('\n');
        }

        String text = builder.toString();
        if (filter1 != null && !text.contains(filter1)) {
            return false;
        }
        if (filter2 != null && !text.contains(filter2)) {
            return false;
        }
        return true;
    }

    private String describe(Function function) {
        return function == null ? "<none>" : function.getName() + "@" + function.getEntryPoint();
    }

    private String stripHexPrefix(String input) {
        return input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
    }
}