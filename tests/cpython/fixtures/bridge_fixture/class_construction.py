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


def clear_traceback(error):
    # 真实 traceback 的 frame 合法持有 prepared namespace；显式撤销后检查构造自身已释放引用。
    error.__traceback__ = None


class TransformNamespace(Namespace):
    def __setitem__(self, name, value):
        if name == '__module__':
            value = 'mapped.' + value
        super().__setitem__(name, value)


class TransformMeta(Meta):
    @classmethod
    def __prepare__(meta, name, bases):
        return TransformNamespace()

    def __new__(meta, name, bases, namespace):
        result = super().__new__(meta, name, bases, namespace)
        result.__qualname__ = 'rebuilt.' + name
        result.__name__ = 'renamed_' + name
        return result


TransformBase = type.__new__(TransformMeta, 'TransformBase', (object,), {})


def never_body():
    events.append('unexpected body')
    raise AssertionError('body should not run')


class ShadowNamespace(Namespace):
    def __getitem__(self, name):
        if name == '__name__':
            reads.append('namespace:__name__')
        return super().__getitem__(name)


class ShadowMeta(Meta):
    @classmethod
    def __prepare__(meta, name, bases):
        return ShadowNamespace(__name__='namespace_name', x='namespace_cell')


ShadowBase = type.__new__(ShadowMeta, 'ShadowBase', (object,), {})
prepare_callback = None


def set_prepare_callback(callback):
    global prepare_callback
    prepare_callback = callback


class ChangingMeta(Meta):
    @classmethod
    def __prepare__(meta, name, bases):
        prepare_callback()
        return Namespace()


ChangingBase = type.__new__(ChangingMeta, 'ChangingBase', (object,), {})


class EagerBase:
    def __init_subclass__(cls):
        if cls.__dict__.get('eager_new'):
            cls()
        mode = cls.__dict__.get('eager_property')
        if mode:
            value = object.__new__(cls)
            if mode == 'setter':
                value.value = 1
            else:
                del value.value
        if cls.__dict__.get('eager_inherited'):
            assert cls.inherited_callback() is cls


class SetNameMeta(type):
    def __new__(meta, name, bases, namespace):
        values = dict(namespace)
        descriptor = values.pop('marker', None)
        created = super().__new__(meta, name, bases, values)
        if descriptor is not None:
            created.marker = descriptor
            descriptor.__set_name__(created, 'marker')
        return created


SetNameBase = type.__new__(SetNameMeta, 'SetNameBase', (object,), {})
