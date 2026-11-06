# 函数定义来源与限定名语义验收。
# global定义重置限定名；nonlocal改变存储位置但不改定义的词法路径。
def publish():
    global promoted
    def promoted(value):
        def child():
            return value + 1
        return child
    return promoted

promoted = publish()
child = promoted(8)
assert promoted.__qualname__ == "promoted"
assert child.__qualname__ == "promoted.<locals>.child"
assert child() == 9

def owner():
    def selected():
        return "initial"
    first = selected

    def replace():
        nonlocal selected
        def selected():
            return "replacement"
        return selected

    replacement = replace()
    return first, replacement, selected

first, replacement, visible = owner()
assert first.__qualname__ == "owner.<locals>.selected"
assert replacement.__qualname__ == "owner.<locals>.replace.<locals>.selected"
assert visible is replacement and first is not replacement
assert first() == "initial" and visible() == "replacement"

def choose(flag):
    if flag:
        def chosen():
            return "if"
    else:
        def chosen():
            return "else"
    return chosen

left = choose(True)
right = choose(False)
assert left.__qualname__ == right.__qualname__ == "choose.<locals>.chosen"
assert left is not right and left() == "if" and right() == "else"
print(promoted.__qualname__, child.__qualname__, child())
print(first.__qualname__, first())
print(replacement.__qualname__, visible is replacement, visible())
print(left.__qualname__, right.__qualname__, left(), right())
