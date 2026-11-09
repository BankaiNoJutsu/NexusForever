"""Contact sheet of frames from a corpus video: py -3.13 -I gap9_sheet.py <id> <out.jpg> <cols> <width> t1 t2 ...  (frames stay outside the repo)."""
import sqlite3, subprocess, sys, os, tempfile, shutil
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import video_corpus as vc
vid, out, cols, w = int(sys.argv[1]), sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
times = [float(x) for x in sys.argv[5:]]
row = sqlite3.connect(vc.DB).execute("select file,title,upload_date,url from video where id=?", (vid,)).fetchone()
print(row)
d = tempfile.mkdtemp()
for i, t in enumerate(times):
    subprocess.run([vc.FFMPEG, "-y", "-loglevel", "error", "-ss", str(t), "-i", os.path.join(vc.ROOT, row[0]), "-frames:v", "1", "-vf", f"scale={w}:-1", "-q:v", "3", os.path.join(d, f"f{i:03d}.jpg")], check=True)
rows = (len(times) + cols - 1) // cols
subprocess.run([vc.FFMPEG, "-y", "-loglevel", "error", "-framerate", "1", "-i", os.path.join(d, "f%03d.jpg"), "-vf", f"tile={cols}x{rows}", "-frames:v", "1", "-q:v", "3", out], check=True)
shutil.rmtree(d); print(out, times)
