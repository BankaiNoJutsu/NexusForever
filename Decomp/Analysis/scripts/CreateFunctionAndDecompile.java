// Disassemble at an address, create a function from entry through the next
// known function (or optional end address), decompile, and print C.
//
// Usage:
//   -postScript CreateFunctionAndDecompile.java <entry> [end]
//@category NexusForever

import ghidra.app.cmd.disassemble.DisassembleCommand;
import ghidra.app.cmd.function.CreateFunctionCmd;
import ghidra.app.decompiler.DecompInterface;
import ghidra.program.model.address.AddressSet;
import ghidra.program.model.symbol.SourceType;
import ghidra.app.decompiler.DecompileOptions;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.decompiler.component.DecompilerUtils;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.listing.Listing;

public class CreateFunctionAndDecompile extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 1) {
            printerr("Usage: CreateFunctionAndDecompile.java <entry> [end]");
            return;
        }

        Address entry = toAddr(stripHexPrefix(args[0]));
        if (entry == null) {
            printerr("Could not resolve entry address.");
            return;
        }

        Address end = null;
        if (args.length > 1) {
            end = toAddr(stripHexPrefix(args[1]));
        }
        if (end == null) {
            Function next = findFunctionAfter(currentProgram.getListing(), entry);
            if (next == null) {
                printerr("Could not resolve end address; pass explicit end.");
                return;
            }
            end = next.getEntryPoint();
        }

        println("entry=" + entry);
        println("end=" + end);

        DisassembleCommand disassemble = new DisassembleCommand(entry, null, true);
        if (!disassemble.applyTo(currentProgram, monitor)) {
            printerr("Disassemble failed at " + entry);
            return;
        }

        Function existing = getFunctionAt(entry);
        if (existing == null) {
            existing = getFunctionContaining(entry);
        }
        AddressSet desiredBody = new AddressSet(entry, end.subtractNoWrap(1));
        if (existing != null && existing.getBody().getNumAddresses() < desiredBody.getNumAddresses() / 4) {
            println("replacingSmallFunction=" + describe(existing) + " oldBody=" + existing.getBody().getNumAddresses());
            currentProgram.getFunctionManager().removeFunction(existing.getEntryPoint());
            existing = null;
        }
        if (existing == null) {
            CreateFunctionCmd create = new CreateFunctionCmd(desiredBody, SourceType.USER_DEFINED);
            if (!create.applyTo(currentProgram, monitor)) {
                printerr("CreateFunction failed at " + entry + ": " + create.getStatusMsg());
                return;
            }
            existing = getFunctionAt(entry);
            if (existing == null) {
                existing = getFunctionContaining(entry);
            }
            println("functionCreated=" + describe(existing));
        }
        else {
            println("functionAlready=" + describe(existing) + " body=" + existing.getBody());
        }

        if (existing == null) {
            printerr("No function available after create.");
            return;
        }

        DecompInterface decompiler = setUpDecompiler();
        try {
            if (!decompiler.openProgram(currentProgram)) {
                printerr("Decompiler open failed: " + decompiler.getLastMessage());
                return;
            }

            DecompileResults results = decompiler.decompileFunction(existing, 180, monitor);
            if (!results.decompileCompleted() || results.getDecompiledFunction() == null) {
                printerr("Decompile failed: " + results.getErrorMessage());
                return;
            }

            println("decompiledEntry=" + existing.getEntryPoint());
            println("decompiledBody=" + existing.getBody());
            println(results.getDecompiledFunction().getC());
        }
        finally {
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

    private String describe(Function function) {
        return function == null ? "<none>" : function.getName() + "@" + function.getEntryPoint();
    }

    private String stripHexPrefix(String input) {
        return input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
    }
}
