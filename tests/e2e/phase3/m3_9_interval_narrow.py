# 合流值域证明只替换原加法结果，保留原盒身份与参数/异常时序。
def choose(flag):
    if flag:
        value = 1000
    else:
        value = 1007
    original = value
    result = value + 0
    print(value is original, result is value)
    return result + 2

print(choose(True), choose(False))

def mixed(flag):
    if flag:
        value = True
    else:
        value = 7
    return value + 2
print(mixed(True), mixed(False))

def edge(flag):
    if flag:
        value = 9223372036854775807
    else:
        value = -9223372036854775808
    return value + 1
print(edge(True), edge(False))

def missing(flag):
    if flag:
        value = 3
    return value + 2
try:
    missing(False)
except UnboundLocalError:
    print('unbound')
print(missing(True))

def growing(n):
    value = 0
    for index in range(n):
        value = value + 1
        print(value)
    return value
print('growth', growing(4))

class Other:
    def __add__(self, other):
        print('custom-add', other)
        return 'other-result'

def unknown(flag, supplied):
    if flag:
        value = 3
    else:
        value = supplied
    return value + 2
print(unknown(True, Other()), unknown(False, Other()))

def observed(flag):
    if flag:
        value = 1000
    else:
        value = 1007
    alias = value
    result = value + 2
    return value is alias, result
print(observed(True), observed(False))

def protected(flag):
    try:
        if flag:
            value = 3
        else:
            value = 7
        return value + 2
    finally:
        print('finally')
print(protected(True))

def suspended(flag):
    if flag:
        value = 3
    else:
        value = 7
    yield value
    yield value + 2
print(list(suspended(False)))

def closure(flag):
    value = 3
    def read():
        return value
    if flag:
        value = 7
    return value + 2, read()
print(closure(True), closure(False))
