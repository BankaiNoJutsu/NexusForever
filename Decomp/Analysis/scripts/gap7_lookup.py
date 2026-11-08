import sqlite3,sys
sys.stdout.reconfigure(encoding="utf-8")
con=sqlite3.connect(r"I:\tools\video_index\corpus.db")
ids=[int(x) for x in sys.argv[1:]]
for i in ids:
    print(con.execute("select id,file,title,upload_date,url,duration_s,has_subs from video where id=?",(i,)).fetchall())
