# ryupy: pending
# 已支持PEP585 native根与Literal值不误读typing.ClassVar；不扩复杂native语义。
import typing as t
import collections.abc as abc
CV=t.ClassVar
cv=CV[int]
roots=(list,dict,tuple,set,frozenset,abc.Iterable,abc.Iterator,abc.Sequence,
       abc.Mapping,abc.MutableMapping,abc.Collection,abc.Container)
try:
    for replacement in (list,'changed',None):
        t.ClassVar=replacement
        for root in roots:
            for value in (CV,cv):
                native=root[value]
                assert native.__origin__ is root
                assert native.__args__==(value,)
                assert native==root[value]
        assert t.Literal[1,'plain',None].__args__==(1,'plain',None)
finally: t.ClassVar=CV
print('native-roots',len(roots),'classvar-values-not-checked;literal-stable')
