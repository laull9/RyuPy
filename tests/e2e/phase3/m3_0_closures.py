# M3.0：共享 cell、late binding 与默认参数捕获。
def counter():
    value = 0

    def add(amount):
        nonlocal value
        value += amount
        return value

    def read():
        return value

    return add, read


add, read = counter()
print(add(2), read(), add(3), read())
late = []
early = []
for index in range(3):
    late.append(lambda: index)
    early.append(lambda value=index: value)
print(late[0](), late[1](), late[2]())
print(early[0](), early[1](), early[2]())
