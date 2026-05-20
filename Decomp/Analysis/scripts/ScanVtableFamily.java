//@category NexusForever
// Scan a memory range for contiguous vtable-like entries that start with a
// known pair of function pointers, then print a configurable number of slots.

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.mem.Memory;

public class ScanVtableFamily extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 4) {
            printerr("Usage: ScanVtableFamily.java <start> <end> <slot0> <slot1> [slots]");
            return;
        }

        long startOffset = parseHex(args[0]);
        long endOffset = parseHex(args[1]);
        long slot0Target = parseHex(args[2]);
        long slot1Target = parseHex(args[3]);
        int slotsToPrint = args.length > 4 ? Integer.parseInt(args[4]) : 16;

        Memory memory = currentProgram.getMemory();
        FunctionManager functionManager = currentProgram.getFunctionManager();
        int pointerSize = currentProgram.getDefaultPointerSize();

        println(String.format(
            "scan start=%08x end=%08x slot0=%08x slot1=%08x slots=%d",
            startOffset, endOffset, slot0Target, slot1Target, slotsToPrint));

        int matches = 0;
        for (long offset = startOffset; offset <= endOffset - (long) pointerSize * 2; offset += pointerSize) {
            Address address = toAddr(offset);
            long slot0 = readPointer(memory, address, pointerSize);
            long slot1 = readPointer(memory, address.add(pointerSize), pointerSize);
            if (slot0 != slot0Target || slot1 != slot1Target) {
                continue;
            }

            matches++;
            println(String.format("match vtable=%s", address));
            for (int slot = 0; slot < slotsToPrint; slot++) {
                Address slotAddress = address.add((long) slot * pointerSize);
                long value = readPointer(memory, slotAddress, pointerSize);
                Address valueAddress = toAddr(value);
                Function function = functionManager.getFunctionAt(valueAddress);
                if (function == null) {
                    function = functionManager.getFunctionContaining(valueAddress);
                }
                String functionText = function == null ? "<no function>" : function.getName() + "@" + function.getEntryPoint();
                println(String.format("  slot +%d %s -> %08x %s", slot, slotAddress, value, functionText));
            }
        }

        println("matches=" + matches);
    }

    private long parseHex(String input) {
        String normalised = input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
        return Long.parseUnsignedLong(normalised, 16);
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
