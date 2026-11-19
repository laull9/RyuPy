# 真正多模块、yield/send/close/yield-from与genexpr；预期由同次固定CPython生成。
import stream_state as stream

source = stream.sequence(2)
stream.marker = 20
print(next(source))
stream.__dict__['marker'] = 90
print(source.send(3))
source.close()
print(stream.events)

lazy = stream.lazy()
stream.marker = 30
print(next(lazy))
stream.marker = 40
print(list(lazy))
print(list(stream.captured_builtins()))

stream.events.clear()
delegate = stream.delegated(1)
print(next(delegate))
print(delegate.send(2))
try:
    next(delegate)
except StopIteration as stopped:
    print(stopped.value)
try:
    next(delegate)
except StopIteration as stopped:
    print('exhausted', stopped.value is None, stopped.args)
print(stream.events)

original = stream.marker
paused = stream.sequence(4)
print(next(paused))
try:
    paused.throw(ValueError('injected'))
except ValueError as error:
    print(type(error).__name__, str(error))
print(stream.events[-1], stream.marker == original)

trace = []
token = ['kept']
def callable_globals():
    trace.append('callee')
    return token
def consume(first, second):
    trace.append('consume')
    return first is token, first[:], second
delayed = stream.delayed(callable_globals, consume)
print(next(delayed), trace)
token.append('changed')
try:
    delayed.send(5)
except StopIteration as stopped:
    print(stopped.value, trace)
