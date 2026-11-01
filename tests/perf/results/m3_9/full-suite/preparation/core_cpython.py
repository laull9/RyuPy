"""固定CPython四core同源函数kernel；原main载入和输入重建/校验不计时。"""
import contextlib
import gc
import io
import json
from pathlib import Path
import runpy
import sys
import time
script, expected_path, mode, warmups, samples = sys.argv[1:]
name = Path(script).stem
expected = Path(expected_path).read_text()
stream = io.StringIO()
with contextlib.redirect_stdout(stream):
    namespace = runpy.run_path(script, run_name='__main__')
assert stream.getvalue() == expected
state = {}
def reset():
    if name == 'mandelbrot':
        state['pixels'] = [['.'] * 240 for _ in range(120)]
    elif name == 'matmul':
        state['c'] = namespace['make_matrix'](100, 1)
        state['b'] = namespace['make_matrix'](100, 2)
    elif name == 'nbody':
        state['bodies'] = namespace['make_bodies']()
        namespace['offset_momentum'](state['bodies'])
        assert str(round(namespace['energy'](state['bodies']), 9)) == expected.splitlines()[0]
def work():
    if name == 'fib':
        rec, iterate = namespace['fib_rec'], namespace['fib_iter']
        state['recursive'] = rec(30)
        value = 0
        for _ in range(200000):
            value = iterate(90)
        state['iterative'] = value
    elif name == 'mandelbrot':
        result = state['pixels']
        mandel = namespace['mandel']
        for y in range(120):
            row = result[y]
            for x in range(240):
                row[x] = '#' if mandel(-0.8 + x * 0.0005, -0.1 + y * 0.001, 1000) == 1000 else '.'
    elif name == 'matmul':
        for _ in range(3):
            state['c'] = namespace['matmul'](state['c'], state['b'], 100)
    elif name == 'nbody':
        namespace['advance'](state['bodies'], 0.01, 100000)
    else:
        raise ValueError(name)
def verify():
    if name == 'fib':
        assert state['recursive'] == 832040 and state['iterative'] == 2880067194370816120
    elif name == 'mandelbrot':
        assert ''.join(''.join(row) + '\n' for row in state['pixels']) == expected
    elif name == 'matmul':
        total = 0.0
        for row in state['c']:
            for value in row:
                total += value
        assert str(round(total, 3)) == expected.splitlines()[-1]
    else:
        assert str(round(namespace['energy'](state['bodies']), 9)) == expected.splitlines()[-1]
for _ in range(int(warmups)):
    reset(); work(); verify()
for sample in range(int(samples)):
    reset()
    before = [row['collections'] for row in gc.get_stats()]
    start = time.perf_counter(); work(); elapsed = time.perf_counter() - start
    after = [row['collections'] for row in gc.get_stats()]
    verify()
    row = {'name': name, 'sample': sample, 'kind': 'kernel_time' if mode == 'time' else 'kernel_resources'}
    if mode == 'time':
        row['elapsed_ms'] = elapsed * 1000
    else:
        row.update({f'gc{i}': after[i] - before[i] for i in range(3)})
    print(json.dumps(row))
