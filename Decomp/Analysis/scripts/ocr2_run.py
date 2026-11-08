r"""ocr2: second OCR pass over retail footage, reading an upscaled crop of the top-right challenge widget plus the
whole frame at 1.5x. Jobs come from a JSON list [{"v": video id, "a": start s, "b": end s, "step": s}]
and results go to I:\tools\video_index\ocr2\<v>_<a>_<b>.jsonl as {"v","t","w": top-right crop text (3x), "b": whole frame text (1.5x; the widget can be dragged anywhere)}.

  py -3.13 -I ocr2_run.py jobs.json [--workers 2]
Needs ocr_server.ps1 (Windows OCR over stdin), ffmpeg (winget Gyan build) and corpus.db for the video path. Set TEMP/TMP to
I:\tools\claude-tmp. Finished jobs (.done file) are skipped, so the run can be resumed."""
import json, os, sys, subprocess, shutil, sqlite3, threading, queue, time, glob

FFMPEG = (r"C:\Users\Bankai\AppData\Local\Microsoft\WinGet\Packages"
          r"\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin\ffmpeg.exe")
PS1 = os.path.join(os.path.dirname(os.path.abspath(__file__)), "ocr_server.ps1")
OUT = r"I:\tools\video_index\ocr2"
TMP = r"I:\tools\claude-tmp\ocr2_frames"
FILTER = ("[0:v]fps={fps:.5f},split=2[x][y];"
          "[x]crop=iw*0.45:ih*0.30:iw*0.55:ih*0.05,scale=1800:-1:flags=lanczos[c];"
          "[y]scale=1920:-1:flags=lanczos[b]")


class Ocr:
    def __init__(self):
        self.p = subprocess.Popen(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", PS1],
                                  stdin=subprocess.PIPE, stdout=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1)

    def read(self, path):
        self.p.stdin.write(path + "\n"); self.p.stdin.flush()
        try:
            return json.loads(self.p.stdout.readline())["t"]
        except Exception:
            return ""

    def close(self):
        try:
            self.p.stdin.close(); self.p.wait(timeout=10)
        except Exception:
            self.p.kill()


def vpath(con, vid):
    r = con.execute("select file from video where id=?", (vid,)).fetchone()
    return os.path.join(r"I:\Videos", r[0].replace("/", os.sep)) if r else None


def do_job(job, ocr, ffsem):
    vid, a, b, step = job["v"], float(job["a"]), float(job["b"]), float(job["step"])
    name = f"{vid}_{int(a)}_{int(b)}"
    done = os.path.join(OUT, name + ".done")
    if os.path.exists(done):
        return
    con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
    path = vpath(con, vid)
    if not path or not os.path.exists(path):
        open(done, "w").write("missing"); return
    d = os.path.join(TMP, name)
    shutil.rmtree(d, ignore_errors=True); os.makedirs(d)
    t0 = time.time()
    with ffsem:
        subprocess.run([FFMPEG, "-v", "error", "-y", "-ss", f"{a:.2f}", "-t", f"{b - a:.2f}", "-i", path,
                        "-filter_complex", FILTER.format(fps=1.0 / step),
                        "-map", "[c]", "-q:v", "2", os.path.join(d, "c_%05d.jpg"),
                        "-map", "[b]", "-q:v", "2", os.path.join(d, "b_%05d.jpg")],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    n = len(glob.glob(os.path.join(d, "c_*.jpg")))
    with open(os.path.join(OUT, name + ".jsonl"), "w", encoding="utf-8") as fo:
        for k in range(1, n + 1):
            c = os.path.join(d, f"c_{k:05d}.jpg"); bb = os.path.join(d, f"b_{k:05d}.jpg")
            w = ocr.read(c) if os.path.exists(c) else ""
            bt = ocr.read(bb) if os.path.exists(bb) else ""
            fo.write(json.dumps({"v": vid, "t": round(a + (k - 1) * step, 2), "w": w, "b": bt}, ensure_ascii=False) + "\n")
    shutil.rmtree(d, ignore_errors=True)
    open(done, "w").write("ok")
    print(f"{name} frames={n} step={step} {time.time() - t0:.0f}s", flush=True)


def worker(q, ffsem):
    ocr = Ocr()
    while True:
        try:
            job = q.get_nowait()
        except queue.Empty:
            break
        try:
            do_job(job, ocr, ffsem)
        except Exception as e:
            print("ERR", job, e, flush=True)
    ocr.close()


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True); os.makedirs(TMP, exist_ok=True)
    jobs = json.load(open(sys.argv[1], encoding="utf-8"))
    nw = int(sys.argv[sys.argv.index("--workers") + 1]) if "--workers" in sys.argv else 2
    q = queue.Queue()
    for j in jobs:
        q.put(j)
    ffsem = threading.Semaphore(2)
    ts = [threading.Thread(target=worker, args=(q, ffsem)) for _ in range(nw)]
    [t.start() for t in ts]; [t.join() for t in ts]
    print("ALL DONE", flush=True)
