# 普通构造 B 的唯一 AOT 编译源：CP 正行为与明确 Ryu 边界分开运行。
# 只打印固定 marker；末尾保留真实模块/调用者帧，不输出地址或环境目录。
# 本文件行号及 LF 源字节属于验收合同，改动后必须重新审定固定期望。
import sys
from dataclasses import dataclass
from enum import Enum
from unittest import TestCase

if len(sys.argv) != 2:
    raise ValueError('expected exactly one construction mode')
mode = sys.argv[1]
if mode != 'positive' and mode != 'ryupy-boundaries':
    raise ValueError('unknown construction mode')

if mode == 'positive':
    # 根对象只验身份、缺省哨兵与容器传递，不引入富比较长尾。
    MISSING = object()

    def supplied(value=MISSING):
        return value is MISSING

    def identity(value):
        return value

    ctor = object
    a = ctor()
    b = object()
    assert supplied() and not supplied(None)
    assert ctor is object and a is not b
    assert type(a) is object and type(b) is object
    assert identity(a) is a and [a][0] is a and {'value': a}['value'] is a
    print('sentinel-ok')

    # foreign/None 不调用失败 init；只有返回真实实例才继续初始化。
    allocation_log = []
    foreign = object()

    class Foreign:
        def __new__(cls, value):
            allocation_log.append(value)
            return foreign

        def __init__(self, value):
            raise AssertionError('foreign init')

    assert Foreign(1) is foreign and allocation_log == [1]
    assert type(foreign) is object

    class Nothing:
        def __new__(cls):
            allocation_log.append('none')
            return None

        def __init__(self):
            raise AssertionError('none init')

    assert Nothing() is None and allocation_log == [1, 'none']
    token = object()
    order_log = []

    class Ordered:
        def __new__(cls, value):
            assert value is token
            order_log.append('new')
            return object.__new__(cls)

        def __init__(self, value):
            assert value is token
            order_log.append('init')

    assert type(Ordered(value=token)) is Ordered
    assert order_log == ['new', 'init']

    # 返回子类时选择实际 Child 的 init，而不是请求的 Parent 的 init。
    class Parent:
        def __new__(cls, value):
            return object.__new__(Child)

        def __init__(self, value):
            raise AssertionError('wrong requested-class init')

    class Child(Parent):
        def __init__(self, value):
            self.value = value

    result = Parent(7)
    assert type(result) is Child and result.value == 7
    cache_log = []

    class Cached:
        def __new__(cls, value):
            return saved

        def __init__(self, value):
            cache_log.append(value)

    saved = object.__new__(Cached)
    assert Cached(2) is saved and Cached(3) is saved
    assert type(saved) is Cached and cache_log == [2, 3]

    # new 内重绑 init 对本次构造立即生效，别名/一等调用不绕过协议。
    def later(self, value):
        self.value = value + 10

    class Mutable:
        def __new__(cls, value):
            cls.__init__ = later
            return object.__new__(cls)

        def __init__(self, value):
            raise AssertionError('stale init')

    assert Mutable(4).value == 14
    Alias = Mutable
    assert Alias(5).value == 15

    def through(factory):
        return factory(6)

    assert through(Alias).value == 16
    print('allocation-ok')

    # 类体最终 own 函数在类装饰器前静态化；已有 wrapper 保持原身份。
    observed = []

    def raw(cls, *args, **kwargs):
        return object.__new__(cls)

    def observe(cls):
        assert type(cls.__dict__['__new__']).__name__ == 'staticmethod'
        observed.append(cls.__name__)
        return cls

    def replace(function):
        return raw

    @observe
    class FromOwn:
        def __new__(cls):
            return object.__new__(cls)

    @observe
    class FromAlias:
        __new__ = raw

    @observe
    class FromDecorator:
        @replace
        def __new__(cls):
            raise AssertionError('decorated original')

    @observe
    class Conditional:
        __new__ = None
        if True:
            __new__ = raw

    explicit = staticmethod(raw)

    @observe
    class FromExplicit:
        __new__ = explicit

    assert observed == ['FromOwn', 'FromAlias', 'FromDecorator', 'Conditional', 'FromExplicit']
    own_instance = FromOwn()
    assert type(own_instance) is FromOwn
    assert own_instance.__new__ is FromOwn.__new__
    for cls in [FromAlias, FromDecorator, Conditional, FromExplicit]:
        instance = cls()
        assert type(instance) is cls and instance.__new__ is raw
    assert FromExplicit.__dict__['__new__'] is explicit

    class Late:
        pass

    Late.__new__ = raw
    assert type(Late.__dict__['__new__']).__name__ == 'function'
    assert type(object.__new__(Late).__new__).__name__ == 'method'
    assert type(Late()) is Late

    # 零参 super 用真实定义 owner；继承槽不复制到 Leaf，晚绑定使缓存失效。
    binding_log = []

    class Base:
        def __new__(cls, *args):
            assert cls is Leaf
            binding_log.append('new')
            return super().__new__(cls)

        def __init__(self, *args):
            assert type(self) is Leaf
            binding_log.append('init')
            super().__init__()

    class Leaf(Base):
        pass

    assert '__new__' not in Leaf.__dict__
    assert type(Leaf(1)) is Leaf and binding_log == ['new', 'init']

    def replacement(cls, *args):
        assert cls is Leaf
        binding_log.append('replacement')
        return object.__new__(cls)

    Base.__new__ = replacement
    assert type(Leaf(2)) is Leaf
    assert binding_log == ['new', 'init', 'replacement', 'init']
    assert '__new__' not in Leaf.__dict__

    # 仅已有 classmethod/staticmethod 槽形态，不扩一般 descriptor。
    wrapper_log = []

    class ClassNew:
        @classmethod
        def __new__(bound, explicit_cls, value):
            assert bound is explicit_cls and explicit_cls is ClassNew
            wrapper_log.append(value)
            return object.__new__(explicit_cls)

    class StaticInit:
        @staticmethod
        def __init__(value):
            wrapper_log.append(value)

    class ClassInit:
        @classmethod
        def __init__(cls, value):
            assert cls is ClassInit
            wrapper_log.append(value)

    assert type(ClassNew(3)) is ClassNew
    assert type(StaticInit(4)) is StaticInit
    assert type(ClassInit(5)) is ClassInit
    assert wrapper_log == [3, 4, 5]
    print('binding-ok')

    # 每种失败同时锁异常精确类型和活跃异常的 context 身份。
    class BadReturn:
        def __init__(self):
            return 7

    class BindNew:
        def __new__(cls, required):
            return object.__new__(cls)

    class BindInit:
        def __init__(self, required):
            pass

    class NewError:
        def __new__(cls):
            raise RuntimeError('new-error')

    class InitError:
        def __init__(self):
            raise RuntimeError('init-error')

    for cls, expected in [(BadReturn, TypeError), (BindNew, TypeError), (BindInit, TypeError), (NewError, RuntimeError), (InitError, RuntimeError)]:
        try:
            raise ValueError('active')
        except ValueError as active:
            try:
                cls()
            except (TypeError, RuntimeError) as error:
                assert type(error) is expected and error.__context__ is active
                print(cls.__name__, type(error).__name__)
            else:
                raise AssertionError('error missing')

    argument_log = []

    def arg(value):
        argument_log.append(value)
        return value

    class NonCallable:
        __new__ = None

    try:
        NonCallable(arg(1), value=arg(2))
    except TypeError as error:
        assert type(error) is TypeError and argument_log == [1, 2]
    else:
        raise AssertionError('noncallable new')
    print('errors-ok')
    print('positive-assertions-ok')

    # 故意终止于原始三帧；此处已退出全部捕获 handler，不能附带异常链。
    class Failure:
        def __new__(cls):
            raise ValueError('new-body')

    def caller():
        Failure()

    caller()

