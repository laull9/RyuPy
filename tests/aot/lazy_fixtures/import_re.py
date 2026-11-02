# 目标模块冷导入与真实正则工作；不加热循环或内部计时。
import re
import re as repeated

pattern = re.compile('a+')
print(pattern.fullmatch('aaa').group(0))
print(re is repeated)
print(pattern is repeated.compile('a+'))
