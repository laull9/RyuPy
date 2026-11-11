# ryupy: pending
# CP成功；新factory不把slots伪装普通字段，正文后Finish明确拒绝。
def make():
    class C:
        __slots__ = ()
    return C

print(make().__name__)
