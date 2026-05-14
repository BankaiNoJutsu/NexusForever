// Dump nearby defined data entries and any function-pointer values around a
// target address. This is useful for callback/string registration tables where
// direct references only hit one cell inside a larger structure.
//
// Usage from analyzeHeadless:
//   -postScript DumpNearbyData.java <address> [slots]
//
// Examples:
//   -postScript DumpNearbyData.java 140b73550
//   -postScript DumpNearbyData.java 140b73550 12
//@category NexusForever

import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import ghidra.program.model.listing.Listing;
import ghidra.program.model.symbol.ReferenceIterator;
import ghidra.program.model.symbol.ReferenceManager;

public class DumpNearbyData extends GhidraScript {
    private Listing listing;
    private FunctionManager functionManager;
    private ReferenceManager referenceManager;

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

        Address target = toAddr(stripHexPrefix(args[0]));
        if (target == null) {
            printerr("Could not resolve address: " + args[0]);
            return;
        }

        int slots = args.length > 1 ? Integer.parseInt(args[1]) : 8;
        int pointerSize = currentProgram.getDefaultPointerSize();

        listing = currentProgram.getListing();
        functionManager = currentProgram.getFunctionManager();
        referenceManager = currentProgram.getReferenceManager();

        Data containing = listing.getDefinedDataContaining(target);
        println("target=" + target + " containing=" + describe(containing));

        for (int i = -slots; i <= slots; i++) {
            Address address;
            try {
                address = target.getAddressSpace().getAddress(target.getOffset() + (long) i * pointerSize);
            }
            catch (Exception ignored) {
                continue;
            }

            Data data = listing.getDefinedDataContaining(address);
            println(String.format("slot %+d %s -> %s", i, address, describe(data)));
            if (data != null && data.getMinAddress().equals(address) && data.getNumComponents() > 0) {
                for (int c = 0; c < data.getNumComponents(); c++) {
                    Data component = data.getComponent(c);
                    println("    component " + c + " " + component.getMinAddress() + " -> " + describe(component));
                }
            }
        }
    }

    private String describe(Data data) {
        if (data == null) {
            return "<no defined data>";
        }

        Object value = data.getValue();
        String valueText = value == null ? "null" : value.toString();
        if (valueText.length() > 120) {
            valueText = valueText.substring(0, 120) + "...";
        }

        String functionText = "";
        if (value instanceof Address) {
            Address addressValue = (Address) value;
            Function function = functionManager.getFunctionAt(addressValue);
            if (function == null) {
                function = functionManager.getFunctionContaining(addressValue);
            }
            if (function != null) {
                functionText = " function=" + function.getName() + "@" + function.getEntryPoint();
            }
        }

        int refCount = 0;
        ReferenceIterator iterator = referenceManager.getReferencesTo(data.getMinAddress());
        while (iterator.hasNext()) {
            iterator.next();
            refCount++;
        }

        return data.getMinAddress() + " type=" + data.getDataType().getDisplayName() +
            " refs=" + refCount + " value=" + valueText + functionText;
    }

    private String stripHexPrefix(String input) {
        return input.startsWith("0x") || input.startsWith("0X") ? input.substring(2) : input;
    }
}