# 精确内建 __class__ 身份与用户属性遮蔽；保留原始 descriptor 合同。
for value, cls in [({},dict),([],list),((),tuple),(set(),set),(frozenset(),frozenset),(b'',bytes),(bytearray(),bytearray),('',str),(1,int),(True,bool),(1.0,float),(None,type(None))]:
    assert value.__class__ is cls and type(value) is cls
class D(dict): pass
assert D().__class__ is D
class Mask(dict):
    @property
    def __class__(self): return 'masked'
assert Mask().__class__ == 'masked' and type(Mask()) is Mask
class Named:
    __class__ = 'explicit'
assert Named().__class__ == 'explicit'
print('builtin-class-and-descriptor: ok')
