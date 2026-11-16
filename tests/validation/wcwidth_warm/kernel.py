"""四调用点两个API：保原非计时合同，新增独立暖计量模式，不替换真实缓存。"""
import os
from wcwidth import wcwidth, wcswidth
from wcwidth.wcwidth import _wcmatch_version, _wcversion_value


def require(condition, reason):
    if not condition:
        raise RuntimeError(reason)


def snapshot():
    a = wcwidth.cache_info()
    b = _wcmatch_version.cache_info()
    c = _wcversion_value.cache_info()
    return ((a.hits, a.misses, a.maxsize, a.currsize),
            (b.hits, b.misses, b.maxsize, b.currsize),
            (c.hits, c.misses, c.maxsize, c.currsize))


def clear_all():
    wcwidth.cache_clear()
    _wcmatch_version.cache_clear()
    _wcversion_value.cache_clear()


def kernel(n):
    a = 0
    b = 0
    c = 0
    d = 0
    for unused in range(n):
        a += wcswidth("abc")
        b += wcswidth("汉字")
        c += wcwidth("🎉")
        d += wcswidth("e\u0301")
    return (a, b, c, d)


def checked(n):
    value = kernel(n)
    require(value == (3 * n, 4 * n, 2 * n, n), "四 API 结果不符")
    return value


def main():
    require(os.environ.get("UNICODE_VERSION") == "latest", "固定环境缺失")
    clear_all()
    zero = ((0, 0, 1000, 0), (0, 0, 8, 0), (0, 0, 128, 0))
    require(snapshot() == zero, "三个缓存未清空")
    print("cleared", snapshot())
    print("first", checked(1))
    first = ((0, 8, 1000, 8), (3, 1, 8, 1), (0, 0, 128, 0))
    require(snapshot() == first, "首次调用缓存路径不符")
    print("first-cache", snapshot())
    # 只清版本层不清外层，真实 wcwidth 命中应遮蔽下层调用。
    _wcmatch_version.cache_clear()
    _wcversion_value.cache_clear()
    print("outer-hit", checked(1))
    require(snapshot() == ((8, 8, 1000, 8), (0, 0, 8, 0), (0, 0, 128, 0)),
            "外层命中却重算版本")
    print("outer-hit-cache", snapshot())
    # 重新开始一次完整状态，不把上一段下层空状态冒称全热。
    clear_all()
    checked(1)
    print("warm-before", snapshot())
    print("warm-result", checked(100))
    require(snapshot() == ((800, 8, 1000, 8), (3, 1, 8, 1), (0, 0, 128, 0)),
            "暖循环调用数或缓存状态不符")
    print("warm-after", snapshot())
    clear_all()
    require(snapshot() == zero, "退出前缓存清理失败")
    print("final-cleared", snapshot())



# 新计量模式与旧非计时main分开；每个进程仅初始化一次。
def warm():
    from time import perf_counter
    require(os.environ.get("UNICODE_VERSION") == "latest", "固定环境缺失")
    clear_all()
    checked(1)
    for batch in range(16):
        if batch < 4:
            value = kernel(100000)
            elapsed = 0.0
        else:
            start = perf_counter()
            value = kernel(100000)
            elapsed = perf_counter() - start
        require(value == (300000, 400000, 200000, 100000), "batch结果不符")
        state = snapshot()
        require(state == (((batch + 1) * 800000, 8, 1000, 8),
                          (3, 1, 8, 1), (0, 0, 128, 0)), "batch缓存不符")
        # 单行同时保存真实耗时、结果和全部缓存字段；外部先落原流再解析。
        print("batch", batch, elapsed, value, state)
    clear_all()
    require(snapshot() == ((0, 0, 1000, 0), (0, 0, 8, 0), (0, 0, 128, 0)), "末清失败")
    print("complete", 16)


import sys
if len(sys.argv) == 2 and sys.argv[1] == "contract":
    main()
elif len(sys.argv) == 2 and sys.argv[1] == "warm":
    warm()
else:
    raise RuntimeError("需要contract或warm模式")
