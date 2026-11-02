def old(a=0, b=0, c=0):
    print('old', a, b, c)
    return 'old-result'


def new(a=0, b=0, c=0):
    print('new', a, b, c)
    return 'new-result'


f = old
