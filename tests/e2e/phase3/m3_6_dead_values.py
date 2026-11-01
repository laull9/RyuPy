# 窄死值消除只去掉未读Use/Box，不能丢循环后绑定、闭包或失败调用。
def unused(limit):
    for i in range(limit):
        pass
    return 'done'

def observed(limit):
    for i in range(limit):
        pass
    return i

def captured(limit):
    functions = []
    for i in range(limit):
        functions.append(lambda: i)
    return [fn() for fn in functions]

print(unused(4), observed(4), captured(4))
try:
    for i in range(1):
        discarded = int('bad')
except ValueError:
    print('call-error-preserved')

def suspended():
    for i in range(2):
        yield i

print(list(suspended()))

# 赋值结果未使用也必须执行独立的未绑定局部检查。
def unbound_read(flag):
    if flag:
        value = 1
    unused = value

try:
    unbound_read(False)
except UnboundLocalError:
    print('unbound-error-preserved')

def branch_read(flag):
    if flag:
        value = 1
    else:
        unused = value
    return value

try:
    branch_read(False)
except UnboundLocalError:
    print('else-unbound-preserved')
print(branch_read(True))

def both_branches(flag):
    if flag:
        value = 1
    else:
        value = None
    return value

print(both_branches(True), both_branches(False))

def nested_branch(first, second):
    if first:
        if second:
            value = 2
        else:
            value = 3
    unused = value

for flags in [(True, True), (True, False), (False, True)]:
    try:
        print(nested_branch(flags[0], flags[1]))
    except UnboundLocalError:
        print('nested-unbound-preserved')

def direct_read():
    unused = value
    value = 1

try:
    direct_read()
except UnboundLocalError:
    print('direct-unbound-preserved')

def only_else(flag):
    if flag:
        pass
    else:
        value = 4
    unused = value

try:
    only_else(True)
except UnboundLocalError:
    print('only-else-unbound-preserved')

def sibling_branch(first, second):
    if first:
        if second:
            value = 5
    else:
        unused = value
        value = 6

try:
    sibling_branch(False, True)
except UnboundLocalError:
    print('sibling-unbound-preserved')
