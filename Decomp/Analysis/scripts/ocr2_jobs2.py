"""ocr2: job list 2 = windows (+-70 s, merged) around subtitle cues of flagged challenge names in dated videos outside the 160-video set;
videos whose title/description mention a flagged name (META hits) are read whole when shorter than 15 min (step 2) else skipped."""
import json, sqlite3, collections, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
con = sqlite3.connect(r'I:\tools\video_index\corpus.db')
A = {v['id'] for v in json.load(open(r'I:\tools\video_index\challenge_videos.json', encoding='utf-8'))}
H = json.load(open(r'I:\tools\video_index\ocr2\cue_hits.json', encoding='utf-8'))
cues = collections.defaultdict(list); meta = {}
for n, l in H.items():
    for h in l:
        if h['vid'] in A: continue
        if h['t'] is None: meta[h['vid']] = (n, h['title'])
        else: cues[h['vid']].append((h['t'], n))
jobs = []
for v, l in cues.items():
    dur = con.execute('select duration_s from video where id=?', (v,)).fetchone()[0] or 0
    l.sort(); w = []
    for s, n in l:
        if w and s - 70 <= w[-1][1]: w[-1][1] = s + 70
        else: w.append([max(0, s - 70), s + 70])
    for a, b in w:
        jobs.append(dict(v=v, a=a, b=min(b, dur) if dur else b, step=2.0))
for v, (n, title) in meta.items():
    if v in cues: continue
    dur = con.execute('select duration_s from video where id=?', (v,)).fetchone()[0] or 0
    if 0 < dur <= 900 and 'Housing' not in title:
        jobs.append(dict(v=v, a=0, b=dur, step=2.0))
    print('META', v, int(dur), n, title[:60])
json.dump(jobs, open(r'I:\tools\video_index\ocr2\jobs2.json', 'w'))
print(len(jobs), sum(j['b'] - j['a'] for j in jobs) / 3600, 'hours')
