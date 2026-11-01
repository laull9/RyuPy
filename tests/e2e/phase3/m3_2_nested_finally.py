# finally的退出原因逐层保存，内层正常结束不能抹掉外层return/异常。
def ordinary():
    try:
        return 11
    finally:
        try:
            print("inner body")
        finally:
            print("inner cleanup")


print(ordinary())


def normal_inner():
    try:
        try:
            print("before")
        except ValueError:
            print("unused")
        print("after inner")
    finally:
        print("outer cleanup")


normal_inner()


def pending_return():
    try:
        yield "body"
        return 13
    finally:
        try:
            yield "inner body"
        finally:
            yield "inner cleanup"
        yield "outer tail"


it = pending_return()
print(next(it), next(it), next(it), next(it))
try:
    next(it)
except StopIteration as error:
    print("return", error.value)


def pending_error():
    try:
        yield "start"
        raise ValueError("kept")
    finally:
        try:
            yield "nested"
        finally:
            print("nested done")


it = pending_error()
print(next(it), next(it))
try:
    next(it)
except ValueError as error:
    print("error", str(error))
