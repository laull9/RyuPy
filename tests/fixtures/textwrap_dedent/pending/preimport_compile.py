# ryupy: pending
# CP完整正期待：五次compile和可改变结果的private代理；NI负门不能代签。
import re
original = re.compile
calls = []
private = []
class White:
    def sub(self, replacement, text):
        return "  forced"
class Leading:
    def findall(self, text):
        assert text == "  forced"
        return ["  "]
def proxy(*args, **kwargs):
    calls.append((args, kwargs))
    if args[0] == "^[ \t]+$":
        value = White()
        private.append(value)
        return value
    if args[0] == "(^[ \t]*)(?:[^ \t\n])":
        value = Leading()
        private.append(value)
        return value
    return original(*args, **kwargs)
re.compile = proxy
import textwrap
assert len(calls) == 5
assert textwrap._whitespace_only_re is private[0]
assert textwrap._leading_whitespace_re is private[1]
assert textwrap.dedent("unrelated") == "forced"
print("PENDING_PREIMPORT_CP_OK", len(calls))
