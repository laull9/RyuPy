# D：动态handler经throw/close/yield-from恢复，正常return与PEP479分开。
def child(kind, error, events):
    try:
        yield "ready"
    except kind as caught:
        assert caught is error
        yield "handled"
        return 23
    finally:
        events.append("child finally")


def delegate(kind, error, events):
    value = yield from child(kind, error, events)
    yield value


def broken(kind, original, stop):
    try:
        raise original
    except kind:
        yield "before stop"
        raise stop


def run():
    events = []
    original = ValueError("send")
    it = delegate(ValueError, original, events)
    assert next(it) == "ready"
    assert it.throw(original) == "handled"
    assert next(it) == 23
    assert next(it, "done") == "done"
    assert events == ["child finally"]
    events = []
    it = child(ValueError, original, events)
    assert next(it) == "ready"
    it.close()
    assert events == ["child finally"]
    original = ValueError("outer")
    stop = StopIteration("inner")
    it = broken(ValueError, original, stop)
    assert next(it) == "before stop"
    try:
        next(it)
    except RuntimeError as error:
        assert error.__cause__ is stop
        assert error.__context__ is stop
        assert stop.__context__ is original
    else:
        assert False
    print("protocol delegate and pep479")
