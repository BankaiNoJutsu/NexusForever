"""Search title/description (meta_fts) and optionally list NEW videos only (see gap_newvideos_search.py).
Usage: py -3.13 -I gap9_newmeta.py "<fts query>" [limit]   |   py -3.13 -I gap9_newmeta.py --dump <out.tsv>
"""
import sqlite3, sys
sys.stdout.reconfigure(encoding="utf-8")
NEW = r"I:\tools\video_index\corpus.db"
OLD = r"I:\tools\video_index\corpus_20261108_before_rebuild.db"
db = sqlite3.connect(NEW)
old_files = set(r[0] for r in sqlite3.connect(OLD).execute("select file from video"))
vids = {r[0]: r for r in db.execute("select id,file,title,upload_date,url,has_subs,duration_s,path,channel from video") if r[1] not in old_files}
if sys.argv[1] == "--dump":
    with open(sys.argv[2], "w", encoding="utf-8") as f:
        for v in sorted(vids.values(), key=lambda x: x[3] or ""):
            f.write("\t".join(str(x) for x in (v[0], v[3], v[5], int(v[6] or 0), v[8], v[2], v[1])) + "\n")
    print(len(vids)); sys.exit(0)
q = sys.argv[1]; limit = int(sys.argv[2]) if len(sys.argv) > 2 else 60
n = 0
for rowid, in db.execute("select rowid from meta_fts where meta_fts match ?", (q,)):
    v = vids.get(rowid)
    if not v: continue
    print(f"{v[0]}\t{v[3]}\tsubs={v[5]}\t{int(v[6] or 0)}s\t{v[2][:80]}\t{v[4]}")
    n += 1
    if n >= limit: break
print("hits", n)
