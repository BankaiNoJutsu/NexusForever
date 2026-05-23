// RegistrationTableXref — parse the server opcode registration table from raw
// .data memory, create artificial xrefs from table slots to reader functions,
// and identify the consumer dispatch function.
//
// The registration table is stored in the .data section as 32-bit RVAs packed
// at 4-byte intervals. Ghidra may or may not apply relocations — this script
// searches for both 32-bit RVAs and 64-bit full addresses.
//
// After creating xrefs, a subsequent Ghidra decomp pass will be able to trace
// the consumer dispatch by following references to the reader functions.
//
// Usage: -postScript RegistrationTableXref.java function_labels.csv
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

public class RegistrationTableXref extends GhidraScript {

    // Known entity aux reader addresses and their function names from decomp
    private static final long[][] KNOWN_READERS = {
        {0x140095c20L, 0x025F}, // Server0x025F_ReadPayload
        {0x140095ce0L, 0x0260}, // opcode 0x0260/0x0261 consumer
        {0x140095a80L, 0x0261}, // opcode 0x0261 consumer
        {0x1400959c0L, 0x0263}, // Server0x0263_ReadPayload
        {0x140095b40L, 0x0264}, // Server0x0264_ReadPayload
        {0x140097620L, 0x08F4}, // Server0x08F4_ReadPayload (also 0x087F, 0x0938)
        {0x140097690L, 0x093D}, // Server0x093D_ReadPayload
        {0x140097ee0L, 0x0939}, // Server0x0939_ReadPayload
        {0x140097f70L, 0x093E}, // Server0x093E_ReadPayload
        {0x1400980f0L, 0x08CC}, // ServerUInt32WideString_ReadPayload
    };

    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        String csvPath = args.length > 0 ? args[0] : null;

        Memory memory = currentProgram.getMemory();
        FunctionManager funcMgr = currentProgram.getFunctionManager();
        ReferenceManager refMgr = currentProgram.getReferenceManager();
        Listing listing = currentProgram.getListing();
        long imageBase = currentProgram.getImageBase().getOffset();

        println("=== RegistrationTableXref ===");
        println("Image base: 0x" + Long.toHexString(imageBase));

        // Find data blocks
        List<MemoryBlock> dataBlocks = new ArrayList<>();
        for (MemoryBlock block : memory.getBlocks()) {
            if (block.isInitialized() &&
                (block.getName().contains(".data") || block.getName().contains(".rdata"))) {
                dataBlocks.add(block);
            }
        }

        // Step 1: Find reader addresses in .data/.rdata
        // Try both 32-bit RVA and 64-bit full-address formats
        Map<Long, Long> readerToStorageAddr = new LinkedHashMap<>(); // reader VA -> storage VA
        Map<Long, List<Long>> storageToReaders = new TreeMap<>();    // storage VA -> readers

        for (MemoryBlock block : dataBlocks) {
            long start = block.getStart().getOffset();
            long end = block.getEnd().getOffset();
            long size = end - start;

            println("Scanning " + block.getName() + " 0x" + Long.toHexString(start) +
                    " size=0x" + Long.toHexString(size));

            long chunkSize = 0x100000; // 1MB
            for (long pos = start; pos < end; pos += chunkSize) {
                long chunkEnd = Math.min(pos + chunkSize, end);
                int chunkLen = (int)(chunkEnd - pos);
                byte[] chunk = new byte[chunkLen];
                try {
                    memory.getBytes(toAddr(pos), chunk);

                    // Search for 32-bit RVAs (4-byte aligned)
                    for (int i = 0; i <= chunkLen - 4; i += 4) {
                        long val32 = readU32LE(chunk, i);
                        for (long[] entry : KNOWN_READERS) {
                            long readerFull = entry[0];
                            long rva = readerFull - imageBase;
                            if (val32 == rva) {
                                long storageAddr = pos + i;
                                readerToStorageAddr.put(readerFull, storageAddr);
                                storageToReaders.computeIfAbsent(storageAddr, k -> new ArrayList<>())
                                    .add(readerFull);
                            }
                        }
                    }

                    // Also search for 64-bit full addresses
                    for (int i = 0; i <= chunkLen - 8; i += 8) {
                        long val64 = readU64LE(chunk, i);
                        for (long[] entry : KNOWN_READERS) {
                            if (val64 == entry[0]) {
                                long storageAddr = pos + i;
                                if (!readerToStorageAddr.containsKey(entry[0])) {
                                    readerToStorageAddr.put(entry[0], storageAddr);
                                    storageToReaders.computeIfAbsent(storageAddr, k -> new ArrayList<>())
                                        .add(entry[0]);
                                }
                            }
                        }
                    }
                } catch (MemoryAccessException e) {
                    continue;
                }
            }
        }

