# F：保真实源码控制流形状，不冒termios/Graph或完整项目执行。
class Errors:
    def __init__(self, kind):
        self.error = kind
        self.NetworkXNoPath = kind


def terminal_shape(errors, events):
    try:
        events.append("setup")
        try:
            yield "fd"
        finally:
            events.append("restore")
            raise ValueError("restore failure")
    except errors.error:
        events.append("suppressed")


def path_shape(errors, values):
    for value in values:
        try:
            if value == 1:
                raise ValueError("missing")
            yield value, [value]
        except errors.NetworkXNoPath:
            pass


def run():
    errors = Errors(ValueError)
    events = []
    it = terminal_shape(errors, events)
    assert next(it) == "fd"
    assert next(it, "done") == "done"
    assert events == ["setup", "restore", "suppressed"]
    assert list(path_shape(errors, [0, 1, 2])) == [(0, [0]), (2, [2])]
    print("workload shapes terminal and paths")
