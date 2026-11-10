"""Compare how the server reads Spell4Effects DataBits words with the client's SpellEffectType.tbl data typing.

SpellEffectType.tbl has one row per effect type: Id, Flags, DataType00..09. Observed codes: 0 = float, 1 = integer,
2 = raw / untyped (ids, flags, or unused), 3 = seen only on UnitPropertyModifier words 02..04. The client validator
(Spell4Effects_ValidateRow 140567af0) treats the code 0 and 1 slots as float and int; see
RESTORATION_GAP8_SPELL_EFFECT_NATIVE_READS_20261108.md section 0.

This script reads the typing rows from a TSV exported from the reference database and scans the server sources for
how each effect type reads each word (bit-cast to float or used as an integer):

    mysql ... wildstar_client -N -e "select * from spelleffecttype order by ID" > types.tsv
    mysql ... wildstar_client -N -e "select effectType, count(*), sum(dataBits00<>0), ..., sum(dataBits09<>0)
                                     from spell4effects group by effectType" > usage.tsv
    python spell_effect_data_type_audit.py types.tsv usage.tsv

Output: coverage/spell_effect_data_type_audit.csv, one row per (effect type, word) that the table types as float or
int, or that the server reads, or that retail rows populate. A "mismatch" is a word the table types as float that
the server reads as an integer, or the reverse. It is a review queue: the table says how a word is encoded, not what
it means.
"""
import csv
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ANALYSIS = os.path.dirname(HERE)
ROOT = os.path.dirname(os.path.dirname(ANALYSIS))
SPELL = os.path.join(ROOT, 'Source', 'NexusForever.Game', 'Spell')
CODE = {0: 'float', 1: 'int', 2: 'raw', 3: 'code3'}


def enum_names():
    names = {}
    path = os.path.join(ROOT, 'Source', 'NexusForever.Game.Static', 'Spell', 'SpellEffectType.cs')
    for line in open(path, encoding='utf-8-sig'):
        match = re.match(r'\s*([A-Za-z0-9_]+)\s*=\s*(0x[0-9A-Fa-f]+|\d+)', line)
        if match:
            names[match.group(1)] = int(match.group(2), 0)
    return names


def scan_block(text, reads):
    """Record every DataBitsNN read in a block as float (bit-cast) or int."""
    floats = set(int(n) for n in re.findall(r'UInt32BitsToSingle\(\s*(?:info\.)?(?:[Ee]ntry|effect|row)\.DataBits0(\d)', text))
    floats |= set(int(n) for n in re.findall(r'(?:ReadFloat|ToFloat|AsFloat|BitsToFloat)\(\s*(?:info\.)?(?:[Ee]ntry|effect|row)\.DataBits0(\d)', text))
    every = set(int(n) for n in re.findall(r'DataBits0(\d)', text))
    for word in every:
        kind = 'float' if word in floats else 'int'
        # a word read both ways in the same block is reported as both
        plain = len(re.findall(r'(?<!\()(?:info\.)?(?:[Ee]ntry|effect|row)\.DataBits0%d' % word, text))
        cast = len(re.findall(r'\(\s*(?:info\.)?(?:[Ee]ntry|effect|row)\.DataBits0%d\s*\)' % word, text))
        if word in floats and plain > cast:
            kind = 'float+int'
        reads.setdefault(word, set()).add(kind)


