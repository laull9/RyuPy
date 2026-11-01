# bench: category=numeric
# bench: desc=五体问题 N-Body，列表 + 浮点动态运算，10 万步
PI = 3.14159265358979323
SOLAR_MASS = 4 * PI * PI
DAYS_PER_YEAR = 365.24


def make_bodies():
    sun = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, SOLAR_MASS]
    jupiter = [
        4.84143144246472090e00,
        -1.16032004402742839e00,
        -1.03622044471123109e-01,
        1.66007664274403694e-03 * DAYS_PER_YEAR,
        7.69901118419740425e-03 * DAYS_PER_YEAR,
        -6.90460016972063023e-05 * DAYS_PER_YEAR,
        9.54791938424326609e-04 * SOLAR_MASS,
    ]
    saturn = [
        8.34336671824457987e00,
        4.12479856412430479e00,
        -4.03523417114321381e-01,
        -2.76742510726862411e-03 * DAYS_PER_YEAR,
        4.99852801234917238e-03 * DAYS_PER_YEAR,
        2.30417297573763929e-05 * DAYS_PER_YEAR,
        2.85885980666130812e-04 * SOLAR_MASS,
    ]
    uranus = [
        1.28943695621391310e01,
        -1.51111514016986312e01,
        -2.23307578892655734e-01,
        2.96460137564761618e-03 * DAYS_PER_YEAR,
        2.37847173959480950e-03 * DAYS_PER_YEAR,
        -2.96589568540237556e-05 * DAYS_PER_YEAR,
        4.36624404335156298e-05 * SOLAR_MASS,
    ]
    neptune = [
        1.53796971148509165e01,
        -2.59193146099879641e01,
        1.79258772950371181e-01,
        2.68067772490389322e-03 * DAYS_PER_YEAR,
        1.62824170038242295e-03 * DAYS_PER_YEAR,
        -9.51592254519715870e-05 * DAYS_PER_YEAR,
        5.15138902046611451e-05 * SOLAR_MASS,
    ]
    return [sun, jupiter, saturn, uranus, neptune]


def offset_momentum(bodies):
    px = 0.0
    py = 0.0
    pz = 0.0
    for b in bodies:
        px -= b[3] * b[6]
        py -= b[4] * b[6]
        pz -= b[5] * b[6]
    sun = bodies[0]
    sun[3] = px / SOLAR_MASS
    sun[4] = py / SOLAR_MASS
    sun[5] = pz / SOLAR_MASS


def energy(bodies):
    e = 0.0
    n = len(bodies)
    for i in range(n):
        b = bodies[i]
        e += 0.5 * b[6] * (b[3] * b[3] + b[4] * b[4] + b[5] * b[5])
        for j in range(i + 1, n):
            c = bodies[j]
            dx = b[0] - c[0]
            dy = b[1] - c[1]
            dz = b[2] - c[2]
            e -= b[6] * c[6] / (dx * dx + dy * dy + dz * dz) ** 0.5
    return e


def advance(bodies, dt, steps):
    n = len(bodies)
    for _ in range(steps):
        for i in range(n):
            b = bodies[i]
            for j in range(i + 1, n):
                c = bodies[j]
                dx = b[0] - c[0]
                dy = b[1] - c[1]
                dz = b[2] - c[2]
                d2 = dx * dx + dy * dy + dz * dz
                mag = dt / (d2 * d2 ** 0.5)
                bm = b[6] * mag
                cm = c[6] * mag
                b[3] -= dx * cm
                b[4] -= dy * cm
                b[5] -= dz * cm
                c[3] += dx * bm
                c[4] += dy * bm
                c[5] += dz * bm
        for b in bodies:
            b[0] += dt * b[3]
            b[1] += dt * b[4]
            b[2] += dt * b[5]


bodies = make_bodies()
offset_momentum(bodies)
print(round(energy(bodies), 9))
advance(bodies, 0.01, 100000)
print(round(energy(bodies), 9))
