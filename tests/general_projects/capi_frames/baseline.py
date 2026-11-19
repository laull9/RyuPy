# 独立CP oracle：两个原生扩展直接观察真实调用字典、源码和嵌套回调，异常类型/args保持原值。
import native_scope_probe as scope
import native_callback_probe as callback
facts = scope.facts()
assert facts[0] is globals() and facts[1] is globals() and facts[2] == __file__
def probe():
    values = scope.facts()
    assert values[0] is globals() and values[2] == __file__
    return 42
before, value, restored = callback.call(probe)
assert before is globals() and value == 42 and restored
try:
    scope.fail()
except KeyError as error:
    assert error.args == ('native-scope-probe',)
else:
    raise AssertionError('native exception lost')
print('native-capi-baseline-ok')
