# abs 走类型特殊方法，不强制返回数值；实例上的同名属性不应遮蔽。
events = []
result = []
class Number:
    def __abs__(self):
        events.append('abs')
        return result
value = Number()
value.__abs__ = lambda: 7
print(abs(value) is result, events)
class NoneResult:
    def __abs__(self):
        return None
print(abs(NoneResult()) is None)
class Fail:
    def __abs__(self):
        raise ValueError('abs failed')
try:
    abs(Fail())
except ValueError as error:
    print(str(error))
