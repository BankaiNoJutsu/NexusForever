import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.*;
import ghidra.program.model.data.*;
import ghidra.program.model.listing.*;
import ghidra.program.model.symbol.*;
import java.io.*;
import java.util.*;

public class SurveyLabels extends GhidraScript {
    @Override
    public void run() throws Exception {
        PrintWriter out = new PrintWriter(new FileWriter("I:/tools/survey_labels.txt"));
        FunctionManager fm = currentProgram.getFunctionManager();
        int total = 0, defaultNamed = 0, thunks = 0, userNamed = 0;
        for (Function f : fm.getFunctions(true)) {
            total++;
            if (f.isThunk()) thunks++;
            String n = f.getName();
            if (n.startsWith("FUN_")) defaultNamed++;
            else userNamed++;
        }
        out.println("functions=" + total + " defaultNamed=" + defaultNamed + " otherNamed=" + userNamed + " thunks=" + thunks);

        // strings: referenced by how many distinct functions
        Listing listing = currentProgram.getListing();
        ReferenceManager rm = currentProgram.getReferenceManager();
        int strings = 0, rtti = 0, uniqueStr = 0, funcsWithStr = 0;
        Map<Function, Integer> perFunc = new HashMap<>();
        List<String> rttiSamples = new ArrayList<>();
        List<String> uniqueSamples = new ArrayList<>();
        DataIterator it = listing.getDefinedData(true);
        while (it.hasNext() && !monitor.isCancelled()) {
            Data d = it.next();
            if (!d.hasStringValue()) continue;
            strings++;
            Object v = d.getValue();
            String s = v == null ? "" : v.toString();
            if (s.startsWith(".?A")) {
                rtti++;
                if (rttiSamples.size() < 12) rttiSamples.add(s);
            }
            Set<Function> users = new HashSet<>();
            for (Reference r : rm.getReferencesTo(d.getAddress())) {
                Function f = fm.getFunctionContaining(r.getFromAddress());
                if (f != null) users.add(f);
            }
            if (users.size() == 1 && s.length() >= 6) {
                uniqueStr++;
                Function f = users.iterator().next();
                perFunc.merge(f, 1, Integer::sum);
                if (uniqueSamples.size() < 25 && f.getName().startsWith("FUN_")) uniqueSamples.add(f.getEntryPoint() + " <- \"" + (s.length() > 60 ? s.substring(0, 60) : s) + "\"");
            }
        }
        for (Function f : perFunc.keySet()) if (f.getName().startsWith("FUN_")) funcsWithStr++;
        out.println("definedStrings=" + strings + " rttiTypeDescriptors=" + rtti + " uniquelyReferencedStrings(len>=6)=" + uniqueStr + " unnamedFunctionsWithAUniqueString=" + funcsWithStr);
        out.println("rtti samples: " + rttiSamples);
        for (String s : uniqueSamples) out.println("  " + s);

        // symbols that are not default
        SymbolTable st = currentProgram.getSymbolTable();
        int labelsNonDefault = 0, vtableLike = 0;
        SymbolIterator si = st.getAllSymbols(true);
        while (si.hasNext()) {
            Symbol s = si.next();
            if (s.getSource() == SourceType.DEFAULT) continue;
            labelsNonDefault++;
            if (s.getName().contains("vftable")) vtableLike++;
        }
        out.println("nonDefaultSymbols=" + labelsNonDefault + " vftableSymbols=" + vtableLike);
        out.close();
    }
}
