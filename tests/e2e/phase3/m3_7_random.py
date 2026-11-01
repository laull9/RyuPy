# 固定CPython3.12.13随机序列：完整种子、跨字位数、拒绝采样及自定义序列协议。
import random

for seed in [0, 1, -1, 42, (1 << 80) + 7, "abc", "雪", b"abc", bytearray(b"abc")]:
    generator = random.Random(seed)
    print([generator.getrandbits(k) for k in [0, 1, 31, 32, 33, 64, 65]])
    print(generator.random(), generator.random())

generator = random.Random(42)
print(type(generator) is random.Random, isinstance(generator, random.Random))
print(generator.randrange(16), generator.randrange(-100, 100, 7), generator.randrange(100, -100, -9))
print(generator.randint(-9, 9), generator.randrange(1 << 80))
items = list(range(10))
generator.seed(42)
print(generator.shuffle(items), items)
print(generator.choice("abcd"), generator.getrandbits(32))

random.seed(42)
independent = random.Random(42)
print(random.random() == independent.random())
random.seed(7)
print(random.random(), independent.random())

for action in ["negative_bits", "float_bits", "zero_range", "zero_step", "empty_choice"]:
    generator.seed(42)
    try:
        if action == "negative_bits":
            generator.getrandbits(-1)
        elif action == "float_bits":
            generator.getrandbits(1.0)
        elif action == "zero_range":
            generator.randrange(0)
        elif action == "zero_step":
            generator.randrange(1, 9, 0)
        else:
            generator.choice([])
    except (ValueError, TypeError, IndexError) as error:
        print(type(error).__name__)
    print(generator.getrandbits(32))


class Index:
    def __index__(self):
        return 16


generator.seed(42)
print(generator.randrange(Index()), generator.getrandbits(32))


class Sequence:
    def __init__(self):
        self.values = [1, 2, 3]
        self.events = []

    def __len__(self):
        self.events.append("len")
        return len(self.values)

    def __getitem__(self, index):
        self.events.append(("get", index))
        return self.values[index]

    def __setitem__(self, index, value):
        self.events.append(("set", index))
        self.values[index] = value


sequence = Sequence()
generator.seed(4)
print(generator.choice(sequence), sequence.events)
sequence.events = []
generator.shuffle(sequence)
print(sequence.values, sequence.events)


class LargeIndex:
    def __index__(self):
        return 1 << 80


generator.seed(42)
print(generator.randrange(LargeIndex()), generator.getrandbits(32))
