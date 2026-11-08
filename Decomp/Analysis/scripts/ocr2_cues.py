"""ocr2: find subtitle-cue hits of flagged challenge names in dated retail videos -> I:\tools\video_index\ocr2\cue_hits.json"""
import sqlite3, json, sys, re, collections
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
FLAGGED = r"C:\Users\Bankai\AppData\Local\Temp\claude\748f060f2bb734af\e29eaf45-82ed-4f0c-b695-3a341bb120a2\scratchpad\flagged_challenges.json"
OUT = r"I:\tools\video_index\ocr2\cue_hits.json"
con = sqlite3.connect(r'I:\tools\video_index\corpus.db')
fl = json.load(open(FLAGGED, encoding='utf-8'))
names = {}
for f in fl:
    n = f['name']
    if not n: continue
    n = re.sub(r'^(VISTA CHALLENGE|IMBUEMENT): ?', '', n, flags=re.I).strip()
    names.setdefault(n, []).append(f['id'])
# generic phrases that are too common to trust
hits = collections.defaultdict(list)
for n, ids in names.items():
    q = '"' + n.replace('"', '""') + '"'
    rows = con.execute("select v.id, v.upload_date, v.title, c.start, substr(c.text,1,100), v.url from cue_fts f join cue c on c.rowid=f.rowid join video v on v.id=c.video_id where cue_fts match ? and v.upload_date<'20181128' and v.upload_date!='' order by v.upload_date, c.start limit 200", (q,)).fetchall()
    for r in rows:
        hits[n].append(dict(vid=r[0], date=r[1], title=r[2], t=r[3], text=r[4], url=r[5], ids=ids))
    meta = con.execute("select v.id, v.upload_date, v.title, v.url from meta_fts f join video v on v.id=f.rowid where meta_fts match ? and v.upload_date<'20181128' and v.upload_date!='' limit 30", (q,)).fetchall()
    for r in meta:
        hits[n].append(dict(vid=r[0], date=r[1], title=r[2], t=None, text='META', url=r[3], ids=ids))
json.dump(hits, open(OUT, 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
for n in names:
    vs = {h['vid'] for h in hits[n]}
    print(f"{n:42} ids={names[n]} cues={sum(1 for h in hits[n] if h['t'] is not None):3} videos={len(vs)}")
