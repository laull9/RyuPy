# 独立多模块消费者：输出由同次固定CPython生成，手写输入无golden。
import growth
import consume

print(growth.listed, growth.keys, sorted(growth.selected))
print(growth.item)
print(growth.frame_sizes, sorted(growth.after_delete), growth.before('abc'), growth.after('abc'))
print(consume.observe())
print(growth.same_globals, growth.origin, growth.failed_same)
print(growth.events)
print('namespace-eager-ok')
