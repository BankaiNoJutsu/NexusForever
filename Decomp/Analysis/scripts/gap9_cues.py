"""Print deduped subtitle cues for a corpus id in a window: py -3.13 -I gap9_cues.py <id> <t0> <t1>"""
import sqlite3, sys
sys.stdout.reconfigure(encoding="utf-8")
db = sqlite3.connect(r"I:\tools\video_index\corpus.db")
vid, a, b = int(sys.argv[1]), float(sys.argv[2]), float(sys.argv[3])
last = ""
out = []
for s, t in db.execute("select start,text from cue where video_id=? and start between ? and ? order by start", (vid, a, b)):
    # rolling captions: print only the new tail
    if last and t.startswith(last[-30:]) : pass
    out.append((int(s), t))
seen = set(); 
for s, t in out:
    if t in seen: continue
    seen.add(t); print(s, t[:160])
