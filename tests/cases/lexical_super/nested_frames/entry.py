# 关键字专用参数与可变位置参数不充当首个位置形参。
import subject
obj = subject.Child()
other = subject.Child()
own, absent, keyword_only, variadic = obj.functions()
assert own(other) is other
for call in [absent, lambda: keyword_only(other=other), lambda: variadic(other)]:
    try:
        call()
    except RuntimeError as exc:
        assert str(exc) == 'super(): no arguments'
    else:
        assert False
print('nested functions own their argument frames')
