import struct, re, glob, collections, sys
sys.stdout.reconfigure(encoding='utf-8')
src = ''
for f in glob.glob(r'I:\GIT\NexusForever-unified-20261106\Source\NexusForever.Game\**\*.cs', recursive=True):
    src += open(f, encoding='utf-8-sig', errors='ignore').read()
handled_names = set(re.findall(r'\[PrerequisiteCheck\(PrerequisiteType\.(\w+)\)\]', src))
enum = open(r'I:\GIT\NexusForever-unified-20261106\Source\NexusForever.Game.Static\Prerequisite\PrerequisiteType.cs', encoding='utf-8-sig').read()
name_to_num = {m.group(1): int(m.group(2)) for m in re.finditer(r'(?m)^\s+(\w+)\s*=\s*(\d+),', enum)}
num_to_names = collections.defaultdict(list)
for k, v in name_to_num.items():
    num_to_names[v].append(k)
handled = {name_to_num[n] for n in handled_names if n in name_to_num}
missing_names = [n for n in handled_names if n not in name_to_num]
d = open(r'I:\tools\extract\DB\Prerequisite.tbl', 'rb').read()
rs = struct.unpack_from('<Q', d, 24)[0]; n = struct.unpack_from('<Q', d, 48)[0]; ro = struct.unpack_from('<Q', d, 64)[0]
b = 0x60 + ro
slots = collections.Counter(); comps = collections.defaultdict(collections.Counter); rows = collections.defaultdict(set)
for i in range(n):
    r = struct.unpack_from('<15I', d, b + i * rs)
    for k in range(3):
        t = r[2 + k]
        if t:
            slots[t] += 1; comps[t][r[5 + k]] += 1; rows[t].add(r[0])
unh = sorted(((slots[t], t) for t in slots if t not in handled), reverse=True)
tot = sum(slots.values())
print('handler registrations', len(handled_names), 'distinct numbers', len(handled), 'unresolved names', missing_names)
print('types in use', len(slots), 'slots', tot, '| unhandled types', len(unh), 'slots', sum(s for s, _ in unh), '%.2f%%' % (100 * sum(s for s, _ in unh) / tot))
for s, t in unh:
    print('%3d %-40s slots=%-5d prereq rows=%-5d comparisons=%s' % (t, '/'.join(num_to_names.get(t, ['?'])), s, len(rows[t]), dict(comps[t])))
