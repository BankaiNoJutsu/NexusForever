import sqlite3, sys
sys.stdout.reconfigure(encoding='utf-8')
db = sqlite3.connect(r'I:\tools\video_index\corpus.db')
for vid in [int(a) for a in sys.argv[1:]]:
    v=db.execute("select title,url,upload_date from video where id=?",(vid,)).fetchone()
    print('=====',vid,v)
    rows=db.execute("select start,text from cue where video_id=? order by start",(vid,)).fetchall()
    out=[]; prev=''
    for s,t in rows:
        t=t.strip()
        # rolling captions: keep only the last line of the cue text
        last=t.split('\n')[-1]
        if last!=prev: out.append((int(s),last)); prev=last
    print(' '.join(f"[{s}]{l}" for s,l in out))
