import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.scalar.*;
import ghidra.program.model.lang.*;

// DumpFunctionImmediates.java - dumps all immediate values seen as operands within a function
public class DumpFunctionImmediates extends ghidra.app.script.GhidraScript {
    @Override
    public void run() throws Exception {
        String addrStr = getScriptArgs()[0];
        Address startAddr = toAddr(Long.parseUnsignedLong(addrStr, 16));
        Function fn = getFunctionContaining(startAddr);
        if (fn == null) {
            println("No function at " + addrStr);
            return;
        }
        println("function=" + fn.getName() + "@" + fn.getEntryPoint());
        println("function_body_start=" + fn.getBody().getMinAddress());
        println("function_body_end=" + fn.getBody().getMaxAddress());
        Listing listing = currentProgram.getListing();
        InstructionIterator iter = listing.getInstructions(fn.getBody(), true);
        while (iter.hasNext()) {
            Instruction instr = iter.next();
            String mnemonic = instr.getMnemonicString();
            for (int i = 0; i < instr.getNumOperands(); i++) {
                Scalar scalar = instr.getScalar(i);
                if (scalar != null) {
                    long val = scalar.getValue();
                    // Print any value in relevant ranges
                    if ((val >= 0x7C0 && val <= 0x7D0) || (val >= 0xBD00 && val <= 0xBE00) || (val >= 0x3D0 && val <= 0x3F0)) {
                        println("instr=" + instr.getAddress() + " " + mnemonic + " operand[" + i + "]=" + Long.toHexString(val));
                    }
                }
            }
        }
        println("scan_complete");
    }
}
