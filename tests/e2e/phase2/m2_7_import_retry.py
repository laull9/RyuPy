# M2.7：失败模块允许重试，成功依赖的状态保留。
from fixtures.failure_case import state

try:
    from fixtures.failure_case import unstable
except RuntimeError:
    print("first failed", state.attempts)
from fixtures.failure_case import unstable
print(unstable.ready, state.attempts)
try:
    import ryupy_deliberately_missing_module
except ModuleNotFoundError:
    print("missing module")
