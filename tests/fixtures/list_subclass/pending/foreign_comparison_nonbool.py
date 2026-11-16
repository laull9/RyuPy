# ryupy: pending
# CP合法：表达式比较返回原对象；当前bool比较ABI必须显式NI，不可擅自调用bool。
class L(list):
    pass
class Result:
    def __bool__(self):
        print('UNEXPECTED BOOL')
        return True
result = Result()
class Foreign:
    def __eq__(self, other):
        print('eq-result')
        return result
    def __ne__(self, other):
        print('ne-result')
        return result
value = L([1])
foreign = Foreign()
print((value == foreign) is result)
print((foreign == value) is result)
print((value != foreign) is result)
print((foreign != value) is result)
