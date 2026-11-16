"""C1 类构造 oracle：外部元类、prepared namespace、重入与失败观察。"""
events = []
reads = []
disposed_namespaces = 0


class Namespace(dict):
    def __contains__(self, name):
        raise AssertionError('class lookup must use __getitem__ once')

    def __del__(self):
        global disposed_namespaces
        disposed_namespaces += 1

    def __getitem__(self, name):
        if name in ('prepared', 'body_value', 'missing_name'):
            reads.append(name)
        return super().__getitem__(name)

    def __setitem__(self, name, value):
        if name == 'body_value':
            events.append('write:body_value')
        super().__setitem__(name, value)


class Meta(type):
    @classmethod
    def __prepare__(meta, name, bases):
        events.append('prepare:' + name)
        return Namespace(prepared=7)

    def __new__(meta, name, bases, namespace):
        events.append('new:' + name)
        return super().__new__(meta, name, bases, dict(namespace))


class Base(metaclass=Meta):
    def __init_subclass__(cls):
        events.append('init_subclass:' + cls.__name__)
        # 构造回调中的 classmethod 收到同一正在建立的真实类型。
        callback = cls.__dict__.get('on_construct')
        if callback is not None:
            result = cls.on_construct()
            assert result is cls


def reset():
    global disposed_namespaces
    events.clear()
    reads.clear()
    disposed_namespaces = 0


def body():
    events.append('body')
    return 11


def snapshot():
    return list(events), list(reads)


def disposed():
    return disposed_namespaces


class FailPrepareMeta(type):
    @classmethod
    def __prepare__(meta, name, bases):
        raise ValueError('prepare marker')


class NonePrepareMeta(type):
    __prepare__ = None


class ForeignFailureBase:
    def __init_subclass__(cls):
        raise ValueError('init_subclass marker')


class OtherMeta(type):
    pass


class OtherBase(metaclass=OtherMeta):
    pass


FailPrepareBase = type.__new__(FailPrepareMeta, 'FailPrepareBase', (object,), {})
NonePrepareBase = type.__new__(NonePrepareMeta, 'NonePrepareBase', (object,), {})


class FailInitMeta(Meta):
    def __init__(cls, name, bases, namespace):
        super().__init__(name, bases, namespace)
        if name != 'FailInitBase':
            raise ValueError('meta init marker')


FailInitBase = type.__new__(FailInitMeta, 'FailInitBase', (Base,), {})


def fail_body():
    raise ValueError('body marker')


def never_body():
    events.append('unexpected body')
    raise AssertionError('body should not run')
