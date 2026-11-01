# bench: category=control
# bench: desc=try / except 抛捕 ValueError、自定义异常、finally 与无异常路径
class AppError(Exception):
    pass


def parse(i):
    if i % 3 == 0:
        raise ValueError("bad")
    return i


def guarded(i):
    try:
        if i % 5 == 0:
            raise AppError("app")
        return i
    finally:
        pass


caught = 0
total = 0
for i in range(100000):
    try:
        total += parse(i)
    except ValueError:
        caught += 1
for i in range(100000):
    try:
        total += guarded(i)
    except AppError as e:
        caught += 1
for i in range(1000000):
    try:
        total += i
    except KeyError:
        caught += 1
print(caught, total)
