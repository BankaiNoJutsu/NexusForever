// ParseRegistrationTable - Walk the registration table entries in raw binary memory
// to extract ALL server-to-client reader function addresses. This bypasses Ghidra's
// reference tracking completely by reading the actual memory layout at runtime.
//
// The registration function Network_RegisterServerOpcode_0351 at 0x14006c290
// stores entries in a global table. Each entry has this layout (from decomp):
//   offset 0x00: uint64 next_ptr (linked list / table pointer)
//   offset 0x08: uint32 opcode
//   offset 0x0C: uint32 data_size
//   offset 0x10: uint64 client_writer_fn (client-to-server writer)
//   offset 0x18: uint64 server_reader_fn (server-to-client reader)
//   offset 0x20: uint32 flags_1
//   offset 0x24: uint32 flags_2
// Total: 0x28 (40) bytes per entry
//
// This script:
// 1. Finds the global table address from the registration function's first store
// 2. Walks all entries in the table
// 3. Extracts server reader addresses (non-zero)
// 4. Creates data references in Ghidra for tracking
// 5. Outputs candidate CSV rows for downstream manual review
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.*;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.*;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryAccessException;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.symbol.*;
import ghidra.util.exception.CancelledException;

public class ParseRegistrationTable extends GhidraScript {

    private static final String REG_FUNC_ADDR = "14006c290";

    // Entry layout constants
    private static final int ENTRY_SIZE = 0x28; // 40 bytes
    private static final int OPCODE_OFFSET   = 0x08;
    private static final int DATA_SIZE_OFFSET = 0x0C;
    private static final int CLIENT_WRITER_OFFSET = 0x10;
    private static final int SERVER_READER_OFFSET = 0x18;
    private static final int FLAGS1_OFFSET = 0x20;
    private static final int FLAGS2_OFFSET = 0x24;

    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        String outputPath = args.length > 0 ? args[0] : null;

        Memory memory = currentProgram.getMemory();
        FunctionManager funcMgr = currentProgram.getFunctionManager();
        ReferenceManager refMgr = currentProgram.getReferenceManager();

        // Find the registration function
        Address regAddr = toAddr(REG_FUNC_ADDR);
        Function regFunc = funcMgr.getFunctionAt(regAddr);
        if (regFunc == null) {
            printerr("Registration function not found at " + REG_FUNC_ADDR);
            return;
        }

        println("=== ParseRegistrationTable ===");
        println("Registration function: " + regFunc.getName() + " at " + regAddr);
        println("Function size: " + regFunc.getBody().getNumAddresses() + " bytes");

        // Strategy: read the first 0x200 bytes of the registration function's
        // code to find MOVABS instructions that store global table pointers.
        // The pattern is: MOVABS RAX, <global_table> ; MOV qword ptr [RAX+offset], ...
        // We need to find the base address of the table.

        // Alternative: search for the known reader addresses in memory as function
        // pointers. The readers are at known addresses:
        long[] KNOWN_READERS = {
            0x140095c20L, 0x140095ce0L, 0x140095a80L, 0x1400959c0L, 0x140095b40L,
            0x140097620L, 0x140097ee0L, 0x140097690L, 0x140097f70L, 0x1400980f0L
        };

        // Search .data and .rdata sections for these reader addresses
        List<MemoryBlock> dataBlocks = new ArrayList<>();
        for (MemoryBlock block : memory.getBlocks()) {
            if (block.isInitialized() && (block.getName().contains(".data") ||
                block.getName().contains(".rdata"))) {
                dataBlocks.add(block);
                println("Data block: " + block.getName() + " at " + block.getStart() +
                        " size=" + block.getSize());
            }
        }

        // For each known reader, find where its address is stored in data sections
        Map<Long, Long> readerToStorage = new LinkedHashMap<>();
        for (long readerAddr : KNOWN_READERS) {
            boolean found = false;
            for (MemoryBlock block : dataBlocks) {
                byte[] data = new byte[8];
                long start = block.getStart().getOffset();
                long end = block.getEnd().getOffset();

                // Read in chunks to avoid OOM
                long chunkSize = 0x100000; // 1MB chunks
                for (long pos = start; pos < end && !found; pos += (chunkSize - 8)) {
                    long chunkEnd = Math.min(pos + chunkSize, end);
                    try {
                        int chunkLen = (int)(chunkEnd - pos);
                        byte[] chunk = new byte[chunkLen];
                        memory.getBytes(toAddr(pos), chunk);

                        for (int i = 0; i <= chunkLen - 8; i += 8) {
                            long val = readUInt64LE(chunk, i);
                            if (val == readerAddr) {
                                long storageAddr = pos + i;
                                readerToStorage.put(readerAddr, storageAddr);
                                println("Found reader " + Long.toHexString(readerAddr) +
                                        " stored at " + Long.toHexString(storageAddr) +
                                        " in " + block.getName());
                                found = true;
                                break;
                            }
                        }
                    } catch (MemoryAccessException e) {
                        continue;
                    }
                }
                if (found) break;
            }
            if (!found) {
                println("WARNING: Could not find storage for reader " + Long.toHexString(readerAddr));
            }
        }

