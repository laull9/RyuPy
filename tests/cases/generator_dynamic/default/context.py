# C：先通过observer普通控制，再观察getter相位与双生成器异常身份。
from observer import observe_active


class Target:
    def __init__(self, original, replacement):
        self.original = original
        self.replacement = replacement

    @property
    def error(self):
        assert observe_active() is self.original
        if self.replacement is not None:
            raise self.replacement
        return ValueError


def active(target, original):
    try:
        raise original
    except target.error as caught:
        assert caught is original
        assert observe_active() is original
        yield "paused"
        assert observe_active() is original
        raise


def getter_failure(target, original, events):
    try:
        raise original
    except target.error:
        events.append("wrong target")
        yield 1
    except TypeError:
        events.append("wrong sibling")
        yield 2


def run():
    one = ValueError("one")
    two = ValueError("two")
    first = active(Target(one, None), one)
    second = active(Target(two, None), two)
    assert next(first) == "paused"
    assert next(second) == "paused"
    caller = KeyError("caller")
    try:
        raise caller
    except KeyError:
        for it, original in ((first, one), (second, two)):
            assert observe_active() is caller
            try:
                next(it)
            except ValueError as caught:
                assert caught is original
            else:
                assert False
            assert observe_active() is caller
    original = ValueError("target original")
    replacement = TypeError("getter")
    events = []
    it = getter_failure(Target(original, replacement), original, events)
    try:
        next(it)
    except TypeError as caught:
        assert caught is replacement
        assert caught.__context__ is original
    else:
        assert False
    assert events == []
    print("context getter and caller identity")
