# ryupy: pending
# 普通函数绑定、保存别名和旧TextWrapper共享字典的真实Python入口。
import textwrap

assert textwrap.dedent(text="  x") == "x"
for call in [lambda: textwrap.dedent(), lambda: textwrap.dedent("x", "y"), lambda: textwrap.dedent("x", text="y"), lambda: textwrap.dedent(other="x")]:
    try:
        call()
        raise AssertionError("missing binding error")
    except TypeError as error:
        print("binding-error", str(error))
saved = textwrap.dedent
marker = []
def replacement(text):
    return marker
textwrap.dedent = replacement
assert textwrap.dedent is replacement
assert textwrap.dedent("anything") is marker
assert saved("  x") == "x"
class Product:
    def wrap(self, text):
        return [text]
    def fill(self, text):
        return "fill:" + text
seen = []
def factory(width=70, **kwargs):
    seen.append(width)
    return Product()
textwrap.TextWrapper = factory
assert textwrap.TextWrapper is factory
assert textwrap.wrap("x", 3) == ["x"]
assert textwrap.fill("y") == "fill:y"
assert seen == [3, 70]
print("BINDING_OK", seen)
