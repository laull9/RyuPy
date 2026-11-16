value = 10

def read(offset=1):
    local = value + offset
    return local

def bump(amount):
    global value
    value += amount
    return value

def measure(seq):
    return len(seq)

def make_measure():
    def inner(seq):
        return len(seq)
    return inner
