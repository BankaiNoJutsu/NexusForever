// Scan initialized .data/.rdata for 64-bit pointers equal to a target address.
//
// Usage:
//   -postScript FindPointerInData.java <target> [maxMatches]
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryBlock;

public class FindPointerInData extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length < 1) {
            printerr("Usage: FindPointerInData.java <target> [maxMatches]");
            return;
        }

        long target = parseHex(args[0]);
        int maxMatches = args.length > 1 ? Integer.parseInt(args[1]) : 32;
        Memory memory = currentProgram.getMemory();
        FunctionManager functionManager = currentProgram.getFunctionManager();

        println("target=" + formatHex(target) + " maxMatches=" + maxMatches);

        int matches = 0;
        for (MemoryBlock block : memory.getBlocks()) {
            if (!block.isInitialized()) {
                continue;
            }
            String name = block.getName();
            if (!name.contains(".data") && !name.contains(".rdata")) {
                continue;
            }

            long start = block.getStart().getOffset();
            long end = block.getEnd().getOffset();
            long chunkSize = 0x100000L;
            for (long pos = start; pos < end; pos += chunkSize) {
                long chunkEnd = Math.min(pos + chunkSize, end);
                int chunkLen = (int) (chunkEnd - pos);
                byte[] chunk = new byte[chunkLen];
                try {
                    memory.getBytes(toAddr(pos), chunk);
                }
                catch (Exception ex) {
                    continue;
                }

                for (int i = 0; i <= chunkLen - 8; i += 8) {
                    long value = readU64(chunk, i);
                    if (value != target) {
                        continue;
                    }

                    long dataAddr = pos + i;
                    Address address = toAddr(dataAddr);
                    Function function = functionManager.getFunctionContaining(address);
                    println(String.format(
                        "match data=%s block=%s containing=%s",
                        address,
                        name,
                        function == null ? "<none>" : function.getName() + "@" + function.getEntryPoint()));
                    matches++;
                    if (matches >= maxMatches) {
                        println("matches=" + matches);
                        return;
                    }
                }
            }
        }

        println("matches=" + matches);
    }

    private long readU64(byte[] chunk, int offset) {
        return (long) (chunk[offset] & 0xFF)
            | ((long) (chunk[offset + 1] & 0xFF) << 8)
            | ((long) (chunk[offset + 2] & 0xFF) << 16)
            | ((long) (chunk[offset + 3] & 0xFF) << 24)
            | ((long) (chunk[offset + 4] & 0xFF) << 32)
            | ((long) (chunk[offset + 5] & 0xFF) << 40)
            | ((long) (chunk[offset + 6] & 0xFF) << 48)
            | ((long) (chunk[offset + 7] & 0xFF) << 56);
    }

    private long parseHex(String input) {
        String normalised = input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
        return Long.parseUnsignedLong(normalised, 16);
    }

    private String formatHex(long value) {
        return "0x" + Long.toHexString(value);
    }
}
