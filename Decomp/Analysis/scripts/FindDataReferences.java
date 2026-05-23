// Print code references to a data address.
//
// Usage:
//   -postScript FindDataReferences.java <dataAddress> [maxRefs]
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class FindDataReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 1) {
            printerr("Usage: FindDataReferences.java <dataAddress> [maxRefs]");
            return;
        }

        Address target = toAddr(parseHex(args[0]));
        int maxRefs = args.length > 1 ? Integer.parseInt(args[1]) : 64;
        ReferenceManager referenceManager = currentProgram.getReferenceManager();
        ReferenceIterator iterator = referenceManager.getReferencesTo(target);

        println("target=" + target);
        int count = 0;
        while (iterator.hasNext() && count < maxRefs) {
            Reference reference = iterator.next();
            Address from = reference.getFromAddress();
            Function function = getFunctionContaining(from);
            String functionText = function == null
                ? "<no_fn>"
                : function.getName() + "@" + function.getEntryPoint();
            Instruction instruction = currentProgram.getListing().getInstructionAt(from);
            String instructionText = instruction == null ? "<no_insn>" : instruction.toString();
            println(String.format(
                "ref from=%s type=%s function=%s insn=%s",
                from,
                reference.getReferenceType(),
                functionText,
                instructionText));
            count++;
        }

        println("total_refs=" + count);
    }

    private long parseHex(String input) {
        String normalised = input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
        return Long.parseUnsignedLong(normalised, 16);
    }
}
