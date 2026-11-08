"""ocr2: extract one frame (full and/or widget crop) from a video in challenge_videos.json or corpus.db.
  py -3.13 -I ocr2_grab.py <video id> <seconds> [full|crop|both] [scale]
Writes I:\tools\claude-tmp\g_<id>_<t>_{full,crop}.{jpg,png}"""
import sys, os, subprocess, sqlite3
FF = r"C:\Users\Bankai\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin\ffmpeg.exe"
CROP = "crop=iw*0.45:ih*0.30:iw*0.55:ih*0.05"
def vpath(vid):
    con = sqlite3.connect(r'I:\tools\video_index\corpus.db')
    return os.path.join(r"I:\Videos", con.execute("select file from video where id=?", (vid,)).fetchone()[0].replace("/", os.sep))
if __name__ == "__main__":
    vid = int(sys.argv[1]); t = float(sys.argv[2]); mode = sys.argv[3] if len(sys.argv) > 3 else "both"; sc = sys.argv[4] if len(sys.argv) > 4 else "2"
    p = vpath(vid); base = rf"I:\tools\claude-tmp\g_{vid}_{t:g}"
    if mode in ("full", "both"):
        subprocess.run([FF, "-v", "error", "-y", "-ss", str(t), "-i", p, "-frames:v", "1", "-q:v", "2", base + "_full.jpg"])
        print(base + "_full.jpg")
    if mode in ("crop", "both"):
        subprocess.run([FF, "-v", "error", "-y", "-ss", str(t), "-i", p, "-frames:v", "1", "-vf", f"{CROP},scale=iw*{sc}:-1:flags=lanczos", base + "_crop.png"])
        print(base + "_crop.png")
