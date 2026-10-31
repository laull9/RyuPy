# ryupy: pending
# M3.8：待桥接的标准库 API；执行路径仍需宿主侧证据确认。
from decimal import Decimal, InvalidOperation, localcontext

with localcontext() as context:
    context.prec = 8
    print(Decimal("0.1") + Decimal("0.2"))
    print(Decimal(1) / Decimal(7))
try:
    Decimal("not-a-number")
except InvalidOperation:
    print("decimal error")
