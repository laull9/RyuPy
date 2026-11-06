escaped = []
fail = True
def save(fn):
    escaped.append(fn)
    return fn
def maybe_fail():
    if fail:
        raise ValueError('class body failed')