        println("\nFound " + readerToStorageAddr.size() + " of " + KNOWN_READERS.length +
                " known readers in data sections");

        if (readerToStorageAddr.isEmpty()) {
            printerr("No readers found. The table may use a different storage format.");
            return;
        }

        // Step 2: Walk contiguous memory around found entries to extract
        // ALL function pointers (not just known ones)
        Set<Long> allStorageAddrs = new TreeSet<>(storageToReaders.keySet());
        Set<Long> allReaderAddrs = new LinkedHashSet<>();

        // Determine the address range where the table lives
        Long minStorage = allStorageAddrs.iterator().next();
        Long maxStorage = null;
        for (Long a : allStorageAddrs) maxStorage = a;

        println("Table spans: 0x" + Long.toHexString(minStorage) +
                " - 0x" + Long.toHexString(maxStorage));

        // Expand range to capture the full table
        long tableStart = minStorage - 0x1000;
        long tableEnd = maxStorage + 0x10000;
        int entryCount = 0;

        try {
            int scanLen = (int)(tableEnd - tableStart);
            byte[] tableData = new byte[scanLen];
            memory.getBytes(toAddr(tableStart), tableData);

            // Walk 4-byte-aligned values looking for function pointers
            // A function pointer is a 32-bit RVA in the range 0x070000-0x0B0000
            for (int i = 0; i <= scanLen - 4; i += 4) {
                long rva = readU32LE(tableData, i);
                // Function RVAs are in the text section range
                if (rva >= 0x70000 && rva <= 0xB00000) {
                    long fullAddr = imageBase + rva;
                    Address funcAddr = toAddr(fullAddr);
                    if (funcAddr == null) continue;

                    // Check if it might be a real function
                    Function f = funcMgr.getFunctionAt(funcAddr);
                    Instruction inst = listing.getInstructionAt(funcAddr);
                    if (f != null || inst != null) {
                        long storageAddr = tableStart + i;
                        allReaderAddrs.add(fullAddr);

                        // Create a DATA reference from storage to the function
                        try {
                            refMgr.addMemoryReference(
                                toAddr(storageAddr),
                                funcAddr,
                                RefType.DATA,
                                SourceType.USER_DEFINED,
                                0
                            );
                        } catch (Exception e) {
                            // Reference might already exist
                        }

                        entryCount++;
                        if (entryCount <= 20) {
                            String name = f != null ? f.getName() : ("0x" + Long.toHexString(fullAddr));
                            println("  [0x" + Long.toHexString(storageAddr) + "] -> " + name);
                        }
                    }
                }
            }
            println("Created " + entryCount + " data references from table entries to functions");

        } catch (MemoryAccessException e) {
            printerr("Error reading table: " + e.getMessage());
        }

