"""ocr2: job list 3 = windows (+-W s, merged) around subtitle cues that say 'challenge' in dated videos outside the 160-video set and outside jobs2
(dungeon/raid/adventure/PvP titles excluded); step 4 s. Detection pass only; hits are refined at 1 s afterwards."""
import sqlite3, json, sys, collections, re
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
W = float(sys.argv[1]) if len(sys.argv) > 1 else 50
STEP = float(sys.argv[2]) if len(sys.argv) > 2 else 4
con = sqlite3.connect(r'I:\tools\video_index\corpus.db')
A = {v['id'] for v in json.load(open(r'I:\tools\video_index\challenge_videos.json', encoding='utf-8'))}
EXC = re.compile(r'veteran|dungeon|raid|adventure|arena|battleground|warplot|housing|decor|datascape|genetic|stormtalon|skullcano|kel voreth|sanctuary|ultimate protogames|redmoon|shiphand|pvp|gladiator|hoverboard|ship', re.I)
rows = con.execute("select c.video_id,c.start,v.title,v.duration_s from cue_fts f join cue c on c.rowid=f.rowid join video v on v.id=c.video_id where cue_fts match 'challenge OR challenges' and v.upload_date<'20181128' and v.upload_date!=''").fetchall()
by = collections.defaultdict(list); dur = {}; title = {}
for v, s, t, d in rows:
    if v in A or EXC.search(t): continue
    by[v].append(s); dur[v] = d or 0; title[v] = t
jobs = []
for v, l in by.items():
    l.sort(); w = []
    for s in l:
        if w and s - W <= w[-1][1]: w[-1][1] = s + W
        else: w.append([max(0, s - W), s + W])
    for a, b in w:
        jobs.append(dict(v=v, a=a, b=min(b, dur[v]) if dur[v] else b, step=STEP))
json.dump(jobs, open(r'I:\tools\video_index\ocr2\jobs3.json', 'w'))
print(len(by), 'videos', len(jobs), 'windows', sum(j['b'] - j['a'] for j in jobs) / 3600, 'hours', sum((j['b'] - j['a']) / STEP for j in jobs), 'frames')
