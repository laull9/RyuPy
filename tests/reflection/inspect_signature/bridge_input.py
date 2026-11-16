# ryupy: pending
# CP真实inspect与CIL映射：bootstrap身份、monkeypatch、defaults/annotations及活BoundArguments。
import inspect
import bridge_callbacks as compiled

saved_signature = inspect.signature
assert compiled.current_signature_api() is saved_signature
assert compiled.api_identity(saved_signature) is saved_signature
snapshot = inspect.signature(compiled.direct)
assert type(snapshot) is inspect.Signature
parameter = snapshot.parameters["value"]
assert type(parameter) is inspect.Parameter
assert parameter.kind is inspect.Parameter.POSITIONAL_OR_KEYWORD
assert parameter.annotation is inspect.Parameter.empty is inspect.Signature.empty is inspect._empty
assert snapshot.parameters["value"].default is compiled.shared
assert compiled.api_identity(snapshot) is snapshot
assert compiled.snapshot(compiled.direct).parameters["value"].default is compiled.shared

replacement = []
compiled.direct.__defaults__ = (replacement,)
keywords = {"option": replacement}
compiled.direct.__kwdefaults__ = keywords
annotations = {"value": int, "return": str}
compiled.direct.__annotations__ = annotations
updated = inspect.signature(compiled.direct)
assert updated.parameters["value"].default is replacement
assert updated.parameters["option"].default is replacement
assert updated.parameters["value"].annotation is int
assert updated.return_annotation is str
assert compiled.direct() is replacement
assert snapshot.parameters["value"].default is compiled.shared
keywords["option"] = 9
assert inspect.signature(compiled.direct).parameters["option"].default == 9
annotations["return"] = int
assert inspect.signature(compiled.direct).return_annotation is int
assert compiled.direct.__globals__ is compiled.__dict__

bound = compiled.make_bound()
assert type(bound) is inspect.BoundArguments
bound.arguments = {"value": replacement, "option": 11}
arguments, args, kwargs = compiled.read_bound(bound)
assert arguments is bound.arguments
assert args[0] is replacement and kwargs == {"option": 11}
assert compiled.api_identity(bound) is bound
assert compiled.apply_bound(bound) is bound
external = saved_signature(lambda a=3: a).bind()
assert compiled.apply_bound(external) is external
assert external.arguments == {"a": 3}
assert inspect.signature(compiled.wrapper).parameters["value"].default is replacement
custom = inspect.Signature([inspect.Parameter("chosen", inspect.Parameter.KEYWORD_ONLY)])
compiled.wrapper.__signature__ = custom
assert inspect.signature(compiled.wrapper) is custom
assert inspect.signature(compiled.wrapper, follow_wrapped=False) is custom

def changed(value):
    return custom

inspect.signature = changed
assert compiled.current_signature_api() is changed
assert compiled.api_identity(saved_signature) is saved_signature
assert compiled.inspect_names(compiled.direct) == ["chosen"]
assert compiled.api_identity(inspect.Parameter.empty) is inspect.Parameter.empty
assert compiled.api_identity(inspect.Parameter.KEYWORD_ONLY) is inspect.Parameter.KEYWORD_ONLY
inspect.signature = saved_signature
assert compiled.current_signature_api() is saved_signature
assert compiled.inspect_names(compiled.direct) == ["value", "option"]
del compiled.direct.__defaults__
del compiled.direct.__kwdefaults__
try:
    compiled.direct()
except TypeError:
    print("bridge: deletion affects next call")
else:
    raise AssertionError("missing defaults must fail")
print("bridge: real identity live metadata and monkeypatch")
