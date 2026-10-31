# M2.7：相对导入、缓存身份、模块全局与 from 绑定快照。
import fixtures.module_case as package
from fixtures.module_case import state
from fixtures.module_case.state import value
from fixtures.module_case.worker import increment
import fixtures.module_case.state as again

print(package.label, state is again, state.value, value)
print(increment(), state.value, value)
print(state.__name__, state.__package__)
print(__name__)
if __name__ == "__main__":
    print("entry")
