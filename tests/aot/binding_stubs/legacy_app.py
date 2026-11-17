# 两整数旧ABI的真实别名、关键字导出和严格参数协议。
import harvest_math as native

print(native.add(2147483647, 1), native.subtract(-7, 3))
print(getattr(native, 'class')(9, 4), native.alias(9, 4))
print(native.add is native.alias)
for kind in ['arity', 'kwargs', 'bool', 'overflow']:
    try:
        if kind == 'arity':
            native.add(1)
        elif kind == 'kwargs':
            native.add(left=1, right=2)
        elif kind == 'bool':
            native.add(True, 1)
        else:
            native.add(2**31, 1)
    except Exception as error:
        print(kind, type(error).__name__)
print('legacy-stub-ok')
