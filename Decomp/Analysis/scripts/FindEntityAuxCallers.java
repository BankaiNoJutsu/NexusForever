// Find all callers of the entity auxiliary reader functions and output their
// addresses for labeling. These callers are the consumer dispatch functions
// that process entity stat/combat/visibility packets.
//
// Usage from analyzeHeadless as an extra post-script:
//   -postScript ExportNexusForeverAnalysis.java ... \
//   -postScript FindEntityAuxCallers.java <output-csv-path>
//
// Outputs candidate CSV rows for review before appending to function_labels.csv.
//@category NexusForever

import java.io.FileWriter;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class FindEntityAuxCallers extends GhidraScript {

    // All 10 entity auxiliary server-to-client opcode reader addresses
    private static final String[] ENTITY_AUX_READERS = {
        "140095c20", // 0x025F reader
        "140095ce0", // 0x0260 reader
        "140095a80", // 0x0261 reader
        "1400959c0", // 0x0263 reader
        "140095b40", // 0x0264 reader
        "140097620", // 0x08F4 reader
        "140097ee0", // 0x0939 reader
        "140097690", // 0x093D reader
        "140097f70", // 0x093E reader
        "1400980f0", // 0x08CC reader (shared ServerUInt32WideString)
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

        Set<String> callerAddresses = new LinkedHashSet<>();

        for (String readerAddr : ENTITY_AUX_READERS) {
            Address target = toAddr(readerAddr);
            if (target == null) {
                println("WARN: Could not resolve " + readerAddr);
                continue;
            }

            ReferenceIterator refs = refMgr.getReferencesTo(target);
            int callerCount = 0;
            while (refs.hasNext()) {
                Reference ref = refs.next();
                Address fromAddr = ref.getFromAddress();
                Function caller = funcMgr.getFunctionContaining(fromAddr);
                if (caller != null) {
                    String callerAddrStr = caller.getEntryPoint().toString();
                    // Skip self-references (the reader function itself)
                    if (!callerAddrStr.equals(readerAddr)) {
                        callerAddresses.add(callerAddrStr);
                        callerCount++;
                    }
                } else {
                    // Caller is not in a known function - likely data or thunk
                    println("INFO: Reference to " + readerAddr + " from non-function " + fromAddr);
                }
            }
            println("Reader " + readerAddr + " has " + callerCount + " non-self callers.");
        }

        println("\n=== Found " + callerAddresses.size() + " unique caller addresses ===");

        // Print to stdout (captured in Ghidra log)
        for (String addr : callerAddresses) {
            Function f = funcMgr.getFunctionAt(toAddr(addr));
            String name = f != null ? f.getName() : "UNKNOWN";
            println(addr + " -> " + name);
        }

        // Write candidate CSV rows for manual review before durable import.
        if (outputPath != null) {
            try (PrintWriter pw = new PrintWriter(new FileWriter(outputPath, true))) {
                for (String addr : callerAddresses) {
                    pw.println("WildStar64.exe," + addr + ",EntityAuxConsumer_CallerOf_AuxReaders,"
                        + "Candidate caller of entity auxiliary opcode readers; review before adding durable label.");
                }
            }
            println("Appended " + callerAddresses.size() + " candidate labels to " + outputPath);
        }
    }
}
