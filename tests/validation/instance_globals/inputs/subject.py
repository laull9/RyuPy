# A1真实功能源：无推导式，只直接globals及普通定义/异常副作用。
def identities():
    return globals(), globals()


def read_value():
    return value


def direct_rebound():
    return globals()


def shadow(globals):
    return globals()


def captured():
    return len([])


def fail_publish():
    global before_failure
    before_failure = 'written'
    raise ValueError('publish')
