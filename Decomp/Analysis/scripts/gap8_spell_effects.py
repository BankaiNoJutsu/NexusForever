"""Gap 8: list all Spell4Effects rows (any type) for given spell ids, with prerequisite columns (read-only)."""
import struct, sys
sys.path.insert(0, r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts')
sys.stdout.reconfigure(encoding='utf-8')
exec(open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('want = ')[0])
sp = {int(x) for x in sys.argv[1:]}
for i in range(ne):
    r = struct.unpack_from('<%dI' % (rse//4), e, be + i*rse)
    if r[1] in sp:
        print('spell=%d "%s" eff=%d type=0x%X tf=%d dur=%d fl=%d db=%s prq=%s' % (r[1], spname(r[1]), r[0], r[3], r[2], r[7], r[8], [hex(x) if x>9999 else x for x in r[9:15]], r[29:34]))
