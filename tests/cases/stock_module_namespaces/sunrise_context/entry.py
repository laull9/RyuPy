# 多模块定义点、重入及真实Python调用者恢复，全部预期来自同次CPython。
import reservoir
import relay
import inherited_frame

print(reservoir.deleted, reservoir.module_frame)
print(relay.direct())
print(relay.through_python())
print(relay.nested())
print('__builtins__' not in reservoir.owner.__dict__)
print('sunrise-context-ok')
