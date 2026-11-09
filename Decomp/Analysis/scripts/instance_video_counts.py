import sqlite3, sys
sys.stdout.reconfigure(encoding='utf-8')
db = sqlite3.connect(r'I:\tools\video_index\corpus.db')
names = ['Stormtalon','Kel Voreth','Skullcano','Sanctuary of the Swordmaiden','Protogames','Coldblood','Genetic Archives','Datascape','Redmoon Terror','Halls of the Bloodsworn','Infinite Generator','Ohmna','Avatus','Laveka','Kuralak','Maelstrom','Gauntlet']
for n in names:
    q = '"%s"' % n
    try:
        r = db.execute("select count(*), sum(has_subs), min(upload_date), max(upload_date) from video where rowid in (select rowid from meta_fts where meta_fts match ?) and upload_date < '20181128' and upload_date <> ''", (q,)).fetchone()
    except Exception as e:
        r = str(e)
    print(n, r)
