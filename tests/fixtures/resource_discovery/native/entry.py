# ryupy: pending
# 候选pending：普通资源调用必须在真实运行期导入，Static冷归属在同图先受验。
import pkg
import sys
import importlib.resources

print('before', 'pkg.data' in sys.modules)
print(importlib.resources.files('pkg.data').joinpath('sample.txt').read_text(), end='')
print('after', 'pkg.data' in sys.modules)
