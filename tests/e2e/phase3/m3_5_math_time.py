# M3.5：常用标量数学与受控短计时，平台 libm 使用容差而非假定逐位一致。
import math
import time

print(math.sqrt(9), math.floor(-1.2), math.ceil(-1.2), math.trunc(-1.2))
print(math.floor(10**100), math.trunc(10**100))
print(math.fabs(-2), math.isfinite(3), math.isinf(math.inf), math.isnan(math.nan))
print(abs(math.log(8, 2) - 3) < 1e-12, abs(math.log10(10**1000) - 1000) < 1e-10)
print(abs(math.exp(1) - math.e) < 1e-12, math.pow(2, 3))
print(abs(math.sin(math.pi / 2) - 1) < 1e-12, math.cos(0), math.tan(0))
print(math.tau == 2 * math.pi, math.pow(math.nan, 0), math.pow(1, math.nan))
for operation in [lambda: math.sqrt(-1), lambda: math.log(0), lambda: math.sin(math.inf), lambda: math.pow(-2, 0.5)]:
    try:
        operation()
    except ValueError:
        print("domain")
for operation in [lambda: math.exp(1000), lambda: math.floor(math.inf), lambda: math.isfinite(10**1000)]:
    try:
        operation()
    except OverflowError:
        print("overflow")
try:
    math.log(2, 1)
except ZeroDivisionError:
    print("zero base log")
class Number:
    def __float__(self):
        return 4.0
class Index:
    def __index__(self):
        return 0
class Integral:
    def __floor__(self):
        return "custom floor"
    def __ceil__(self):
        return 7
    def __trunc__(self):
        return 8
print(math.sqrt(Number()), math.floor(Integral()), math.ceil(Integral()), math.trunc(Integral()))
print(time.time() > 0, time.monotonic() <= time.monotonic())
start = time.perf_counter()
print(time.sleep(0.001))
print(time.perf_counter() >= start)
print(time.sleep(Index()))
try:
    time.sleep(-1)
except ValueError:
    print("negative sleep")
try:
    time.sleep(Number())
except TypeError:
    print("sleep requires index")