        println("\nFound " + readerToStorage.size() + " reader addresses in data sections");

        // Now find the registration table itself: look for a contiguous array of
        // 40-byte entries that contain these reader addresses at offset 0x18.
        // Walk backwards from each found reader storage address to find the
        // table base.

        Set<Long> tableBases = new LinkedHashSet<>();
        for (Map.Entry<Long, Long> entry : readerToStorage.entrySet()) {
            long readerAddr = entry.getKey();
            long storageAddr = entry.getValue();

            // The reader is at offset 0x18 in the entry, so entry base = storageAddr - 0x18
            long entryBase = storageAddr - SERVER_READER_OFFSET;
            tableBases.add(entryBase);

            // Read the full entry
            try {
                byte[] entryBytes = new byte[ENTRY_SIZE];
                memory.getBytes(toAddr(entryBase), entryBytes);

                long opcode = readUInt32LE(entryBytes, OPCODE_OFFSET);
                long dataSize = readUInt32LE(entryBytes, DATA_SIZE_OFFSET);
                long clientWriter = readUInt64LE(entryBytes, CLIENT_WRITER_OFFSET);
                long serverReader = readUInt64LE(entryBytes, SERVER_READER_OFFSET);
                long flags1 = readUInt32LE(entryBytes, FLAGS1_OFFSET);
                long flags2 = readUInt32LE(entryBytes, FLAGS2_OFFSET);

                println(String.format("Entry at 0x%x: opcode=0x%04X size=0x%X " +
                    "writer=0x%x reader=0x%x flags=0x%X/0x%X",
                    entryBase, opcode, dataSize, clientWriter, serverReader, flags1, flags2));
            } catch (MemoryAccessException e) {
                println("Error reading entry at " + Long.toHexString(entryBase));
            }
        }

        println("\nTotal unique entry bases: " + tableBases.size());

        // Collect all reader addresses and output
        Set<String> allServerReaders = new LinkedHashSet<>();
        for (Long tableBase : tableBases) {
            // Walk contiguous entries (entries are likely in an array)
            for (int offset = 0; offset < 0x10000; offset += ENTRY_SIZE) {
                long entryAddr = tableBase + offset;
                try {
                    byte[] entryBytes = new byte[ENTRY_SIZE];
                    memory.getBytes(toAddr(entryAddr), entryBytes);

                    long opcode = readUInt32LE(entryBytes, OPCODE_OFFSET);
                    long serverReader = readUInt64LE(entryBytes, SERVER_READER_OFFSET);

                    // Stop if opcode is zero and reader is zero (no more entries)
                    if (opcode == 0 && serverReader == 0 && offset > 0) {
                        break;
                    }

                    if (serverReader != 0) {
                        allServerReaders.add(Long.toHexString(serverReader));

                        // Try to label the reader function
                        Function readerFunc = funcMgr.getFunctionAt(toAddr(serverReader));
                        if (readerFunc != null) {
                            String label = String.format("ServerReader_%04X", opcode);
                            readerFunc.setName(label, SourceType.USER_DEFINED);
                            println("Labeled 0x" + Long.toHexString(serverReader) + " as " + label);
                        }

                        // Create data reference from table entry to reader
                        refMgr.addMemoryReference(
                            toAddr(entryAddr + SERVER_READER_OFFSET),
                            toAddr(serverReader),
                            RefType.DATA,
                            SourceType.ANALYSIS,
                            0
                        );
                    }
                } catch (Exception e) {
                    break;
                }
            }
        }

        println("\nTotal server-to-client readers found: " + allServerReaders.size());

        // Output candidate CSV rows for manual review before durable import.
        if (outputPath != null && !allServerReaders.isEmpty()) {
            try (PrintWriter pw = new PrintWriter(new FileWriter(outputPath, true))) {
                for (String addr : allServerReaders) {
                    Function f = funcMgr.getFunctionAt(toAddr(addr));
                    String name = f != null ? f.getName() : ("ServerReader_" + addr.substring(addr.length() - 6));
                    pw.println("WildStar64.exe," + addr + "," + name +
                        ",Candidate server-to-client opcode reader extracted from registration table; review before adding durable label.");
                }
            }
            println("Appended " + allServerReaders.size() + " candidate labels to " + outputPath);
        }

        println("=== ParseRegistrationTable complete ===");
    }

    private static long readUInt32LE(byte[] data, int offset) {
        return (long)(data[offset] & 0xFF) |
               ((long)(data[offset+1] & 0xFF) << 8) |
               ((long)(data[offset+2] & 0xFF) << 16) |
               ((long)(data[offset+3] & 0xFF) << 24);
    }

    private static long readUInt64LE(byte[] data, int offset) {
        return (long)(data[offset] & 0xFF) |
               ((long)(data[offset+1] & 0xFF) << 8) |
               ((long)(data[offset+2] & 0xFF) << 16) |
               ((long)(data[offset+3] & 0xFF) << 24) |
               ((long)(data[offset+4] & 0xFF) << 32) |
               ((long)(data[offset+5] & 0xFF) << 40) |
               ((long)(data[offset+6] & 0xFF) << 48) |
               ((long)(data[offset+7] & 0xFF) << 56);
    }
}
