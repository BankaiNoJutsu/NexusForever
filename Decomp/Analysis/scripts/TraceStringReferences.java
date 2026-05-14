// Trace incoming references for a string or address, including one or more
// levels of data-table indirection when the immediate xref is not inside a
// function body.
//
// Usage from analyzeHeadless:
//   -postScript TraceStringReferences.java <string-or-address> [max-depth]
//
// Examples:
//   -postScript TraceStringReferences.java GetRapidTransportCooldown
//   -postScript TraceStringReferences.java 140b43640 3
//@category NexusForever

import java.util.ArrayList;
import java.util.HashSet;
import java.util.Set;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class TraceStringReferences extends GhidraScript {
    private Listing listing;
    private FunctionManager functionManager;
    private ReferenceManager referenceManager;

    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        if (args.length == 0) {
            printerr("Missing string literal or address.");
            return;
        }

        int maxDepth = args.length > 1 ? Integer.parseInt(args[1]) : 2;
        listing = currentProgram.getListing();
        functionManager = currentProgram.getFunctionManager();
        referenceManager = currentProgram.getReferenceManager();

        Address target = resolveTarget(args[0]);
        if (target == null) {
            printerr("Could not resolve target: " + args[0]);
            return;
        }

        Data targetData = listing.getDefinedDataAt(target);
        String value = targetData != null && targetData.hasStringValue()
            ? String.valueOf(targetData.getValue())
            : "<non-string>";

        println("Tracing references for " + target + " -> " + value);
        traceIncoming(target, 0, maxDepth, new HashSet<Address>());
    }

    private Address resolveTarget(String input) {
        if (looksLikeAddress(input)) {
            return toAddr(input.startsWith("0x") ? input.substring(2) : input);
        }

        for (Data data : listing.getDefinedData(true)) {
            if (!data.hasStringValue()) {
                continue;
            }

            Object raw = data.getValue();
            if (raw == null) {
                continue;
            }

            if (input.equals(raw.toString())) {
                return data.getMinAddress();
            }
        }

        return null;
    }

    private void traceIncoming(Address address, int depth, int maxDepth, Set<Address> visited) {
        if (depth > maxDepth || !visited.add(address)) {
            return;
        }

        ArrayList<Reference> refs = referencesTo(address);
        String prefix = indent(depth);
        println(prefix + "target " + address + " refs=" + refs.size());

        if (refs.isEmpty()) {
            return;
        }

        for (Reference ref : refs) {
            Address from = ref.getFromAddress();
            Function function = functionManager.getFunctionContaining(from);
            if (function != null) {
                Instruction instruction = listing.getInstructionContaining(from);
                String location = instruction != null ? instruction.toString() : "<no instruction>";
                println(prefix + "  function " + function.getName() + " @ " + function.getEntryPoint() +
                    " via " + from + " :: " + location);
                continue;
            }

            Data data = listing.getDefinedDataContaining(from);
            if (data != null) {
                println(prefix + "  data " + data.getMinAddress() +
                    " type=" + data.getDataType().getDisplayName() +
                    " via " + from +
                    " value=" + limit(data));
                traceIncoming(data.getMinAddress(), depth + 1, maxDepth, visited);
                continue;
            }

            println(prefix + "  raw " + from + " refType=" + ref.getReferenceType());
        }
    }

    private ArrayList<Reference> referencesTo(Address address) {
        ArrayList<Reference> refs = new ArrayList<>();
        ReferenceIterator iterator = referenceManager.getReferencesTo(address);
        while (iterator.hasNext() && !monitor.isCancelled()) {
            refs.add(iterator.next());
        }
        return refs;
    }

    private boolean looksLikeAddress(String input) {
        String value = input.startsWith("0x") ? input.substring(2) : input;
        if (value.isEmpty()) {
            return false;
        }

        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            boolean hex = (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!hex) {
                return false;
            }
        }

        return true;
    }

    private String limit(Data data) {
        String value = data.toString();
        return value.length() > 120 ? value.substring(0, 120) + "..." : value;
    }

    private String indent(int depth) {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < depth; i++) {
            builder.append("  ");
        }
        return builder.toString();
    }
}