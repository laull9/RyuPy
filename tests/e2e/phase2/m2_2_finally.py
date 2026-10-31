# M2.2：finally 覆盖返回值，循环跳转仍执行清理。
def returning():
    try:
        return 1
    finally:
        print("return cleanup")
        return 2


print(returning())
for value in range(3):
    try:
        if value == 0:
            continue
        break
    finally:
        print("loop cleanup", value)
try:
    try:
        raise ValueError("first")
    finally:
        raise RuntimeError("cleanup")
except RuntimeError as error:
    print(str(error), type(error.__context__).__name__)
try:
    print("body")
except ValueError:
    print("unexpected error")
else:
    print("else")
finally:
    print("last")
