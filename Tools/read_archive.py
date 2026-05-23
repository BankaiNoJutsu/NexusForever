#!/usr/bin/env python3
"""Extract and search WildStar .archive files for Fortune/Lua data.
Header: magic(4) + version(4) + padding(32) + unknown(8) + count(8) + totalsize(8) = 64 bytes
File table at 0x40, each entry: name_hash(8) + offset(8) + csize(8) + usize(8) = 32 bytes
"""
import struct
import sys
import zlib
from pathlib import Path

def read_archive(path):
    f = open(path, 'rb')
    magic = f.read(4)
    if magic != b'KCAP':
        f.close()
        raise ValueError(f"Bad magic: {magic.hex()}")
    version = struct.unpack('<I', f.read(4))[0]
    f.seek(0x28)  # skip padding
    unknown = struct.unpack('<Q', f.read(8))[0]
    file_count = struct.unpack('<Q', f.read(8))[0]
    total_size = struct.unpack('<Q', f.read(8))[0]
    print(f"Archive: {path} v{version}, {file_count} files, {total_size} bytes")

    entries = []
    for i in range(file_count):
        entry = f.read(32)
        if len(entry) < 32:
            break
        name_hash, offset, csize, usize = struct.unpack('<QQQQ', entry)
        entries.append((name_hash, offset, csize, usize))

    print(f"  Read {len(entries)} entries")
    return entries, f

def search(entries, fh, max_scan=10000):
    found = []
    for i, (name_hash, offset, csize, usize) in enumerate(entries[:max_scan]):
        if offset > 0x7FFFFFFF or csize > 0x10000000:  # Skip invalid entries
            continue
        try:
            fh.seek(offset)
        except (OverflowError, ValueError):
            continue
        raw = fh.read(min(csize, 256))
        try:
            data = zlib.decompress(raw, -15)
        except:
            data = raw

        if b'\x1bLua' in data:
            fh.seek(offset)
            full = fh.read(csize)
            try:
                full = zlib.decompress(full, -15)
            except:
                pass
            text = full.decode('latin-1', errors='replace')
            lower = text.lower()
            if 'fortune' in lower or 'madame' in lower or 'fay' in lower or 'card' in lower:
                print(f"\n  [{i}] Lua hash=0x{name_hash:016x} size={usize}")
                for line in text.split('\n'):
                    ll = line.lower()
                    if any(k in ll for k in ['fortune', 'madame', 'fay', 'card', 'weight', 'reward']):
                        print(f"    {line.strip()[:150]}")
                found.append((i, name_hash, 'fortune-lua'))
            else:
                found.append((i, name_hash, 'lua-other'))

    return found

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print("usage: read_archive.py <path-to-ClientDataEN.archive>", file=sys.stderr)
        sys.exit(2)

    path = Path(sys.argv[1])
    entries, fh = read_archive(path)
    print(f"Scanning {len(entries)} entries for Lua with fortune keywords...")
    found = search(entries, fh, len(entries))
    print(f"\nFound {len(found)} matching Lua files ({sum(1 for f in found if f[2]=='fortune-lua')} fortune-related).")
    fh.close()
