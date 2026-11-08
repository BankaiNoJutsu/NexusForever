"""OCR sweep of retail challenge footage (reads timers, tier lists and challenge names).

For each video in I:\\tools\\video_index\\challenge_videos.json (dated retail videos whose title/description mention
"challenge") it samples frames, runs the Windows built-in OCR (ocr_server.ps1) and appends one JSON line per frame to
I:\\tools\\video_index\\ocr\\<video id>.jsonl: {"v": id, "t": seconds, "text": "..."}. Already finished videos are skipped,
so the run can be resumed. Frames are temporary.

  python -I ocr_challenges.py run [--only ID,ID] [--max-frames 150] [--min-step 2] [--max-step 30]
"""
import json, os, subprocess, sys, tempfile, shutil, threading, queue, time

FFMPEG = (r"C:\Users\Bankai\AppData\Local\Microsoft\WinGet\Packages"
          r"\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin\ffmpeg.exe")
ROOT = r"I:\Videos"
LIST = r"I:\tools\video_index\challenge_videos.json"
OUT = r"I:\tools\video_index\ocr"
PS1 = os.path.join(os.path.dirname(os.path.abspath(__file__)), "ocr_server.ps1")
TMP = r"I:\tools\claude-tmp\ocr_frames"


def extract(video, t, path):
    subprocess.run([FFMPEG, "-y", "-ss", f"{t:.2f}", "-i", video, "-frames:v", "1", "-vf", "scale='min(1280,iw)':-2",
                    "-q:v", "3", path], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return os.path.exists(path) and os.path.getsize(path) > 0


class Ocr:
    def __init__(self):
        self.p = subprocess.Popen(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", PS1],
                                  stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1)

    def read(self, path):
        self.p.stdin.write(path + "\n")
        self.p.stdin.flush()
        line = self.p.stdout.readline()
        try:
            return json.loads(line)["t"]
        except Exception:
            return ""

    def close(self):
        try:
            self.p.stdin.close()
            self.p.wait(timeout=10)
        except Exception:
            self.p.kill()


def run(only, max_frames, min_step, max_step):
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(TMP, exist_ok=True)
    vids = json.load(open(LIST, encoding="utf-8"))
    if only:
        vids = [v for v in vids if v["id"] in only]
    ocr = Ocr()
    for n, v in enumerate(vids):
        done = os.path.join(OUT, f"{v['id']}.done")
        if os.path.exists(done):
            continue
        path = os.path.join(ROOT, v["file"].replace("/", os.sep))
        if not os.path.exists(path):
            open(done, "w").write("missing")
            continue
        dur = float(v["dur"] or 0) or 300.0
        step = min(max(dur / max_frames, min_step), max_step)
        times = [i * step + 1.0 for i in range(int(dur / step))]
        out = os.path.join(OUT, f"{v['id']}.jsonl")
        t0 = time.time()
        with open(out, "w", encoding="utf-8") as fo:
            for t in times:
                fp = os.path.join(TMP, f"f_{v['id']}.jpg")
                if os.path.exists(fp):
                    os.remove(fp)
                if not extract(path, t, fp):
                    continue
                text = ocr.read(fp)
                fo.write(json.dumps({"v": v["id"], "t": round(t, 1), "text": text}, ensure_ascii=False) + "\n")
        open(done, "w").write("ok")
        print(f"[{n + 1}/{len(vids)}] {v['id']} {v['title'][:50]} frames={len(times)} step={step:.1f}s {time.time() - t0:.0f}s", flush=True)
    ocr.close()


if __name__ == "__main__":
    a = sys.argv[1:]
    if a and a[0] == "run":
        only = set(int(x) for x in a[a.index("--only") + 1].split(",")) if "--only" in a else None
        mf = int(a[a.index("--max-frames") + 1]) if "--max-frames" in a else 150
        mn = float(a[a.index("--min-step") + 1]) if "--min-step" in a else 2.0
        mx = float(a[a.index("--max-step") + 1]) if "--max-step" in a else 30.0
        run(only, mf, mn, mx)
