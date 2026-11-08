"""Turn the OCR sweep into challenge timer candidates.

For every video it collects mm:ss readings per frame and clusters them by C = frame_time + remaining (a real-time
countdown keeps C constant). A cluster with at least MIN_FRAMES frames is a running countdown; the first reading of the
cluster bounds the limit: limit in [first_remaining, first_remaining + gap], where gap is the time since the previous frame
without that countdown (or since a "Challenge Active" banner if one was read). Challenge names from the flagged list that
appear in the frame text or the video title near the cluster are attached.

  python -I ocr_analyze.py [--min-frames 4] [--tol 3] [--csv out.csv]
Output goes to stdout as a table and optionally CSV. Readings are OCR guesses: confirm any figure you rely on by viewing a
frame (video_corpus.py frame ...).
"""
import json, os, re, sys, csv, collections

OUT = r"I:\tools\video_index\ocr"
LIST = r"I:\tools\video_index\challenge_videos.json"
FLAGGED = r"C:\Users\Bankai\AppData\Local\Temp\claude\748f060f2bb734af\e29eaf45-82ed-4f0c-b695-3a341bb120a2\scratchpad\flagged_challenges.json"
TIMER = re.compile(r"(?<![\d:])(\d{1,2}):([0-5]\d)(?![\d:])")


def clusters(frames, tol, min_frames):
    cand = []
    for t, text in frames:
        for m in TIMER.finditer(text):
            rem = int(m.group(1)) * 60 + int(m.group(2))
            if 3 <= rem <= 3600:
                cand.append((t, rem))
    if not cand:
        return []
    buckets = collections.defaultdict(list)
    for t, rem in cand:
        buckets[round((t + rem) / tol)].append((t, rem))
    merged = []
    for k, lst in buckets.items():
        lst = lst + buckets.get(k + 1, [])
        frames_in = {t for t, _ in lst}
        if len(frames_in) >= min_frames:
            merged.append(sorted(set(lst)))
    # drop clusters wholly contained in a larger one
    merged.sort(key=len, reverse=True)
    out = []
    used = set()
    for c in merged:
        key = {t for t, _ in c}
        if key & used and len(key - used) < min_frames:
            continue
        used |= key
        out.append(c)
    return out


def main():
    a = sys.argv[1:]
    min_frames = int(a[a.index("--min-frames") + 1]) if "--min-frames" in a else 4
    tol = float(a[a.index("--tol") + 1]) if "--tol" in a else 3.0
    csv_path = a[a.index("--csv") + 1] if "--csv" in a else None
    vids = {v["id"]: v for v in json.load(open(LIST, encoding="utf-8"))}
    flagged = []
    try:
        flagged = json.load(open(FLAGGED, encoding="utf-8"))
    except OSError:
        pass
    names = [(f["name"], f["id"], f["tier_goals_ms"]) for f in flagged if f["name"]]
    rows = []
    for fn in sorted(os.listdir(OUT)):
        if not fn.endswith(".jsonl"):
            continue
        vid = int(fn[:-6])
        frames = []
        for line in open(os.path.join(OUT, fn), encoding="utf-8"):
            try:
                d = json.loads(line)
            except ValueError:
                continue
            frames.append((d["t"], d["text"]))
        if not frames:
            continue
        times = [t for t, _ in frames]
        title = vids.get(vid, {}).get("title", "")
        for cl in clusters(frames, tol, min_frames):
            t_first, rem_first = cl[0]
            prev = [t for t in times if t < t_first]
            t_prev = max(prev) if prev else 0.0
            banner = [t for t, x in frames if "challenge active" in x.lower() and t <= t_first and t_first - t < 15]
            gap = t_first - (max(banner) if banner else t_prev)
            span = (cl[0][0] - 20, cl[-1][0] + 20)
            blob = (title + " " + " ".join(x for t, x in frames if span[0] <= t <= span[1])).lower()
            hit = [f"{n}#{i}" for n, i, g in names if n.lower() in blob]
            rows.append(dict(video=vid, date=vids.get(vid, {}).get("date", ""), title=title[:60], n=len(cl),
                             t_first=t_first, rem_first=rem_first, rem_max=max(r for _, r in cl), rem_min=min(r for _, r in cl),
                             limit_low=max(r for _, r in cl), limit_high=max(r for _, r in cl) + round(gap),
                             banner=bool(banner), names=";".join(hit)))
    rows.sort(key=lambda r: (r["video"], r["t_first"]))
    for r in rows:
        print(f"{r['video']:>5} {r['date']} n={r['n']:>3} first@{r['t_first']:>6.1f}s rem={r['rem_first']:>4}s limit~{r['limit_low']}-{r['limit_high']}s "
              f"{'B' if r['banner'] else ' '} {r['names'][:40]:40} {r['title']}")
    if csv_path and rows:
        with open(csv_path, "w", newline="", encoding="utf-8") as f:
            w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
            w.writeheader()
            w.writerows(rows)
    print(len(rows), "countdown clusters in", len({r['video'] for r in rows}), "videos")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    main()
