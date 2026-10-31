# M1.4 里程碑：ASCII Mandelbrot，纯 float 热循环。
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


for y in range(-12, 13):
    line = ""
    for x in range(-39, 21):
        if mandel(x / 20.0, y / 10.0, 200) == 200:
            line = line + "#"
        else:
            line = line + "."
    print(line)