def server_reads(names):
    reads = {}

    # interpreter: switch (entry.EffectType) { case SpellEffectType.X: ... break; }
    source = open(os.path.join(SPELL, 'Effect', 'SpellEffectInterpretation.cs'), encoding='utf-8-sig').read()
    for match in re.finditer(r'((?:\s*case SpellEffectType\.[A-Za-z0-9_]+:)+)(.*?)\bbreak;', source, re.S):
        for name in re.findall(r'SpellEffectType\.([A-Za-z0-9_]+)', match.group(1)):
            if name in names:
                scan_block(match.group(2), reads.setdefault(names[name], {}))

    # handlers that read the entry directly
    for file_name in ('SpellEffectHandler.cs', 'SpellEffectHandler.Vitals.cs'):
        source = open(os.path.join(SPELL, file_name), encoding='utf-8-sig').read()
        parts = re.split(r'(\[SpellEffectHandler\(SpellEffectType\.[A-Za-z0-9_]+\)\])', source)
        index = 1
        while index < len(parts):
            attributes = [parts[index]]
            body = parts[index + 1]
            # consecutive attributes share one method
            while body.strip() == '' and index + 2 < len(parts):
                index += 2
                attributes.append(parts[index])
                body = parts[index + 1]
            # the method ends at the next handler attribute; trim at the next top-level method that is not a handler
            for attribute in attributes:
                name = re.search(r'SpellEffectType\.([A-Za-z0-9_]+)', attribute).group(1)
                if name in names:
                    scan_block(body, reads.setdefault(names[name], {}))
            index += 2
    return reads


def main():
    types_path, usage_path = sys.argv[1], sys.argv[2]
    names = enum_names()
    by_id = {value: name for name, value in names.items()}

    typing = {}
    for line in open(types_path, encoding='utf-8'):
        parts = line.rstrip('\n').split('\t')
        if len(parts) >= 12 and parts[0].isdigit():
            typing[int(parts[0])] = [int(p) for p in parts[2:12]]

    usage = {}
    for line in open(usage_path, encoding='utf-8'):
        parts = line.rstrip('\n').split('\t')
        if len(parts) >= 12 and parts[0].isdigit():
            usage[int(parts[0])] = [int(float(p)) for p in parts[1:12]]

    reads = server_reads(names)
    rows = []
    for effect in sorted(set(typing) | set(reads) | set(usage)):
        codes = typing.get(effect, [None] * 10)
        counts = usage.get(effect, [0] * 11)
        for word in range(10):
            code = codes[word]
            server = '/'.join(sorted(reads.get(effect, {}).get(word, [])))
            populated = counts[word + 1]
            if code in (None, 2) and not server and not populated:
                continue
            status = ''
            if code == 0 and server == 'int':
                status = 'mismatch: table float, server int'
            elif code == 1 and server == 'float':
                status = 'mismatch: table int, server float'
            elif code in (0, 1) and populated and not server:
                status = 'typed and populated, server does not read'
            elif code == 2 and server.startswith('float'):
                status = 'server float, table raw'
            elif not server and populated:
                status = 'populated, server does not read'
            rows.append({'effectType': effect, 'name': by_id.get(effect, ''), 'word': 'DataBits%02d' % word,
                         'tableType': CODE.get(code, '') if code is not None else 'no row', 'serverReads': server,
                         'retailRows': counts[0], 'retailNonZero': populated, 'status': status})

    out = os.path.join(ANALYSIS, 'coverage', 'spell_effect_data_type_audit.csv')
    with open(out, 'w', newline='', encoding='utf-8') as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()), quoting=csv.QUOTE_ALL)
        writer.writeheader()
        writer.writerows(rows)

    def show(title, predicate):
        chosen = [r for r in rows if predicate(r)]
        print('%s: %d' % (title, len(chosen)))
        for r in sorted(chosen, key=lambda r: -r['retailNonZero'])[:40]:
            print('  %3d %-28s %s table=%-6s server=%-9s rows=%-6d nonZero=%d' % (
                r['effectType'], r['name'], r['word'], r['tableType'], r['serverReads'], r['retailRows'], r['retailNonZero']))

    print('effect types with a typing row=%d, read by the server=%d' % (len(typing), len(reads)))
    show('MISMATCH (encoding differs)', lambda r: r['status'].startswith('mismatch'))
    show('typed float/int and populated but not read by the server', lambda r: r['status'] == 'typed and populated, server does not read')
    show('server reads as float where the table says raw', lambda r: r['status'] == 'server float, table raw')


if __name__ == '__main__':
    main()
