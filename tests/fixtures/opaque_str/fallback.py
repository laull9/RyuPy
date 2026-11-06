# 消费opaque值时沿原动态语义，不从Bool结果误判为可静态执行。
log = []
class Token:
    value = 17
    def __bool__(self):
        log.append('bool')
        return True
    def __eq__(self, other):
        log.append('eq')
        return True
    def __add__(self, other):
        log.append('add')
        return 23
    def __getitem__(self, key):
        log.append('index')
        return self
    def __call__(self):
        log.append('call')
        return self
    def __str__(self):
        log.append('str')
        return 'token'

def truth(s: str) -> int:
    if s:
        return 1
    return 0

def equal(s: str) -> bool:
    return s == 'x'

def add(s: str) -> int:
    return s + 1

def index(s: str) -> str:
    return s[0]

def call(s: str) -> str:
    return s()

def attr(s: str) -> int:
    return s.value

def convert(s: str) -> str:
    return str(s)

def negative(s: str) -> bool:
    return not s

def both(s: str) -> str:
    return s and s

value = Token()
assert truth(value) == 1
assert equal(value) is True
assert add(value) == 23
assert index(value) is value
assert call(value) is value
assert attr(value) == 17
assert convert(value) == 'token'
assert negative(value) is False
assert both(value) is value
assert log == ['bool', 'eq', 'add', 'index', 'call', 'str', 'bool', 'bool']
print('opaque-fallback-protocols')
