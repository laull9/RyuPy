"""完整原26项/消费者三项的样本及实际SDK证据门，不将新进程数据称为暖态。"""
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tests'))
from benchlib import suite


def verify(path, selected):
    directory = ROOT/('tests/benchmarks' if selected == 'original' else 'tests/perf/generator_consumers')
    expected = {b.name for b in suite.discover(directory)}
    assert len(expected) == (26 if selected == 'original' else 3)
    data = json.loads(path.read_text())
    assert data['runs'] == 5 and data['warmup'] == 1
    assert len(data['rows']) == len(expected) and {r['name'] for r in data['rows']} == expected
    for row in data['rows']:
        assert set(row['samples']) == {'CP', 'A', 'B'}
        for samples in row['samples'].values():
            assert len(samples) == 5 and all(s['ms'] > 0 for s in samples)
        for arm in ('A', 'B'):
            assert {'main_ms', 'alloc_bytes'} <= row['stats'][arm].keys()
            assert row['free_bytes_before_build'][arm] >= 4*1024**3
            manifest = json.loads((path.parent/'manifests'/row['name']/arm/'ryupy-artifact.json').read_text())
            assert manifest['toolchain']['sdk_version'] == '10.0.403'
            assert manifest['toolchain']['target_framework_major'] == 10
    return len(expected)


if __name__ == '__main__': print(verify(Path(sys.argv[1]), sys.argv[2]))
