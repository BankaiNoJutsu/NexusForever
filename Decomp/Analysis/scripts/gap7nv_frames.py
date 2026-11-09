"""Gap 7 new-video helper. Usage (py -3.13 -I gap7nv_frames.py ...):
  sheet <id> <start_s> <dur_s> <step_s> <cols> <rows> <out.jpg> [tile_w]   contact sheet
  frames <id> <out_prefix> <t1> <t2> ...                                   full-res frames
  probe <id>                                                               width/height/fps
  crop <id> <t> <x> <y> <w> <h> <scale> <out.png>                          crop and enlarge
Outputs belong in the session scratchpad, never the repository."""
import sqlite3, subprocess, sys, os
sys.stdout.reconfigure(encoding="utf-8")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import video_corpus as vc
FF = vc.FFMPEG; FP = FF.replace("ffmpeg.exe", "ffprobe.exe")
def path(i):
    f = sqlite3.connect(vc.DB).execute("select file from video where id=?", (int(i),)).fetchone()[0]
    return os.path.join(vc.ROOT, f)
a = sys.argv[1:]
cmd = a[0]
if cmd == "probe":
    print(subprocess.run([FP, "-v", "error", "-select_streams", "v", "-show_entries", "stream=width,height,r_frame_rate", "-of", "csv=p=0", path(a[1])], capture_output=True, text=True).stdout)
elif cmd == "sheet":
    i, s, d, st, c, r, out = a[1], a[2], a[3], a[4], a[5], a[6], a[7]
    w = a[8] if len(a) > 8 else "640"
    subprocess.run([FF, "-y", "-loglevel", "error", "-ss", s, "-t", d, "-i", path(i), "-vf", f"fps=1/{st},scale={w}:-1,tile={c}x{r}", "-frames:v", "1", out], check=True)
elif cmd == "frames":
    i, pre = a[1], a[2]
    for t in a[3:]:
        subprocess.run([FF, "-y", "-loglevel", "error", "-ss", t, "-i", path(i), "-frames:v", "1", "-q:v", "2", f"{pre}_{t}.jpg"], check=True)
elif cmd == "crop":
    i, t, x, y, w, h, sc, out = a[1:9]
    subprocess.run([FF, "-y", "-loglevel", "error", "-ss", t, "-i", path(i), "-frames:v", "1", "-vf", f"crop={w}:{h}:{x}:{y},scale=iw*{sc}:ih*{sc}:flags=lanczos", out], check=True)
