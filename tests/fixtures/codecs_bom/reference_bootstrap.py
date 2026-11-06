# 仅 CP 参考使用：版本守门及显式受控源码遮蔽，不编入 RyuPy。
import os
import sys

assert sys.implementation.name == 'cpython'
assert sys.version_info[:3] == (3, 12, 13)
assert len(sys.argv) >= 3 and len(sys.argv) % 2 == 1
for index in range(1, len(sys.argv), 2):
    mode, filename = sys.argv[index:index + 2]
    assert mode in ('native', 'shadow')
    assert os.path.isabs(filename)
    if mode == 'shadow':
        # codecs 通常在 CP 启动阶段已缓存；显式删缓存后才验证源目录优先。
        # 调用者同时传 -X frozen_modules=off，不能宣称默认 CP 启动语义。
        sys.modules.pop('codecs', None)
        sys.path.insert(0, os.path.dirname(filename))
    with open(filename, 'rb') as source:
        code = compile(source.read(), filename, 'exec')
    exec(code, {'__name__': '__main__', '__file__': filename})
