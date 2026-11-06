# 已知非法tuple完整交给Optional；Union仅拆最外层tuple。
import typing as t
def error(label, call):
    try:
        call()
    except TypeError as value:
        print(label, str(value))
    else:
        assert False
for value in [(), (int,), (int, str)]:
    error('optional-tuple', lambda: t.Optional[value])
error('union-empty', lambda: t.Union[()])
error('union-nested-tuple', lambda: t.Union[(int,), str])
error('optional-root', lambda: t.Optional[t.Union])
error('union-root', lambda: t.Union[t.Optional, int])
