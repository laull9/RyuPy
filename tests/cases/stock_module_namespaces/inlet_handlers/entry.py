# 暂停后活读目标，throw保异常身份；as清理与外层当前异常跨暂停恢复。
import inlet_stream as stream
import inlet_targets as target
source = stream.route()
print(next(source))
stream.target = (KeyError, ValueError)
problem = ValueError('injected inlet')
print(source.throw(problem))
try:
    next(source)
except StopIteration as stopped:
    print(stopped.value)
print(stream.events)
source = stream.route()
print(next(source))
stream.target = KeyError
problem = ValueError('unmatched inlet')
try:
    source.throw(problem)
except ValueError as error:
    print(error is problem)
source = stream.getter(target)
print(next(source))
problem = KeyError('target getter')
print(source.throw(problem))
source.close()
print(stream.events)
