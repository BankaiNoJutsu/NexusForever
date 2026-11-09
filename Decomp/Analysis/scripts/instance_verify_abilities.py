import re, csv, glob, sqlite3, sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
root = r'I:\GIT\NexusForever-unified-20261106'
tags = {int(r['spell4_id']): r['description'] for r in csv.DictReader(open(root + r'\Decomp\Analysis\coverage\spell4_internal_tags_20261109.csv', encoding='utf-8'))}
db = sqlite3.connect(r'I:\tools\video_index\corpus.db')

dungeon_titles = {
    'StormtalonsLair': ["Stormtalon", "Aethros", "Blade-Wind", "Blade Wind"],
    'RuinsOfKelVoreth': ["Kel Voreth", "Kelvoreth", "Ruins of Kel", "Trogun", "Grond", "Drokk"],
    'Skullcano': ["Skullcano", "Tugga", "Thunderfoot", "Octog", "Mordechai"],
    'SanctuaryOfTheSwordmaiden': ["Sanctuary of the Swordmaiden", "Swordmaiden", "Ondu", "Skash", "Rayna", "Selene"],
    'ProtogamesAcademy': ["Protogames Academy", "Wrathbone", "Invulnotron"],
    'ColdbloodCitadel': ["Coldblood"],
    'GeneticArchives': ["Genetic Archives", "Kuralak", "X-89", "Phage Maw", "Phageborn"],
    'Datascape': ["Datascape", "System Daemons", "Avatus", "Maelstrom", "Gloomclaw"],
}


def keyword(desc):
    d = re.sub(r'^(\[[^\]]+\])+\s*e\d+\s*-\s*', '', desc)
    parts = [p.strip() for p in d.split(' - ')]
    parts = [re.sub(r'\s*\(.*?\)\s*', ' ', p).strip() for p in parts]
    junk = {'base', 'proxy', 'tier 1', 'tier 2', 'base n', 'aura', 'damage', 'target selection', 'channel', 'summon', 'na', 'n'}
    cands = [p for p in parts[1:] if p.lower() not in junk and not p.lower().startswith('tier')]
    return (cands[0] if cands else parts[-1]).strip()


files = glob.glob(root + r'\Source\NexusForever.Script.Instance\**\*ProvisionalEncounterScripts.cs', recursive=True)
rows = []
for f in files:
    folder = [k for k in dungeon_titles if '\\' + k + '\\' in f]
    if not folder:
        continue
    inst = folder[0]
    src = open(f, encoding='utf-8-sig').read()
    for m in re.finditer(r'public const uint (\w+)\s*=\s*(\d+)u;', src):
        sid = int(m.group(2))
        if sid in tags:
            rows.append((inst, m.group(1), sid, tags[sid], keyword(tags[sid])))

vid_by_inst = {}
for inst, pats in dungeon_titles.items():
    cond = ' or '.join(['title like ? or description like ? or zone like ?'] * len(pats))
    vs = db.execute(f"select id, channel, upload_date from video where has_subs=1 and upload_date<>'' and upload_date<'20181128' and ({cond})",
                    [x for p in pats for x in ('%' + p + '%',) * 3]).fetchall()
    vid_by_inst[inst] = {v[0]: v[1] for v in vs}
    print(inst, 'subtitled retail videos', len(vs), 'channels', len(set(v[1] for v in vs)))

fts_cols = [r[1] for r in db.execute("pragma table_info(cue)")]
out = []
for inst, name, sid, desc, kw in rows:
    vids = vid_by_inst[inst]
    phrase = kw.replace("'", ' ').replace('"', ' ')
    alt = {phrase, phrase.replace(' ', ''), phrase.replace("'s", 's')}
    hit_vids = set()
    for ph in alt:
        words = ph.split()
        if not words:
            continue
        q = '"' + ' '.join(words) + '"'
        try:
            for (rid,) in db.execute("select rowid from cue_fts where cue_fts match ?", (q,)):
                r = db.execute("select video_id from cue where rowid=?", (rid,)).fetchone()
                if r and r[0] in vids:
                    hit_vids.add(r[0])
        except sqlite3.OperationalError:
            pass
    chans = set(vids[v] for v in hit_vids)
    out.append((inst, name, sid, kw, len(hit_vids), len(chans)))

for o in out:
    print(f"{o[0]:26s} {o[1]:32s} {o[2]:6d} kw='{o[3]}' videos={o[4]} channels={o[5]}")
print('total', len(out), 'with >=2 channels', sum(1 for o in out if o[5] >= 2), 'with 1 channel', sum(1 for o in out if o[5] == 1), 'with 0', sum(1 for o in out if o[5] == 0))
