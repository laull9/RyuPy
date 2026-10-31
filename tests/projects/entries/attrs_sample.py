# 数据类：attrs 使用元类生成 __init__ / __eq__ / __repr__。
import attrs


@attrs.define
class Point:
    x: int
    y: int = 0


point = Point(1, 2)
print(point, point == Point(1, 2))
print(attrs.asdict(point))
