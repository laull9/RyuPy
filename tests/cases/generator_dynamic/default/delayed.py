# A：属性目标按异常分派延迟求值，参数Name不冻结为静态异常。
class Target:
    def __init__(self, events, kind, failure):
        self.events = events
        self.kind = kind
        self.failure = failure

    @property
    def error(self):
        self.events.append("lookup")
        if self.failure is not None:
            raise self.failure
        return self.kind


def normal(target):
    try:
        yield "start"
        yield "end"
    except target.error:
        assert False


def rebound(target, later):
    try:
        yield "ready"
        raise ValueError("original")
    except target.error:
        yield "caught"
    except later.error:
        assert False


def local_target(kind):
    try:
        kind = yield "kind"
        raise ValueError("local")
    except kind:
        yield "local caught"


global_kind = TypeError


def global_target():
    try:
        yield "global ready"
        raise ValueError("global")
    except global_kind:
        yield "global caught"


def closure_target(kind):
    def generate():
        try:
            yield "free ready"
            raise ValueError("free")
        except kind:
            yield "free caught"

    def replace(value):
        nonlocal kind
        kind = value

    return generate(), replace


def run():
    global global_kind
    events = []
    target = Target(events, TypeError, None)
    later = Target(events, ValueError, RuntimeError("unused"))
    assert list(normal(target)) == ["start", "end"]
    assert events == []
    it = rebound(target, later)
    assert next(it) == "ready"
    assert events == []
    target.kind = ValueError
    assert list(it) == ["caught"]
    assert events == ["lookup"]
    it = local_target(TypeError)
    assert next(it) == "kind"
    assert it.send(ValueError) == "local caught"
    assert next(it, "done") == "done"
    it = global_target()
    assert next(it) == "global ready"
    global_kind = ValueError
    assert list(it) == ["global caught"]
    it, replace = closure_target(TypeError)
    assert next(it) == "free ready"
    replace(ValueError)
    assert list(it) == ["free caught"]
    print("delayed rebound short circuit")
