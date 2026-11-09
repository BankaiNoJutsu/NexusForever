import struct, sys, os, glob
sys.stdout.reconfigure(encoding='utf-8')
ids = {int(a) for a in sys.argv[1:]}
for path in sorted(glob.glob(r'I:\tools\extract\DB\*.tbl')):
    d = open(path,'rb').read()
    if len(d) < 80: continue
    rs = struct.unpack_from('<Q', d, 24)[0]; n = struct.unpack_from('<Q', d, 48)[0]; b = 0x60 + struct.unpack_from('<Q', d, 64)[0]
    if rs == 0 or rs % 4 or b + rs*n > len(d): continue
    for i in range(n):
        base = b + i*rs
        for c in range(rs//4):
            v = struct.unpack_from('<I', d, base + c*4)[0]
            if v in ids:
                print(os.path.basename(path), 'rec', i, 'id', struct.unpack_from('<I', d, base)[0], 'col', c, 'text', v)
