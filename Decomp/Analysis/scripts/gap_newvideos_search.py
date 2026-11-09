"""Search only the videos added to I:\\Videos after the 2026-11-08 08:52 corpus build (restoration gaps 5 to 8 re-search).

Usage: py -3.13 -I gap_newvideos_search.py "<fts query>" [limit]
New = present in corpus.db but absent from corpus_20261108_before_rebuild.db (a backup of the earlier corpus).
Prints: video id, upload date (retail if < 20181128), title, url, cue second, cue text.
"""
import sqlite3, sys

sys.stdout.reconfigure(encoding="utf-8")
NEW = r"I:\tools\video_index\corpus.db"
OLD = r"I:\tools\video_index\corpus_20261108_before_rebuild.db"

db = sqlite3.connect(NEW)
old_files = set(r[0] for r in sqlite3.connect(OLD).execute("select file from video"))
vids = {}
for row in db.execute("select id, file, title, upload_date, url, has_subs from video"):
    if row[1] not in old_files:
        vids[row[0]] = row

if len(sys.argv) < 2:
    print("new videos", len(vids), "with subs", sum(1 for v in vids.values() if v[5]))
    sys.exit(0)

q = sys.argv[1]
limit = int(sys.argv[2]) if len(sys.argv) > 2 else 40
cols = [r[1] for r in db.execute("pragma table_info(cue)")]
vcol = "video_id" if "video_id" in cols else cols[0]
scol = [c for c in cols if c.startswith("start")][0]
tcol = [c for c in cols if c in ("text", "body")][0]
n = 0
for rowid, in db.execute("select rowid from cue_fts where cue_fts match ?", (q,)):
    r = db.execute(f"select {vcol}, {scol}, {tcol} from cue where rowid=?", (rowid,)).fetchone()
    if r is None or r[0] not in vids:
        continue
    v = vids[r[0]]
    print(f"{v[0]}\t{v[3]}\t{v[2][:70]}\t{v[4]}\t{int(r[1])}s\t{r[2][:110]}")
    n += 1
    if n >= limit:
        break
print("hits", n)
