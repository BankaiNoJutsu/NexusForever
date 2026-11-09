"""Export Jabbithole creature_spells links for the instance bosses scripted as provisional encounters.

Reads the Jabbithole SQL dump (creatures, spells, creature_spells), joins creature -> spell by the dump's own ids, then checks each
spell's game_id against the 16042 Spell4 table (name must match) and prints the cast time from the 16042 table.
Output: Decomp/Analysis/coverage/jabbithole_boss_spell_links_20261109.csv (read-only on every input).
Evidence tier: Correlated. Jabbithole creature ids are NOT Creature2 ids, so bosses are joined by name only.
"""
import csv, os, re, struct, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
DUMP = r'I:\GIT\NexusForever-assessment\jabbithole_mysql'
OUT = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\coverage\jabbithole_boss_spell_links_20261109.csv'
BOSSES = ['Blade-Wind the Invoker', 'Aethros', 'Stormtalon', 'Forgemaster Trogun', 'Grond the Corpsemaker', 'Slavemaster Drokk',
          'Stew-Shaman Tugga', 'Thunderfoot', 'Bosun Octog', 'Mordechai Redmoon', 'Ondu Lifeweaver', 'Ondu - Plague Splatter',
          'Moldwood Overlord Skash', 'Rayna Darkspeaker', 'Spiritmother Selene the Corrupted', 'Wrathbone',
          'Kuralak the Defiler', 'Phage Maw', 'Experiment X-89', 'Avatus', 'Maelstrom Authority', 'Gloomclaw']


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


def load(name, want):
    rx = re.compile(r"^INSERT INTO `\w+` \((.*?)\) VALUES \((.*)\);\s*$")
    for line in open(os.path.join(DUMP, name + '.sql'), encoding='utf-8', errors='replace'):
        if not line.startswith('INSERT'):
            continue
        m = rx.match(line)
        if not m:
            continue
        cols = [c.strip('` ') for c in m.group(1).split(',')]
        vals = split_values(m.group(2))
        if len(vals) == len(cols):
            r = dict(zip(cols, vals))
            yield {k: r.get(k) for k in want}


src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('e, rse, ne, be')[0]
exec(src)
names, cast = {}, {}
for i in range(n4):
    r = struct.unpack_from('<%dI' % (rs4 // 4), s4, b4 + i * rs4)
    names[r[0]] = spname(r[0]); cast[r[0]] = r[7]

creatures = {r['id']: r for r in load('creatures', ['id', 'zone_id', 'name', 'health_max', 'level_max', 'is_veteran', 'difficulty'])
             if r['name'] in BOSSES}
spells = {r['id']: r for r in load('spells', ['id', 'game_id', 'name', 'tier', 'castinfo', 'target'])}
rows = []
for r in load('creature_spells', ['creature_id', 'spell_id', 'enabled']):
    c = creatures.get(r['creature_id'])
    s = spells.get(r['spell_id'])
    if not c or not s or not s['game_id']:
        continue
    g = int(s['game_id'])
    bn = names.get(g)
    match = 'name_match' if (bn or '').strip().lower() == (s['name'] or '').strip().lower() else ('not_in_16042' if bn is None else 'name_differs')
    rows.append([c['name'], c['id'], c['zone_id'], c['health_max'], c['level_max'], c['is_veteran'], c['difficulty'],
                 g, s['name'], s['tier'], s['castinfo'], s['target'], match, cast.get(g, ''), r['enabled']])
rows.sort(key=lambda x: (x[0], x[1], x[8], x[7]))
with open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f)
    w.writerow(['boss', 'jabbithole_creature_id', 'zone', 'health_max', 'level_max', 'is_veteran', 'difficulty', 'spell4_game_id',
                'spell_name', 'tier', 'castinfo', 'target', 'check_16042_name', 'cast_time_ms_16042', 'enabled'])
    w.writerows(rows)
print(len(rows), 'rows ->', OUT)
cnt = collections.Counter(r[12] for r in rows)
print(dict(cnt))
