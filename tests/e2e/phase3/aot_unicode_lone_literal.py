# Unicode孤立代理字面量与动态chr构造保持同一码点，静态常量不经U+FFFD替换。
literal = "\ud800x"
constructed = chr(0xd800) + "x"
print(literal == constructed, ord(literal[0]), len(literal))
