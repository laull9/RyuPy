# 字符串正负步进切片：同次 CPython 对照，覆盖 BMP、非 BMP 和孤立代理项。
# 用 chr 避开已登记的源码孤立代理项常量缺口；每组只含一种代理项，避免旧的重配对表示缺口。
texts = ["", "abc", "A😀猫𝄞Z", chr(0xd800) + "abc", chr(0xdc00) + "xyz"]
bounds = [None, -20, -2, -1, 0, 1, 2, 20]
for text in texts:
    total = 0
    points = 0
    for start in bounds:
        for stop in bounds:
            for step in [-20, -3, -2, -1, 1, 2, 3, 20]:
                result = text[start:stop:step]
                print([ord(char) for char in result])
                total += len(result)
                for char in result:
                    points += ord(char)
    print(total, points)
text = "A😀猫𝄞Z" * 500
print(text[::-1][::-1] == text)
print(len(text[::3]), len(text[::-3]), text[::-1000000])
