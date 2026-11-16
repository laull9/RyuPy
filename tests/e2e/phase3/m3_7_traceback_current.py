# 当前异常文本和链按CP逐字对照；帧格式差异另由真实帧测试登记，不以limit=0宣称完整格式兼容。
import traceback
import io

print(repr(traceback.format_exc()))
empty = io.StringIO()
print(traceback.print_exc(file=empty))
print(repr(empty.getvalue()))
try:
    raise ValueError('inner')
except ValueError:
    try:
        raise TypeError('outer')
    except TypeError:
        print(repr(traceback.format_exc(limit=0)))
        print(repr(traceback.format_exc(limit=0, chain=False)))
        target = io.StringIO()
        print(traceback.print_exc(0, target, False))
        print(repr(target.getvalue()))
        for bad in ['bad', 1.5, -1.5, 2**100, -(2**100)]:
            try:
                traceback.format_exc(bad)
            except (TypeError, ValueError, OverflowError) as error:
                print(type(error).__name__)
print(repr(traceback.format_exc()))
class Broken:
    def write(self, value):
        raise RuntimeError('writer')
try:
    traceback.print_exc(file=Broken())
except RuntimeError as error:
    print(str(error))
class Recording:
    def __init__(self):
        self.parts = []
    def write(self, text):
        self.parts.append(text)
recording = Recording()
traceback.print_exc(file=recording)
print(recording.parts)
try:
    raise ValueError('record')
except ValueError:
    recording.parts.clear()
    traceback.print_exc(0, recording)
    print(recording.parts)

# 无活动异常也校验 limit，且有效 limit 后 chain 仅调用一次。
class ChainFlag:
    def __init__(self):
        self.calls = 0
    def __bool__(self):
        self.calls += 1
        return True
flag = ChainFlag()
for printing in [False, True]:
    recording.parts.clear()
    flag.calls = 0
    for bad in ['bad', 1.5, -1.5, 2**100, -(2**100)]:
        try:
            if printing:
                traceback.print_exc(bad, recording, flag)
            else:
                traceback.format_exc(bad, flag)
        except (TypeError, ValueError, OverflowError) as error:
            print(type(error).__name__, flag.calls)
    print(recording.parts)
    if printing:
        print(traceback.print_exc(0, recording, flag), flag.calls, recording.parts)
    else:
        print(repr(traceback.format_exc(0, flag)), flag.calls)
