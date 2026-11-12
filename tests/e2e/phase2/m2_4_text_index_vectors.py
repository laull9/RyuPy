# 下标和切片公开路径：不向终端直接输出孤立代理，以码点完整比较。
# 动态构造孤立代理，避免源码字面量编码路径；独立代理拼成有效配对仍是 D009。
texts = ['', 'abc', '中文é', chr(0xd800), chr(0xdc00)]
for n in (0, 1, 7, 15, 16, 31, 32, 63, 64, 127, 128):
    texts.append('中' * n + '😀' + 'xyz' + chr(0xd800))
for text in texts:
    for i in range(-len(text) - 1, len(text) + 1):
        try:
            print(i, ord(text[i]), ord(str.__getitem__(text, i)))
        except IndexError:
            print(i, 'IndexError')
    for i in (-9223372036854775808, 9223372036854775807):
        try:
            print(text[i])
        except IndexError:
            print('extreme IndexError')
    for step in (-3, -1, 1, 2):
        for start in (-len(text) - 2, -1, 0, len(text) // 2, len(text) + 2):
            print(list(map(ord, text[start::step])))
