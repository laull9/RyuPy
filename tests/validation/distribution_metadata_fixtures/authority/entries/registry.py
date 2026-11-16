# 真native Registry未初始化目标；旧路径规则保留，不使用source绕过hooks。
import sys
from importlib import metadata
points = metadata.entry_points()
point = points['native']
# CP metadata内部可能已导入math；明确移除cache，不能把cache命中当Registry证明。
if 'math' in sys.modules:
    del sys.modules['math']
if 'math' in sys.modules:
    raise RuntimeError('registry cache removal failed')
print('discovered', len(points), 'registry-cold')
# protected native re也必须先真清cache，不能在配置阶段被closed标签拒绝。
if 're' in sys.modules:
    del sys.modules['re']
if 're' in sys.modules:
    raise RuntimeError('protected registry cache removal failed')
sys.path_hooks = []
print('native', point.load() > 3)
print('native-re', callable(points['native_re'].load()))
