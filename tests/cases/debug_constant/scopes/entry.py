# 固定非 -O 的语言常量，与宿主 Release/Debug 配置无关。
print('module', __debug__, type(__debug__) is bool)
def outer(default=__debug__):
    captured = __debug__
    def inner(value=__debug__):
        return __debug__, captured, value
    return default, inner()
class C:
    value = __debug__
    def read(self, default=__debug__):
        return __debug__, default
print('function-closure', outer())
print('class', C.value, C().read())
print('conditional', 'enabled' if __debug__ else 'disabled')
