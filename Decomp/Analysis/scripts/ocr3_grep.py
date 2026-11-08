r"""ocr3: fuzzy name grep over all OCR frames (I:\tools\video_index\ocr2\*.jsonl): for every flagged (time-tiered) challenge name,
report videos whose widget-crop or whole-frame text contains the name's two longest words (or the whole name) within one frame,
with first/last time and the mm:ss tokens of those frames. Complements ocr2_names.py (exact normalised match only).
  py -3.13 -I ocr3_grep.py [--since-mtime <unix seconds>]   (only files newer than the given time, for the new sweep)"""
import json, glob, os, re, sys, collections, sqlite3
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
OUT = r"I:\tools\video_index\ocr2"
since = float(sys.argv[sys.argv.index("--since-mtime") + 1]) if "--since-mtime" in sys.argv else 0
allc = json.load(open(os.path.join(OUT, "all_challenges.json"), encoding="utf-8"))
names = {}
for o in allc:
    if not o["tt"] or not o["name"]:
        continue
    n = re.sub(r"^(vista challenge|imbuement|vista)\s*:\s*", "", o["name"], flags=re.I).strip()
    words = sorted({w for w in re.findall(r"[a-z']+", n.lower()) if len(w) >= 5}, key=len, reverse=True)[:2]
    if words:
        names.setdefault(n, (o["id"], words))
con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
hits = collections.defaultdict(list)
for fn in glob.glob(os.path.join(OUT, "*.jsonl")):
    if os.path.getmtime(fn) < since:
        continue
    for line in open(fn, encoding="utf-8"):
        d = json.loads(line)
        txt = (d["w"] + " " + d["b"]).lower()
        for n, (cid, words) in names.items():
            if all(w in txt for w in words):
                hits[(d["v"], n, cid)].append((d["t"], re.findall(r"(?<![\d:])\d{1,2}:[0-5]\d(?![\d:])", d["w"] + " " + d["b"])))
for (vid, n, cid), l in sorted(hits.items(), key=lambda x: (x[0][1], x[0][0])):
    l.sort()
    info = con.execute("select upload_date, title from video where id=?", (vid,)).fetchone()
    toks = sorted({x for _, ts in l for x in ts})[:6]
    print(f"{n[:26]:26} #{cid:<5} v{vid:>5} {info[0]} n={len(l):>3} t={l[0][0]:g}..{l[-1][0]:g} {toks} | {info[1][:40]}")
