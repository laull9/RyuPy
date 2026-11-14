# 新API已登记的NI负门，不是CP正例替代品；普通赋值与旧wrap/fill仍成功。
import textwrap
import re

saved = textwrap.dedent
white = textwrap._whitespace_only_re
leading = textwrap._leading_whitespace_re
marker = []
textwrap._leading_whitespace_re = marker
assert textwrap._leading_whitespace_re is marker
try:
    saved(None)
    raise AssertionError("subject error must precede leading guard")
except TypeError:
    pass
for value in ["plain", "  x"]:
    try:
        saved(value)
        raise AssertionError("missing leading NI")
    except NotImplementedError:
        pass
textwrap._whitespace_only_re = marker
assert textwrap._whitespace_only_re is marker
try:
    saved(None)
    raise AssertionError("missing whitespace NI")
except NotImplementedError:
    pass
assert textwrap.wrap("one two", 4) == ["one", "two"]
assert textwrap.fill("one two", 4) == "one\ntwo"
textwrap._whitespace_only_re = white
textwrap._leading_whitespace_re = leading
textwrap.re = marker
assert textwrap.re is marker
assert saved("plain") == "plain"
try:
    saved("  x")
    raise AssertionError("missing re binding NI")
except NotImplementedError:
    pass
textwrap.re = re
assert saved("  x") == "x"
# stock还受译后体积预算；小正例不推导实际最大可接受margin。
assert saved(" " * 64 + "x") == "x"
for size, message in [(16379, "re: unsupported translated pattern size limit"), (16380, "re: pattern length exceeds supported resource limit")]:
    try:
        saved(" " * size + "x")
        raise AssertionError("missing stock regex resource limit")
    except NotImplementedError as error:
        assert str(error) == message
print("NI_PRIVATE_OK")
