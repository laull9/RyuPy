# M1.5：@dataclass 编译为 .NET class / struct。
from dataclasses import dataclass


@dataclass
class Body:
    x: float
    v: float
    m: float


@dataclass(frozen=True)
class Vec2:
    x: int
    y: int


def step(b: Body, dt: float) -> None:
    b.x = b.x + b.v * dt


def dot(a: Vec2, b: Vec2) -> int:
    return a.x * b.x + a.y * b.y


b = Body(0.0, 2.0, 1.0)
for _ in range(10):
    step(b, 0.5)
print(b.x)
print(dot(Vec2(1, 2), Vec2(3, 4)))
