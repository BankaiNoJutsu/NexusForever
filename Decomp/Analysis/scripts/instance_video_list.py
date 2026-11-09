import sqlite3, sys
sys.stdout.reconfigure(encoding='utf-8')
db = sqlite3.connect(r'I:\tools\video_index\corpus.db')
cols=[r[1] for r in db.execute("pragma table_info(video)")]
print(cols)
for n in ['Blade-Wind','Blade Wind','Aethros','Stormtalon']:
    rows = db.execute("select id,upload_date,title,url,has_subs,duration_s from video where rowid in (select rowid from meta_fts where meta_fts match ?) and upload_date<'20181128' and upload_date<>'' order by upload_date", ('"%s"'%n,)).fetchall()
    print('==',n,len(rows))
    for r in rows[:40]: print(r[0],r[1],r[2][:70],r[4],r[5])
