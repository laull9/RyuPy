# 独立消费布局与模块名，不依赖另一套用例的helper或绑定。
import mineral
import chamber

print(mineral.old(-5), mineral.fresh(-5), mineral.retained(-5))
print(mineral.from_literal(-6))
print(chamber.observe())
print(chamber.reenter())
print(chamber.recover())
print('__builtins__' not in mineral.namespace)
print('quartz-context-ok')
