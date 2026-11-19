# E：运行期类型值与元组验证；目标失败不可落入同级bare。
def match(kind, original, events):
    try:
        yield "ready"
        raise original
    except kind:
        events.append("matched")
        yield "matched"
    except:
        events.append("bare")
        yield "bare"


def tuple_target(kind, other, original):
    try:
        raise original
    except (kind, other):
        yield "tuple"


def run():
    original = ValueError("original")
    for kind, expected in ((ValueError, "matched"), (TypeError, "bare"),
                           ((TypeError, ValueError), "matched"), ((), "bare")):
        events = []
        it = match(kind, original, events)
        assert next(it) == "ready"
        assert list(it) == [expected]
        assert events == [expected]
    assert list(tuple_target(TypeError, ValueError, original)) == ["tuple"]
    for kind in (7, original, (ValueError, 7)):
        events = []
        it = match(kind, original, events)
        assert next(it) == "ready"
        try:
            next(it)
        except TypeError as error:
            assert error.__context__ is original
        else:
            assert False
        assert events == []
    print("targets validation and bare")
