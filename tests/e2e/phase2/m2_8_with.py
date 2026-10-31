# ryupy: pending
# M2.8：上下文进入 / 退出顺序、异常抑制与进入失败。
class Context:
    def __init__(self, name, suppress=False, fail=False):
        self.name = name
        self.suppress = suppress
        self.fail = fail

    def __enter__(self):
        print("enter", self.name)
        if self.fail:
            raise ValueError("enter failed")
        return self.name

    def __exit__(self, kind, value, traceback):
        print("exit", self.name, kind.__name__ if kind else None)
        return self.suppress


with Context("a"), Context("b", suppress=True) as name:
    print("body", name)
    raise ValueError("suppressed")
try:
    with Context("outer"), Context("inner", fail=True):
        print("unreachable")
except ValueError:
    print("enter failure caught")
