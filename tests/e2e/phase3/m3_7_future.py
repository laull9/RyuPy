"""Python 3 中已强制启用的 future 特性保留真实导入绑定。"""
from __future__ import division, unicode_literals, generator_stop
from __future__ import division as true_division
import __future__
print(3 / 2, type('文本').__name__)
print(division is __future__.division, division.compiler_flag)
print(division.getOptionalRelease(), division.getMandatoryRelease())
print(repr(unicode_literals), generator_stop.mandatory)
print(__future__.annotations.getMandatoryRelease(), __future__.annotations.compiler_flag)
print(__future__.all_feature_names)
print(true_division is division, division in [true_division])
