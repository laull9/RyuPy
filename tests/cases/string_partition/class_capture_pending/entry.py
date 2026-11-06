# 既有str子类class/super捕获边界的独立CP参考；仅据CP冻结，不据静态审查断言JIT失败。
events = []
for method in ('partition', 'rpartition'):
    class Text(str):
        pass
    source = Text('a:b:c')
    def new(self, sep):
        events.append('new')
        return 'new-result'
    def old(self, sep):
        events.append('old')
        return 'old-result'
    def change():
        events.append('argument')
        setattr(Text, method, new)
        return ':'
    events.clear()
    if method == 'partition':
        result = Text.partition(source, change())
    else:
        result = Text.rpartition(source, change())
    assert events == ['argument']
    print(method, 'class-inherited', result, events)
    setattr(Text, method, old)
    events.clear()
    if method == 'partition':
        result = Text.partition(source, change())
    else:
        result = Text.rpartition(source, change())
    assert result == 'old-result' and events == ['argument', 'old']
    print(method, 'class-override', result, events)
    setattr(Text, method, old)
    class Child(Text):
        def call(self):
            if method == 'partition':
                return super().partition(change())
            return super().rpartition(change())
    events.clear()
    result = Child('a:b:c').call()
    assert result == 'old-result' and events == ['argument', 'old']
    print(method, 'super-override', result, events)
print('class-capture-probe-ok')
