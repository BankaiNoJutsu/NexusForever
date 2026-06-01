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
import ghidra.app.cmd.disassemble.DisassembleCommand;
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

        int instructionCount = 16;
        int addressCount = args.length;
        if (args.length > 1) {
            try {
                int maybeCount = Integer.parseInt(args[args.length - 1]);
                if (maybeCount > 0 && maybeCount <= 4096) {
                    instructionCount = maybeCount;
                    addressCount = args.length - 1;
                }
            }
            catch (NumberFormatException ignored) {
                // Every arg is an address.
            }
        }

        Listing listing = currentProgram.getListing();
        FunctionManager functionManager = currentProgram.getFunctionManager();
        DecompInterface decompiler = null;

        for (int argIndex = 0; argIndex < addressCount; argIndex++) {
            if (addressCount > 1) {
                println("==== " + args[argIndex] + " ====");
            }

            Address target = toAddr(stripHexPrefix(args[argIndex]));
            if (target == null) {
                printerr("Could not resolve address: " + args[argIndex]);
                continue;
            }

            Function function = functionManager.getFunctionAt(target);
            if (function == null) {
                function = functionManager.getFunctionContaining(target);
            }

            println("target=" + target);
            println("functionAtOrContaining=" + describe(function));
            println("functionBefore=" + describe(findFunctionBefore(listing, target)));
            println("functionAfter=" + describe(findFunctionAfter(listing, target)));

            if (function != null) {
                if (decompiler == null) {
                    decompiler = setUpDecompiler();
                    if (!decompiler.openProgram(currentProgram)) {
                        printerr("Could not open program in decompiler: " + decompiler.getLastMessage());
                        return;
                    }
                }

                DecompileResults results = decompiler.decompileFunction(function, 120, monitor);
                if (!results.decompileCompleted() || results.getDecompiledFunction() == null) {
                    printerr("Decompile failed: " + results.getErrorMessage());
                    continue;
                }

                println(results.getDecompiledFunction().getC());
                continue;
            }

            Instruction instruction = listing.getInstructionAt(target);
            if (instruction == null) {
                instruction = listing.getInstructionContaining(target);
            }

            if (instruction == null) {
                DisassembleCommand command = new DisassembleCommand(target, null, true);
                if (command.applyTo(currentProgram, monitor)) {
                    instruction = listing.getInstructionAt(target);
                    if (instruction == null) {
                        instruction = listing.getInstructionContaining(target);
                    }
                }
            }

            if (instruction == null) {
                printerr("No instruction found at or containing target.");
                continue;
            }

            for (int i = 0; i < instructionCount && instruction != null; i++) {
                println(String.format("%s: %s", instruction.getAddress(), instruction));
                instruction = instruction.getNext();
            }
        }

        if (decompiler != null) {
            decompiler.dispose();
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