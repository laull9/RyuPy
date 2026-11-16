# 采样握手前保持源与所有结果根；握手后读内容/身份，避免仅持有无用引用。
import sys

def produce(a, b, count, size):
    rows = []
    for outer in range(1):
        row = []
        for column in range(size):
            total = 0.0
            for index in range(count):
                total += a[column] * b[index]
            row.append(total)
        rows.append(row)
    return rows[0]

def verify(sources, pieces, size, scenario):
    total = 0.0
    for group in range(4):
        source = sources[group]
        selected = pieces[group]
        for value in source:
            total += value
        position = 0
        for part in selected:
            for value in part:
                assert value is source[position]
                total += value
                position += 1
        assert position == (size - 1 if scenario == 'near' else size)
    return total

size = int(sys.argv[1])
scenario = sys.argv[2]
phase = sys.argv[3]
inputs = [float(i) for i in range(size)]
sources = []
pieces = []
for group in range(4):
    values = produce(inputs, [1.0], 1, size)
    result = []
    if scenario == 'near':
        result.append(values[:size - 1])
    elif scenario == 'cumulative':
        for start in range(0, size, 256):
            result.append(values[start:start + 256])
    else:
        result.append(values[:])
    sources.append(values)
    pieces.append(result)
# 清除循环临时变量的额外根，采样对象只由sources/pieces持有。
values = None
result = None
inputs = None
checksum = 0.0
if phase == 'released':
    checksum = verify(sources, pieces, size, scenario)
    sources = None
    pieces = None
print('READY')
assert input() == 'ACK'
if phase == 'held':
    checksum = verify(sources, pieces, size, scenario)
print('DONE', size, scenario, phase, checksum)
