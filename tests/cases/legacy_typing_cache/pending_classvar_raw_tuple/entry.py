# 单一CP真值：scalar暖键不覆盖新单项tuple原始键。
import typing as t
key = t.List[int]
warm = t.List[key]
t.ClassVar = list
assert t.List[key] is warm
try:
    t.List[(key,)]
except TypeError as error:
    print('raw-tuple-cold', str(error))
else:
    assert False, 'new raw tuple key must run the cold body'
