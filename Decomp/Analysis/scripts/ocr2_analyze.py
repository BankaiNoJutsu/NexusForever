r"""ocr2: analysis of the second OCR pass (I:\tools\video_index\ocr2\*.jsonl).

For every video it merges all windows, reads mm:ss timers from the widget crop text, clusters them by C = t + remaining (a real-time
countdown keeps C constant, tolerance --tol), and for each cluster of >= --min-frames frames reports the first reading, the
bound on the limit (first remaining .. first remaining + gap to the previous frame without the countdown, or since a
'Challenge Active/begins' banner), the challenge NAME read next to the timer (matched against all 538 Challenge.tbl names;
flagged ones marked *) and the threshold list read beside the timer (":00 / 1:00 / 1:10" style tokens).
  py -3.13 -I ocr2_analyze.py [--min-frames 4] [--tol 2] [--csv out.csv]
Readings are OCR guesses: view a frame (ocr2_grab.py) before relying on a figure."""
import json, os, re, sys, csv, collections, difflib, glob, sqlite3

OUT = r"I:\tools\video_index\ocr2"
TIMER = re.compile(r"(?<![\d:])(\d{1,2}):([0-5]\d)(?![\d:])")
THR = re.compile(r"(?<![\d])(\d{0,2}:[0-5]\d)(?![\d:])")
BANNER = re.compile(r"challenge\s*(active|begins|start)|active!|begins!", re.I)


def norm(s):
    return re.sub(r"[^a-z0-9]", "", s.lower())


def load_names():
    a = json.load(open(os.path.join(OUT, "all_challenges.json"), encoding="utf-8"))
    names = {}
    for o in a:
        n = re.sub(r"^(vista challenge|imbuement|vista)\s*:\s*", "", o["name"], flags=re.I).strip() if o["name"] else ""
        if len(norm(n)) >= 8:
            names.setdefault(n, []).append(o)
    return names


NAMES = None


def match_names(text, names, fuzzy_text=""):
    """names whose normalised string occurs in the normalised text (exact) or whose words mostly match (fuzzy)."""
    nt = norm(text)
    toks = {t for t in re.split(r"[^a-z0-9']+", fuzzy_text.lower()) if len(t) >= 3}
    out = []
    for n, rows in names.items():
        nn = norm(n)
        if nn in nt:
            out.append((n, rows, 1.0)); continue
        words = [w for w in re.split(r"[^a-z0-9']+", n.lower()) if len(w) >= 3]
        if len(words) >= 2:
            if all(_close(w, toks) for w in words):
                out.append((n, rows, 0.8))
    return out


_CACHE = {}


def _close(w, toks):
    if w in toks:
        return True
    for t in toks:
        if abs(len(t) - len(w)) > 2:
            continue
        k = (w, t)
        r = _CACHE.get(k)
        if r is None:
            sm = difflib.SequenceMatcher(None, w, t)
            r = _CACHE[k] = sm.quick_ratio() >= 0.8 and sm.ratio() >= 0.8
        if r:
            return True
    return False


def clusters(frames, tol, min_frames):
    cand = []
    for t, text in frames:
        for m in TIMER.finditer(text):
            rem = int(m.group(1)) * 60 + int(m.group(2))
            if 3 <= rem <= 3600:
                cand.append((t, rem))
    # drop static readings (system clock, chat timestamps, quest timers): a value that is read in many frames of the same
    # neighbourhood does not count down; a real countdown value is visible for one second only
    times = sorted({t for t, _ in frames})
    step = min([b - a for a, b in zip(times, times[1:]) if b > a] or [2.0])
    limit_n = max(2, int(1.5 / step) + 1)
    by_val = collections.defaultdict(list)
    for t, rem in cand:
        by_val[rem].append(t)
    keep = []
    for t, rem in cand:
        near = {x for x in by_val[rem] if abs(x - t) <= 12}
        if len(near) <= limit_n:
            keep.append((t, rem))
    cand = keep
    buckets = collections.defaultdict(list)
    for t, rem in cand:
        buckets[round((t + rem) / tol)].append((t, rem))
    merged = []
    for k, lst in buckets.items():
        lst = lst + buckets.get(k + 1, [])
        if len({t for t, _ in lst}) >= min_frames:
            merged.append(sorted(set(lst)))
    merged.sort(key=len, reverse=True)
    out, used = [], set()
    for c in merged:
        key = {t for t, _ in c}
        if key & used and len(key - used) < min_frames:
            continue
        used |= key
        out.append(c)
    return out


