# 运行器：上下文管理 + 异常恢复。
from . import store
from .model import NamedRecord


class Session:
    def __init__(self, label):
        self.label = label
        self.count = 0

    def __enter__(self):
        print("open", self.label)
        return self

    def __exit__(self, kind, value, traceback):
        print("close", self.label, kind is None)
        return False


def run(values):
    with Session("run") as session:
        for value in values:
            store.add(NamedRecord("n" + str(value), value, "tag"))
            session.count += 1
        if session.count > 3:
            raise ValueError("too many")
        return session.count
