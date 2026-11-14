"""Map every prerequisite type to the native code that evaluates it, and audit the server's citations.

Input: Ghidra C output of PrerequisiteManager_EvaluateTypeSlot (WildStar64.exe 1404a2100), produced with

    run_ghidra_analysis.ps1 -ExportOnly -SkipDefaultExport -SkipCoverage -Targets WildStar64.exe `
        -ExtraPostScript Gap8Decomp.java -ExtraPostScriptArgs @(<evaluator.txt>, '1404a2100')

plus exports/WildStar64.exe/function_pointer_family_slots.csv and functions.csv from the normal export.

Usage:
    python prerequisite_evaluator_case_map.py <evaluator.txt> [--slots <tsv>] [--spell-refs <tsv>]

    --slots       TSV "type<TAB>slotCount" for Prerequisite.tbl (all three type columns)
    --spell-refs  TSV "type<TAB>slotWeightedReferences<TAB>distinctReferencedPrerequisites" for Spell4 /
                  Spell4Effects prerequisite columns. Count each matching type slot in a referenced prerequisite;
                  a table-row/column referencing a row with two matching slots contributes two, not one.
                  The output's historical spellReferences column retains this weighted meaning. Type 102's
                  352 weighted / 302 direct reference census is recorded in PREREQUISITE_102_PRIMARY_SUBJECT_20261114.md.

Outputs (under Decomp/Analysis/coverage):
    prerequisite_evaluator_case_map.csv      one row per evaluator case
    prerequisite_native_handler_audit.csv    one row per server citation or handled type, with the native handler

The evaluator calls most types through the manager vtable. The vtable base 140b67740 is not derived here; it is
validated by the cases whose body matches the type's client error text (see
PREREQUISITE_HANDLER_TABLE_AUDIT_20261110.md). This script parses decompiler text, so joined or fall-through cases
are reported as "joined" or "inline" without a handler rather than guessed.
"""
import argparse
import csv
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ANALYSIS = os.path.dirname(HERE)
ROOT = os.path.dirname(os.path.dirname(ANALYSIS))
EXPORT = os.path.join(ANALYSIS, 'exports', 'WildStar64.exe')
VTABLE = 0x140b67740
STUB = '140001ba0'


def read_tsv(path, columns):
    result = {}
    if not path:
        return result
    for line in open(path, encoding='utf-8'):
        parts = line.rstrip('\n').split('\t')
        if len(parts) < columns or not parts[0].isdigit():
            continue
        result[int(parts[0])] = [int(p) for p in parts[1:columns]]
    return result


