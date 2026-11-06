# 既有类/实例__dict__反射边界：保留参考原设置方式，不与普通直接属性赋值混同。
class Text(str):
    @property
    def partition(self):
        return lambda sep: 'property-result'
    @property
    def rpartition(self):
        return lambda sep: 'property-result'


source = Text('a:b')
for method in ('partition', 'rpartition'):
    source.__dict__[method] = lambda sep: 'instance-result'
    assert getattr(source, method)(':') == 'property-result'
    print(method + '-property-over-dict', getattr(source, method)(':'))
    delattr(Text, method)
    assert getattr(source, method)(':') == 'instance-result'
    print(method + '-dict-over-root', getattr(source, method)(':'))
print('dict-setup-pending-ok')
