# 终端：wcwidth 的列宽计算（含组合字符与宽字符）。
from wcwidth import wcswidth, wcwidth

print(wcswidth("abc"), wcswidth("汉字"), wcwidth("🎉"))
print(wcswidth("e\u0301"))
