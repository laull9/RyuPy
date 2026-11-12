# ryupy: pending
# 源码孤立代理字面量不得替换成 U+FFFD；与索引优化分开保留未修边界。
print(ord("\ud800"), ord("\udc00"))
# 反向切片不能将两个独立码点合并为一项（既有 D009）。
text = chr(0xdc00) + chr(0xd800)
print(list(map(ord, text[::-1])))
