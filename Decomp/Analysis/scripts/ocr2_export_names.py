r"""ocr2: export all Challenge.tbl rows (id, name, flag, tier goals) to I:\tools\video_index\ocr2\all_challenges.json (read-only on the extracted tables)."""
import struct, json, sys
def text():
    d = open(r'I:\tools\extract_en\en-US.bin', 'rb').read()
    h = struct.unpack_from('<IIII10Q', d, 0)
    rc, ro, sl, so = h[10], h[11], h[12], h[13]
    t = {}
    for i in range(rc):
        i_d, off = struct.unpack_from('<II', d, 96 + ro + i * 8)
        s = 96 + so + off * 2
        e = s
        while d[e:e + 2] != b'\0\0' or (e - s) % 2:
            e += 2 if (e - s) % 2 == 0 else 1
        t[i_d] = d[s:e].decode('utf-16le')
    return t
def tbl(name):
    d = open(rf'I:\tools\extract\DB\{name}.tbl', 'rb').read()
    (m, v, nl, u1, rs, fc, fo, rc, ts, ro, mx, lo, u2) = struct.unpack_from('<IIQQQQQQQQQQQ', d, 0)
    return [struct.unpack_from('<%dI' % (rs // 4), d, 0x60 + ro + i * rs) for i in range(rc)]
T = text(); ch = tbl('Challenge'); tid = {r[0]: r for r in tbl('ChallengeTier')}
out = []
for r in ch:
    gs = [tid[c][1] for c in (r[10], r[11], r[12]) if c in tid]
    out.append(dict(id=r[0], type=r[1], flags=r[3], name=T.get(r[13], ""), goals=gs, tt=bool(r[3] & 8)))
json.dump(out, open(r'I:\tools\video_index\ocr2\all_challenges.json', 'w', encoding='utf-8'), indent=0, ensure_ascii=False)
print(len(out), sum(1 for o in out if o['tt']), sum(1 for o in out if o['name']))
