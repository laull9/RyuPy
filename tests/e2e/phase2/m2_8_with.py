# M2.8：上下文进入 / 退出顺序、异常抑制、进入失败与跳转清理。
class Context(object):
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


def early_return():
    with Context("return"):
        print("before return")
        return "done"


print(early_return())

for index in range(4):
    with Context("loop" + str(index)):
        if index == 1:
            continue
        if index == 3:
            break
        print("loop body", index)
print("loop done")


class BadExit:
    def __enter__(self):
        return self

    def __exit__(self, kind, value, traceback):
        raise RuntimeError("exit failed")


try:
    with BadExit():
        print("bad body")
except RuntimeError as error:
    print("exit failure", error)
