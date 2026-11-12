"""Linux原生进程测量；RSS为全后代采样和，不冒充精确cgroup峰值。"""
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import time


def require(value, message):
    if not value: raise ValueError(message)


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def save(path, value): Path(path).write_text(json.dumps(value, indent=2, ensure_ascii=False)+'\n')


def tree_rss(root_pid, known, proc=Path('/proc')):
    rows = {}
    for entry in proc.iterdir():
        if not entry.name.isdigit(): continue
        try:
            raw = (entry/'stat').read_text(); fields = raw[raw.rfind(')')+2:].split()
            rows[int(entry.name)] = (int(fields[1]), int(fields[2]), int(fields[21]))
        except (OSError, ValueError, IndexError): continue
    known.add(root_pid)
    while True:
        more = {pid for pid, (parent, group, _) in rows.items() if parent in known or group == root_pid}
        if more <= known: break
        known.update(more)
    # 进程退出与采样竞争时允许遗漏，报告明确这是下界估计；共享页可能重复计入RSS。
    return sum(row[2] for pid, row in rows.items() if pid in known)*os.sysconf('SC_PAGE_SIZE')


def capture(prefix, argv, cwd, env, timeout, sample_tree=False):
    prefix = Path(prefix); prefix.parent.mkdir(parents=True, exist_ok=True)
    save(str(prefix)+'.argv.json', {'argv': [str(x) for x in argv], 'cwd': str(cwd), 'env': env})
    start = time.perf_counter(); peak = 0; samples = 0; failure = None; known = set()
    with open(str(prefix)+'.stdout', 'xb') as out, open(str(prefix)+'.stderr', 'xb') as err:
        process = subprocess.Popen(argv, cwd=cwd, env=env, stdin=subprocess.DEVNULL,
                                   stdout=out, stderr=err, start_new_session=True)
        try:
            if sample_tree:
                while process.poll() is None:
                    peak = max(peak, tree_rss(process.pid, known)); samples += 1
                    if time.perf_counter()-start > timeout:
                        failure = 'timeout'; break
                    time.sleep(0.01)
            else:
                # 阻塞wait避免Popen.wait(timeout)轮询量化短进程wall。
                def expired(_signum, _frame): raise subprocess.TimeoutExpired(argv, timeout)
                previous = signal.signal(signal.SIGALRM, expired)
                signal.setitimer(signal.ITIMER_REAL, timeout)
                try: process.wait()
                except subprocess.TimeoutExpired: failure = 'timeout'
                finally:
                    signal.setitimer(signal.ITIMER_REAL, 0); signal.signal(signal.SIGALRM, previous)
        except BaseException:
            failure = 'capture-error'
            raise
        finally:
            if failure:
                try: os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError: pass
            process.wait()
    elapsed = (time.perf_counter()-start)*1000
    # 对正常退出仍回收遗留进程组，不允许后台子进程污染后续样本。
    try: os.killpg(process.pid, signal.SIGKILL)
    except ProcessLookupError: pass
    record = {'exit': process.returncode, 'failure': failure, 'wall_ms': elapsed,
              'raw_streams_complete': failure is None,
              'process_tree_rss_sampled_peak_bytes': peak if sample_tree else None,
              'rss_samples': samples, 'rss_scope': '10ms descendant sum of RSS; shared pages counted per process; not exact peak'}
    save(str(prefix)+'.json', record); return record


def positive(record, prefix, golden):
    require(record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'], '退出/原流不完整')
    require(Path(str(prefix)+'.stdout').read_bytes() == golden and Path(str(prefix)+'.stderr').read_bytes() == b'', '输出不等于完整CP黄金')


def elf(path):
    with Path(path).open('rb') as stream: header = stream.read(64)
    require(len(header) == 64 and header[:6] == b'\x7fELF\x02\x01'
            and int.from_bytes(header[18:20], 'little') == 62 and os.access(path, os.X_OK), '非真实Linux x64 ELF可执行入口')


def inventory(root):
    root = Path(root).resolve(); rows = {}; allocated = 0
    require(root.is_dir(), '缺少完整standalone目录')
    for path in sorted(root.rglob('*')):
        name = path.relative_to(root).as_posix()
        if path.is_symlink():
            require(path.resolve().is_relative_to(root) and path.resolve().is_file(), '链接逃逸/断链/目录链接')
            rows[name] = {'symlink': os.readlink(path)}; continue
        if path.is_dir(): continue
        require(path.is_file(), '产物含特殊文件')
        stat = path.stat(); allocated += stat.st_blocks*512
        rows[name] = {'sha256': sha(path), 'bytes': stat.st_size, 'mode': stat.st_mode & 0o777}
    require(rows, '空standalone目录')
    return {'files': rows, 'logical_bytes': sum(x.get('bytes', 0) for x in rows.values()),
            'allocated_bytes': allocated, 'file_count': len(rows)}
