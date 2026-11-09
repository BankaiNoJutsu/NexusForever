"""Compare Jabbithole creature sightings (coordinates.sql) with the runtime world `entity` rows of the provisional boss scripts.

Inputs (read-only): the Jabbithole dump (coordinates, creatures), the runtime world copy
`artifacts/character-list-hosted/database-copies/world.sql` (table `entity`), and the ScriptFilterCreatureId ids in
`*ProvisionalEncounterScripts.cs`. Jabbithole creature ids are not Creature2 ids, so each script class is joined to Jabbithole by boss name.
Coordinates share one space (verified where both exist: Tugga, X-89, Avatus, Thunderfoot, Blade-Wind agree within about 2 units).
Output: coverage/jabbithole_boss_coordinates_20261109.csv. Evidence tier: Correlated.
"""
import csv, glob, math, os, re, sys, collections, statistics
sys.stdout.reconfigure(encoding='utf-8')
DUMP = r'I:\GIT\NexusForever-assessment\jabbithole_mysql'
ROOT = r'I:\GIT\NexusForever-unified-20261106'
WORLD = ROOT + r'\artifacts\character-list-hosted\database-copies\world.sql'
OUT = ROOT + r'\Decomp\Analysis\coverage\jabbithole_boss_coordinates_20261109.csv'
CLASS_TO_NAMES = {
    'BladeWind': ['Blade-Wind the Invoker'], 'Aethros': ['Aethros'], 'Stormtalon': ['Stormtalon'],
    'Trogun': ['Forgemaster Trogun'], 'Grond': ['Grond the Corpsemaker'], 'Drokk': ['Slavemaster Drokk'],
    'Tugga': ['Stew-Shaman Tugga'], 'Thunderfoot': ['Thunderfoot'], 'Octog': ['Bosun Octog'], 'Mordechai': ['Mordechai Redmoon'],
    'Ondu': ['Ondu Lifeweaver'], 'Skash': ['Moldwood Overlord Skash'], 'Rayna': ['Rayna Darkspeaker'],
    'Selene': ['Spiritmother Selene the Corrupted'], 'Wrathbone': ['Wrathbone'], 'KuralakTheDefiler': ['Kuralak the Defiler'],
    'PhageMaw': ['Phage Maw'], 'ExperimentX89': ['Experiment X-89'], 'Avatus': ['Avatus'],
    'MaelstromAuthority': ['Maelstrom Authority'],
}


def split_values(s):
    vals = []; i = 0; n = len(s)
    while i < n:
        c = s[i]
        if c in ' ,':
            i += 1; continue
        if c == "'":
            j = i + 1; buf = []
            while j < n:
                if s[j] == '\\' and j + 1 < n:
                    buf.append(s[j + 1]); j += 2; continue
                if s[j] == "'":
                    if j + 1 < n and s[j + 1] == "'":
                        buf.append("'"); j += 2; continue
                    break
                buf.append(s[j]); j += 1
            vals.append(''.join(buf)); i = j + 1
        else:
            j = i
            while j < n and s[j] != ',':
                j += 1
            t = s[i:j].strip(); i = j
            vals.append(None if t == 'NULL' else t)
    return vals


jh = {}
rx = re.compile(r"^INSERT INTO `\w+` \((.*?)\) VALUES \((.*)\);\s*$")
for line in open(os.path.join(DUMP, 'creatures.sql'), encoding='utf-8', errors='replace'):
    m = rx.match(line) if line.startswith('INSERT') else None
    if not m:
        continue
    cols = [c.strip('` ') for c in m.group(1).split(',')]
    v = split_values(m.group(2))
    if len(v) == len(cols):
        r = dict(zip(cols, v)); jh[r['id']] = (r['name'], r['zone_id'], r['is_veteran'])
names = {n for ns in CLASS_TO_NAMES.values() for n in ns}
want = {i for i, (n, z, v) in jh.items() if n in names}
pts = collections.defaultdict(list)
crx = re.compile(r"\((\d+),(-?\d+),(-?\d+),(-?\d+),(\d+),'Creature',")
for line in open(os.path.join(DUMP, 'coordinates.sql'), encoding='utf-8', errors='replace'):
    if line.startswith('INSERT'):
        for m in crx.finditer(line):
            if m.group(5) in want:
                pts[m.group(5)].append((int(m.group(2)), int(m.group(3)), int(m.group(4))))

ids = {}
for f in glob.glob(ROOT + r'\Source\NexusForever.Script.Instance\**\*ProvisionalEncounterScripts.cs', recursive=True):
    s = open(f, encoding='utf-8-sig').read()
    for m in re.finditer(r'\[ScriptFilterCreatureId\(([^)]*)\)\]\s*public class (\w+?)ProvisionalEncounterScript', s):
        for n in re.findall(r'\d+', m.group(1)):
            ids[int(n)] = m.group(2)
ids = {k: v for k, v in ids.items() if v in CLASS_TO_NAMES}

ent = collections.defaultdict(list)
erx = re.compile(r'\((\d+),(\d+),(\d+),(\d+),(\d+),(-?[\d.e+-]+),(-?[\d.e+-]+),(-?[\d.e+-]+),')
for line in open(WORLD, encoding='utf-8', errors='replace'):
    if line.startswith('INSERT INTO `entity` '):
        for m in erx.finditer(line):
            if int(m.group(3)) in ids:
                ent[int(m.group(3))].append((int(m.group(1)), int(m.group(4)), int(m.group(5)), float(m.group(6)), float(m.group(7)), float(m.group(8))))


def med(p):
    return tuple(round(statistics.median(c[i] for c in p)) for i in range(3))


rows = []
for cid, cls in sorted(ids.items(), key=lambda kv: (kv[1], kv[0])):
    cands = [(j, jh[j]) for j in pts if jh[j][0] in CLASS_TO_NAMES[cls]]
    e = ent.get(cid, [])
    best = None
    for j, (n, z, vet) in cands:
        for er in e:
            d = min(math.dist(er[3:6], p) for p in pts[j])
            if best is None or d < best[0]:
                best = (d, j, z, vet, len(pts[j]))
    if e:
        er = e[0]
        rows.append([cls, cid, 'entity_row', er[0], er[1], er[2], er[3], er[4], er[5],
                     round(best[0], 1) if best else '', best[1] if best else '', best[2] if best else '', best[4] if best else ''])
    else:
        zs = sorted(cands, key=lambda c: -len(pts[c[0]]))[:2]
        for j, (n, z, vet) in zs:
            m = med(pts[j])
            rows.append([cls, cid, 'NO_entity_row', '', '', '', m[0], m[1], m[2], '', j, z, len(pts[j])])
with open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f)
    w.writerow(['script_class', 'creature2_id', 'status', 'entity_id', 'world', 'area', 'x_or_jabbithole_median_x', 'y', 'z',
                'nearest_jabbithole_sighting_distance', 'jabbithole_creature_id', 'jabbithole_zone', 'jabbithole_points'])
    w.writerows(rows)
for r in rows:
    print(r)
print(len(rows), 'rows ->', OUT)
