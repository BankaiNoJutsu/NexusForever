r"""ocr3: look for retail videos (upload date before 2018-11-28) whose title/description or subtitle cues mention
variants of the still-unread flagged challenge names. Prints video id, date, title, cue second.
  py -3.13 -I ocr3_find_names.py"""
import sqlite3, sys
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
con = sqlite3.connect(r"I:\tools\video_index\corpus.db")
Q = {
    "Treeclimber": ['treeclimber', '"tree climber"', 'treeclimbers'],
    "Windrunner": ['"wind runner"', 'windrunner'],
    "Stairway to Heaven": ['"stairway to heaven"', 'stairway'],
    "Powering Down": ['"power down" AND challenge', '"powering down" AND challenge', '"powering down" AND scanbot', '"power down" AND scanbot', '"powering down" AND generator'],
    "Windwalker": ['windwalker'],
    "To the Roof": ['"to the roof" AND challenge'],
    "Race to the Top": ['"race to the top" AND challenge'],
    "Diabolic Drift Dash": ['"diabolic drift"', '"drift dash"'],
    "Windspire Ascent": ['windspire'],
    "Grounding the Storm": ['"grounding the storm"', '"weather control" AND challenge'],
    "Rock Climber": ['"rock climber"'],
    "Entrance Exam": ['"entrance exam"'],
    "Race to the Unknown": ['"race to the unknown"'],
    "Ravenok Racer": ['"ravenok racer"', '"ravenok race"'],
    "Squirg Run": ['"squirg run"'],
    "Windwalker2": ['"wind walker"'],
    "Windy Climb": ['"windy climb"'],
    "Whirlwind Racer": ['"whirlwind racer"'],
    "Jungle Dash": ['"jungle dash"'],
    "Dockside Delivery": ['"dockside delivery"'],
    "Supply Run": ['"supply run" AND challenge'],
    "Honey Samples": ['"honey samples"'],
    "Resonance Runner": ['"resonance runner"'],
    "Murgh Along": ['"murgh along"'],
    "Peep Chase": ['"peep chase"'],
    "Faster and Fearless": ['"faster and fearless"'],
    "One Small Hop": ['"one small hop"'],
    "Owanee Speedway": ['"owanee speedway"'],
    "Scaling Mount Tempest": ['"mount tempest"'],
    "Eldan Wall Scaler": ['"wall scaler"'],
    "Red Light Green Light": ['"red light green light"'],
    "Splashin' Dash": ['"splashin dash"', '"splashin\'"'],
    "Kickin It": ['"kickin it" AND challenge'],
    "Surveying Device": ['"surveying device"'],
    "Veggie Delight": ['"veggie delight"'],
    "Sandcastle Critique": ['"sandcastle critique"'],
    "Getting a Better Signal": ['"better signal"'],
    "Cellar Dwellers": ['"cellar dwellers"'],
    "Pulp-Gulp": ['"pulp gulp"', 'guzzler'],
    "Just Keep Swinging": ['"just keep swinging"'],
    "Another Sassy Mess": ['"sassy mess"', '"scruffy mess"'],
    "Diagnostic Tests": ['"diagnostic tests"'],
    "Can You Dig It": ['"can you dig it"'],
    "How to Save a Life": ['"how to save a life"'],
    "Contents Under Pressure": ['"contents under pressure"'],
    "XT-9 Hazardous Hallway": ['"hazardous hallway"'],
    "Broodrot Barnstorming": ['barnstorming'],
    "Mudwalker Ascent": ['"mudwalker ascent"'],
    "Climbing the Pyre": ['"climbing the pyre"'],
    "Skeech Saboteur": ['"skeech saboteur"'],
    "Phantasmal Flora": ['"phantasmal flora"'],
    "Shock to Your Systems": ['"shock to your systems"'],
    "Swiftfoot Supplies": ['"swiftfoot supplies"'],
    "View From a Hill": ['"view from a hill"'],
    "To the High Flame": ['"to the high flame"'],
    "Getting": ['"race to the xas"', '"xas dig site"'],
}
for name, qs in Q.items():
    seen = set()
    for q in qs:
        rows = con.execute("select v.id, v.upload_date, v.title, v.duration_s from meta_fts f join video v on v.id=f.rowid where meta_fts match ? and v.upload_date<'20181128' and v.upload_date!=''", (q,)).fetchall()
        for r in rows:
            if ('m', r[0]) not in seen:
                seen.add(('m', r[0])); print(f"{name:24} META {r[0]:>5} {r[1]} {int(r[3] or 0):>6}s {r[2][:70]}")
        rows = con.execute("select v.id, v.upload_date, v.title, c.start, substr(c.text,1,70) from cue_fts f join cue c on c.rowid=f.rowid join video v on v.id=c.video_id where cue_fts match ? and v.upload_date<'20181128' and v.upload_date!='' order by v.upload_date limit 12", (q,)).fetchall()
        for r in rows:
            if ('c', r[0], int(r[3] // 20)) not in seen:
                seen.add(('c', r[0], int(r[3] // 20))); print(f"{name:24} CUE  {r[0]:>5} {r[1]} @{int(r[3]):>5}s {r[2][:40]} | {r[4]}")
