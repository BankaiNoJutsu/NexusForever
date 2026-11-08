r"""ocr2: contact sheet of widget crops with time labels, for eyeballing readings.
  py -3.13 -I ocr2_sheet.py <video id> <t1,t2,...> [x0=0.6 y0=0.05 w=0.4 h=0.32] [cols=2]  -> I:\tools\claude-tmp\sheet_<id>_<t1>.jpg
Each tile is a widget crop scaled to 700 px wide with the video time written on it."""
import sys, os, subprocess, sqlite3
FF = r"C:\Users\Bankai\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin\ffmpeg.exe"
FONT = "C\:/Windows/Fonts/arial.ttf"
vid = int(sys.argv[1]); ts = [float(x) for x in sys.argv[2].split(",")]
x0, y0, w, h = (float(x) for x in (sys.argv[3].split(",") if len(sys.argv) > 3 else "0.6,0.05,0.4,0.32".split(",")))
cols = int(sys.argv[4]) if len(sys.argv) > 4 else 2
con = sqlite3.connect(r'I:\tools\video_index\corpus.db')
p = os.path.join(r"I:\Videos", con.execute("select file from video where id=?", (vid,)).fetchone()[0].replace("/", os.sep))
tiles = []
for t in ts:
    out = rf"I:\tools\claude-tmp\tile_{vid}_{t:g}.png"
    vf = f"crop=iw*{w}:ih*{h}:iw*{x0}:ih*{y0},scale=700:-1:flags=lanczos,drawtext=fontfile='{FONT}':text='t={t:g}':x=4:y=4:fontsize=28:fontcolor=yellow:box=1:boxcolor=black@0.6"
    subprocess.run([FF, "-v", "error", "-y", "-ss", str(t), "-i", p, "-frames:v", "1", "-vf", vf, out])
    if os.path.exists(out): tiles.append(out)
ins = []
for tl in tiles: ins += ["-i", tl]
rows = [tiles[i:i + cols] for i in range(0, len(tiles), cols)]
sheet = rf"I:\tools\claude-tmp\sheet_{vid}_{ts[0]:g}.jpg"
n = len(tiles)
layout = "|".join(f"{(i % cols)}0_0".replace("0_0", "") for i in range(n))  # placeholder, replaced below
# xstack layout: x positions use w0, y positions use h0
pos = []
for i in range(n):
    cx, cy = i % cols, i // cols
    pos.append(("0" if cx == 0 else "+".join(["w0"] * cx)) + "_" + ("0" if cy == 0 else "+".join(["h0"] * cy)))
fc = "".join(f"[{i}:v]" for i in range(n)) + f"xstack=inputs={n}:layout={'|'.join(pos)}[o]"
subprocess.run([FF, "-v", "error", "-y"] + ins + ["-filter_complex", fc, "-map", "[o]", "-q:v", "3", sheet])
print(sheet)
