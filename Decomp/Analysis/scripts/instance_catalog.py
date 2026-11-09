import csv, re, glob, collections, os, sys
sys.stdout.reconfigure(encoding='utf-8')
root = r'I:\GIT\NexusForever-unified-20261106'
# objective id -> (instance folder, name) from the repo enums
obj = {}
for p in glob.glob(root + r'\Source\NexusForever.Script.Instance\**\PublicEventObjective.cs', recursive=True):
    rel = os.path.relpath(p, root + r'\Source\NexusForever.Script.Instance').split(os.sep)
    inst = '/'.join(rel[:-1])
    for m in re.finditer(r'^\s*(\w+)\s*=\s*(\d+)', open(p, encoding='utf-8-sig').read(), re.M):
        obj.setdefault(int(m.group(2)), []).append((inst, m.group(1)))

rows = list(csv.DictReader(open(root + r'\Decomp\Analysis\coverage\spell4_internal_tags_20261109.csv', encoding='utf-8')))
fam = collections.defaultdict(lambda: collections.defaultdict(list))
for r in rows:
    if not r['encounter'] or r['encounter'][0] != 'e':
        continue
    t = r['tag']
    if t in ('[DEPRECATED]', '[DEPRECATE]', '[DEP]', '[TEST]', '[DELETE]', '[OLD][DELETE]', '[UNUSED]', '[BETA]', '[PROTOTYPE]'):
        continue
    fam[t][int(r['encounter'][1:])].append(r)


def ability(desc):
    d = re.sub(r'^(\[[^\]]+\])+\s*e\d+\s*-\s*', '', desc)
    d = re.sub(r'\s*-\s*Tier\s*\d+\s*$', '', d)
    return d.strip()


out = ['# Boss ability catalog from Spell4 internal tags (2026-11-09)', '',
       'Generated from `coverage/spell4_internal_tags_20261109.csv` by `scripts/instance_catalog.py`. Evidence: Mapped-level input from client table descriptions (developer naming), see [INSTANCE_SPELL4_INTERNAL_TAGS_20261109.md](INSTANCE_SPELL4_INTERNAL_TAGS_20261109.md). `eNNN` is joined to the repo `PublicEventObjective` enum by number: a match is a number coincidence checked only for `[STL]` and `[KV]`; unmatched numbers are listed without a name. Each line is a distinct developer ability name with its Spell4 ids (tiers merged) and the tier-1 CastTime in ms when non-zero. This is an inventory, not a cast order or interval.', '']
interesting = ['[STL]', '[KV]', '[SK]', '[CBC]', '[GA]', '[DS]', '[RMT]', '[IC]', '[UP]', '[UPJ]', '[TS]']
for t in interesting:
    if t not in fam:
        continue
    out.append(f'## {t}')
    for e in sorted(fam[t]):
        names = obj.get(e, [])
        label = ', '.join(f'{i}: {n}' for i, n in names) or 'no matching objective enum name'
        out.append(f'### e{e} ({label}), {len(fam[t][e])} rows')
        groups = collections.OrderedDict()
        for r in sorted(fam[t][e], key=lambda r: int(r['spell4_id'])):
            groups.setdefault(ability(r['description']), []).append(r)
        for a, rs in groups.items():
            ids = ' '.join(r['spell4_id'] for r in rs[:6]) + (' ...' if len(rs) > 6 else '')
            cast = sorted({int(r['cast_time_ms']) for r in rs if int(r['cast_time_ms'])})
            out.append(f'* {a} ({len(rs)}): {ids}' + (f'; castMs {cast[:4]}' if cast else ''))
        out.append('')
open(root + r'\Decomp\Analysis\INSTANCE_BOSS_ABILITY_CATALOG_20261109.md', 'w', encoding='utf-8').write('\n'.join(out) + '\n')
print(len(out), 'lines')
