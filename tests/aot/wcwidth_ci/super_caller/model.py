# Legacy类模型和副作用回调；不将class/del冒称为Instance能力。
events = []


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


# 正向类身份别名；同名异身份反例在独立Legacy模块，不重定义类。
OriginalMiddle = Middle


def old(self, value):
    return 'old:' + self.tag + ':' + value


def new(self, value):
    return 'new:' + self.tag + ':' + value


def changed(self):
    return 'changed:' + self.tag


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


def target(value):
    return value


def getter(self):
    events.append('get')
    return target


def broken(self):
    events.append('get')
    raise ValueError('getter')
