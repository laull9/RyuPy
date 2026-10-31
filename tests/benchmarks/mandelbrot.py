# 基准：Mandelbrot 集合内部区域（240×120、1000 次上限），大部分点跑满迭代。
def mandel(cr: float, ci: float, max_iter: int) -> int:
    zr = 0.0
    zi = 0.0
    n = 0
    while n < max_iter:
        zr2 = zr * zr
        zi2 = zi * zi
        if zr2 + zi2 > 4.0:
            return n
        zi = 2.0 * zr * zi + ci
        zr = zr2 - zi2 + cr
        n += 1
    return max_iter


for y in range(120):
    line = ""
    for x in range(240):
        cr = -0.8 + x * 0.0005
        ci = -0.1 + y * 0.001
        if mandel(cr, ci, 1000) == 1000:
            line = line + "#"
        else:
            line = line + "."
    print(line)
