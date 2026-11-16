import optparse

def construct():
    optparse.Option('-n', type='invalid')

try:
    raise ValueError('outer-context')
except ValueError:
    construct()
