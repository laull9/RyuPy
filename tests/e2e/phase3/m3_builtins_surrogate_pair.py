# ryupy: pending
# 两个独立代理码点不能因UTF16相邻而被当成一个码点。
text = chr(0xd800) + chr(0xdc00)
try:
    ord(text)
except TypeError:
    print("两个独立码点")
else:
    print("错误合并代理项")
