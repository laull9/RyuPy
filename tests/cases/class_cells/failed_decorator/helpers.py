escaped = []
def fail(cls):
    escaped.append(cls)
    raise ValueError('class decorator failed')