elif mode == 'ryupy-boundaries':
    # 此模式不跑 CP 参考；合法但尚不支持的槽历史明确拒绝。

    ordinary_log = []

    class Ordinary:
        def __new__(cls):
            ordinary_log.append('new')
            return object.__new__(cls)

    original = Ordinary.__dict__['__new__']
    assert type(Ordinary()) is Ordinary and ordinary_log == ['new']

    def reset_root():
        Ordinary.__new__ = object.__new__

    def remove_new():
        del Ordinary.__new__

    def reset_root_wrapper():
        Ordinary.__new__ = staticmethod(object.__new__)

    successful_calls = 1
    for change in [reset_root, remove_new, reset_root_wrapper]:
        try:
            change()
        except NotImplementedError as error:
            assert type(error) is NotImplementedError
        else:
            raise AssertionError('rare slot history silently accepted')
        assert Ordinary.__dict__['__new__'] is original
        assert ordinary_log == ['new'] * successful_calls
        assert type(Ordinary()) is Ordinary
        successful_calls += 1
        assert ordinary_log == ['new'] * successful_calls

    class NoOwn:
        pass

    try:
        del NoOwn.__new__
    except AttributeError as error:
        assert type(error) is AttributeError
    else:
        raise AssertionError('missing own new deletion accepted')
    assert '__new__' not in NoOwn.__dict__ and type(NoOwn()) is NoOwn
    print('ordinary-mutation-atomic-ok')

    # 真实专用路径先成功。dataclass 无基类，TestCase 只改测试自有祖先。
    @dataclass
    class Data:
        value: int

    class Error(Exception):
        pass

    class Color(Enum):
        RED = 1

    class CaseParent(TestCase):
        pass

    class CaseChild(CaseParent):
        pass

    assert Data(1).value == 1 and Data(2).value == 2
    assert isinstance(Error('message'), Exception)
    assert Color(1) is Color.RED
    case = CaseChild()
    assert isinstance(case, TestCase) and type(case) is CaseChild
    assert case._testMethodName == 'runTest'
    special_log = []

    def special_new(cls, *args):
        special_log.append('new')
        return object.__new__(cls)

    def special_init(self, *args):
        special_log.append('init')

    # Data 保留生成 init，仅 late new 必须挡在已缓存快计划分配之前。
    # Error 另验 init 无副作用；setter 成功后才捕获实际构造拒绝。
    for cls in [Data, Error]:
        if cls is Error:
            cls.__init__ = special_init
        cls.__new__ = special_new
        try:
            cls(1)
        except NotImplementedError as error:
            assert type(error) is NotImplementedError and special_log == []
        else:
            raise AssertionError('special layout custom new ignored')

    CaseParent.__init__ = special_init
    CaseParent.__new__ = special_new
    case_factory = CaseChild
    try:
        case_factory()
    except NotImplementedError as error:
        assert type(error) is NotImplementedError and special_log == []
    else:
        raise AssertionError('late ancestor custom new ignored')

    # Enum 现有守卫在写入时拒绝；锁定该阶段，不冒称构造已执行。
    enum_stage = 'set'
    try:
        Color.__new__ = special_new
        enum_stage = 'construct'
        Color(1)
    except NotImplementedError as error:
        assert type(error) is NotImplementedError and special_log == []
        assert enum_stage == 'set'
    else:
        raise AssertionError('Enum custom new silently accepted')
    print('special-layout-guard-ok')
    print('ryupy-boundaries-ok')

    # 单独的原始调用者删除仍保留的 own 槽，故意产生确定的两帧终端错误。
    assert Ordinary.__dict__['__new__'] is original

    def boundary_caller():
        del Ordinary.__new__

    boundary_caller()
