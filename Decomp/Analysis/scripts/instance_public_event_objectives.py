import struct, sys
sys.stdout.reconfigure(encoding='utf-8')
src = open(r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\scripts\gap8_effect_rows.py', encoding='utf-8').read().split('def table')[0]
exec(src)
def table(name):
    d = open(r'I:\tools\extract\DB\%s.tbl' % name, 'rb').read()
    return d, struct.unpack_from('<Q', d, 24)[0], struct.unpack_from('<Q', d, 48)[0], 0x60 + struct.unpack_from('<Q', d, 64)[0]
d, rs, n, b = table('PublicEventObjective')
print('record size', rs, 'words', rs//4, 'rows', n)
fields = ['Id','PublicEventId','Flags','TypeSpecificFlags','WorldLocation2Id','TeamId','TextId','TextIdOther','TextIdShort','TextIdOtherShort','TypeEnum','Count','ObjectId','FailureTimeMs','TargetGroupIdRewardPane','CategoryEnum','LiveEventIdCounter','ParentId','QuestDirectionId','MedalPointValue','TextIdParticipantAdd','TextIdStart','DisplayOrder']
for i in range(n):
    r = struct.unpack_from('<%dI' % (rs // 4), d, b + i * rs)
    if r[1] == 446:
        nm = text(r[6]) if r[6] in tx else ''
        print(f"{r[0]:5d} type={r[10]:3d} count={r[11]:3d} failMs={r[13]:7d} medal={r[19]:4d} order={r[22]:3d} parent={r[17]:5d} | {nm[:60]}")
