# M3.0：异常绑定退出处理器后，逃逸闭包必须看到空 cell。
def returning():
    try:
        raise ValueError('return')
    except ValueError as error:
        def read():
            return error
        return read


def jumping():
    readers = []
    for index in range(3):
        try:
            raise ValueError('loop')
        except ValueError as error:
            readers.append(lambda: error)
            if index == 0:
                continue
            break
    return readers


def raising():
    readers = []
    try:
        try:
            raise ValueError('original')
        except ValueError as error:
            readers.append(lambda: error)
            raise RuntimeError('replacement')
    except RuntimeError as replacement:
        print(str(replacement.__context__))
    return readers[0]


readers = [returning(), raising()]
readers.extend(jumping())
for read in readers:
    try:
        read()
    except NameError as error:
        print(type(error).__name__)


def outer_binding():
    error = None
    def read():
        return error
    def catch():
        nonlocal error
        try:
            raise ValueError('nonlocal')
        except ValueError as error:
            return read
    return catch()


try:
    outer_binding()()
except NameError as error:
    print(type(error).__name__)
try:
    raise
except RuntimeError as error:
    print(str(error))
