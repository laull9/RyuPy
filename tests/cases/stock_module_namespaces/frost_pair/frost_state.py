token = 'before'
problem = RuntimeError
def reveal():
    return globals()
def make(prefix):
    def reader():
        return prefix, token
    return reader
