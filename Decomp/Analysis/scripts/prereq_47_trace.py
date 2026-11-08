import re, glob, collections, sys
sys.stdout.reconfigure(encoding='utf-8')
ids = {}
for l in open(r'I:\tools\prereq_refs_detail.tsv', encoding='utf-8'):
    t, tab, col, eid, pid = l.rstrip('\n').split('\t')
    if t == '47' and tab == 'Spell4' and col in ('PrerequisiteIdCasterCast', 'PrerequisiteIdTargetCast'):
        ids[int(eid)] = col
print('spells with a type 47 cast gate', len(ids))
hits = collections.defaultdict(list)
for f in glob.glob(r'I:\GIT\NexusForever-unified-20261106\Source\**\*.cs', recursive=True):
    if any(x in f for x in ('\\obj\\', '\\bin\\', 'Tests', 'GameTable\\Model')):
        continue
    src = open(f, encoding='utf-8-sig', errors='ignore').read()
    for m in re.finditer(r'(?<![0-9A-Za-z_.])(\d{3,6})u?(?![0-9A-Za-z_.])', src):
        v = int(m.group(1))
        if v in ids:
            line = src.count('\n', 0, m.start()) + 1
            hits[v].append((f.split('Source\\')[1], line))
print('referenced by numeric literal in non-test source:', len(hits))
for v, loc in sorted(hits.items())[:40]:
    print(' ', v, ids[v], loc[:3])
