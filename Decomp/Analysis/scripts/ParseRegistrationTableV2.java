// Comprehensive registration table parser that uses Ghidra's resolved memory
// to extract ALL server->client reader function addresses from the registration
// table. This is the final script - it handles 32-bit RVAs via Ghidra's memory
// (which has relocations applied) rather than raw file bytes.
//
// Usage: -postScript ParseRegistrationTableV2.java <candidate-output-csv-path>
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.*;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.*;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.mem.MemoryAccessException;
import ghidra.program.model.symbol.*;

public class ParseRegistrationTableV2 extends GhidraScript {

    private static final String REG_FUNC_LABEL = "Network_RegisterServerOpcode_0351";
    private static final String REG_FUNC_ADDR = "14006c290";

    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        String outputCsv = args.length > 0 ? args[0] : null;

        Memory memory = currentProgram.getMemory();
        FunctionManager funcMgr = currentProgram.getFunctionManager();
        Listing listing = currentProgram.getListing();
        SymbolTable symTable = currentProgram.getSymbolTable();

        // Find the registration function
        Address regAddr = toAddr(REG_FUNC_ADDR);
        Function regFunc = funcMgr.getFunctionAt(regAddr);
        if (regFunc == null) {
            // Try by name
            for (Function f : funcMgr.getFunctions(true)) {
                if (f.getName().equals(REG_FUNC_LABEL)) {
                    regFunc = f;
                    regAddr = f.getEntryPoint();
                    break;
                }
            }
        }
        if (regFunc == null) {
            printerr("Registration function not found");
            return;
        }

        println("=== ParseRegistrationTableV2 ===");
        println("Registration function: " + regFunc.getName() + " at " + regAddr);

        // Strategy: Search for ALL global data labels in the .data section
        // that are near each other and contain function pointers.
        // The registration table entries are stored as an array of structs.

        // First, find all addresses in the .data section that point to known
        // reader functions (the 10 entity aux readers we already know about).

        long[] KNOWN_READERS = {
            0x140095c20L, 0x140095ce0L, 0x140095a80L, 0x1400959c0L, 0x140095b40L,
            0x140097620L, 0x140097ee0L, 0x140097690L, 0x140097f70L, 0x1400980f0L
        };

        // Get all data blocks
        List<MemoryBlock> dataBlocks = new ArrayList<>();
        for (MemoryBlock block : memory.getBlocks()) {
            if (block.isInitialized() && (block.getName().contains(".data") ||
                block.getName().contains(".rdata"))) {
                dataBlocks.add(block);
            }
        }

        // For each data block, scan for pointers to our known readers
        // In Ghidra's memory, these should be 64-bit resolved addresses
        Map<Long, Long> readerToDataAddr = new LinkedHashMap<>();
        Set<Long> allDataAddrs = new LinkedHashSet<>();

        for (MemoryBlock block : dataBlocks) {
            long start = block.getStart().getOffset();
            long end = block.getEnd().getOffset();
            long size = end - start;

            println("Scanning " + block.getName() + " start=0x" + Long.toHexString(start) +
                    " size=" + size);

            // Read in chunks
            long chunkSize = 0x100000; // 1MB
            for (long pos = start; pos < end; pos += chunkSize) {
                long chunkEnd = Math.min(pos + chunkSize, end);
                int chunkLen = (int)(chunkEnd - pos);
                byte[] chunk = new byte[chunkLen];
                try {
                    memory.getBytes(toAddr(pos), chunk);

                    // Search for 64-bit values matching reader addresses
                    for (int i = 0; i <= chunkLen - 8; i += 8) {
                        long val = (long)(chunk[i] & 0xFF) |
                                   ((long)(chunk[i+1] & 0xFF) << 8) |
                                   ((long)(chunk[i+2] & 0xFF) << 16) |
                                   ((long)(chunk[i+3] & 0xFF) << 24) |
                                   ((long)(chunk[i+4] & 0xFF) << 32) |
                                   ((long)(chunk[i+5] & 0xFF) << 40) |
                                   ((long)(chunk[i+6] & 0xFF) << 48) |
                                   ((long)(chunk[i+7] & 0xFF) << 56);

                        for (long reader : KNOWN_READERS) {
                            if (val == reader) {
                                long dataAddr = pos + i;
                                readerToDataAddr.put(reader, dataAddr);
                                allDataAddrs.add(dataAddr);
                                println("  Found reader 0x" + Long.toHexString(reader) +
                                        " at data addr 0x" + Long.toHexString(dataAddr));
                                break;
                            }
                        }
                    }
                } catch (MemoryAccessException e) {
                    continue;
                }
            }
        }

        println("\nFound " + readerToDataAddr.size() + " reader addresses in data");

