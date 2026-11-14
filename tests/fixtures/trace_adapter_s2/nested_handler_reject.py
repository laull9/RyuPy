# 同activation多cleanup仍未实现；明确准入拒绝，不让第二个Forward改写S1协议。
try:
    raise ValueError("outer")
except ValueError:
    if True:
        try:
            raise TypeError("inner")
        except TypeError:
            raise RuntimeError("replacement")
