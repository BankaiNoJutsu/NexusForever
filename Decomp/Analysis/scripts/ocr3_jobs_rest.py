r"""ocr3: job list of the set-3 windows (jobs3.json from ocr2_jobs3.py) that pass 2 had not read yet (no .done file),
ordered 2015, 2016, 2017, 2018, 2013, 2012, 2011 -> I:\tools\ocr3\jobs_rest.json
  py -3.13 -I ocr3_jobs_rest.py
Then run:  py -3.13 -I ocr2_run.py I:\tools\ocr3\jobs_rest.json --workers 3   (with TEMP/TMP set to I:\tools\claude-tmp)"""
import json, os, sqlite3, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
OUT = r"I:\tools\video_index\ocr2"
os.makedirs(r"I:\tools\ocr3", exist_ok=True)
con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
jobs = json.load(open(os.path.join(OUT, "jobs3.json"), encoding="utf-8"))
ORDER = {"2015": 0, "2016": 1, "2017": 2, "2018": 3, "2013": 4, "2012": 5, "2011": 6, "2014": 7}
rem = []
for j in jobs:
    name = f"{j['v']}_{int(j['a'])}_{int(j['b'])}"
    if os.path.exists(os.path.join(OUT, name + ".done")):
        continue
    d = con.execute("select upload_date from video where id=?", (j["v"],)).fetchone()[0]
    rem.append((ORDER.get(d[:4], 9), d, j))
rem.sort(key=lambda x: (x[0], x[1], x[2]["v"], x[2]["a"]))
json.dump([r[2] for r in rem], open(r"I:\tools\ocr3\jobs_rest.json", "w"))
print(len(rem), "windows", sum(r[2]["b"] - r[2]["a"] for r in rem) / 3600, "hours")
