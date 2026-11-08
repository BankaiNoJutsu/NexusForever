"""Searchable corpus over I:\\Videos (retail WildStar footage) for witness questions.

Usage:
  python -I video_corpus.py build            # (re)build I:\\tools\\video_index\\corpus.db from index.csv + .srt + .description
  python -I video_corpus.py find "reward track" [--limit 40] [--type Paths]
  python -I video_corpus.py frame "<relative file>" <seconds> [out.jpg]   # one frame via ffmpeg

The database lives outside the repository (it is large and derived). Subtitle hits give
(video, start second, text) so a frame can be extracted at that moment and looked at.
Evidence tier of anything found this way is "Observed" at most; see VIDEO_WITNESS_GOAL_PROMPT.md.
"""
import csv, os, re, sqlite3, subprocess, sys

ROOT = r"I:\Videos"
DB_DIR = r"I:\tools\video_index"
DB = os.path.join(DB_DIR, "corpus.db")
# The 2013 Panda3D ffmpeg cannot decode some files (pix_fmt errors); use the winget Gyan build.
FFMPEG = (r"C:\Users\Bankai\AppData\Local\Microsoft\WinGet\Packages"
          r"\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin\ffmpeg.exe")

TS = re.compile(r"(\d+):(\d+):(\d+)[,.](\d+)\s*-->")


def ts_seconds(m):
    h, mi, s, ms = (int(x) for x in m.groups())
    return h * 3600 + mi * 60 + s + ms / 1000.0


def parse_srt(path):
    try:
        text = open(path, encoding="utf-8-sig", errors="replace").read()
    except OSError:
        return
    for block in re.split(r"\r?\n\r?\n", text):
        lines = [l for l in block.splitlines() if l.strip()]
        for i, l in enumerate(lines):
            m = TS.search(l)
            if m:
                body = " ".join(re.sub(r"<[^>]+>", "", x).strip() for x in lines[i + 1:])
                if body:
                    yield ts_seconds(m), body
                break


def build():
    os.makedirs(DB_DIR, exist_ok=True)
    if os.path.exists(DB):
        os.remove(DB)
    con = sqlite3.connect(DB)
    con.executescript(
        """
        CREATE TABLE video(id INTEGER PRIMARY KEY, file TEXT UNIQUE, title TEXT, type TEXT, zone TEXT,
                           class TEXT, path TEXT, faction TEXT, channel TEXT, upload_date TEXT, url TEXT,
                           duration_s REAL, description TEXT, has_subs INTEGER DEFAULT 0);
        CREATE VIRTUAL TABLE meta_fts USING fts5(title, description, content='');
        CREATE TABLE cue(video_id INTEGER, start REAL, text TEXT);
        CREATE VIRTUAL TABLE cue_fts USING fts5(text, content='cue', content_rowid='rowid');
        """
    )
    idx = os.path.join(ROOT, "_INDEX", "index.csv")
    with open(idx, encoding="utf-8-sig", newline="") as f:
        for r in csv.DictReader(f):
            cur = con.execute(
                "INSERT OR IGNORE INTO video(file,title,type,zone,class,path,faction,channel,upload_date,url,duration_s,description)"
                " VALUES(?,?,?,?,?,?,?,?,?,?,?,?)",
                (r["file"], r["title"], r["type"], r["zone_or_location"], r["class"], r["path"], r["faction"],
                 r["channel"], r["upload_date"], r["url"], float(r["duration_s"] or 0), r["description"]),
            )
            vid = cur.lastrowid
            if not vid:
                continue
            con.execute("INSERT INTO meta_fts(rowid,title,description) VALUES(?,?,?)", (vid, r["title"], r["description"]))
            base = os.path.splitext(os.path.join(ROOT, r["file"]))[0]
            d, stem = os.path.dirname(base), os.path.basename(base)
            subs = [p for p in (os.path.join(d, n) for n in os.listdir(d) if n.startswith(stem[:60]) and n.endswith(".srt"))] if os.path.isdir(d) else []
            # prefer exact-stem srt files; fall back to prefix matches
            exact = [p for p in subs if os.path.basename(p).startswith(stem + ".")]
            for p in (exact or subs)[:1]:
                n = 0
                for start, body in parse_srt(p):
                    c = con.execute("INSERT INTO cue(video_id,start,text) VALUES(?,?,?)", (vid, start, body))
                    con.execute("INSERT INTO cue_fts(rowid,text) VALUES(?,?)", (c.lastrowid, body))
                    n += 1
                if n:
                    con.execute("UPDATE video SET has_subs=1 WHERE id=?", (vid,))
    con.commit()
    v = con.execute("SELECT count(*), sum(has_subs) FROM video").fetchone()
    c = con.execute("SELECT count(*) FROM cue").fetchone()[0]
    print(f"videos={v[0]} with_subs={v[1]} cues={c}")


def find(q, limit=40, vtype=None):
    con = sqlite3.connect(DB)
    print("== subtitle cues ==")
    sql = ("SELECT v.type, v.title, v.file, c.start, c.text FROM cue_fts f JOIN cue c ON c.rowid=f.rowid "
           "JOIN video v ON v.id=c.video_id WHERE cue_fts MATCH ?" + (" AND v.type=?" if vtype else "") + " LIMIT ?")
    args = [q] + ([vtype] if vtype else []) + [limit]
    for t, title, file, start, text in con.execute(sql, args):
        print(f"[{t}] {title[:70]} @{int(start)}s :: {text[:140]}\n    {file}")
    print("== titles/descriptions ==")
    sql = ("SELECT v.type, v.title, v.file, v.duration_s FROM meta_fts f JOIN video v ON v.id=f.rowid "
           "WHERE meta_fts MATCH ?" + (" AND v.type=?" if vtype else "") + " LIMIT ?")
    for t, title, file, dur in con.execute(sql, args):
        print(f"[{t}] {title[:90]} ({int(dur or 0)}s)\n    {file}")


def frame(rel, sec, out=None):
    out = out or os.path.join(DB_DIR, "frames", re.sub(r"[^A-Za-z0-9]+", "_", rel)[:60] + f"_{int(float(sec))}.jpg")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    subprocess.run([FFMPEG, "-y", "-ss", str(sec), "-i", os.path.join(ROOT, rel), "-frames:v", "1", "-q:v", "3", out],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)
    print(out)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # -I ignores PYTHONIOENCODING
    cmd = sys.argv[1]
    if cmd == "build":
        build()
    elif cmd == "find":
        a = sys.argv[2:]
        lim = int(a[a.index("--limit") + 1]) if "--limit" in a else 40
        typ = a[a.index("--type") + 1] if "--type" in a else None
        find(a[0], lim, typ)
    elif cmd == "frame":
        frame(sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else None)
