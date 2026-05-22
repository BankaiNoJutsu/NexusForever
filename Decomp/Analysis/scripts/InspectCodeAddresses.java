// Decompile one or more functions by address in a single headless pass.
//
// Usage from analyzeHeadless:
//   -postScript InspectCodeAddresses.java <address> [address...]
//@category NexusForever

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileOptions;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.decompiler.component.DecompilerUtils;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.listing.Listing;

public class InspectCodeAddresses extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        if (args.length == 0) {
            printerr("Missing address.");
            return;
        }

        DecompInterface decompiler = setUpDecompiler();
        try {
            if (!decompiler.openProgram(currentProgram)) {
                printerr("Could not open program in decompiler: " + decompiler.getLastMessage());
                return;
            }

            Listing listing = currentProgram.getListing();
            for (String arg : args) {
                inspectAddress(decompiler, listing, arg);
            }
        }
        finally {
            decompiler.dispose();
        }
    }

    private void inspectAddress(DecompInterface decompiler, Listing listing, String addressText) throws Exception {
        Address target = toAddr(stripHexPrefix(addressText));
        if (target == null) {
            printerr("Could not resolve address: " + addressText);
            return;
        }

        Function function = getFunctionAt(target);
        if (function == null) {
            function = getFunctionContaining(target);
        }

        println("target=" + target);
        println("functionAtOrContaining=" + describe(function));
        println("functionBefore=" + describe(findFunctionBefore(listing, target)));
        println("functionAfter=" + describe(findFunctionAfter(listing, target)));

        if (function == null) {
            printerr("No function found at or containing target: " + addressText);
            return;
        }

        DecompileResults results = decompiler.decompileFunction(function, 120, monitor);
        if (!results.decompileCompleted() || results.getDecompiledFunction() == null) {
            printerr("Decompile failed for " + addressText + ": " + results.getErrorMessage());
            return;
        }

        println(results.getDecompiledFunction().getC());
    }

    private DecompInterface setUpDecompiler() {
        DecompileOptions options = DecompilerUtils.getDecompileOptions(state.getTool(), currentProgram);
        DecompInterface decompiler = new DecompInterface();
        decompiler.setOptions(options);
        decompiler.toggleCCode(true);
        decompiler.toggleSyntaxTree(true);
        decompiler.setSimplificationStyle("decompile");
        return decompiler;
    }

    private String describe(Function function) {
        return function == null ? "<none>" : function.getName() + "@" + function.getEntryPoint();
    }

    private Function findFunctionBefore(Listing listing, Address target) {
        Function previous = null;
        FunctionIterator iterator = listing.getFunctions(true);
        while (iterator.hasNext()) {
            Function candidate = iterator.next();
            if (candidate.getEntryPoint().compareTo(target) >= 0) {
                break;
            }
            previous = candidate;
        }
        return previous;
    }

    private Function findFunctionAfter(Listing listing, Address target) {
        FunctionIterator iterator = listing.getFunctions(true);
        while (iterator.hasNext()) {
            Function candidate = iterator.next();
            if (candidate.getEntryPoint().compareTo(target) > 0) {
                return candidate;
            }
        }
        return null;
    }

    private String stripHexPrefix(String input) {
        return input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
    }
}