        // Step 3: Label all found reader functions
        int labeled = 0;
        Set<String> csvLines = new LinkedHashSet<>();
        for (Long addr : allReaderAddrs) {
            Address fAddr = toAddr(addr);
            if (fAddr == null) continue;
            Function f = funcMgr.getFunctionAt(fAddr);
            String currentName = f != null ? f.getName() : "";
            if (currentName.startsWith("FUN_") || currentName.startsWith("LAB_") ||
                currentName.startsWith("DAT_") || currentName.isEmpty()) {
                String label = "ServerReader_0x" + Long.toHexString(addr).substring(4);
                try {
                    if (f != null) {
                        f.setName(label, SourceType.USER_DEFINED);
                        labeled++;
                    }
                } catch (Exception e) {}
                csvLines.add("WildStar64.exe," + Long.toHexString(addr) + "," + label +
                    ",Server->client opcode reader extracted from registration table.");
            }
        }
        println("Labeled " + labeled + " new reader functions as ServerReader_*");

        // Step 4: Try to find the dispatch function
        // The dispatch function references the registration table's base address
        // or iterates through it. Look for functions near the registration function
        // that have many xrefs (they'd be called for each incoming packet).
        println("\n=== Searching for dispatch function ===");

        // Find the registration function
        Function regFunc = funcMgr.getFunctionAt(toAddr(0x14006c290L));
        if (regFunc != null) {
            // Look for functions that reference any of the storage addresses
            // (indicating they read from the table)
            Set<Long> candidateDispatch = new LinkedHashSet<>();
            for (Long storageAddr : allStorageAddrs) {
                ReferenceIterator refs = refMgr.getReferencesTo(toAddr(storageAddr));
                while (refs.hasNext()) {
                    Reference ref = refs.next();
                    Address fromAddr = ref.getFromAddress();
                    Function caller = funcMgr.getFunctionContaining(fromAddr);
                    if (caller != null) {
                        long callerAddr = caller.getEntryPoint().getOffset();
                        if (callerAddr != 0x14006c290L) { // Not the reg function itself
                            candidateDispatch.add(callerAddr);
                        }
                    }
                }
            }

            if (!candidateDispatch.isEmpty()) {
                println("Candidate dispatch functions (reference table data):");
                for (Long addr : candidateDispatch) {
                    Function f = funcMgr.getFunctionAt(toAddr(addr));
                    String name = f != null ? f.getName() : "?";
                    println("  0x" + Long.toHexString(addr) + " " + name);
                    csvLines.add("WildStar64.exe," + Long.toHexString(addr) + "," +
                        (f != null ? f.getName() : ("sub_" + Long.toHexString(addr))) +
                        ",Candidate consumer dispatch function — references registration table.");
                }
            } else {
                println("No direct references to table from outside registration function.");
                println("Dispatch is likely through a global pointer (indirect) — need Ghidra GUI.");
            }
        }

        // Step 5: Write CSV
        if (csvPath != null && !csvLines.isEmpty()) {
            try (PrintWriter pw = new PrintWriter(new FileWriter(csvPath, true))) {
                for (String line : csvLines) {
                    pw.println(line);
                }
            }
            println("\nAppended " + csvLines.size() + " lines to " + csvPath);
        }

        println("\n=== RegistrationTableXref complete ===");
        println("Total reader functions found: " + allReaderAddrs.size());
        println("Total xrefs created: " + csvLines.size());
    }

    private static long readU32LE(byte[] data, int off) {
        return (long)(data[off] & 0xFF) |
               ((long)(data[off+1] & 0xFF) << 8) |
               ((long)(data[off+2] & 0xFF) << 16) |
               ((long)(data[off+3] & 0xFF) << 24);
    }

    private static long readU64LE(byte[] data, int off) {
        return (long)(data[off] & 0xFF) |
               ((long)(data[off+1] & 0xFF) << 8) |
               ((long)(data[off+2] & 0xFF) << 16) |
               ((long)(data[off+3] & 0xFF) << 24) |
               ((long)(data[off+4] & 0xFF) << 32) |
               ((long)(data[off+5] & 0xFF) << 40) |
               ((long)(data[off+6] & 0xFF) << 48) |
               ((long)(data[off+7] & 0xFF) << 56);
    }
}
