# 不依赖父包尚未发布的子模块属性。
import sys
left = sys.modules['ms_pkg.left']
left_spec = left.__spec__
left_loader = left.__loader__
saw_incomplete = not hasattr(left, 'complete')
