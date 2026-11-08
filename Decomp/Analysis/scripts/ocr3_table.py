r"""ocr3: build the per-challenge table (all 80 flagged ids) for CHALLENGE_TIME_TIERED_OCR_PASS3_20261108.md from section 6 of the
trigger census plus the readings below.  py -3.13 -I ocr3_table.py > I:\tools\ocr3\table.md"""
import re, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
CENSUS = r"I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\CHALLENGE_TIME_TIERED_TRIGGER_CENSUS_20261108.md"
R = {  # id: (limit label, videos, what was read)
    1002: ("120 s EXACT (pass 2)", "585 2014-06-05 IdrR35AkrpI; 2464; 3433; 3464; 6764 2018-06-07", "first visible 1:58 / 1:57 (pass 2)"),
    956: ("65 s EXACT (pass 2)", "1731 2015-01-25; 5343 2014-06-27", "first visible 1:04 (pass 2)"),
    979: ("1200 s EXACT (pass 2)", "1823 2014-06-16; 1828 2014-06-11; 4284 2014-09-29", "19:59 / 19:58 first visible (pass 2)"),
    273: ("360 s EXACT (pass 2, two videos)", "1691 2014-07-06; 6498 2014-05-14", "5:58 / 5:59 first visible (pass 2)"),
    981: ("unknown (same name and goals as 979; no clip separates the two ids)", "none attributable", "see 979"),
    753: ("80 s (pass 1/2, five videos)", "1886, 1932, 1969, 2471, 2992", "first values 78-79 (pass 2)"),
    511: ("60 s EXACT (pass 2, one video)", "6641 2014-04-23 Pb88yMDWcoI", "0:58 first visible at 2 s (pass 2)"),
    743: ("120 s PROBABLE (upgraded)", "1887 2014-06-22 2ApdFoFpGoo", "pass 3: widget absent at 31 s, 01:59 at 32 s, 01:58 at 33 s, 01:57 at 33.5-34 s; first visible = limit minus 1"),
    67: ("45 s PROBABLE", "4863 2014-10-04 pTfaRRfZWcc", "pass 3: widget absent at 947.7 s, faint 00:44 at 947.9 s, 00:44 at 948-948.4 s, 00:43 at 949 s (title Windspire Ascent on the widget)"),
    923: ("600 s PROBABLE (>= 599)", "6474 2014-06-18 kqEpcSwGunc (FABkit video, real challenge on screen)", "pass 3: widget absent at 14.0 s, 09:59 at 14.5 s, 09:58 at 15-15.5 s, 09:57 at 16 s"),
    609: ("30 s PROBABLE (>= 29)", "6015 2014-06-22 l_WvtM682VY", "pass 3: widget absent at 124.0 s, 00:29 at 124.5-125 s, 00:28 at 125.5-126 s (title Rock Climber)"),
    680: ("112 s PROBABLE (111 to 113; NOT 120)", "6793 2014-06-27 YISTZ4z37l4", "pass 3: Codex Start clicked t=31 s, distance to start 14 m, 10, 6, 3, 1 m at t=30-33 s; widget absent at 35.0 s, 01:51 at 35.5 s, 01:50 at 37 s, 01:27 at 60 s, Complete card at 74 s"),
}
rows = []
txt = open(CENSUS, encoding="utf-8").read()
sec = txt.split("## 6. Per-challenge table")[1].split("## 7.")[0]
for l in sec.splitlines():
    m = re.match(r"\| (\d+) \| (.*?) \| (\d) \| (\w) \| ([\w-]+) \| (.*?) \|", l)
    if m:
        rows.append(m.groups())
print(f"| Id | Challenge | Tier goals (s) | Limit | Videos (id, upload date, YouTube id) | What was read |")
print("|---|---|---|---|---|---|")
for cid, name, ty, mech, prod, tiers in rows:
    cid = int(cid)
    lim, vids, what = R.get(cid, ("unknown", "none found", "no retail clip with a readable widget in the library"))
    print(f"| {cid} | {name or '(no text)'} | {tiers} | {lim} | {vids} | {what} |")
print(len(rows), "rows", file=sys.stderr)
