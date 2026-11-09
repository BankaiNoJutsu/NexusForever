r"""Windows-OCR sweep of one corpus video for text patterns (new 2026-11-09 gap 5/6 pass).
  py -3.13 -I gap9_ocr.py <id> <a> <b> <step> <regex> [--crop right|full] [--out <jsonl>]
Extracts frames every <step> s (whole frame scaled to 1920, plus 'right' = right 45% x top 60% at 2.5x), OCRs both,
prints the frames whose text matches <regex> (case-insensitive) and writes all text to --out. Frames live in a temp dir outside the repo.
Uses ocr_server.ps1 (Windows.Media.Ocr)."""
import sys, os, re, json, sqlite3, subprocess, tempfile, shutil, glob
sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import video_corpus as vc
from ocr2_run import Ocr
vid, a, b, step, rx = int(sys.argv[1]), float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4]), re.compile(sys.argv[5], re.I)
out = sys.argv[sys.argv.index("--out") + 1] if "--out" in sys.argv else None
row = sqlite3.connect(vc.DB).execute("select file,title from video where id=?", (vid,)).fetchone()
path = os.path.join(vc.ROOT, row[0])
d = tempfile.mkdtemp(prefix="g9ocr_", dir=os.environ.get("G9TMP"))
flt = ("[0:v]fps={:.5f},split=2[x][y];[x]crop=iw*0.45:ih*0.6:iw*0.55:0,scale=iw*2.5:-1:flags=lanczos[c];"
       "[y]scale=1920:-1:flags=lanczos[b]").format(1.0 / step)
subprocess.run([vc.FFMPEG, "-v", "error", "-y", "-ss", str(a), "-t", str(b - a), "-i", path, "-filter_complex", flt,
                "-map", "[c]", "-q:v", "2", os.path.join(d, "c_%05d.jpg"), "-map", "[b]", "-q:v", "2", os.path.join(d, "b_%05d.jpg")], check=True)
n = len(glob.glob(os.path.join(d, "c_*.jpg")))
ocr = Ocr(); fo = open(out, "w", encoding="utf-8") if out else None
print(row[1], "frames", n)
for k in range(1, n + 1):
    t = a + (k - 1) * step
    c = ocr.read(os.path.join(d, f"c_{k:05d}.jpg")); bb = ocr.read(os.path.join(d, f"b_{k:05d}.jpg"))
    if fo: fo.write(json.dumps({"t": t, "c": c, "b": bb}, ensure_ascii=False) + "\n")
    for lab, txt in (("c", c), ("b", bb)):
        m = rx.search(txt)
        if m:
            s = max(0, m.start() - 60); print(f"{int(t)}s[{lab}] ...{txt[s:m.end()+100]}")
ocr.close(); shutil.rmtree(d, ignore_errors=True)
