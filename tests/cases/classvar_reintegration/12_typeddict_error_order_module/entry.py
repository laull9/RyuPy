# ryupy: pending
# 独立模块级源：原09完整保留pending，不因旧局部class编译边界删减错误或副作用断言。
# 与原09相同12场景；类定义只在模块级for/try内，不在任何函数体中。
import typing as t
CV=t.ClassVar
cv=CV[int]
regular=t.List[int]
sentinel=object()
count=0
events=[]

def field(label,annotation):
    events.append(label)
    return annotation

try:
    for mode,replacement in (('canonical',CV),('list',list),('string','changed'),('none',None)):
        t.ClassVar=replacement
        subjects=[(CV,'Plain typing.ClassVar is not valid as type argument')]
        if mode=='canonical':
            subjects.append((cv,'typing.ClassVar[int] is not valid as type argument'))
        if mode=='list':
            subjects.append((regular,'typing.List[int] is not valid as type argument'))
        for style in ('functional','class'):
            for value,wanted in subjects:
                events=[]
                Built=sentinel
                try:
                    if style=='functional':
                        Built=t.TypedDict('Built',{'before':field('before',int),
                                                  'subject':field('subject',value),
                                                  'after':field('after',str)})
                    else:
                        class Built(t.TypedDict):
                            before:field('before',int)
                            subject:field('subject',value)
                            after:field('after',str)
                except TypeError as error:
                    assert str(error)==wanted
                    assert Built is sentinel
                    assert events==['before','subject','after']
                    print(style,str(error),'before,subject,after','old-binding')
                else: assert False
                count+=1
finally: t.ClassVar=CV
assert count==12
print('typeddict-errors',count)
