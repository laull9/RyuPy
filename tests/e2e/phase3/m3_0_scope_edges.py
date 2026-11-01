# M3.0：多层捕获、定义位置、默认参数和空 cell 的异常边界。
def factory(start):
    value = start
    readers = []
    for index in range(3):
        def read():
            return value, index
        readers.append(read)
    def change(next_value):
        nonlocal value
        value = next_value
    return readers, change


readers, change = factory(10)
print(readers[0](), readers[2]())
change(25)
print(readers[1]())
other, _ = factory(90)
print(other[0](), readers[0]())


def recursion():
    def even(n):
        return True if n == 0 else odd(n - 1)
    def odd(n):
        return False if n == 0 else even(n - 1)
    return even


print(recursion()(8), recursion()(7))


def transitive():
    value = 1
    def middle():
        nonlocal value
        value = 2
        def inner():
            nonlocal value
            value += 3
            return value
        return inner
    return middle()


print(transitive()())
value = 100


def global_barrier():
    value = 5
    def middle():
        global value
        def inner():
            return value
        return inner
    return middle()


print(global_barrier()())


def defaults(seed):
    def make():
        def call(a=seed, *args, b=seed + 1, **kwargs):
            return a, args, b, kwargs
        return call
    return make()


print(defaults(7)(9, 10, b=11, flag=12))
print(defaults(7)())
fn = lambda a=3, *args, b=4, **kw: (a, args, b, kw)
print(fn(), fn(8, 9, b=10, flag=11))
print((lambda x: lambda y: lambda z: x + y + z)(1)(2)(3))


def default_lambda(seed):
    def call(fn=lambda value=seed: value):
        return fn()
    return call


print(default_lambda(17)())


def empty_cell():
    def read():
        return missing
    try:
        read()
    except UnboundLocalError:
        print('wrong subtype')
    except NameError as error:
        print(type(error).__name__)
    missing = 42
    return read


print(empty_cell()())


def local_cell():
    def read():
        return missing
    try:
        print(missing)
    except UnboundLocalError as error:
        print(type(error).__name__)
    missing = 6
    return read


print(local_cell()())


def branch(flag):
    if flag:
        def chosen():
            return 1
    else:
        def chosen():
            return 2
    return chosen


print(branch(True)(), branch(False)())

# def 的默认值每次定义只求值一次，可变默认值在该函数的调用间共享。
def mutable_defaults():
    def append(value, items=[]):
        items.append(value)
        return items
    return append


first = mutable_defaults()
second = mutable_defaults()
a = first(1)
b = first(2)
print(a is b, a, second(9))


def shadowing():
    value = 11
    def outer_reader():
        return value
    def local_reader():
        value = 22
        return value
    return outer_reader, local_reader


outer_reader, local_reader = shadowing()
print(outer_reader(), local_reader())


def nested_global():
    def set_value():
        global newly_created
        newly_created = 99
    return set_value


nested_global()()
print(newly_created)
for number in range(3):
    def module_read(value=number):
        return value
    print(module_read())


def exception_cell():
    try:
        raise ValueError('captured')
    except ValueError as error:
        def read():
            return error
        print(str(read()))
    return read


read_error = exception_cell()
try:
    read_error()
except NameError as error:
    print(type(error).__name__)

# 默认参数先求值，再调用；lambda 可以出现在普通表达式接受的任意位置。
assert (lambda: True)()
print(f'{(lambda: 18)()}')
try:
    raise (lambda: ValueError('lambda raise'))()
except ValueError as error:
    print(str(error))
