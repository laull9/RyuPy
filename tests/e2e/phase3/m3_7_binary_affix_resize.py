# ryupy: pending
# 明确未支持：边界__index__改变bytearray长度；保留CPython原合法结果，不伪转正。
for mode in ['grow', 'shrink']:
    value = bytearray(b'ab')
    class ChangeLength:
        def __index__(self):
            if mode == 'grow':
                value.extend(b'cd')
            else:
                del value[1:]
            return 0
    print(mode, value.endswith(b'a', ChangeLength(), 999), value)

# start增长、end恢复：最终length相同也发生过结构变动。
restored = bytearray(b'ab')
class GrowStart:
    def __index__(self):
        restored.append(99)
        return 0
class RestoreEnd:
    def __index__(self):
        del restored[2:]
        return 2
print(restored.startswith(b'ab', GrowStart(), RestoreEnd()), restored)
class ShrinkAndRestore:
    def __index__(self):
        del restored[1:]
        restored.append(98)
        return 0
print(restored.startswith(b'ab', ShrinkAndRestore()), restored)
