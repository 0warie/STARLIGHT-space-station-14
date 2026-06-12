#!/usr/bin/env python3
# Get the VGRoid grid name out of a decompressed replay state blob.
#   unzip -p replay.zip _replay/data_0.dat | tail -c +5 | zstd -dc > data_0.bin
#   python3 vgroid_name.py data_0.bin
import re, sys

data = open(sys.argv[1], 'rb').read()

# GetFTLName output (<BorerName>-NN-X) is written inline as: 01 <len> <utf8>.
# Find every such name and record where each occurs.
names = {}
for m in re.finditer(rb'[A-Za-z][A-Za-z]+-[1-9][0-9]-[A-Z]', data):
    p, s = m.start(), m.group(0)
    if data[p - 1] == len(s) and data[p - 3] == 0x01:      # inline-string framing
        names.setdefault(s.decode(), []).append(p)

# A gateway destination's name is also stored as a gateway DestinationName ref,
# whose record starts with four zero bytes. The VGRoid name has no such ref.
for n, ps in sorted(names.items()):
    gw = sum(1 for p in ps if data[p - 8:p - 4] == b'\x00\x00\x00\x00')
    role = 'VGRoid' if gw == 0 else 'gateway destination'
    print(f'{n:18s} refs={len(ps)} gateway-refs={gw}  -> {role}')
