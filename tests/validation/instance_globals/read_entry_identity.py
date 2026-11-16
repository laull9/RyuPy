# 仅本次静态入口身份读取；非完整PE/实际调用图验收器，不执行程序集。
import hashlib
import json
from pathlib import Path
import struct
import sys

P = Path(sys.argv[1]).resolve(strict=True)
b = P.read_bytes()
def u16(o): return struct.unpack_from('<H', b, o)[0]
def u32(o): return struct.unpack_from('<I', b, o)[0]
pe = u32(60); count = u16(pe + 6); opt = pe + 24; sec = opt + u16(pe + 20)
def offset(rva):
    for i in range(count):
        s = sec + 40 * i
        if u32(s + 12) <= rva < u32(s + 12) + max(u32(s + 8), u32(s + 16)):
            return u32(s + 20) + rva - u32(s + 12)
    raise ValueError('unmapped RVA')
dirs = opt + (112 if u16(opt) == 0x20b else 96)
cli = offset(u32(dirs + 14 * 8)); token = u32(cli + 20)
md = offset(u32(cli + 8)); pos = md + 16 + u32(md + 12); pos = md + ((pos - md + 3) & ~3)
streams = {}; n = u16(pos + 2); pos += 4
for _ in range(n):
    start, size = u32(pos), u32(pos + 4); end = b.index(0, pos + 8)
    name = b[pos + 8:end].decode(); streams[name] = (md + start, size)
    pos = md + ((end - md + 4) & ~3)
t = streams['#~' if '#~' in streams else '#-'][0]; heap = b[t + 6]; valid = struct.unpack_from('<Q', b, t + 8)[0]
rows = {}; pos = t + 24
for i in range(64):
    if valid >> i & 1: rows[i] = u32(pos); pos += 4
sw = 4 if heap & 1 else 2; gw = 4 if heap & 2 else 2; bw = 4 if heap & 4 else 2
def ix(tab): return 4 if rows.get(tab, 0) >= 65536 else 2
def coded(tabs, bits): return 4 if max(rows.get(x, 0) for x in tabs) >= 1 << (16-bits) else 2
def read(o, w): return u32(o) if w == 4 else u16(o)
def string(v):
    s = streams['#Strings'][0] + v
    return b[s:b.index(0, s)].decode()
sizes = {0:2+sw+3*gw, 1:coded([0,26,35,1],2)+2*sw,
         2:4+2*sw+coded([2,1,27],2)+ix(4)+ix(6), 3:ix(4),
         4:2+sw+bw, 5:ix(6), 6:8+sw+bw+ix(8)}
offsets = {}
for i in range(7):
    offsets[i] = pos; pos += rows.get(i,0)*sizes[i]
if token >> 24 != 6: raise ValueError('entry not MethodDef')
rid = token & 0xffffff; method = offsets[6]+(rid-1)*sizes[6]
name = string(read(method+8,sw)); blob = read(method+8+sw,bw)
z = streams['#Blob'][0]+blob; first=b[z]
if first < 128: length=first; z+=1
elif first < 192: length=((first&63)<<8)|b[z+1]; z+=2
else: raise ValueError('unexpected long signature')
owner = None
for r in range(1,rows[2]+1):
    o=offsets[2]+(r-1)*sizes[2]
    m=read(o+4+2*sw+coded([2,1,27],2)+ix(4),ix(6))
    next_m=rows[6]+1 if r==rows[2] else read(o+sizes[2]+4+2*sw+coded([2,1,27],2)+ix(4),ix(6))
    if m <= rid < next_m:
        owner={'name':string(read(o+4,sw)), 'namespace':string(read(o+4+sw,sw)), 'flags':u32(o)}
result={'pe':str(P),'size':len(b),'sha256':hashlib.sha256(b).hexdigest(), 'cli_entry_token':hex(token),
        'method_name':name,'method_flags':hex(u16(method+6)), 'signature_blob':b[z:z+length].hex(),'owner':owner,
        'scope':'entry identity only; no Call graph/rooting/execution proof'}
print(json.dumps(result,indent=2))
