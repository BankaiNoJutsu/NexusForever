import re, collections, sys
sys.stdout.reconfigure(encoding='utf-8')
vals = {}
for l in open(r'I:\tools\prereq_values.tsv', encoding='utf-8'):
    pid, rest = l.rstrip('\n').split('\t')
    slots = []
    for s in rest.split(' ; '):
        m = re.match(r't(\d+) c(\d+) o(\d+) v(\d+)', s)
        slots.append(tuple(int(x) for x in m.groups()))
    vals[int(pid)] = slots
SERVER = {('Spell4', c) for c in ['PrerequisiteIdCasterCast', 'PrerequisiteIdTargetCast', 'PrerequisiteIdAoeTarget', 'PrerequisiteIdAoePreferredTarget', 'PrerequisiteIdCasterPersistence', 'PrerequisiteIdTargetPersistence']} | \
         {('Spell4Effects', c) for c in ['PrerequisiteIdCasterApply', 'PrerequisiteIdTargetApply', 'PrerequisiteIdCasterPersistence', 'PrerequisiteIdTargetPersistence']} | \
         {('Creature2', 'PrerequisiteIdVisibility'), ('Creature2', 'PrerequisiteIdActivateSpell00'), ('Achievement', 'PrerequisiteIdObjective'), ('CommunicatorMessages', 'PrerequisiteId')}
def cmp(c, a, b):
    return {1: a == b, 2: a != b, 3: a > b, 4: a >= b, 5: a < b, 6: a <= b}.get(c, False)
def flag(c, f):
    return f if c == 1 else (not f) if c == 2 else False
def old(t, c, v):
    return cmp(c, 0, v) if t == 24 else cmp(c, 1, v)       # entity phase 0; 25: caster and target share a phase -> 1
def new(t, c, v):
    return flag(c, ((1 >> (v & 31)) & 1) != 0)               # default mask 1
refs = collections.defaultdict(set)
for l in open(r'I:\tools\prereq_refs_detail.tsv', encoding='utf-8'):
    t, tab, col, eid, pid = l.rstrip('\n').split('\t')
    if int(t) in (24, 25) and (tab, col) in SERVER:
        refs[(int(t), tab, col)].add((int(eid), int(pid)))
tot = collections.Counter()
flips = collections.defaultdict(list)
for (t, tab, col), s in sorted(refs.items()):
    for eid, pid in s:
        for (st, c, o, v) in vals[pid]:
            if st != t: continue
            o_, n_ = old(t, c, v), new(t, c, v)
            tot[(t, o_, n_)] += 1
            if o_ != n_:
                flips[(t, tab, col)].append((eid, pid, c, v, o_, n_))
print('server-evaluated slot references (type, old, new):', dict(tot))
for k, v in flips.items():
    print(k, len(v), 'e.g.', v[:3])
print()
# which 24/25 slots exist per value and comparison (all prerequisites)
dist = collections.Counter()
for pid, slots in vals.items():
    for (st, c, o, v) in slots:
        if st in (24, 25): dist[(st, c, v)] += 1
print('24/25 slot distribution (type, comparison, value) -> count:')
for k, v in sorted(dist.items()): print('  ', k, v)
