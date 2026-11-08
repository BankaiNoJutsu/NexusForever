import sqlite3,sys
sys.stdout.reconfigure(encoding="utf-8")
con=sqlite3.connect(r"I:\tools\video_index\corpus.db")
q=sys.argv[1]
rows=con.execute("select v.id,v.upload_date,v.duration_s,v.title,c.start,c.text,v.file from cue_fts f join cue c on c.rowid=f.rowid join video v on v.id=c.video_id where cue_fts match ? order by v.id,c.start",(q,)).fetchall()
last=None
for r in rows:
    print(r[0],r[1],int(r[2]),r[3][:70],int(r[4]),'|',r[5][:110])
