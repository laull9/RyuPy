# ryupy: pending
# CP的有限source代理正期待；sys.modules替换能力也在域外，不能冒作产品源码图测试。
import re
import sys
import types
proxy = types.ModuleType("re")
exec("def compile(*args, **kwargs):\n    return original.compile(*args, **kwargs)\ndef sub(*args, **kwargs):\n    return original.sub(*args, **kwargs)\n", proxy.__dict__)
proxy.original = re
proxy.escape = re.escape
proxy.VERBOSE = re.VERBOSE
proxy.MULTILINE = re.MULTILINE
sys.modules["re"] = proxy
import textwrap
assert textwrap.re is proxy
assert textwrap.wrap("one two", 4) == ["one", "two"]
assert textwrap.fill("one two", 4) == "one\ntwo"
assert textwrap.dedent("  x\n    y") == "x\n  y"
print("PENDING_SOURCE_CP_OK")