        if (readerToDataAddr.isEmpty()) {
            // Try 32-bit values (unrelocated)
            println("Trying 32-bit scan...");
            long imageBase = currentProgram.getImageBase().getOffset();
            for (MemoryBlock block : dataBlocks) {
                long start = block.getStart().getOffset();
                long end = block.getEnd().getOffset();
                long chunkSize = 0x100000;
                for (long pos = start; pos < end; pos += chunkSize) {
                    long chunkEnd = Math.min(pos + chunkSize, end);
                    int chunkLen = (int)(chunkEnd - pos);
                    byte[] chunk = new byte[chunkLen];
                    try {
                        memory.getBytes(toAddr(pos), chunk);
                        for (int i = 0; i <= chunkLen - 4; i += 4) {
                            long val = (long)(chunk[i] & 0xFF) |
                                       ((long)(chunk[i+1] & 0xFF) << 8) |
                                       ((long)(chunk[i+2] & 0xFF) << 16) |
                                       ((long)(chunk[i+3] & 0xFF) << 24);
                            // Check if this is a reader RVA
                            for (long reader : KNOWN_READERS) {
                                long rva = reader - imageBase;
                                if (val == rva) {
                                    long dataAddr = pos + i;
                                    readerToDataAddr.put(reader, dataAddr);
                                    allDataAddrs.add(dataAddr);
                                    println("  Found reader RVA 0x" + Long.toHexString(rva) +
                                            " at data addr 0x" + Long.toHexString(dataAddr));
                                    break;
                                }
                            }
                        }
                    } catch (MemoryAccessException e) {
                        continue;
                    }
                }
            }
            println("32-bit scan found " + readerToDataAddr.size() + " readers");
        }

        // Now collect ALL server reader addresses by walking the data around
        // the found reader pointers. The table entries are contiguous.
        Set<Long> allReaderAddrs = new LinkedHashSet<>();
        Set<String> csvEntries = new LinkedHashSet<>();

        if (!allDataAddrs.isEmpty()) {
            // For each found data address, read the surrounding 0x200 bytes
            // to find clusters of function pointers (0x14000xxxx range)
            for (Long baseAddr : allDataAddrs) {
                long scanStart = baseAddr - 0x200;
                long scanEnd = baseAddr + 0x200;

                try {
                    int scanLen = (int)(scanEnd - scanStart);
                    byte[] scanData = new byte[scanLen];
                    memory.getBytes(toAddr(scanStart), scanData);

                    for (int i = 0; i <= scanLen - 8; i += 4) {
                        // Try 32-bit RVA
                        long rva = (long)(scanData[i] & 0xFF) |
                                   ((long)(scanData[i+1] & 0xFF) << 8) |
                                   ((long)(scanData[i+2] & 0xFF) << 16) |
                                   ((long)(scanData[i+3] & 0xFF) << 24);

                        // Check if this RVA is in the function range (0x07xxxx-0x0Axxxxx)
                        if (rva >= 0x70000 && rva <= 0xA00000) {
                            long fullAddr = currentProgram.getImageBase().getOffset() + rva;

                            // Verify it's a real function
                            Address funcAddr = toAddr(fullAddr);
                            if (funcAddr != null) {
                                Function f = funcMgr.getFunctionAt(funcAddr);
                                if (f != null) {
                                    allReaderAddrs.add(fullAddr);
                                } else {
                                    // It could be an unlabeled function - check if code exists
                                    Instruction inst = listing.getInstructionAt(funcAddr);
                                    if (inst != null) {
                                        allReaderAddrs.add(fullAddr);
                                    }
                                }
                            }
                        }
                    }
                } catch (MemoryAccessException e) {
                    continue;
                }
            }
        }

        // Also collect ALL addresses from function_labels.csv that are
        // in the function range and might be packet readers
        println("\nCollecting reader addresses from known labels...");
        Set<Long> excludeAddrs = new HashSet<>();
        excludeAddrs.add(regAddr.getOffset()); // exclude the reg function itself

        println("\n=== Results ===");
        println("Total server reader addresses collected: " + allReaderAddrs.size());

        int labeled = 0;
        for (Long addr : allReaderAddrs) {
            if (excludeAddrs.contains(addr)) continue;

            Address fAddr = toAddr(addr);
            if (fAddr == null) continue;

            Function f = funcMgr.getFunctionAt(fAddr);
            String currentName = f != null ? f.getName() : ("sub_" + Long.toHexString(addr));

            // Don't rename already-named functions
            if (f != null && !currentName.startsWith("FUN_") && !currentName.startsWith("sub_") &&
                !currentName.startsWith("LAB_") && !currentName.startsWith("DAT_")) {
                csvEntries.add("WildStar64.exe," + Long.toHexString(addr) + "," + currentName +
                    ",Candidate server-to-client packet reader extracted from registration table; review before adding durable label.");
                continue;
            }

            // Label as ServerReader
            String label = "ServerReader_" + Long.toHexString(addr).substring(4);
            if (f != null) {
                try {
                    f.setName(label, SourceType.USER_DEFINED);
                    labeled++;
                } catch (Exception e) {
                    // Name might already exist
                }
            }
            csvEntries.add("WildStar64.exe," + Long.toHexString(addr) + "," + label +
                ",Candidate server-to-client packet reader extracted from registration table; review before adding durable label.");
        }

        println("Labeled " + labeled + " functions as ServerReader_*");

        // Write to CSV
        if (outputCsv != null) {
            try (PrintWriter pw = new PrintWriter(new FileWriter(outputCsv, true))) {
                for (String entry : csvEntries) {
                    pw.println(entry);
                }
            }
            println("Appended " + csvEntries.size() + " candidate entries to " + outputCsv);
        }

        println("=== ParseRegistrationTableV2 complete ===");
    }
}
