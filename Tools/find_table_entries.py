#!/usr/bin/env python3
"""Find registration table entries in WildStar64.exe by searching for
known server-to-client reader function addresses."""
import struct
import sys
from pathlib import Path

readers = [0x140095c20, 0x140095ce0, 0x140095a80, 0x1400959c0, 0x140095b40,
           0x140097620, 0x140097ee0, 0x140097690, 0x140097f70, 0x1400980f0]

image_base = 0x140000000

if len(sys.argv) != 2:
    print("usage: find_table_entries.py <path-to-WildStar64.exe>", file=sys.stderr)
    sys.exit(2)

exe_path = Path(sys.argv[1])

with exe_path.open('rb') as f:
    data = f.read()

# Parse PE sections
e_lfanew = struct.unpack_from('<I', data, 0x3C)[0]
nt_headers = e_lfanew + 4
file_header = nt_headers + 20
sizeof_opt = struct.unpack_from('<H', data, nt_headers + 16)[0]
sect_off = file_header + sizeof_opt

sections = []
for i in range(20):
    off = sect_off + i * 40
    name = data[off:off+8].rstrip(b'\x00').decode('ascii', errors='replace')
    vsize = struct.unpack_from('<I', data, off + 8)[0]
    vaddr = struct.unpack_from('<I', data, off + 12)[0]
    raw_size = struct.unpack_from('<I', data, off + 16)[0]
    raw_off = struct.unpack_from('<I', data, off + 20)[0]
    if not name:
        break
    sections.append((name, vaddr, vsize, raw_off, raw_size))

def va_to_file(va):
    rva = va - image_base
    for name, vaddr, vsize, raw_off, raw_size in sections:
        if vaddr <= rva < vaddr + vsize:
            return raw_off + (rva - vaddr)
    return None

# Find each reader in data sections and collect their file offsets
print("=== Section Layout ===")
for name, vaddr, vsize, raw_off, raw_size in sections:
    print(f"  {name}: VA=0x{vaddr:X} size=0x{vsize:X} file=0x{raw_off:X}-0x{raw_off+raw_size:X}")

print("\n=== Searching for table entries (reader at offset 0x18) ===")
found_entries = []
for name, vaddr, vsize, raw_off, raw_size in sections:
    if not (name.startswith('.data') or name.startswith('.rdata') or 'data' in name.lower()):
        continue
    sect_data = data[raw_off:raw_off+raw_size]

    for reader in readers:
        reader_bytes = struct.pack('<Q', reader)
        pos = 0
        while True:
            idx = sect_data.find(reader_bytes, pos)
            if idx == -1:
                break

            # Entry base: reader is at offset 0x18
            if idx >= 0x18:
                entry_off = idx - 0x18
                entry_va = image_base + vaddr + entry_off

                # Read candidate entry fields
                opcode = struct.unpack_from('<I', sect_data, entry_off + 0x08)[0]
                data_sz = struct.unpack_from('<I', sect_data, entry_off + 0x0C)[0]
                writer = struct.unpack_from('<Q', sect_data, entry_off + 0x10)[0]
                sreader = struct.unpack_from('<Q', sect_data, entry_off + 0x18)[0]
                f1 = struct.unpack_from('<I', sect_data, entry_off + 0x20)[0]
                f2 = struct.unpack_from('<I', sect_data, entry_off + 0x24)[0]

                if sreader == reader and 0x000 <= opcode <= 0x2000:
                    found_entries.append((entry_va, opcode, data_sz, writer, sreader, f1, f2))
                    print(f"  VA=0x{entry_va:X}: opcode=0x{opcode:04X} size=0x{data_sz:X} writer=0x{writer:X} reader=0x{sreader:X} flags=0x{f1:X}/0x{f2:X}")

            pos = idx + 1
            if pos > len(sect_data):
                break

print(f"\nFound {len(found_entries)} table entries")

# If we found entries, try to derive the table base
if found_entries:
    # Sort by VA
    found_entries.sort()
    print("\n=== Sorted entries ===")
    for e in found_entries:
        entry_va, opcode, data_sz, writer, sreader, f1, f2 = e
        print(f"  VA=0x{entry_va:08X} opcode=0x{opcode:04X} size={data_sz} reader=0x{sreader:08X}")

    # Find gaps of exactly 0x28 (entry size) to identify contiguous tables
    print("\n=== Contiguous tables (0x28 gaps) ===")
    tables = []
    current_table = [found_entries[0]]
    for i in range(1, len(found_entries)):
        gap = found_entries[i][0] - found_entries[i-1][0]
        if gap == 0x28:
            current_table.append(found_entries[i])
        else:
            if len(current_table) >= 2:
                tables.append(list(current_table))
            current_table = [found_entries[i]]
    if len(current_table) >= 2:
        tables.append(list(current_table))

    for t in tables:
        base_va = t[0][0]
        end_va = t[-1][0]
        count = len(t)
        print(f"  Table at 0x{base_va:X}: {count} entries, range 0x{base_va:X}-0x{end_va:X}")
        for e in t:
            entry_va, opcode, data_sz, writer, sreader, f1, f2 = e
            # Try to match with known reader addresses
            reader_id = "?"
            if sreader in readers:
                reader_id = f"ENTITY_AUX_{readers.index(sreader)}"
            print(f"    0x{entry_va:X} op=0x{opcode:04X} sz={data_sz:3d} rdr=0x{sreader:X} ({reader_id})")
