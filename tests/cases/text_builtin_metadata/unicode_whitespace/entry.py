# Python空白、真实根描述符及精确内建类型身份合同。
from decimal import Decimal
spaces = '\t\n\v\f\r \x1c\x1d\x1e\x1f\x85\xa0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000'
for ch in spaces:
    assert ch.isspace() and str.isspace(ch)
    assert (ch+'12.30'+ch).strip() == '12.30'
    assert (ch+'a'+ch+'b'+ch).split() == ['a', 'b']
    assert (ch+'a'+ch+'b'+ch).split(None) == ['a', 'b']
    assert str(Decimal(ch+'12.30'+ch)) == '12.30'
for text in ['', 'x', ' x ', '\u200b', '\ufeff', '\ud800']:
    assert not text.isspace()
assert '  a  b  '.split(None, 0) == ['a  b  ']
assert '  a  b  '.split(None, 1) == ['a', 'b  ']
assert '\x1ca\x1c'.strip(' ') == '\x1ca\x1c'
class S(str):
    def isspace(self): return 'override'
s = S(' ')
assert s.isspace() == 'override' and str.isspace(s) is True
assert ' '.isspace() is True
saved = ' '.isspace
assert saved() is True
for call in [lambda: ' '.isspace(1), lambda: ' '.isspace(x=1), lambda: str.isspace(1)]:
    try: call()
    except TypeError: pass
    else: raise AssertionError('必须拒绝非法参数/receiver')
print('unicode-whitespace: ok')
