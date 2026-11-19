# 动态 handler 清理：probe 只看身份/ambient，不用这些对象证明帧不变。
class Original(Exception):
    pass

class Ambient(Exception):
    pass

class Marker(Exception):
    pass

def active_is(value):
    try:
        raise
    except BaseException as caught:
        if value is None:
            return isinstance(caught, RuntimeError)
        return caught is value

def flow(mode, ambient):
    target = Original
    for index in range(2):
        original = Original(mode)
        try:
            try:
                raise original
            except target as caught:
                print("flow.body", mode, index, caught is original, active_is(original))
                if mode == "return":
                    return "returned"
                if mode == "break":
                    break
                if mode == "continue":
                    continue
        finally:
            try:
                caught
            except NameError:
                print("flow.deleted", mode, index)
            print("flow.restored", mode, index, active_is(ambient))
    return "finished"

print("cleanup.clear-before", active_is(None))
for mode in ("return", "break", "continue", "fallthrough"):
    print("flow.result", mode, flow(mode, None))
print("cleanup.clear-after", active_is(None))

ambient = Ambient("ambient")
try:
    raise ambient
except Ambient:
    print("ambient.before", active_is(ambient))
    print("ambient.result", flow("return", ambient))
    print("ambient.after", active_is(ambient))
print("ambient.clear", active_is(None))

target = Original
original = Original("body")
marker = Marker("body-marker")
try:
    try:
        raise original
    except target as caught:
        raise marker
except Marker as caught:
    print("body.context", caught is marker, caught.__context__ is original)
print("body.clear", active_is(None))

other = Marker
original = Original("unmatched")
try:
    try:
        raise original
    except other:
        print("WRONG.unmatched")
    finally:
        print("unmatched.finally", active_is(original))
except Original as caught:
    print("unmatched.same", caught is original)
print("unmatched.clear", active_is(None))

def override():
    target = Original
    try:
        try:
            raise Original("override")
        except target:
            return "body"
    finally:
        return "finally"

print("finally.override", override())
try:
    try:
        pass
    except target:
        print("WRONG.else-caught")
    else:
        raise Original("else")
except Original:
    print("else.escaped", True)

# 独立 fresh 对象检查 from None 的字段，不通过 probe 观察它们。
fresh_original = Original("suppressed-original")
fresh_marker = Marker("suppressed-marker")
try:
    try:
        raise fresh_original
    except target:
        raise fresh_marker from None
except Marker as caught:
    print("suppression.fields", caught is fresh_marker, caught.__context__ is fresh_original,
          caught.__cause__ is None, caught.__suppress_context__)
class Lookup:
    @property
    def target(self):
        print("nested.lookup", active_is(nested_original))
        raise nested_marker

nested_original = Original("nested-original")
nested_marker = Marker("nested-marker")
lookup = Lookup()
try:
    raise ambient
except Ambient:
    try:
        try:
            raise nested_original
        except lookup.target:
            print("WRONG.nested-selected")
    except Marker as caught:
        print("nested.marker", caught is nested_marker, caught.__context__ is nested_original, active_is(nested_marker))
    print("nested.ambient-restored", active_is(ambient))
print("nested.clear", active_is(None))

print("cleanup.complete", True)
