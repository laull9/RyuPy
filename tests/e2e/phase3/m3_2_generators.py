# ryupy: pending
# M3.2：send / throw / close、委托返回值与耗尽。
def child():
    try:
        value = yield 1
        yield value
    except ValueError:
        yield "handled"
    finally:
        print("child cleanup")
    return 9


def parent():
    result = yield from child()
    yield result


iterator = parent()
print(next(iterator), iterator.send(4))
print(iterator.throw(ValueError("injected")))
print(next(iterator))
try:
    next(iterator)
except StopIteration:
    print("finished")
other = child()
print(next(other))
other.close()
print(next(other, "closed"))
source = [1, 2]
expression = (item * 2 for item in source)
source.append(3)
print(list(expression))
