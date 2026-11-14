# ryupy: pending
# 生成器只读形参挂起写回消除 E2E 验证套件
# 覆盖场景：
# 1. 只读 param 多 yield 读取
# 2. 可变对象别名跨暂停
# 3. 参数重赋值
# 4. send 覆写
# 5. 局部首次初始化
# 6. 闭包 nonlocal 写
# 7. yield from 委托
# 8. throw / close / finally
# locals()与非显式close边界见独立pending observation用例。

def test_readonly_multi_yield():
    def gen(n):
        for i in range(3):
            yield f"step {i}: n={n}"

    print("--- readonly_multi_yield ---")
    for item in gen("hello"):
        print(item)


def test_mutable_param_alias():
    def gen(lst):
        lst.append(1)
        yield len(lst)
        lst.append(2)
        yield len(lst)
        lst.append(3)

    print("--- mutable_param_alias ---")
    data = [0]
    g = gen(data)
    print("yield 1:", next(g))
    print("yield 2:", next(g))
    try:
        next(g)
    except StopIteration:
        pass
    print("final data:", data)


def test_param_reassignment():
    def gen(x):
        yield f"before: {x}"
        x = x * 2
        yield f"after: {x}"

    print("--- param_reassignment ---")
    for item in gen(21):
        print(item)


def test_send_overwrite():
    def gen(p):
        p = yield f"initial: {p}"
        yield f"overwritten p: {p}"

    print("--- send_overwrite ---")
    g = gen("param_val")
    print(next(g))
    print(g.send("sent_val"))


def test_local_first_init():
    def gen(n):
        first = n + 10
        yield f"first: {first}"
        second = first * 2
        yield f"second: {second}"

    print("--- local_first_init ---")
    for item in gen(5):
        print(item)


def test_closure_nonlocal():
    def gen(n):
        def inc():
            nonlocal n
            n += 1
            return n
        yield inc()
        yield inc()
        yield f"final param: {n}"

    print("--- closure_nonlocal ---")
    for item in gen(10):
        print(item)


def test_yield_from():
    def child(prefix, limit):
        for i in range(limit):
            yield f"{prefix}_{i}"

    def parent(tag, count):
        yield f"start_{tag}"
        yield from child(tag, count)
        yield f"end_{tag}"

    print("--- yield_from ---")
    for item in parent("worker", 3):
        print(item)


def test_throw_close_finally():
    def gen_close(name):
        try:
            yield f"open {name}"
            yield f"working {name}"
        finally:
            print(f"closed {name}")

    def gen_throw(name):
        try:
            yield f"start {name}"
            yield f"running {name}"
        except ValueError as e:
            yield f"caught {e}"
        finally:
            print(f"finally {name}")

    print("--- throw_close_finally ---")
    g1 = gen_close("res1")
    print(next(g1))
    g1.close()

    g2 = gen_throw("res2")
    print(next(g2))
    print(g2.throw(ValueError("manual_error")))
    g2.close()




if __name__ == "__main__":
    test_readonly_multi_yield()
    test_mutable_param_alias()
    test_param_reassignment()
    test_send_overwrite()
    test_local_first_init()
    test_closure_nonlocal()
    test_yield_from()
    test_throw_close_finally()
