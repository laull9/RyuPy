# 真实模块dict删除与各词法层隐式as清理；不引入显式local/cell/free del。
try:
    raise ValueError('module')
except ValueError as ValueError:
    del ValueError
fallback = ValueError
slot = 'old'
untouched = 'keep'

def global_clear(Target, error):
    global slot, untouched
    try:
        raise error
    except Target as slot:
        del slot
        return 'cleared'
    except BaseException as untouched:
        return 'wrong'

def local_clear(error):
    try:
        raise error
    except ValueError as caught:
        pass
    return caught

def cell_clear(error):
    try:
        raise error
    except ValueError as caught:
        def read():
            return caught
        return read

def free_clear(error):
    caught = 'old'
    def clear():
        nonlocal caught
        try:
            raise error
        except ValueError as caught:
            pass
    clear()
    return caught

def bare(error):
    try:
        raise error
    except:
        raise

def control(error):
    global slot
    trace = []
    for index in range(3):
        try:
            raise error
        except ValueError as slot:
            if index == 0:
                continue
            break
        finally:
            trace.append(index)
    return trace

def final_override(error):
    global slot
    try:
        try:
            raise error
        except ValueError as slot:
            return 'inner'
    finally:
        return 'outer'
