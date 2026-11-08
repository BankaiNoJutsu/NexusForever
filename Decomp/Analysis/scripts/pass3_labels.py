import re, csv, sys, collections
sys.stdout.reconfigure(encoding='utf-8')
LABELS = r'I:\GIT\NexusForever-unified-20261106\Decomp\Analysis\function_labels.csv'

# ---- part A: resolve two Lua tables to Game.ItemData -------------------------------------------------
renames = {}   # addr -> (old prefix, new name, comment)
lines = open(LABELS, 'rb').read().split(b'\r\n')
existing_names = collections.Counter()
for l in lines:
    p = l.split(b',', 3)
    if len(p) >= 3 and p[0] == b'WildStar64.exe':
        existing_names[p[2].decode()] += 1
for l in lines:
    p = l.split(b',', 3)
    if len(p) < 4 or p[0] != b'WildStar64.exe':
        continue
    name = p[2].decode()
    for table in ('140b66970', '140c58c00'):
        pre = 'Lua_T%s_' % table
        if name.startswith(pre):
            method = name[len(pre):]
            new = 'Lua_GameItemData_' + method
            renames[p[1].decode()] = (name, new, 'Lua method table %s entry %s; class Game.ItemData (registrar string Game.ItemData; method names match the existing Lua_GameItemData_* labels); automatic table-scan labelling pass.' % (table, method))
clash = [a for a, (_, new, _) in renames.items() if existing_names[new] > 0]
print('table rows renamed', len(renames), 'name clashes with existing labels', len(clash), clash[:5])
for a in clash:
    del renames[a]

# ---- part B: stock-library functions identified by their unique error text ----------------------------
rows = [l.rstrip('\n').split('\t') for l in open(r'I:\tools\unique_strings.tsv', encoding='utf-8')]
by_addr = {r[0]: (int(r[1]), r[2:]) for r in rows}
rules = [
    # (name, required strings, max size, source note)
    ('Lua51_luaL_argerror', ['bad argument #%d (%s)', "calling '%s' on bad self (%s)"], 400, 'lauxlib.c luaL_argerror'),
    ('Lua51_luaL_where', ['%s:%d: '], 400, 'lauxlib.c luaL_where'),
    ('Lua51_panic', ['PANIC: unprotected error in call to Lua API (%s)\\n'], 200, 'lauxlib.c panic'),
    ('Lua51_luaL_register', ["name conflict for module '%s'", '_LOADED'], 700, 'lauxlib.c luaL_register'),
    ('Lua51_errfile', ['cannot %s %s: %s'], 400, 'lauxlib.c errfile'),
    ('Lua51_luaG_typeerror', ["attempt to %s %s '%s' (a %s value)", 'attempt to %s a %s value'], 400, 'ldebug.c luaG_typeerror'),
    ('Lua51_addinfo', ['%s:%d: %s'], 260, 'ldebug.c addinfo'),
    ('Lua51_luaX_lexerror', ['%s:%d: %s', "%s near '%s'"], 260, 'llex.c luaX_lexerror'),
    ('Lua51_inclinenumber', ['chunk has too many lines'], 300, 'llex.c inclinenumber'),
    ('Lua51_read_numeral', ['malformed number'], 300, 'llex.c read_numeral'),
    ('Lua51_read_long_string', ['nesting of [[...]] is deprecated', 'unfinished long comment', 'unfinished long string'], 900, 'llex.c read_long_string'),
    ('Lua51_check_match', ["'%s' expected (to close '%s' at line %d)"], 300, 'lparser.c check_match'),
    ('Zlib_inflate_fast', ['invalid distance too far back', 'invalid literal/length code', 'invalid distance code'], 1500, 'zlib inffast.c inflate_fast'),
]
lib = []
for name, need, maxsize, src in rules:
    hits = [a for a, (size, strs) in by_addr.items() if size <= maxsize and all(n in strs for n in need)]
    if len(hits) != 1:
        print('SKIP', name, 'matches', hits)
        continue
    a = hits[0]
    lib.append((a, name, 'Identified by its unique error text %s as stock %s; name assumes an unmodified library and was not compared with the original source; automatic string-anchor pass.' % ('/'.join(need)[:60].replace(',', ' ').replace('"', ''), src), by_addr[a][0]))

# ---- write ---------------------------------------------------------------------------------------------
for a, n, c, sz in lib:
    assert ',' not in c and '"' not in c and ',' not in n, (n, c)
print('library labels', len(lib), [(a, n) for a, n, _, _ in lib])

hdr = b'# Pass 3 (2026-11-08): Game.ItemData Lua tables resolved, and stock Lua 5.1 / zlib functions identified by error text; see LABEL_COVERAGE_PASS_3_20261108.md.\r\n'
raw = open(LABELS, 'rb').read()
if hdr in raw:
    raise SystemExit('already applied')
out = []
n_ren = 0
for l in raw.split(b'\r\n'):
    p = l.split(b',', 3)
    if len(p) >= 4 and p[0] == b'WildStar64.exe' and p[1].decode() in renames and p[2].decode() == renames[p[1].decode()][0]:
        old, new, comment = renames[p[1].decode()]
        l = b','.join([p[0], p[1], new.encode(), comment.encode()])
        n_ren += 1
    out.append(l)
body = b'\r\n'.join(out)
if not body.endswith(b'\r\n'):
    body += b'\r\n'
existing_addr = {l.split(b',', 3)[1].decode().lower() for l in out if l.startswith(b'WildStar64.exe,') and not l.split(b',', 3)[2].startswith(b'FUN_')}
add = b''
n_add = 0
for a, n, c, sz in lib:
    if a.lower() in existing_addr:
        continue
    add += ('WildStar64.exe,%s,%s,%s' % (a, n, c)).encode() + b'\r\n'
    n_add += 1
open(LABELS, 'wb').write(body + hdr + add)
print('renamed', n_ren, 'added', n_add)
