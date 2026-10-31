# M2.4：非 BMP 字符、孤立代理值和常用格式化。
text = "A🙂猫"
print(len(text), text[1], text[::-1])
surrogate = "\ud800"
print(len(surrogate), surrogate == "\ud800")
print(" a,b,,c ".strip().split(","))
print("-".join(["a", "b"]), "aba".replace("a", "x"))
print(text.find("猫"), "missing".find("x"))
print(f"{{value}} {12:04d} {1.25:.1f} {'猫'!r}")
print("a", "b", sep="|", end="!\n")
