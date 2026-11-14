# 属性基值读取恰好一次；函数内类体和装饰器结束前保留局部旧绑定。
events = []
class Marker:
    pass
class Factory:
    @property
    def _dict(self):
        events.append('base')
        return dict
    def make(self):
        old = object()
        Local = old
        def decorate(cls):
            events.append(('decorator', Local is old, cls.seen is old))
            return cls
        @decorate
        class Local(self._dict, Marker):
            seen = Local
            events.append(('body', Local is old))
        return Local, old
factory = Factory()
created, old = factory.make()
print(events, created is old, created.seen is old, isinstance(created(), dict))

def fail():
    raise ValueError('body-failed')
def failed(factory):
    old = object()
    Local = old
    try:
        class Local(factory._dict, Marker):
            events.append('failing-body')
            fail()
    except ValueError as exc:
        return str(exc), Local is old
print(failed(factory), events)
