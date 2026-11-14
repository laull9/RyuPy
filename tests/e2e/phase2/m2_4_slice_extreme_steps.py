# 内部切片计数不能因 i64 极端步长溢出；与同次 CPython 比较实际内容。
text = "A猫😀"
for step in [-9223372036854775808, -9223372036854775807, 9223372036854775806, 9223372036854775807]:
    print([ord(char) for char in text[::step]])
    print(text[1:1:step], ""[::step])
    print([ord(char) for char in text[-9223372036854775808:9223372036854775807:step]])
    print([ord(char) for char in text[9223372036854775807:-9223372036854775808:step]])

# 同一计数合同也服务于 tuple、list、bytes；逐项打印实际容器值和空窗口。
for values in [(10, 20, 30), [10, 20, 30], b"ABC", bytearray(b"XYZ")]:
    for step in [-9223372036854775808, -9223372036854775807, 9223372036854775806, 9223372036854775807]:
        print(values[::step])
        print(values[1:1:step])
        print(values[-9223372036854775808:9223372036854775807:step])
        print(values[9223372036854775807:-9223372036854775808:step])
