import re, csv, glob, os, sqlite3, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
root = r'I:\GIT\NexusForever-unified-20261106'
base = root + r'\Source\NexusForever.Script.Instance'
rows = list(csv.DictReader(open(root + r'\Decomp\Analysis\coverage\spell4_internal_tags_20261109.csv', encoding='utf-8')))
enc = collections.defaultdict(list)
for r in rows:
    if r['encounter'][:1] in ('e', 'w') and r['encounter'][1:].isdigit():
        enc[int(r['encounter'][1:])].append(r)
db = sqlite3.connect(r'I:\tools\video_index\corpus.db')

targets = {
    'Infestation': (r'Expedition\Infestation', ['Infestation']),
    'OutpostM13': (r'Expedition\OutpostM13', ['Outpost M-13', 'Outpost M13']),
    'RageLogic': (r'Expedition\RageLogic', ['Rage Logic']),
    'SpaceMadness': (r'Expedition\SpaceMadness', ['Space Madness']),
    'Gauntlet': (r'Expedition\Gauntlet', ['Gauntlet']),
    'DeepSpaceExploration': (r'Expedition\DeepSpaceExploration', ['Deep Space']),
    'FragmentZero': (r'Expedition\FragmentZero', ['Fragment Zero']),
    'EvilFromTheEther': (r'Expedition\EvilFromTheEther', ['Evil from the Ether']),
    'UltimateProtogames': (r'Dungeon\UltimateProtogames', ['Ultimate Protogames']),
    'HallOfTheHundred(Vault)': (r'WorldStory\HallOfTheHundred', ['Vault of the Archon', 'Hall of the Hundred']),
    'RedMoonTerror': (r'Raid\RedMoonTerror', ['Redmoon Terror', 'Red Moon Terror']),
    'InitializationCoreY83': (r'Raid\InitializationCoreY83', ['Y-83', 'Initialization Core', 'Augmentor']),
    'ShadesEve': (r'EventInstance\ShadesEve', ["Shade's Eve", 'Shades Eve']),
    'JourneyIntoOMNICore1': (r'WorldStory\JourneyIntoOMNICore1', ['OMNICore']),
    'ProtostarsSuperMall': (r'EventInstance\ProtostarsSuperMallInTheSky', ['Winterfest', 'Super Mall']),
}
print(f"{'row':26s} {'objective ids':>13s} {'ids with tag rows':>18s} {'tag rows':>9s} {'retail subtitled videos':>24s} {'channels':>8s}")
for name, (folder, pats) in targets.items():
    ids = set()
    for p in glob.glob(os.path.join(base, folder, '**', 'PublicEventObjective.cs'), recursive=True):
        for m in re.finditer(r'^\s*(\w+)\s*=\s*(\d+)', open(p, encoding='utf-8-sig').read(), re.M):
            ids.add(int(m.group(2)))
    hit = [i for i in ids if i in enc]
    nrows = sum(len(enc[i]) for i in hit)
    cond = ' or '.join(['title like ? or description like ?'] * len(pats))
    vs = db.execute(f"select channel from video where has_subs=1 and upload_date<>'' and upload_date<'20181128' and ({cond})",
                    [x for q in pats for x in ('%' + q + '%',) * 2]).fetchall()
    print(f"{name:26s} {len(ids):13d} {len(hit):18d} {nrows:9d} {len(vs):24d} {len(set(v[0] for v in vs)):8d}")
