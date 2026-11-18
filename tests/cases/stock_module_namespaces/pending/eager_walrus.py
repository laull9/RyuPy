# ryupy: pending
# 通用NamedExpr尚未实现；保持完整CP输入与E0100，不因新推导式能力扩大准入。
values = [(latest := item) for item in [4, 5]]
print(values, latest)
