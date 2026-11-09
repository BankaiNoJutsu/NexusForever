"""Extract one frame from a corpus video by corpus id: py -3.13 -I gap_frame_by_id.py <id> <seconds> <out.jpg>  (frames stay outside the repository)."""
import sqlite3, subprocess, sys, os

sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(__file__))
import video_corpus as vc

vid, sec, out = int(sys.argv[1]), float(sys.argv[2]), sys.argv[3]
row = sqlite3.connect(vc.DB).execute("select file, title, upload_date, url from video where id=?", (vid,)).fetchone()
print(row)
subprocess.run([vc.FFMPEG, "-y", "-loglevel", "error", "-ss", str(sec), "-i", os.path.join(vc.ROOT, row[0]), "-frames:v", "1", "-q:v", "2", out], check=True)
print(out)
