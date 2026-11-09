"""Aggregate NEW-video subtitle hits per video. Usage: py -3.13 -I gap9_agg.py "<fts query>" [--retail] [--show N]
Prints one line per video: id, date, title, url, hit count, first N hit seconds (dedup within 20 s)."""
import sqlite3, sys
sys.stdout.reconfigure(encoding="utf-8")
NEW = r"I:\tools\video_index\corpus.db"; OLD = r"I:\tools\video_index\corpus_20261108_before_rebuild.db"
db = sqlite3.connect(NEW)
old = set(r[0] for r in sqlite3.connect(OLD).execute("select file from video"))
vids = {r[0]: r for r in db.execute("select id,file,title,upload_date,url,has_subs from video") if r[1] not in old}
q = sys.argv[1]; retail = "--retail" in sys.argv
show = int(sys.argv[sys.argv.index("--show")+1]) if "--show" in sys.argv else 3
res = {}
for rowid, in db.execute("select rowid from cue_fts where cue_fts match ?", (q,)):
    r = db.execute("select video_id,start,text from cue where rowid=?", (rowid,)).fetchone()
    if r[0] not in vids: continue
    v = vids[r[0]]
    if retail and not ((v[3] or "9") < "20181128" and (v[3] or "")): continue
    res.setdefault(r[0], []).append((r[1], r[2]))
for vid, hits in sorted(res.items(), key=lambda kv: vids[kv[0]][3] or ""):
    v = vids[vid]; hits.sort()
    sel = []
    for s, t in hits:
        if not sel or s - sel[-1][0] > 20: sel.append((s, t))
    print(f"{vid}\t{v[3]}\t{v[2][:60]}\t{v[4]}\tn={len(sel)}\t" + " | ".join(f"{int(s)}s:{t[:60]}" for s, t in sel[:show]))
print("videos", len(res))
