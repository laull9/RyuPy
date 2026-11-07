# 通用显式super合同；不使用项目名或TLS推断起始类，供默认与Instance同源差分。
events = []
alias = super

class Root:
    def read(self):
        return 'root:' + self.tag

    @property
    def value(self):
        return self.tag

    @classmethod
    def cls(cls):
        return cls

    @staticmethod
    def static():
        return 'static'

class Middle(Root):
    def read(self):
        return 'middle:' + self.tag

class Leaf(Middle):
    def __init__(self, tag):
        self.tag = tag

    def bypass(self):
        return super(Middle, self).read()

class Other:
    pass

def outside(start, receiver):
    return alias(start, receiver)

def invoke(proxy):
    return proxy.read()

def old(self, arg):
    return 'old:' + self.tag + ':' + arg

def new(self, arg):
    return 'new:' + self.tag + ':' + arg

def arg():
    events.append('arg')
    return 'arg'

def replace():
    events.append('arg')
    Root.call = new
    return 'arg'

def remove():
    events.append('arg')
    del Root.call
    return 'arg'

def getter(self):
    events.append('get')
    return arg_target

def broken_getter(self):
    events.append('get')
    raise ValueError('getter')

def arg_target(value):
    return value

def first():
    events.append('first')
    return 1

def second():
    events.append('second')
    return Other()

def run():
    x = Leaf('x')
    y = Leaf('y')
    assert x.bypass() == 'root:x'
    assert super(Leaf, x).read() == 'middle:x'
    p = outside(Middle, x)
    q = outside(Middle, y)
    assert [invoke(p), invoke(q), invoke(p)] == ['root:x', 'root:y', 'root:x']
    assert p.value == 'x' and p.cls() is Leaf and p.static() == 'static'
    x.read = 'shadow'
    assert p.read() == 'root:x'
    assert type(p) is super and isinstance(p, super)
    saved = p.read
    Root.read = new_read
    assert invoke(p) == 'changed:x' and invoke(q) == 'changed:y'
    assert saved() == 'root:x'
    del Root.read
    try:
        invoke(p)
    except AttributeError:
        pass
    else:
        assert False
    Root.read = new_read
    assert invoke(p) == 'changed:x'
    Root.call = old
    events.clear()
    assert p.call(replace()) == 'old:x:arg'
    assert events == ['arg']
    assert p.call('next') == 'new:x:next'
    events.clear()
    assert p.call(remove()) == 'new:x:arg'
    assert events == ['arg']
    events.clear()
    try:
        p.call(arg())
    except AttributeError:
        assert events == []
    else:
        assert False
    Root.call = property(getter)
    assert p.call(arg()) == 'arg'
    assert events == ['get', 'arg']
    events.clear()
    Root.call = property(broken_getter)
    try:
        p.call(arg())
    except ValueError:
        assert events == ['get']
    else:
        assert False
    events.clear()
    Root.call = 42
    try:
        p.call(arg())
    except TypeError:
        assert events == ['arg']
    else:
        assert False
    events.clear()
    try:
        alias(first(), second())
    except TypeError as error:
        assert str(error) == 'super() argument 1 must be a type, not int'
        assert events == ['first', 'second']
    else:
        assert False
    try:
        alias(Middle, Other())
    except TypeError as error:
        assert str(error) == 'super(type, obj): obj must be an instance or subtype of type'
    else:
        assert False
    try:
        alias(Middle, x, 3)
    except TypeError as error:
        assert str(error) == 'super() expected at most 2 arguments, got 3'
    else:
        assert False
    try:
        alias(Middle, x, extra=1)
    except TypeError as error:
        assert str(error) == 'super() takes no keyword arguments'
    else:
        assert False
    # 有界无副作用展开通过既有binder；不外推混合star迭代顺序。
    assert alias(*[Middle, x]).value == 'x'
    assert alias(Middle, x, **{}).value == 'x'
    print('explicit super identity binding mutation phases')

def new_read(self):
    return 'changed:' + self.tag
