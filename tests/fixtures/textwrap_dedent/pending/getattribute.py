# ryupy: pending
# 完整__getattribute__仍受HIR E0100限制；保留原无margin三例的CP零lookup正期待。
import textwrap

saved = textwrap.dedent
lookups = []
class Trap:
    def __getattribute__(self, name):
        lookups.append(name)
        raise AssertionError("unexpected re lookup")
textwrap.re = Trap()
for value, expected in [("plain", "plain"), (" \t\n", "\n"), ("x\n \t", "x\n")]:
    assert saved(value) == expected
assert lookups == []
print("PENDING_GETATTRIBUTE_CP_OK", len(lookups))
