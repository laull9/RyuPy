# ryupy: pending
# 保留既有生成器locals观察与非显式close的销毁时机差异。

def test_implicit_close():
    def gen():
        try:
            yield 1
        finally:
            print("implicit finally")
    g = gen()
    print(next(g))

test_implicit_close()
def test_locals_inspect():
    def gen(a, b):
        loc1 = 100
        keys1 = [k for k in sorted(locals().keys()) if not k.startswith("_")]
        yield keys1
        loc2 = 200
        keys2 = [k for k in sorted(locals().keys()) if not k.startswith("_")]
        yield keys2

    print("--- locals_inspect ---")
    for item in gen("foo", "bar"):
        print(item)

test_locals_inspect()
