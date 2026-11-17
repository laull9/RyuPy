# ryupy: pending
# Unicode孤立代理项字面量必须保留码点；当前编译常量路径发生U+FFFD替换，chr路径另有已验合同。
literal = "\ud800x"
constructed = chr(0xd800) + "x"
print(literal == constructed, ord(literal[0]), len(literal))
