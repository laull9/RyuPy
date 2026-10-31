# ryupy: pending
# M3.7：枚举、循环深拷贝、日期、可复现随机与字符串常量。
import copy
import random
import string
from datetime import datetime, timedelta, timezone
from enum import Enum


class Color(Enum):
    RED = 1
    BLUE = 2


print(Color(1) is Color.RED, Color.BLUE.name)
source = []
source.append(source)
clone = copy.deepcopy(source)
print(clone is not source, clone[0] is clone)
moment = datetime(2024, 2, 28, tzinfo=timezone.utc) + timedelta(days=1)
print(moment.isoformat())
generator = random.Random(42)
print([generator.randrange(100) for _ in range(4)])
print(string.ascii_lowercase[:3], string.digits)
