value = 10

def make_pair(start):
    count = start
    def add(step=1):
        nonlocal count
        count += step
        return count, value
    def read():
        return count
    return add, read

def factory():
    return lambda offset: value + offset
