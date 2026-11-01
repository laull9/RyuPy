# 静态整数赋值前仍应读取内建单例；普通同名槽不能被推成i64而丢回退。
print(Ellipsis is ..., type(Ellipsis).__name__)
Ellipsis = 1
print(Ellipsis, Ellipsis is ..., type(Ellipsis).__name__)
