r"""ocr2: list frames whose OCR text contains the exact (normalised) name of a flagged (time-tiered) challenge, grouped per video and name.
  py -3.13 -I ocr2_names.py [--all]   (--all also searches the unflagged names)
Prints video id, date, name, first/last time seen, number of frames, and the mm:ss tokens seen in those frames (widget text)."""
import json, glob, os, re, sys, collections, sqlite3
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
OUT = r"I:\tools\video_index\ocr2"
allc = json.load(open(os.path.join(OUT, "all_challenges.json"), encoding="utf-8"))
norm = lambda s: re.sub(r"[^a-z0-9]", "", s.lower())
names = {}
for o in allc:
    n = re.sub(r"^(vista challenge|imbuement|vista)\s*:\s*", "", o["name"], flags=re.I).strip()
    if len(norm(n)) >= 9 and (o["tt"] or "--all" in sys.argv):
        names.setdefault(norm(n), (n, o["id"], o["tt"]))
con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
hits = collections.defaultdict(list)
for fn in glob.glob(os.path.join(OUT, "*.jsonl")):
    for line in open(fn, encoding="utf-8"):
        d = json.loads(line)
        nt = norm(d["w"] + d["b"])
        for k, v in names.items():
            if k in nt:
                hits[(d["v"], v[0], v[1])].append((d["t"], re.findall(r"(?<![\d:])\d{1,2}:[0-5]\d(?![\d:])", d["w"] + " " + d["b"])))
for (vid, n, cid), l in sorted(hits.items()):
    l.sort()
    info = con.execute("select upload_date, title from video where id=?", (vid,)).fetchone()
    toks = sorted({x for _, ts in l for x in ts})[:8]
    print(f"{vid:>5} {info[0]} #{cid:<5} {n[:28]:28} frames={len(l):>3} t={l[0][0]:g}..{l[-1][0]:g} tokens={toks} | {info[1][:45]}")
