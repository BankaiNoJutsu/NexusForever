// Analyze the Network_RegisterServerOpcode_0351 packet registration table and
// create artificial cross-references from the dispatch table entries to their
// reader functions. This enables Ghidra's selection algorithm to find consumer
// dispatch functions for server-to-client opcodes that use indirect calls.
//
// The registration table at Network_RegisterServerOpcode_0351 (0x14006c290)
// stores entries of the form:
//   (table_ptr, OPCODE, SIZE, CLIENT_WRITER, SERVER_READER, FLAG1, FLAG2)
//
// For server-to-client opcodes, SERVER_READER is the reader function address.
// The dispatch function loads these via indirect call through function pointers.
// This script extracts reader addresses and writes candidate labels for review.
//
// Usage from analyzeHeadless:
//   -postScript AnalyzeRegistrationTable.java <candidate-output-csv-path>
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.*;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryAccessException;
import ghidra.program.model.pcode.PcodeOp;
import ghidra.program.model.pcode.PcodeOpAST;
import ghidra.program.model.lang.Register;
import ghidra.util.exception.CancelledException;

public class AnalyzeRegistrationTable extends GhidraScript {

    // The registration function that stores all opcode-to-reader mappings
    private static final String REG_FUNC_LABEL = "Network_RegisterServerOpcode_0351";
    private static final String REG_FUNC_ADDR = "14006c290";

    // Entity aux and other server-to-client opcodes we want callers for
    private static final String[] TARGET_READERS = {
        "140095c20", "140095ce0", "140095a80", "1400959c0", "140095b40",
        "140097620", "140097ee0", "140097690", "140097f70", "1400980f0",
        // Also include housing and other blocked readers
        "14009e3c0", // HousingNeighborhoodEntry
        "14009ebf0", // HousingNeighborhoodList
        "14008bf80", // ServerRaidQueueStatus
    };

    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        String outputPath = args.length > 0 ? args[0] : null;

        FunctionManager funcMgr = currentProgram.getFunctionManager();
        ReferenceManager refMgr = currentProgram.getReferenceManager();
        Listing listing = currentProgram.getListing();

        // Find the registration function
        Function regFunc = findRegistrationFunction(funcMgr);
        if (regFunc == null) {
            printerr("Could not find " + REG_FUNC_LABEL + " at " + REG_FUNC_ADDR);
            return;
        }

        println("Found registration function: " + regFunc.getName() + " at " + regFunc.getEntryPoint());

        // Extract all reader addresses mentioned in the registration function
        Set<String> readerAddresses = extractReaderAddresses(regFunc, listing);
        println("Extracted " + readerAddresses.size() + " reader addresses from registration table");

        // Now search for functions that reference these addresses via ANY means
        // (direct calls, indirect calls, data references)
        Set<String> consumerAddresses = new LinkedHashSet<>();

        for (String readerAddr : readerAddresses) {
            Address target = toAddr(readerAddr);
            if (target == null) continue;

            // Get ALL references to this address (including data references)
            ReferenceIterator refs = refMgr.getReferencesTo(target);
            int refCount = 0;
            while (refs.hasNext()) {
                Reference ref = refs.next();
                Address fromAddr = ref.getFromAddress();
                Function caller = funcMgr.getFunctionContaining(fromAddr);
                if (caller != null) {
                    String callerAddr = caller.getEntryPoint().toString();
                    if (!callerAddr.equals(readerAddr)) {
                        consumerAddresses.add(callerAddr);
                        refCount++;
                    }
                }
            }

            if (refCount > 0) {
                println("Reader " + readerAddr + " has " + refCount + " callers");
            } else {
                // No direct references - try data references
                Data data = listing.getDataAt(target);
                if (data != null) {
                    ReferenceIterator dataRefs = refMgr.getReferencesTo(target);
                    while (dataRefs.hasNext()) {
                        Reference ref = dataRefs.next();
                        println("  Data ref to " + readerAddr + " from " + ref.getFromAddress() +
                                " type=" + ref.getReferenceType());
                    }
                }
            }
        }

        println("\n=== Found " + consumerAddresses.size() + " unique consumer addresses ===");

        // Output candidate labels for manual review before durable import.
        if (outputPath != null && !consumerAddresses.isEmpty()) {
            try (PrintWriter pw = new PrintWriter(new FileWriter(outputPath, true))) {
                for (String addr : consumerAddresses) {
                    Function f = funcMgr.getFunctionAt(toAddr(addr));
                    String name = f != null ? f.getName() : addr;
                    pw.println("WildStar64.exe," + addr + ",EntityAuxConsumer_" + name.substring(0, Math.min(30, name.length())) +
                        ",Candidate consumer of entity aux / blocked server-to-client opcodes; review before adding durable label.");
                }
            }
            println("Appended " + consumerAddresses.size() + " candidate labels to " + outputPath);
        } else if (consumerAddresses.isEmpty()) {
            println("WARNING: No consumer callers found. The registration table uses indirect calls that Ghidra cannot track.");
            println("Manual Ghidra GUI work required: open " + REG_FUNC_LABEL +
                    ", trace the function pointer calls through the dispatch infrastructure.");
        }
    }

    private Function findRegistrationFunction(FunctionManager funcMgr) {
        // Try by label first
        for (Function f : funcMgr.getFunctions(true)) {
            if (f.getName().equals(REG_FUNC_LABEL)) {
                return f;
            }
        }
        // Try by address
        Address addr = toAddr(REG_FUNC_ADDR);
        if (addr != null) {
            return funcMgr.getFunctionAt(addr);
        }
        return null;
    }

    private Set<String> extractReaderAddresses(Function regFunc, Listing listing)
            throws CancelledException {
        Set<String> addresses = new LinkedHashSet<>();

        // Walk the instructions of the registration function looking for
        // function pointer references. The pattern is:
        //   (**(code **)*puVar1)(puVar1, OPCODE, SIZE, 0, 0, FUN_XXXXXXXX, 0);
        // The reader is the 6th argument (index 5 from 0).

        InstructionIterator instructions = listing.getInstructions(regFunc.getBody(), true);
        while (instructions.hasNext()) {
            Instruction inst = instructions.next();
            // Look for CALL instructions with function pointer operands
            // or PUSH/MOV instructions that load reader addresses
            String mnemonic = inst.getMnemonicString();
            if (mnemonic.equals("CALL") || mnemonic.equals("LEA") || mnemonic.equals("MOV")) {
                for (int i = 0; i < inst.getNumOperands(); i++) {
                    for (Object opObj : inst.getOpObjects(i)) {
                        if (opObj instanceof Address) {
                            Address addr = (Address) opObj;
                            long offset = addr.getOffset();
                            // Reader functions are in the 0x14007xxxx-0x1400Bxxxx range
                            // (the packet reader code section)
                            if (offset >= 0x140070000L && offset <= 0x140100000L) {
                                addresses.add(addr.toString());
                            }
                        }
                    }
                }
            }
        }

        return addresses;
    }
}