def main():
    global NAMES
    a = sys.argv[1:]
    min_frames = int(a[a.index("--min-frames") + 1]) if "--min-frames" in a else 4
    tol = float(a[a.index("--tol") + 1]) if "--tol" in a else 2.0
    csv_path = a[a.index("--csv") + 1] if "--csv" in a else None
    only = set(int(x) for x in a[a.index("--only") + 1].split(",")) if "--only" in a else None
    srcdir = a[a.index("--dir") + 1] if "--dir" in a else OUT
    NAMES = load_names()
    flagged_ids = {o["id"] for l in NAMES.values() for o in l if o["tt"]}
    con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
    byv = collections.defaultdict(dict)
    for fn in glob.glob(os.path.join(srcdir, "*.jsonl")):
        for line in open(fn, encoding="utf-8"):
            try:
                d = json.loads(line)
            except ValueError:
                continue
            byv[d["v"]][d["t"]] = (d["w"], d["b"])
    rows = []
    for vid in sorted(byv):
        if only and vid not in only:
            continue
        fr = sorted(byv[vid].items())
        frames = [(t, w + " " + b) for t, (w, b) in fr]
        times = [t for t, _ in frames]
        info = con.execute("select title, upload_date, url from video where id=?", (vid,)).fetchone() or ("", "", "")
        step = min([b - a_ for a_, b in zip(times, times[1:]) if b - a_ > 0] or [2.0])
        for cl in clusters(frames, tol, min_frames):
            t_first, rem_first = cl[0]
            tset = {t for t, _ in cl}
            prev = [t for t in times if t < t_first and t not in tset]
            t_prev = max(prev) if prev else 0.0
            ban = [t for t, (w, b) in fr if BANNER.search(w + " " + b) and t <= t_first + 1 and t_first - t < 15]
            gap = t_first - (max(ban) if ban else t_prev)
            lo, hi = cl[0][0] - 12, cl[-1][0] + 12
            span = [(t, w) for t, w in frames if lo <= t <= hi]
            blob = " ".join(w for t, w in span)
            blobw = " ".join(byv[vid][t][0] for t, _ in span)
            hits = match_names(blob, NAMES, blobw)
            # threshold tokens beside the timer
            tc = collections.Counter()
            for t, w in span:
                main = {f"{m.group(1)}:{m.group(2)}" for m in TIMER.finditer(w)}
                for m in THR.finditer(w):
                    tok = m.group(1)
                    tc[tok] += 1
            nsp = max(1, len(span))
            thr = [f"{k}x{v}" for k, v in tc.most_common(8) if v >= max(2, nsp // 4) and re.match(r"^\d?:\d\d$", k)]
            nm = ";".join(f"{n}{'*' if any(o['id'] in flagged_ids for o in rs) else ''}#{'/'.join(str(o['id']) for o in rs)}{'' if q == 1.0 else '~'}" for n, rs, q in hits)
            rows.append(dict(video=vid, date=info[1], url=info[2], title=info[0][:60], n=len(cl), step=step, t_first=t_first, rem_first=rem_first,
                             limit_low=max(r for _, r in cl), gap=round(gap, 1), banner=bool(ban),
                             last_t=cl[-1][0], names=nm, thr=",".join(thr)))
    for r in rows:
        print(f"{r['video']:>5} {r['date']} n={r['n']:>3} first@{r['t_first']:>6.1f} rem={r['rem_first']:>4} lim>={r['limit_low']:>4}(+{r['gap']}) "
              f"{'B' if r['banner'] else ' '} {r['names'][:60]:60} thr[{r['thr']}] | {r['title'][:50]}")
    if csv_path and rows:
        with open(csv_path, "w", newline="", encoding="utf-8") as f:
            w = csv.DictWriter(f, fieldnames=list(rows[0].keys())); w.writeheader(); w.writerows(rows)
    print(len(rows), "clusters in", len({r['video'] for r in rows}), "videos")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    main()
