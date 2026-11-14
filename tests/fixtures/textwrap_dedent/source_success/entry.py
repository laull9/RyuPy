# re在静态图登记，但首次真实导入由native textwrap触发。
import textwrap
import re
assert re.source_re_marker == "dedent-source-success"
assert textwrap.re is re
assert textwrap.wrap("one two", 4) == ["one", "two"]
assert textwrap.fill("one two", 4) == "one\ntwo"
try:
    textwrap.dedent("  x")
    raise AssertionError("missing source re NI")
except NotImplementedError:
    pass
print("NI_SOURCE_SUCCESS_OK")
