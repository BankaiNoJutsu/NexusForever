import collections
p = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\function_labels.csv'
base = open(p, 'rb').read()
gen = open(r'I:\tools\lua_labels_generated.csv', 'rb').read()
names = collections.Counter(l.split(b',')[2] for l in gen.split(b'\r\n') if l)
dups = {n: c for n, c in names.items() if c > 1}
print('rows', len(names), 'duplicate names', len(dups), list(dups.items())[:8])
hdr = b'# Lua method-table pass (2026-11-08): WildStar64.exe functions named from the client Lua registration tables (see LABEL_COVERAGE_PASS_LUA_TABLES_20261108.md). Automatic labels; the class is resolved from registrar strings when confident, else Lua_T<table address>.\r\n'
if hdr in base:
    raise SystemExit('already appended')
open(p, 'wb').write(base + (b'' if base.endswith(b'\n') else b'\r\n') + hdr + gen)
print('appended')
