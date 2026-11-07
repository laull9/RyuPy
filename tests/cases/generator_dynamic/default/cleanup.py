# B：as跨暂停保留；close/throw/return及finally的清理次序。
def selected(kind, error, events):
    try:
        try:
            raise error
        except kind as caught:
            assert caught is error
            yield "caught"
            assert caught is error
    finally:
        try:
            caught
        except UnboundLocalError:
            events.append("cleared")
        else:
            assert False
        events.append("finally")


def later(first, second, events):
    try:
        raise ValueError("second")
    except first:
        assert False
    except second as caught:
        try:
            yield str(caught)
        finally:
            events.append("inner")


def selected_second(first, second, events):
    first_name = "keep"
    second_name = "before"
    try:
        try:
            raise ValueError("second selected")
        except first as first_name:
            assert False
        except second as second_name:
            yield str(second_name)
    finally:
        assert first_name == "keep"
        try:
            second_name
        except UnboundLocalError:
            events.append("only second cleared")
        else:
            assert False


def pending_return(kind, events):
    try:
        raise ValueError("return")
    except kind:
        try:
            return 17
        finally:
            events.append("pending")
            yield "cleanup yield"
            events.append("resumed")


def loop_flow(kind):
    for i in range(3):
        try:
            raise ValueError("loop")
        except kind:
            yield i
            if i == 0:
                continue
            break
    yield "finished"


def run():
    for action in ("normal", "close", "throw"):
        events = []
        error = ValueError("kept")
        it = selected(ValueError, error, events)
        assert next(it) == "caught"
        assert events == []
        if action == "normal":
            assert next(it, "done") == "done"
        elif action == "close":
            it.close()
        else:
            replacement = TypeError("injected")
            try:
                it.throw(replacement)
            except TypeError as caught:
                assert caught is replacement
            else:
                assert False
        assert events == ["cleared", "finally"]
    events = []
    it = later(TypeError, ValueError, events)
    assert next(it) == "second"
    it.close()
    assert events == ["inner"]
    for close in (False, True):
        selected_events = []
        chosen = selected_second(TypeError, ValueError, selected_events)
        assert next(chosen) == "second selected"
        if close:
            chosen.close()
        else:
            assert next(chosen, "done") == "done"
        assert selected_events == ["only second cleared"]
    events = []
    it = pending_return(ValueError, events)
    assert next(it) == "cleanup yield"
    assert events == ["pending"]
    try:
        next(it)
    except StopIteration as end:
        assert end.value == 17
    else:
        assert False
    assert events == ["pending", "resumed"]
    close_events = []
    closing = pending_return(ValueError, close_events)
    assert next(closing) == "cleanup yield"
    closing.close()
    assert next(closing, "closed") == "closed"
    assert close_events == ["pending"]
    assert list(loop_flow(ValueError)) == [0, 1, "finished"]
    print("cleanup flow pending return")
