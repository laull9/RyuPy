# ryupy: pending
# 装饰器表达式/base/body/应用顺序，以及body失败和decorator失败的发布与cell状态。
log = []
escaped = []
def fail():
    raise ValueError('body')
def decorator(name, replace=False, abort=False):
    log.append('eval ' + name)
    def apply(cls):
        log.append('apply ' + name)
        escaped.append(cls)
        if abort:
            raise ValueError('decorator')
        if replace:
            return 'replacement'
        return cls
    return apply

class Base:
    pass

def base():
    log.append('base')
    return Base

def make():
    @decorator('outer', True)
    @decorator('inner')
    class Result(base()):
        log.append('body')
        def owner(self):
            return __class__
    return Result

print(make())
print(log)
print(escaped[0] is escaped[1], escaped[0]().owner() is escaped[0])

def failed_body():
    Result = 'old'
    try:
        class Result:
            def owner(self):
                return __class__
            escaped.append(owner)
            fail()
    except ValueError:
        print(Result)
    try:
        escaped[-1](None)
    except NameError:
        print('body cell empty')

def failed_decorator():
    Result = 'old'
    try:
        @decorator('failure', abort=True)
        class Result:
            def owner(self):
                return __class__
    except ValueError:
        print(Result)
    print(escaped[-1]().owner() is escaped[-1])

failed_body()
failed_decorator()
