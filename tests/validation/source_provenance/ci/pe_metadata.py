"""只读ECMA PE/metadata/IL审计，不加载程序集或运行产品。"""
import struct,pathlib,json,hashlib,sys
p=pathlib.Path(sys.argv[1]); b=p.read_bytes()
def u(o,n=4):return int.from_bytes(b[o:o+n],'little')
pe=u(0x3c); assert b[pe:pe+4]==b'PE\0\0'; ns=u(pe+6,2); oh=pe+24; magic=u(oh,2); dd=oh+(112 if magic==0x20b else 96); sec=oh+u(pe+20,2)
def off(rva):
 for i in range(ns):
  o=sec+40*i; vs,va,rs,raw=u(o+8),u(o+12),u(o+16),u(o+20)
  if va<=rva<va+max(vs,rs):return raw+rva-va
 raise ValueError(('unmapped RVA',rva))
cli=off(u(dd+14*8)); md=off(u(cli+8)); assert b[md:md+4]==b'BSJB'; q=md+16+u(md+12); count=u(q+2,2);q+=4; streams={}
for _ in range(count):
 start,size=u(q),u(q+4); e=b.index(0,q+8); name=b[q+8:e].decode(); streams[name]=(md+start,size);q=q+8+((e-(q+8)+1+3)&~3)
s=streams['#Strings'][0]; blob=streams['#Blob'][0]
def string(i):return b[s+i:b.index(0,s+i)].decode()
def compressed(o):
 a=b[o]
 if a<128:return a,1
 if a<192:return ((a&63)<<8)|b[o+1],2
 return ((a&31)<<24)|int.from_bytes(b[o+1:o+4],'big'),4
def signature(i):
 n,k=compressed(blob+i);return b[blob+i+k:blob+i+k+n].hex()
t=streams.get('#~',streams.get('#-'))[0]; hs=u(t+6,1);valid=u(t+8,8);q=t+24;rows={}
for i in range(64):
 if valid>>i&1:rows[i]=u(q);q+=4
codes={'ResolutionScope':(2,[0,26,35,1]),'TypeDefOrRef':(2,[2,1,27]),'MemberRefParent':(3,[2,1,26,6,27]),'HasConstant':(2,[4,8,23]),'HasCustomAttribute':(5,[6,4,1,2,8,9,10,0,14,23,20,17,26,27,32,35,38,39,40,42,44,43]),'CustomAttributeType':(3,[6,10]),'HasFieldMarshal':(1,[4,8]),'HasDeclSecurity':(2,[2,6,32]),'HasSemantics':(1,[20,23]),'MethodDefOrRef':(1,[6,10]),'MemberForwarded':(1,[4,6])}
schemas={0:['u2','str','guid','guid','guid'],1:['ResolutionScope','str','str'],2:['u4','str','str','TypeDefOrRef',4,6],3:[4],4:['u2','str','blob'],5:[6],6:['u4','u2','u2','str','blob',8],7:[8],8:['u2','u2','str'],9:[2,'TypeDefOrRef'],10:['MemberRefParent','str','blob'],11:['u2','HasConstant','blob'],12:['HasCustomAttribute','CustomAttributeType','blob'],13:['HasFieldMarshal','blob'],14:['u2','HasDeclSecurity','blob'],15:['u2','u4',2],16:['u4',4],17:['blob'],18:[2,20],19:[20],20:['u2','str','TypeDefOrRef'],21:[2,23],22:[23],23:['u2','str','blob'],24:['u2',6,'HasSemantics'],25:[2,'MethodDefOrRef','MethodDefOrRef'],26:['str'],27:['blob'],28:['u2','MemberForwarded','str',26],29:['u4',4],30:['u4','u4'],31:['u4'],32:['u4','u2','u2','u2','u2','u4','blob','str','str'],33:['u4'],34:['u4','u4','u4'],35:['u2','u2','u2','u2','u4','blob','str','str','blob']}
def width(x):
 if isinstance(x,int):return 2 if rows.get(x,0)<65536 else 4
 if x in ('u2','u4'):return int(x[1:])
 if x in ('str','guid','blob'):return 4 if hs & {'str':1,'guid':2,'blob':4}[x] else 2
 bits,tables=codes[x];return 2 if max(rows.get(i,0) for i in tables)<(1<<(16-bits)) else 4
codes['Implementation']=(2,[38,35,39])
codes['TypeOrMethodDef']=(1,[2,6])
schemas.update({36:['u4',35],37:['u4','u4','u4',35],38:['u4','str','blob'],39:['u4','u4','str','str','Implementation'],40:['u4','u4','str','Implementation'],41:[2,2],42:['u2','u2','TypeOrMethodDef','str'],43:['MethodDefOrRef','blob'],44:[42,'TypeDefOrRef']})
tables={}
for i in range(45):
 schema=schemas[i];widths=[width(x) for x in schema]; tables[i]=[]
 for n in range(rows.get(i,0)):
  vals=[]
  for w in widths:vals.append(u(q,w));q+=w
  tables[i].append(vals)
