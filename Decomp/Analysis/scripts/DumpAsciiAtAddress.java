// Dump raw bytes and printable ASCII starting at an address. This helps recover
// short inline strings that Ghidra has not auto-defined in the listing.
//
// Usage from analyzeHeadless:
//   -postScript DumpAsciiAtAddress.java <address> [max-bytes]
//
// Examples:
//   -postScript DumpAsciiAtAddress.java 140b2f000
//   -postScript DumpAsciiAtAddress.java 140b2f000 16
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.mem.Memory;
import ghidra.program.model.mem.MemoryAccessException;

public class DumpAsciiAtAddress extends GhidraScript {
    @Override
    public void run() throws Exception {
        if (currentProgram == null) {
            printerr("No current program.");
            return;
        }

        String[] args = getScriptArgs();
        if (args.length == 0) {
            printerr("Missing address.");
            return;
        }

        Address start = toAddr(stripHexPrefix(args[0]));
        if (start == null) {
            printerr("Could not resolve address: " + args[0]);
            return;
        }

        int maxBytes = args.length > 1 ? Integer.parseInt(args[1]) : 32;
        if (maxBytes <= 0) {
            printerr("max-bytes must be positive.");
            return;
        }

        Memory memory = currentProgram.getMemory();
        StringBuilder hexBuilder = new StringBuilder();
        StringBuilder asciiBuilder = new StringBuilder();

        println("target=" + start + " maxBytes=" + maxBytes);
        for (int i = 0; i < maxBytes; i++) {
            Address current = start.add(i);
            byte value;
            try {
                value = memory.getByte(current);
            }
            catch (MemoryAccessException ex) {
                println("read stopped at " + current + ": " + ex.getMessage());
                break;
            }

            if (hexBuilder.length() > 0) {
                hexBuilder.append(' ');
            }
            hexBuilder.append(String.format("%02x", value & 0xff));

            if (value == 0) {
                asciiBuilder.append("\\0");
                println("bytes=" + hexBuilder);
                println("ascii=" + asciiBuilder);
                return;
            }

            if (value >= 0x20 && value <= 0x7e) {
                asciiBuilder.append((char) value);
            }
            else {
                asciiBuilder.append('.');
            }
        }

        println("bytes=" + hexBuilder);
        println("ascii=" + asciiBuilder);
    }

    private String stripHexPrefix(String input) {
        return input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
    }
}