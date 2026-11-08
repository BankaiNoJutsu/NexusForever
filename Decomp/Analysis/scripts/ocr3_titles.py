r"""ocr3: list dated retail videos (upload before 2018-11-28) NOT in the 160-video challenge set whose title or description contains a
speed/climb/race style keyword (candidate footage of time-tiered General challenges that no earlier job read whole).
Prints id, date, duration, subtitle flag, title; writes I:\tools\ocr3\title_candidates.json as [{v, a, b, step}] whole-video jobs for those <= 900 s.
  py -3.13 -I ocr3_titles.py"""
import json, re, sqlite3, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
A = {v['id'] for v in json.load(open(r'I:\tools\video_index\challenge_videos.json', encoding='utf-8'))}
import glob, os
read = set()
for fn in glob.glob(r"I:\tools\video_index\ocr2\*.done"):
    read.add(int(os.path.basename(fn).split('_')[0]))
KW = re.compile(r"\b(race|racer|sprint|climb|climber|climbing|dash|speedrun|speed run|parkour|hoverboard|jump|jumping|timed|gold medal|gold run|silver|bronze|time trial|ascent|runner|windwalker|treeclimber|stairway|roof|wind|vista)\b", re.I)
EXC = re.compile(r"housing|decor|raid|dungeon|adventure|warplot|arena|battleground|pvp|datascape|genetic|stormtalon|skullcano|kel voreth|sanctuary|redmoon|ultimate protogames|housing", re.I)
rows = con.execute("select id, upload_date, duration_s, has_subs, title, substr(description,1,200) from video where upload_date<'20181128' and upload_date!=''").fetchall()
jobs = []
for i, d, dur, hs, t, desc in rows:
    if i in A or i in read:
        continue
    if KW.search(t) and not EXC.search(t):
        print(f"{i:>5} {d} {int(dur or 0):>6}s subs={hs} {t[:90]}")
        if 0 < (dur or 0) <= 900:
            jobs.append(dict(v=i, a=0, b=dur, step=2.0))
json.dump(jobs, open(r"I:\tools\ocr3\title_candidates.json", "w"))
print(len(jobs), "whole-video jobs", sum(j['b'] for j in jobs) / 3600, "hours")
