// FindDispatch — locate the consumer dispatch function that reads incoming
// server→client packets and dispatches them via the registration table.
//
// The registration function at 0x14006c290 uses a global function pointer:
//   puVar1 = DAT_140c65808;
//   (**(code **)*puVar1)(puVar1, opcode, size, ..., reader, flags);
//
// DAT_140c65808 points to the actual registration routine. The dispatch
// function is different — it's called when a packet arrives, looks up the
// opcode in the table, and calls the reader. It must reference either:
//   a) The same global pointer (DAT_140c65808 or nearby)
//   b) The registration table data addresses (0x140c66xxx range)
//   c) The registration function itself (0x14006c290)
//
// This script:
// 1. Finds xrefs to the key global addresses
// 2. Labels the dispatch candidates
// 3. Appends to function_labels.csv for the next decomp pass
//
// Usage: -postScript FindDispatch.java function_labels.csv
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.*;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.*;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.symbol.*;

public class FindDispatch extends GhidraScript {

    // Key addresses from the registration function decomp
    private static final long REG_FUNC        = 0x14006c290L; // Network_RegisterServerOpcode_0351
    private static final long GLOBAL_PTR      = 0x140c65808L; // DAT_140c65808 — the register-opcode fn ptr
    private static final long TABLE_REGION_START = 0x140c66000L; // .data region with table entries
    private static final long TABLE_REGION_END   = 0x140c67000L;

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

        println("=== FindDispatch ===");

        Set<String> csvLines = new LinkedHashSet<>();
        Set<Long> dispatchCandidates = new LinkedHashSet<>();

        // Step 1: Find xrefs TO the global pointer DAT_140c65808
        println("\n--- Xrefs to global pointer 0x" + Long.toHexString(GLOBAL_PTR) + " ---");
        Address globalAddr = toAddr(GLOBAL_PTR);
        if (globalAddr != null) {
            ReferenceIterator refs = refMgr.getReferencesTo(globalAddr);
            int count = 0;
            while (refs.hasNext()) {
                Reference ref = refs.next();
                Address fromAddr = ref.getFromAddress();
                Function caller = funcMgr.getFunctionContaining(fromAddr);
                if (caller != null) {
                    long addr = caller.getEntryPoint().getOffset();
                    if (addr != REG_FUNC) {
                        dispatchCandidates.add(addr);
                        println("  " + caller.getName() + " @ 0x" + Long.toHexString(addr) +
                                " ref type=" + ref.getReferenceType());
                        count++;
                    }
                }
            }
            println("  Total: " + count + " xrefs (excluding registration function)");
        }

        // Step 2: Look for functions that reference the table data region
        // by checking all addresses in the known table region (0x140c66xxx)
        println("\n--- Scanning table region for xrefs ---");
        for (long addr = TABLE_REGION_START; addr < TABLE_REGION_END; addr += 4) {
            Address dataAddr = toAddr(addr);
            if (dataAddr == null) continue;
            ReferenceIterator refs = refMgr.getReferencesTo(dataAddr);
            while (refs.hasNext()) {
                Reference ref = refs.next();
                Address fromAddr = ref.getFromAddress();
                Function caller = funcMgr.getFunctionContaining(fromAddr);
                if (caller != null) {
                    long cAddr = caller.getEntryPoint().getOffset();
                    if (cAddr != REG_FUNC && cAddr >= 0x140060000L) {
                        dispatchCandidates.add(cAddr);
                    }
                }
            }
        }

        // Step 3: Search for the registration function's callers
        println("\n--- Callers of registration function ---");
        Address regAddr = toAddr(REG_FUNC);
        if (regAddr != null) {
            ReferenceIterator refs = refMgr.getReferencesTo(regAddr);
            while (refs.hasNext()) {
                Reference ref = refs.next();
                Address fromAddr = ref.getFromAddress();
                Function caller = funcMgr.getFunctionContaining(fromAddr);
                if (caller != null) {
                    long addr = caller.getEntryPoint().getOffset();
                    if (!dispatchCandidates.contains(addr)) {
                        dispatchCandidates.add(addr);
                        println("  " + caller.getName() + " @ 0x" + Long.toHexString(addr));
                    }
                }
            }
        }

        // Step 4: Look at what's NEAR the registration function in the call graph
        // The dispatch might be in the same code section (0x14006xxxx-0x1400Axxxx)
        println("\n--- Functions near registration function with many callees ---");
        Function regFunc = funcMgr.getFunctionAt(toAddr(REG_FUNC));
        if (regFunc != null) {
            // Check functions in the same address range
            for (long addr = 0x140060000L; addr < 0x1400A0000L; addr += 0x10) {
                Function f = funcMgr.getFunctionContaining(toAddr(addr));
                if (f != null && f.getEntryPoint().getOffset() != REG_FUNC) {
                    int calleeCount = 0;
                    InstructionIterator insts = listing.getInstructions(f.getBody(), true);
                    while (insts.hasNext()) {
                        Instruction inst = insts.next();
                        if (inst.getMnemonicString().equals("CALL")) {
                            calleeCount++;
                        }
                    }
                    // Dispatch functions call MANY different readers
                    if (calleeCount > 20) {
                        println("  Candidate: " + f.getName() +
                                " @ 0x" + Long.toHexString(f.getEntryPoint().getOffset()) +
                                " (" + calleeCount + " CALL instructions)");
                        dispatchCandidates.add(f.getEntryPoint().getOffset());
                    }
                    addr = f.getBody().getMaxAddress().getOffset();
                }
            }
        }

        // Output
        println("\n=== Found " + dispatchCandidates.size() + " dispatch candidates ===");
        for (Long addr : dispatchCandidates) {
            Function f = funcMgr.getFunctionAt(toAddr(addr));
            String name = f != null ? f.getName() : "0x" + Long.toHexString(addr);
            println("  0x" + Long.toHexString(addr) + " " + name);

            // Label as consumer dispatch
            if (f != null && !name.startsWith("ConsumerDispatch")) {
                try {
                    f.setName("ConsumerDispatch_" + name.substring(0, Math.min(20, name.length())),
                             SourceType.USER_DEFINED);
                } catch (Exception e) {}
            }

            csvLines.add("WildStar64.exe," + Long.toHexString(addr) + "," +
                name + ",Consumer dispatch candidate — may route server→client packets.");
        }

        if (csvPath != null && !csvLines.isEmpty()) {
            try (PrintWriter pw = new PrintWriter(new FileWriter(csvPath, true))) {
                for (String line : csvLines) {
                    pw.println(line);
                }
            }
            println("Appended " + csvLines.size() + " lines to " + csvPath);
        }

        println("=== FindDispatch complete ===");
    }
}
