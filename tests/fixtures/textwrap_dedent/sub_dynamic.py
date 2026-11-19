# ryupy: pending
# 当前同一re的sub必须恰三位置参数，保返回/异常身份；无margin不消费re。
import textwrap
import re

saved = textwrap.dedent
original = re.sub
calls = []
marker = []
def spy(*args, **kwargs):
    calls.append((args, kwargs))
    return marker
re.sub = spy
assert saved("  x\n \t \n    y") is marker
assert calls == [(("(?m)^  ", "", "  x\n\n    y"), {})]
print("sub-args", calls)
error = RuntimeError("sentinel")
def raising(*args, **kwargs):
    raise error
re.sub = raising
try:
    saved("  x")
    raise AssertionError("missing callback error")
except RuntimeError as actual:
    assert actual is error
re.sub = None
try:
    saved("  x")
    raise AssertionError("missing callable error")
except TypeError:
    print("sub-noncallable", True)
re.sub = original
lookups = []
class Trap:
    @property
    def sub(self):
        lookups.append("sub")
        raise AssertionError("unexpected re.sub lookup")
textwrap.re = Trap()
for value, expected in [("plain", "plain"), (" \t\n", "\n"), ("x\n \t", "x\n")]:
    assert saved(value) == expected
assert lookups == []
textwrap.re = re
# 已完成的绑定不因后续公开compile/MULTILINE写入重新编译或失效。
def compile_trap(*args, **kwargs):
    raise AssertionError("unexpected recompilation")
re.compile = compile_trap
re.MULTILINE = object()
assert saved("  after") == "after"
# 大margin必须先交给自定义sub，不能偷套stock资源界限。
re.sub = spy
large = " " * 16380
assert saved(large + "x") is marker
assert calls[-1] == (("(?m)^" + large, "", large + "x"), {})
print("SUB_OK", len(calls), len(calls[-1][0][0]))
