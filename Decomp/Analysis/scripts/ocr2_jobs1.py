"""ocr2: job list 1 = the 160 dated 'challenge' videos minus housing CBDC / Wildman playthroughs, whole video at 2 s."""
import json, sys
A = json.load(open(r'I:\tools\video_index\challenge_videos.json', encoding='utf-8'))
jobs = []
for v in A:
    t = v['title']
    if 'Decor' in t or 'Wildman' in t or 'Wildman' in v['file'] or 'CBDC' in t or 'Housing: Tours' in t or 'Housing:' in t and v['dur'] > 900:
        continue
    dur = float(v['dur'] or 0) or 300.0
    jobs.append(dict(v=v['id'], a=0, b=dur, step=2.0 if dur <= 1800 else 3.0))
json.dump(jobs, open(r'I:\tools\video_index\ocr2\jobs1.json', 'w'))
print(len(jobs), sum(j['b'] for j in jobs) / 3600, 'hours', sum(j['b'] / j['step'] for j in jobs), 'frames')
