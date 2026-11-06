# ryupy: pending
# 从已双跑72场景合同拆出的完整有限应用：两种构造方式、当前导出、None和native。
import typing as t
CV=t.ClassVar
cv=CV[int]
regular=t.List[int]
values=(int,None,type(None),cv,regular,list[int],list[cv],list[CV])
count=0
try:
    for mode,replacement in (('canonical',CV),('list',list),('string','changed'),('none',None)):
        t.ClassVar=replacement
        for value in values:
            if (mode=='canonical' and value is cv) or (mode=='list' and value is regular):
                continue
            Functional=t.TypedDict('Functional',{'before':int,'subject':value,'after':str})
            class ClassStyle(t.TypedDict):
                before:int
                subject:value
                after:str
            expected=type(None) if value is None else value
            for built in (Functional,ClassStyle):
                assert built.__annotations__=={'before':int,'subject':expected,'after':str}
                assert built.__annotations__['subject'] is expected
                assert built.__required_keys__==frozenset(('before','subject','after'))
                assert built.__optional_keys__==frozenset()
                assert built.__total__ is True and built.__bases__==(dict,)
                assert t.is_typeddict(built)
                data=built(before=1,subject='unchecked',after='end')
                assert data=={'before':1,'subject':'unchecked','after':'end'}
                count+=1
finally: t.ClassVar=CV
assert count==60
print('typeddict-applications',count,'alias-identities-and-no-value-enforcement')
