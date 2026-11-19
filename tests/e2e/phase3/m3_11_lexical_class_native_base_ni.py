# ryupy: pending
# CP成功；新factory仅允许ordinary/object，真实dict父类在Prepare明确NI。
def make():
    class C(dict):
        value = 1
    return C

print(make().value)