def parse_cases(path):
    lines = open(path, encoding='utf-8', errors='replace').read().splitlines()
    bodies = {}
    current, buffer = [], []

    def flush():
        for case in current:
            bodies[case] = list(buffer)
        current.clear()
        buffer.clear()

    for line in lines:
        match = re.match(r'\s*case (0x[0-9a-f]+|\d+):', line)
        if match:
            if buffer:
                flush()
            current.append(int(match.group(1), 0))
            continue
        if re.match(r'\s*default:', line):
            flush()
            continue
        if current:
            buffer.append(line)
    flush()
    return bodies


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('evaluator')
    parser.add_argument('--slots')
    parser.add_argument('--spell-refs')
    args = parser.parse_args()

    slot_function = {}
    with open(os.path.join(EXPORT, 'function_pointer_family_slots.csv'), encoding='utf-8') as handle:
        for row in csv.DictReader(handle):
            slot_function[int(row['slot_address'], 16)] = (row['function_entry'], row['function_name'])
    function_entry = {}
    with open(os.path.join(EXPORT, 'functions.csv'), encoding='utf-8') as handle:
        for row in csv.DictReader(handle):
            function_entry[row['name']] = row['entry']

    cases = []
    for case, body in sorted(parse_cases(args.evaluator).items()):
        text = ' '.join(line.strip() for line in body)
        # the decompiler prints some offsets in decimal (for example '+ 400' for +0x190)
        offsets = [int(o, 0) for o in re.findall(r'\*param_1 \+ (0x[0-9a-f]+|\d+)\)\)', text)]
        callees = [name for name in re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*\(', text)
                   if name in function_entry and name != 'PrerequisiteManager_ApplyComparison']
        words = '/'.join(sorted(set(re.findall(r'param_3\[(\d)\]', text))))
        if offsets and len(body) <= 4:
            kind = 'vtable'
            entry, name = slot_function.get(VTABLE + offsets[0], ('', ''))
            offset = '+%#x' % offsets[0]
            note = 'shared stub' if entry == STUB else ''
        elif 'goto' in text and len(body) <= 3 and not callees and not offsets:
            kind, entry, name, offset, note = 'joined', '', '', '', text[:80]
        else:
            kind = 'inline'
            name = callees[0] if callees else ''
            entry = function_entry.get(name, '')
            offset = ''
            note = ('also vtable ' + ','.join('+%#x' % o for o in offsets)) if offsets else ''
        cases.append({'type': case, 'case': '%#x' % case, 'kind': kind, 'vtableOffset': offset, 'functionEntry': entry,
                      'functionName': name, 'slotWordsRead': words, 'bodyLines': len(body), 'note': note})

    coverage = os.path.join(ANALYSIS, 'coverage')
    with open(os.path.join(coverage, 'prerequisite_evaluator_case_map.csv'), 'w', newline='', encoding='utf-8') as handle:
        writer = csv.DictWriter(handle, fieldnames=list(cases[0].keys()), quoting=csv.QUOTE_ALL)
        writer.writeheader()
        writer.writerows(cases)

    by_type = {row['type']: row for row in cases}
    types_by_entry = {}
    for row in cases:
        if row['kind'] == 'vtable' and row['functionEntry']:
            types_by_entry.setdefault(row['functionEntry'], []).append(row['type'])

    # server enum names and handler classes
    enum_names = {}
    enum_path = os.path.join(ROOT, 'Source', 'NexusForever.Game.Static', 'Prerequisite', 'PrerequisiteType.cs')
    for line in open(enum_path, encoding='utf-8-sig'):
        match = re.match(r'\s*([A-Za-z0-9_]+)\s*=\s*(\d+)\s*,?', line)
        if match:
            enum_names.setdefault(int(match.group(2)), match.group(1))
    handlers = {}
    handler_source = {}
    check_dir = os.path.join(ROOT, 'Source', 'NexusForever.Game', 'Prerequisite', 'Check')
    name_to_type = {name: number for number, name in enum_names.items()}
    for file_name in sorted(os.listdir(check_dir)):
        if not file_name.endswith('.cs'):
            continue
        source = open(os.path.join(check_dir, file_name), encoding='utf-8-sig', errors='replace').read()
        for name in re.findall(r'PrerequisiteCheck\(PrerequisiteType\.([A-Za-z0-9_]+)\)', source):
            if name in name_to_type:
                handlers.setdefault(name_to_type[name], []).append(file_name)
                handler_source[name_to_type[name]] = handler_source.get(name_to_type[name], '') + source

    # citations of the form "handler table[N] ... 14xxxxxxx" / "type N: handler table <c>14xxxxxxx</c>"
    citations = {}
    patterns = [re.compile(r'handler table\[(\d+)\][^0-9a-fA-F]{0,60}?(?:[A-Za-z_0-9]+ )?\b(14[0-9a-f]{7})\b'),
                re.compile(r'type (\d+): handler table <c>(14[0-9a-f]{7})</c>')]
    for directory, _, files in os.walk(os.path.join(ROOT, 'Source')):
        if os.sep + 'obj' in directory or os.sep + 'bin' in directory:
            continue
        for file_name in files:
            if not file_name.endswith('.cs'):
                continue
            path = os.path.join(directory, file_name)
            try:
                source = open(path, encoding='utf-8-sig', errors='replace').read()
            except OSError:
                continue
            if 'handler table' not in source:
                continue
            for number, line in enumerate(source.splitlines(), 1):
                for pattern in patterns:
                    for match in pattern.finditer(line):
                        citations.setdefault(int(match.group(1)), set()).add(
                            (match.group(2), os.path.relpath(path, ROOT).replace(os.sep, '/') + ':%d' % number))

    slots = read_tsv(args.slots, 2)
    spell_refs = read_tsv(args.spell_refs, 3)

    audit = []
    for number in sorted(set(by_type) | set(citations) | set(handlers) | set(slots)):
        native = by_type.get(number)
        cited = sorted(citations.get(number, []))
        cited_addresses = sorted({address for address, _ in cited})
        native_entry = native['functionEntry'] if native else ''
        if not cited_addresses:
            status = 'no citation'
        elif native is not None and all(address == native_entry for address in cited_addresses):
            status = 'citation matches'
        else:
            status = 'citation contradicted'
        # does the check class itself name the native evidence it was written from (entry, vtable offset or case)?
        source = handler_source.get(number, '')
        evidence = []
        if native is not None and source:
            if native_entry and native_entry != STUB and native_entry in source:
                evidence.append('entry')
            if native['vtableOffset'] and re.search(r'\+0x0*%x' % int(native['vtableOffset'], 16), source):
                evidence.append('offset')
            if re.search(r'case\W{0,6}0x0*%x' % number, source):
                evidence.append('case')
        audit.append({
            'type': number,
            'enumName': enum_names.get(number, ''),
            'retailSlots': slots.get(number, [0])[0],
            'spellReferences': spell_refs.get(number, [0, 0])[0],
            'serverHandler': ' '.join(handlers.get(number, [])),
            'handlerCitesNative': '/'.join(evidence),
            'nativeKind': native['kind'] if native else 'no case',
            'nativeVtableOffset': native['vtableOffset'] if native else '',
            'nativeFunctionEntry': native_entry,
            'nativeFunctionName': native['functionName'] if native else '',
            'nativeSlotWordsRead': native['slotWordsRead'] if native else '',
            'citedAddresses': ' '.join(cited_addresses),
            'citedAddressReallyServesTypes': ' '.join(
                '%s->%s' % (address, ','.join(str(t) for t in types_by_entry.get(address, [])) or 'none')
                for address in cited_addresses),
            'citationStatus': status,
            'citedIn': ' '.join(location for _, location in cited),
        })
    with open(os.path.join(coverage, 'prerequisite_native_handler_audit.csv'), 'w', newline='', encoding='utf-8') as handle:
        writer = csv.DictWriter(handle, fieldnames=list(audit[0].keys()), quoting=csv.QUOTE_ALL)
        writer.writeheader()
        writer.writerows(audit)

    contradicted = [row for row in audit if row['citationStatus'] == 'citation contradicted']
    print('cases=%d vtable=%d inline=%d joined=%d stub=%d' % (
        len(cases), sum(r['kind'] == 'vtable' for r in cases), sum(r['kind'] == 'inline' for r in cases),
        sum(r['kind'] == 'joined' for r in cases), sum(r['functionEntry'] == STUB for r in cases)))
    print('types with citations=%d contradicted=%d (with a server handler: %d)' % (
        sum(bool(r['citedAddresses']) for r in audit), len(contradicted), sum(bool(r['serverHandler']) for r in contradicted)))
    print('contradicted types by spell references:')
    for row in sorted(contradicted, key=lambda r: (-r['spellReferences'], -r['retailSlots']))[:25]:
        print('  %3d %-32s spellRefs=%-5d slots=%-5d handler=%-45s native=%s %s %s' % (
            row['type'], row['enumName'], row['spellReferences'], row['retailSlots'], row['serverHandler'],
            row['nativeKind'], row['nativeVtableOffset'], row['nativeFunctionEntry']))


if __name__ == '__main__':
    main()
