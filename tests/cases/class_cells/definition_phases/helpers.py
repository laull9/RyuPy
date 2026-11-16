phase = []
escaped = []
class Base:
    pass
class Replacement:
    pass
def method_decorator(closure):
    escaped.append(closure)
    try:
        closure()
    except NameError as exc:
        assert str(exc) == "cannot access free variable '__class__' where it is not associated with a value in enclosing scope"
        phase.append('expression empty')
    else:
        assert False
    def apply(fn):
        escaped.append(fn)
        try:
            fn(None)
        except NameError:
            phase.append('method empty')
        else:
            assert False
        return fn
    return apply
def class_decorator(original):
    assert original.ident(None) is original
    assert original.default(None) is original
    assert original.body_lambda() is original
    escaped.append(original)
    phase.append('class filled')
    return Replacement
