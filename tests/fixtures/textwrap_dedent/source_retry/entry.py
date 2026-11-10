# 已在编译图声明re/state；不预先成功导入re，真实依赖失败经textwrap传播。
import state
try:
    import textwrap
    raise AssertionError("missing original source failure")
except RuntimeError as actual:
    assert actual is state.marker
old = state.escaped[0]
assert old.re is state.user_value
assert not hasattr(old, "dedent")
assert not hasattr(old, "_whitespace_only_re")
assert not hasattr(old, "_leading_whitespace_re")
import textwrap
import re
assert re.source_re_marker == "dedent-source-retry"
assert textwrap.re is re
assert state.attempts == 2
assert state.escaped[1] is textwrap
assert old is not textwrap
assert old.re is state.user_value
assert not hasattr(old, "dedent")
assert textwrap.fill("one two", 4) == "one\ntwo"
try:
    textwrap.dedent("  x")
    raise AssertionError("missing source re NI")
except NotImplementedError:
    pass
print("NI_SOURCE_RETRY_OK")
