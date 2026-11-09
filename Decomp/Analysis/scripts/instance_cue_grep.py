import sqlite3, sys, re
sys.stdout.reconfigure(encoding='utf-8')
db = sqlite3.connect(r'I:\tools\video_index\corpus.db')
pat=re.compile(sys.argv[1], re.I)
for vid in [int(a) for a in sys.argv[2:]]:
    v=db.execute("select title,url,upload_date from video where id=?",(vid,)).fetchone()
    print('==',vid,v[0][:60],v[2],v[1])
    seen=set()
    for s,t in db.execute("select start,text from cue where video_id=? order by start",(vid,)):
        last=t.strip().split('\n')[-1]
        if pat.search(last) and last not in seen:
            seen.add(last); print('  ',int(s),last[:140])
