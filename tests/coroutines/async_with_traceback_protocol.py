# 显式/裸重抛、同深度新调用和同一协程恢复的traceback激活身份。
def names(tb):
    result = []
    while tb is not None:
        result.append(tb.tb_frame.f_code.co_name)
        tb = tb.tb_next
    return result
def reraises(explicit):
    error = ValueError("same")
    try:
        raise error
    except ValueError:
        original = error.__traceback__
        try:
            if explicit:
                raise error
            else:
                raise
        except ValueError as caught:
            print(caught is error, names(caught.__traceback__))
            print(caught.__traceback__ is original, caught.__traceback__.tb_next is original)
reraises(False)
reraises(True)
def helper():
    raise
def from_helper():
    error = ValueError("helper")
    try:
        raise error
    except ValueError:
        try:
            helper()
        except ValueError as caught:
            print(names(caught.__traceback__))
from_helper()
def capture():
    try:
        raise ValueError("slot")
    except ValueError as error:
        return error.__traceback__
first = capture()
second = capture()
print(first.tb_frame is second.tb_frame)
print(first.tb_frame.f_code is second.tb_frame.f_code)
try:
    first.tb_next = first
except ValueError as error:
    print(str(error))
try:
    first.tb_next = 1
except TypeError as error:
    print(str(error))
first.tb_next = second
print(first.tb_next is second)
first.tb_next = None
print(first.tb_next is None)
class Pause:
    def __await__(self):
        yield "waiting"
async def resumed():
    try:
        raise ValueError("before")
    except ValueError as error:
        before = error.__traceback__
    await Pause()
    try:
        raise ValueError("after")
    except ValueError as error:
        after = error.__traceback__
    print(before.tb_frame is after.tb_frame)
    print(before.tb_frame.f_lineno == after.tb_frame.f_lineno)
    print(before.tb_lineno != after.tb_lineno)
c = resumed()
print(c.send(None))
try:
    c.send(None)
except StopIteration:
    pass

class PassThrough:
    def __enter__(self):
        return self
    def __exit__(self, kind, value, tb):
        return False
def propagate():
    with PassThrough():
        raise ValueError("pass-through")
try:
    propagate()
except ValueError as error:
    print(names(error.__traceback__))

def nested_propagate():
    with PassThrough(), PassThrough():
        raise ValueError("nested-pass")
try:
    nested_propagate()
except ValueError as error:
    print(names(error.__traceback__))
async def repeated_throw():
    for index in range(2):
        try:
            await Pause()
        except ValueError as error:
            tb = error.__traceback__
            print(names(tb))
            if index == 0:
                first = tb
            else:
                print(tb is not first)
                while tb is not None and tb is not first:
                    tb = tb.tb_next
                print(tb is first)
same = ValueError("twice")
c = repeated_throw()
print(c.send(None))
print(c.throw(same))
try:
    c.throw(same)
except StopIteration:
    pass

def untouched_gen():
    yield 1
async def untouched_coro():
    return 1
for unstarted in [untouched_gen(), untouched_coro()]:
    try:
        unstarted.throw(ValueError("unstarted"))
    except ValueError as error:
        tb = error.__traceback__
        observed = []
        while tb is not None:
            observed.append((tb.tb_frame.f_code.co_name, tb.tb_lineno))
            tb = tb.tb_next
        print(observed)
