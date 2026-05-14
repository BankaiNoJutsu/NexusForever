// Decompile a function at an address, or dump nearby instructions when the
// address is code that Ghidra has not yet promoted to a function.
//
// Usage from analyzeHeadless:
//   -postScript InspectCodeAddress.java <address> [instruction-count]
//@category NexusForever

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileOptions;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.decompiler.component.DecompilerUtils;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.listing.Listing;

public class InspectCodeAddress extends GhidraScript {
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

        Address target = toAddr(stripHexPrefix(args[0]));
        if (target == null) {
            printerr("Could not resolve address: " + args[0]);
            return;
        }

        int instructionCount = args.length > 1 ? Integer.parseInt(args[1]) : 16;
        Listing listing = currentProgram.getListing();
        FunctionManager functionManager = currentProgram.getFunctionManager();

        Function function = functionManager.getFunctionAt(target);
        if (function == null) {
            function = functionManager.getFunctionContaining(target);
        }

        println("target=" + target);
        println("functionAtOrContaining=" + describe(function));
        println("functionBefore=" + describe(findFunctionBefore(listing, target)));
        println("functionAfter=" + describe(findFunctionAfter(listing, target)));

        if (function != null) {
            DecompInterface decompiler = setUpDecompiler();
            try {
                if (!decompiler.openProgram(currentProgram)) {
                    printerr("Could not open program in decompiler: " + decompiler.getLastMessage());
                    return;
                }

                DecompileResults results = decompiler.decompileFunction(function, 120, monitor);
                if (!results.decompileCompleted() || results.getDecompiledFunction() == null) {
                    printerr("Decompile failed: " + results.getErrorMessage());
                    return;
                }

                println(results.getDecompiledFunction().getC());
            }
            finally {
                decompiler.dispose();
            }
            return;
        }

        Instruction instruction = listing.getInstructionAt(target);
        if (instruction == null) {
            instruction = listing.getInstructionContaining(target);
        }

        if (instruction == null) {
            printerr("No instruction found at or containing target.");
            return;
        }

        for (int i = 0; i < instructionCount && instruction != null; i++) {
            println(String.format("%s: %s", instruction.getAddress(), instruction));
            instruction = instruction.getNext();
        }
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