r"""ocr2: for each cluster in a clusters csv (ocr2_analyze.py --csv) print the widget-crop OCR text of the cluster's second frame,
to read the challenge title next to the countdown.   py -3.13 -I ocr2_snip.py clusters.csv [--notin-a]"""
import csv, json, glob, os, sys, re
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
OUT = r"I:\tools\video_index\ocr2"
rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8')))
A = {v['id'] for v in json.load(open(r'I:\tools\video_index\challenge_videos.json', encoding='utf-8'))}
fr = {}
for fn in glob.glob(os.path.join(OUT, '*.jsonl')):
    for l in open(fn, encoding='utf-8'):
        d = json.loads(l); fr[(d['v'], round(d['t'], 1))] = d
for r in rows:
    v = int(r['video']); t0 = float(r['t_first']); st = float(r['step'])
    if '--notin-a' in sys.argv and v in A: continue
    best = None
    for k in (1, 2, 3):
        d = fr.get((v, round(t0 + k * st, 1)))
        if d and d['w'].strip(): best = d; break
    txt = re.sub(r'\s+', ' ', (best or {}).get('w', ''))[:150]
    print(f"{v:>5} {r['date']} @{t0:>7} rem={r['rem_first']:>4} n={r['n']:>3} | {txt}")
