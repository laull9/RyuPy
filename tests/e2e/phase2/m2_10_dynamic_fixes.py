# M2.10：动态路径的语义修正。
#
# 覆盖五类曾经出错的场景：空容器真值、字符串的码点语义、动态 str() 的拼接、
# 字典覆盖已有键、以及 int 乘方的负指数。

# 1. 空容器为假（Python 最常见的惯用法）
if not []:
    print("empty list")
if not {}:
    print("empty dict")
if not "":
    print("empty str")
if not set():
    print("empty set")
if [0]:
    print("nonempty list")

# 2. 动态 str() 的结果可以直接参与字符串拼接
print(str(1) + "x", str(1.5) + "!", str(True) + "?")

# 3. 字符串按码点计数、下标、切片与迭代（emoji 占 2 个 UTF-16 code unit）
s = "😀a"
print(len(s), s[0], s[1], s[0:1], s[1:], s[::-1])
for ch in s:
    print(ch)

# 4. 覆盖已有键不改变字典大小，也不破坏后续迭代
d = {}
d["a"] = 1
d["a"] = 2
d["b"] = 3
print(len(d), d["a"], list(d.keys()), list(d.values()))

# 5. int ** int 的负指数返回 float（结果类型取决于运行时的指数值，
#    因此这类表达式必须走动态路径）
def power(x, n):
    return x ** n

print(power(2, -1), power(2, 10), 2 ** -1)


# 静态标注函数遇到非常量指数同样按动态处理，语义保持一致
def power_annotated(x: int, n: int) -> int:
    return x ** n


print(power_annotated(2, -1), power_annotated(3, 3))


# 指数是确定的非负字面量时仍走静态快速幂
def cube(x: int) -> int:
    return x ** 3


print(cube(3))

# 6. 排序必须稳定：0.0 与 -0.0 相等，但 repr 不同，能看出原始顺序是否被保留
values = [0.0, -0.0, 0.0, -0.0, 0.0, -0.0, 0.0, -0.0]
print(sorted(values))
print(sorted([3, 1, 2]))

# 7. None 的表示：静态路径与动态路径一致（打印 None，`is None` 为 True）
def returns_none() -> None:
    pass


def none_roundtrip() -> int:
    x = None
    print(x, x is None, returns_none() is None)
    return 0


none_roundtrip()
print(returns_none())
