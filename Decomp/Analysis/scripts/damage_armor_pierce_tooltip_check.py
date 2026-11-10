"""Compare tooltips that state an armor pierce percentage with Damage DataBits04.

For every Jabbithole tooltip (jabbithole.spells.flavor) that states "pierce N% armor", collect DataBits04 of the Damage
rows of that spell and of the spells it reaches through proxy effects, and report whether the stated fraction is among
them. Read-only. Needs the wildstar_client and jabbithole reference databases and the mysql command-line client.
See SPELL_EFFECT_DAMAGE_WORDS_20261110.md.

Usage: damage_armor_pierce_tooltip_check.py [user] [password]
"""
import re
import struct
import subprocess
import sys

MYSQL = r'C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe'
USER = sys.argv[1] if len(sys.argv) > 1 else 'bankai'
PASSWORD = sys.argv[2] if len(sys.argv) > 2 else 'bankai'


def q(sql):
    out = subprocess.run([MYSQL, '--host=127.0.0.1', '--user=' + USER, '--password=' + PASSWORD, '-N', '-B', '-e', sql],
                         capture_output=True, text=True, encoding='utf-8', errors='replace').stdout
    return [l.split('\t') for l in out.splitlines() if l]


def f(v):
    return round(struct.unpack('<f', struct.pack('<I', int(v)))[0], 4)


word = 'ierce'
effects = {}
for sid, et, d0, d1, d3, d4, d5 in q("select spellId, effectType, dataBits00, dataBits01, dataBits03, dataBits04, dataBits05 from wildstar_client.spell4effects where effectType in (8,26,19,27,28,131,12,33)"):
    effects.setdefault(int(sid), []).append((int(et), int(d0), int(d1), f(d3), f(d4), f(d5)))


def damage_words(spell, depth=0, seen=None):
    seen = seen or set()
    if spell in seen or depth > 4:
        return []
    seen.add(spell)
    res = []
    for et, d0, d1, w3, w4, w5 in effects.get(spell, []):
        if et in (8, 12, 33):
            res.append((w3, w4, w5))
        elif et in (26, 19, 27, 28, 131):
            for child in (d0, d1):
                if child:
                    res += damage_words(child, depth + 1, seen)
    return res


rows = q("select game_id, name, tier, replace(flavor, '\\\\n', ' ') from jabbithole.spells where flavor like '%" + word + "%'")
agree = disagree = silent = 0
names = {}
shown = 0
for gid, name, tier, flavor in rows:
    pcts = sorted(set(int(m) / 100 for m in re.findall(r'(?:[Pp]ierc\w*|Armor Pierce increased to) (\d+)%', flavor)))
    if not pcts:
        continue
    words = damage_words(int(gid))
    w4 = sorted(set(w[1] for w in words))
    if not words:
        silent += 1
        continue
    hit = [p for p in pcts if any(abs(p - x) < 0.006 for x in w4)]
    if hit:
        agree += 1
        names.setdefault(name, set()).update(hit)
    else:
        disagree += 1
        if shown < 25:
            shown += 1
            print('DISAGREE', gid, name, tier, pcts, 'w4=', w4, '|', flavor[:150])
print('agreeing abilities:', {k: sorted(v) for k, v in names.items()})
print('tooltips with a pierce percentage:', agree + disagree + silent, 'agree', agree, 'disagree', disagree, 'no damage rows reachable', silent)
