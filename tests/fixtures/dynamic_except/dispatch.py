# 源序与完整动态 tuple 校验。probe 仅验证身份，绝不证明 traceback 不变。
class Original(Exception):
    pass

class Other(Exception):
    pass

class Ordinary:
    pass

original = Original("original")
lookup_error = Other("lookup")

def active_is(value):
    try:
        raise
    except BaseException as caught:
        return caught is value

class Targets:
    def __init__(self, mode):
        self.mode = mode

    @property
    def first(self):
        print("read.first", active_is(original))
        if self.mode == "error":
            raise lookup_error
        if self.mode == "reraise":
            raise
        if self.mode == "invalid":
            return None
        if self.mode == "second":
            return Other
        return Original

    @property
    def second(self):
        print("read.second", active_is(original))
        return Original

    @property
    def invalid(self):
        print("read.invalid", active_is(original))
        return None

def ordered(mode):
    targets = Targets(mode)
    try:
        try:
            if mode == "normal":
                print("normal.body", True)
            else:
                raise original
        except targets.first as caught:
            print("selected.first", caught is original)
        except targets.second as caught:
            print("selected.second", caught is original)
        except Other:
            print("WRONG.lookup-caught")
        else:
            print("normal.else", True)
        finally:
            print("ordered.finally", mode)
    except BaseException as caught:
        if mode == "reraise":
            print("escaped.reraise", caught is original)
        elif mode == "error":
            print("escaped.lookup", caught is lookup_error, caught.__context__ is original)
        else:
            print("escaped.invalid", isinstance(caught, TypeError), caught.__context__ is original)

ordered("normal")
ordered("first")
ordered("second")
ordered("error")
ordered("invalid")
ordered("reraise")

def tuple_case(label, target):
    value = Original(label)
    try:
        try:
            raise value
        except target as caught:
            print(label, "match", caught is value)
        except BaseException as caught:
            print(label, "fallback", caught is value)
    except TypeError as caught:
        print(label, "invalid", caught.__context__ is value, str(caught))

tuple_case("tuple.direct", Original)
tuple_case("tuple.parent", Exception)
tuple_case("tuple.valid", (Other, Original))
tuple_case("tuple.empty", ())
tuple_case("tuple.match-invalid", (Original, None))
tuple_case("tuple.invalid-match", (None, Original))
tuple_case("tuple.nonmatch-invalid", (Other, None))
tuple_case("tuple.nested", ((Original,),))
tuple_case("tuple.match-nested", (Original, (Other,)))
tuple_case("tuple.instance", Original("instance"))
tuple_case("tuple.ordinary", Ordinary)
tuple_case("tuple.none", None)
tuple_case("tuple.tuple-instance", (Original("instance"),))

targets = Targets("first")
try:
    try:
        raise original
    except (targets.first, targets.invalid):
        print("WRONG.tuple-short-circuit")
except TypeError as caught:
    print("tuple.operands", caught.__context__ is original)

targets = Targets("second")
try:
    raise original
except targets.first:
    print("WRONG.dynamic-before-static")
except Original as caught:
    print("mixed.static-tail", caught is original)
except targets.invalid:
    print("WRONG.unreachable")
# 前一个未命中的as绑定不能让下一个目标被误判为已赋值。
def later_target_unbound():
    value = Original("later-target")
    try:
        try:
            raise value
        except Other as missing:
            print("WRONG.first-bound")
        except missing:
            print("WRONG.later-bound")
        except BaseException:
            print("WRONG.later-sibling")
    except UnboundLocalError as caught:
        print("lookup.later-unbound", caught.__context__ is value)

# 正文有赋值语句不代表异常路径执行到了它。
def body_target_unbound(early):
    value = Original("body-target")
    try:
        try:
            if early:
                raise value
            target = Original
        except target:
            print("WRONG.body-bound")
        except BaseException:
            print("WRONG.body-sibling")
    except UnboundLocalError as caught:
        print("lookup.body-unbound", caught.__context__ is value)

later_target_unbound()
body_target_unbound(True)

def handler_body_unbound(early):
    value = Original("handler-body")
    target = Original
    try:
        try:
            if early:
                raise value
            body_local = 7
        except target:
            print("WRONG.handler-body-value", body_local)
        except BaseException:
            print("WRONG.handler-body-sibling")
    except UnboundLocalError as caught:
        print("lookup.handler-body-unbound", caught.__context__ is value)

handler_body_unbound(True)

print("dispatch.complete", True)
