"""CP1b：静态编译 callable 经 Python 同线程重入及受控外线程调用。"""
import bridge_fixture

calls = []


def compiled_callback(value):
    calls.append(value)
    if value == 2:
        return bridge_fixture.call(compiled_callback, 1) + 10
    return value + 3


assert bridge_fixture.call(compiled_callback, 2) == 14
assert calls == [2, 1]
assert bridge_fixture.external_thread(compiled_callback, 5) == 8
assert calls == [2, 1, 5]
assert bridge_fixture.echo(compiled_callback) is compiled_callback
print('CP1b 同线程重入与外线程回调通过')
