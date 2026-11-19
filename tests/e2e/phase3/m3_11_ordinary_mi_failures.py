# ryupy: pending
# 无自定义元类/原生基类的失败时序；异常帧文本不归本合同通过。
def emit(name, value):
    print(name, repr(value))
class A:
    pass
class B:
    pass
class AB(A, B):
    pass
class BA(B, A):
    pass
old = object()
Target = old
events = []
try:
    class Target(A, A):
        events.append('duplicate-body')
except TypeError as exc:
    emit('duplicate_base', [type(exc).__name__, str(exc), events, Target is old])
Target = old
events = []
try:
    class Target(AB, BA):
        events.append('inconsistent-body')
except TypeError as exc:
    emit('inconsistent_c3', [type(exc).__name__, str(exc), events, Target is old])
# 基类表达式失败发生在类体之前；旧名字不可被半成品替换。
Target = old
events = []
def bad_base():
    events.append('base')
    raise ValueError('base-failed')
try:
    class Target(bad_base(), A):
        events.append('unreachable-body')
except ValueError as exc:
    emit('base_expression_failure', [type(exc).__name__, str(exc), events, Target is old])
Target = old
events = []
try:
    class Target(AB):
        events.append('body')
        raise ValueError('body-failed')
except ValueError as exc:
    emit('body_failure_publication', [type(exc).__name__, str(exc), events, Target is old])
try:
    super(AB, A())
except TypeError as exc:
    emit('super_wrong_receiver', [type(exc).__name__, str(exc)])
# 验证失败没有毒化原有类的查找/类型关系。
emit('post_failure_existing_classes', [issubclass(AB, A), isinstance(BA(), B)])
