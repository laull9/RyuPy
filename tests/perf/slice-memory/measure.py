"""握手后读取原始proc；两个层次分列，不把RSS等同存活托管堆。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import select
import subprocess
import sys
import time

HERE = Path(__file__).resolve().parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def capture(command, out, size, scenario, phase, layer):
    out.mkdir(parents=True, exist_ok=False)
    (out / 'command.json').write_text(json.dumps(command))
    with (out / 'stderr').open('wb') as error:
        process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                   stderr=error, bufsize=0, env=os.environ | {'PYTHONUNBUFFERED': '1'})
        try:
            deadline = time.monotonic() + 180
            line = b''
            while not line.endswith(b'\n'):
                remaining = deadline - time.monotonic()
                assert remaining > 0 and select.select([process.stdout], [], [], remaining)[0], 'READY timeout'
                chunk = process.stdout.read(1)
                assert chunk, ('early exit', process.poll())
                line += chunk
            (out / 'ready').write_bytes(line)
            assert line.rstrip() == b'READY', line
            pid = process.pid
            (out / 'pid').write_text(str(pid))
            for name in ('maps', 'stat', 'statm', 'status'):
                (out / name).write_bytes(Path(f'/proc/{pid}/{name}').read_bytes())
            samples = []
            for sample in range(5):
                assert process.poll() is None, 'process not held'
                raw = Path(f'/proc/{pid}/smaps_rollup').read_text()
                (out / f'smaps-{sample}.txt').write_text(raw)
                values = {}
                for row in raw.splitlines():
                    if ':' in row:
                        key, value = row.split(':', 1)
                        if value.strip().endswith('kB'):
                            values[key] = int(value.split()[0])
                assert {'Rss','Pss','Private_Clean','Private_Dirty'} <= values.keys()
                samples.append({'sample': sample, 'monotonic_ns': time.monotonic_ns(), 'kib': values})
                time.sleep(0.15)
            output, _ = process.communicate(b'ACK\n', timeout=180)
            (out / 'stdout').write_bytes(line + output)
            (out / 'exit').write_text(str(process.returncode))
            assert process.returncode == 0 and (out / 'stderr').read_bytes() == b''
            done = [x for x in output.decode().splitlines() if x.startswith('DONE ')]
            assert len(done) == 1
            _, n, s, p, checksum = done[0].split()
            assert (int(n), s, p) == (size, scenario, phase)
            selected = size - 1 if scenario == 'near' else size
            expected = 4 * (size * (size - 1) / 2 + selected * (selected - 1) / 2)
            assert float(checksum) == expected, (done, expected)
            result = {'layer': layer, 'size': size, 'scenario': scenario, 'phase': phase,
                      'samples': samples, 'checksum': checksum, 'pid': pid}
            if layer == 'runtime-nativeaot':
                metrics = [x for x in output.decode().splitlines() if x.startswith('METRIC ')]
                assert len(metrics) == 1
                names = ('liveBytes','heapSizeBytes','fragmentedBytes','committedBytes','gen0','gen1','gen2')
                numbers = list(map(int, metrics[0].split()[1:])); assert len(numbers) == len(names)
                result['managed'] = dict(zip(names, numbers))
                result['runtimeEnvironment'] = [x for x in output.decode().splitlines() if x.startswith('ENV ')]
                assert len(result['runtimeEnvironment']) == 1
            (out / 'result.json').write_text(json.dumps(result, indent=2))
            return result
        finally:
            if process.poll() is None:
                process.kill()
                process.wait()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--round', type=int, choices=range(4), required=True)
    parser.add_argument('--product-a', type=Path, required=True)
    parser.add_argument('--product-b', type=Path, required=True)
    parser.add_argument('--host-a', type=Path, required=True)
    parser.add_argument('--host-b', type=Path, required=True)
    args = parser.parse_args()
    assert sys.version_info[:3] == (3,12,13)
    out = args.output.resolve(); out.mkdir(parents=True, exist_ok=False)
    exes = {'python-nativeaot': {'A':args.product_a.resolve(),'B':args.product_b.resolve()},
            'runtime-nativeaot': {'A':args.host_a.resolve(),'B':args.host_b.resolve()}}
    identity = {layer: {arm:digest(p) for arm,p in arms.items()} for layer,arms in exes.items()}
    (out/'identity.json').write_text(json.dumps(identity, indent=2))
    rows = []
    for size in (32768,131072):
        for scenario in ('near','cumulative','full'):
            for phase in ('held','released'):
                suffix = f'{size}-{scenario}-{phase}'
                # CP也走真实握手与校验；它的proc数据只作oracle附录，不混入双臂主表。
                capture([sys.executable,'-u',str(HERE/'probe.py'),str(size),scenario,phase], out/('CP-'+suffix),size,scenario,phase,'CP-oracle')
                for layer in (('python-nativeaot','runtime-nativeaot') if args.round%2==0 else ('runtime-nativeaot','python-nativeaot')):
                    for arm in ('AB','BA','BA','AB')[args.round]:
                        record = capture([str(exes[layer][arm]),str(size),scenario,phase],out/(layer+'-'+arm+'-'+suffix),size,scenario,phase,layer)
                        rows.append(record | {'round':args.round,'arm':arm})
                        (out/'rows.json').write_text(json.dumps(rows,indent=2))
    assert len(rows)==48
    assert identity == {layer:{arm:digest(p) for arm,p in arms.items()} for layer,arms in exes.items()}
    (out/'COMPLETE').write_text('48 independent processes; 2 layers separated\n')


if __name__=='__main__': main()
