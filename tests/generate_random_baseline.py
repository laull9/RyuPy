"""用固定CPython3.12.13生成random行为事实；不提取或复制其实现源码。"""
import json
from pathlib import Path
import random
import sys

if sys.version_info[:3] != (3, 12, 13):
    raise SystemExit("random基线必须由CPython3.12.13生成")
rows = []
for name, seed in [("zero", 0), ("one", 1), ("negative", -1), ("forty_two", 42),
                   ("large", 2**80 + 7), ("ascii", "abc"), ("unicode", "雪"),
                   ("bytes", b"abc"), ("bytearray", bytearray(b"abc"))]:
    generator = random.Random(seed)
    rows.append({"name": name, "bits": [generator.getrandbits(k) for k in [0, 1, 31, 32, 33, 64, 65]],
                 "random_after_bits": [generator.random(), generator.random()]})
generator = random.Random(17)
digest = 0
for _ in range(1000):
    digest = ((digest * 6364136223846793005) ^ generator.getrandbits(32)) & ((1 << 64) - 1)
report = {"python": "3.12.13", "vectors": rows,
          "rollover": {"seed": 17, "draws": 1000, "digest": digest, "next": generator.getrandbits(32)}}
output = Path(__file__).parent / "data/random_cpython_3_12_13.json"
output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n")
