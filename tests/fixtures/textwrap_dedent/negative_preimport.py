# 每个argv场景必须独立进程；零代理调用，不以NI冒充CP初始化回调正确。
import sys
import re

scenario = sys.argv[1]
calls = []
def compile_proxy(*args, **kwargs):
    calls.append(args)
    raise AssertionError("compile proxy must not run")
class Flags:
    def __index__(self):
        calls.append("index")
        raise AssertionError("flags callback must not run")
if scenario == "compile":
    re.compile = compile_proxy
elif scenario == "flags_value":
    re.MULTILINE = 4
elif scenario == "flags_object":
    re.MULTILINE = Flags()
elif scenario.startswith("backend:"):
    setattr(re, scenario[8:], None)
else:
    raise AssertionError("unknown scenario")
import textwrap
assert textwrap.re is re
assert textwrap.wrap("one two", 4) == ["one", "two"]
assert textwrap.fill("one two", 4) == "one\ntwo"
try:
    textwrap.dedent("  x")
    raise AssertionError("missing initial-state NI")
except NotImplementedError:
    pass
assert calls == []
print("NI_PREIMPORT_OK", scenario)
