r"""Keyframe OCR sweep for taxi-related UI text over a list of corpus ids (new videos, gap 5 re-search 2026-11-09).
  py -3.13 -I gap9_taxi_sweep.py <ids.json> <out.jsonl> [--step 8] [--shard i/n] [--pat <regex>]
ids.json = list of {"id":..}. Output: one JSON line per frame whose OCR text matches the pattern (video id, second, snippet).
Uses ffmpeg -skip_frame nokey (fast, keyframe granularity) and the Windows OCR server; temp frames are deleted."""
import sys, os, re, json, sqlite3, subprocess, tempfile, shutil, glob, time
sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import video_corpus as vc
from ocr2_run import Ocr
PAT = re.compile(r"taxi vendor|transport price|take taxi|not enough|insufficient|can.?t afford|cannot afford|you are on a taxi|on a taxi|taxi", re.I)
if "--pat" in sys.argv: PAT = re.compile(sys.argv[sys.argv.index("--pat") + 1], re.I)
ids = [j["id"] for j in json.load(open(sys.argv[1]))]
out = sys.argv[2]
step = float(sys.argv[sys.argv.index("--step") + 1]) if "--step" in sys.argv else 8
if "--shard" in sys.argv:
    i, n = map(int, sys.argv[sys.argv.index("--shard") + 1].split("/")); ids = ids[i::n]
db = sqlite3.connect(vc.DB); ocr = Ocr()
done = set()
if os.path.exists(out):
    for l in open(out, encoding="utf-8"):
        try: done.add(json.loads(l).get("done"))
        except Exception: pass
fo = open(out, "a", encoding="utf-8")
for vid in ids:
    if vid in done: continue
    row = db.execute("select file from video where id=?", (vid,)).fetchone()
    d = tempfile.mkdtemp(prefix="g9tx_", dir=os.environ.get("G9TMP"))
    t0 = time.time()
    subprocess.run([vc.FFMPEG, "-v", "error", "-y", "-skip_frame", "nokey", "-i", os.path.join(vc.ROOT, row[0]), "-vf",
                    f"fps=1/{step},scale=1920:-1:flags=bicubic", "-q:v", "3", os.path.join(d, "f_%05d.jpg")])
    fs = sorted(glob.glob(os.path.join(d, "f_*.jpg")))
    for k, f in enumerate(fs):
        txt = ocr.read(f)
        m = PAT.search(txt)
        if m:
            s = max(0, m.start() - 80)
            fo.write(json.dumps({"v": vid, "t": k * step, "snip": txt[s:m.end() + 120]}, ensure_ascii=False) + "\n"); fo.flush()
    fo.write(json.dumps({"done": vid, "frames": len(fs), "sec": round(time.time() - t0)}) + "\n"); fo.flush()
    shutil.rmtree(d, ignore_errors=True)
ocr.close(); fo.close()
