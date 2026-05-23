// Find vtable-like pointer arrays whose slot N equals a target function.
//
// Usage:
//   -postScript FindVtableSlotReferences.java <slotIndex> <target> [scanStart] [scanEnd] [maxMatches]
//
// Example:
//   -postScript FindVtableSlotReferences.java 11 1403db050 140b50000 140e00000 32
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.mem.Memory;

public class FindVtableSlotReferences extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 2) {
            printerr("Usage: FindVtableSlotReferences.java <slotIndex> <target> [scanStart] [scanEnd] [maxMatches]");
            return;
        }

        int slotIndex = Integer.parseInt(args[0]);
        long target = parseHex(args[1]);
        long scanStart = args.length > 2 ? parseHex(args[2]) : 0x140b40000L;
        long scanEnd = args.length > 3 ? parseHex(args[3]) : 0x140e00000L;
        int maxMatches = args.length > 4 ? Integer.parseInt(args[4]) : 32;

        int pointerSize = currentProgram.getDefaultPointerSize();
        long slotOffset = (long) slotIndex * pointerSize;
        Memory memory = currentProgram.getMemory();
        FunctionManager functionManager = currentProgram.getFunctionManager();

        println(String.format(
            "slotIndex=%d slotOffset=0x%x target=%s scan=%s..%s max=%d",
            slotIndex, slotOffset, formatHex(target), formatHex(scanStart), formatHex(scanEnd), maxMatches));

        int matches = 0;
        for (long offset = scanStart; offset <= scanEnd - slotOffset - pointerSize; offset += pointerSize) {
            Address slotAddress = toAddr(offset + slotOffset);
            if (memory.getBlock(slotAddress) == null || !memory.getBlock(slotAddress).isInitialized()) {
                continue;
            }
            long value;
            try {
                value = readPointer(memory, slotAddress, pointerSize);
            }
            catch (Exception ex) {
                continue;
            }
            if (value != target) {
                continue;
            }

            Address vtableAddress = toAddr(offset);
            Function function = functionManager.getFunctionAt(toAddr(value));
            if (function == null) {
                function = functionManager.getFunctionContaining(toAddr(value));
            }

            matches++;
            println(String.format(
                "match vtable=%s slot+%d=%s -> %s",
                vtableAddress,
                slotIndex,
                slotAddress,
                function == null ? formatHex(value) : function.getName() + "@" + function.getEntryPoint()));

            if (matches >= maxMatches) {
                break;
            }
        }

        println("matches=" + matches);
    }

    private long parseHex(String input) {
        String normalised = input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
        return Long.parseUnsignedLong(normalised, 16);
    }

    private String formatHex(long value) {
        return "0x" + Long.toHexString(value);
    }

    private long readPointer(Memory memory, Address address, int pointerSize) throws Exception {
        if (pointerSize == 8) {
            return memory.getLong(address);
        }
        if (pointerSize == 4) {
            return Integer.toUnsignedLong(memory.getInt(address));
        }
        throw new IllegalStateException("Unsupported pointer size: " + pointerSize);
    }
}
