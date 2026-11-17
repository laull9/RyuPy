# 独立布局：with/for中的continue、关闭finally、嵌套throw及tuple属性目标活读。
import quarry_flow as flow
import quarry_types as kinds
print(list(flow.read([1, 2, 3], kinds)))
source = flow.suspended()
print(next(source))
problem = OSError('quarry injected')
print(source.throw(problem))
source.close()
print(flow.events)
source = flow.suspended()
print(next(source))
source.close()
print(flow.events)
