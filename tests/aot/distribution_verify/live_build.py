"""采样真实构建期间的SDK落盘流；不是原子观测或性能基准。"""
import json
import os
from pathlib import Path
import re
import subprocess
import time
from common import require, save

STAGE = 'stage=aot-sdk-publish'


def events(data, event):
    # 活跃writer可能只写完半个UTF8字符/半行，仅解析完整换行记录。
    complete = data[:data.rfind(b'\n')+1]
    return [line for line in complete.decode('utf-8').splitlines()
            if line.startswith('[ryupy-stage] event='+event+' ') and STAGE in line]


def capture_live(prefix, command, cwd, env, output, stem, timeout=1200):
    prefix = Path(prefix); prefix.parent.mkdir(parents=True, exist_ok=True)
    command = list(map(str, command)); start = time.monotonic(); observed = None
    row = {'argv': command, 'cwd': str(cwd), 'exit': None}
    evidence = {'scope': '顺序采样，不是原子快照或性能提升证据', 'poll_interval_seconds': 0.1}
    outpath, errpath = prefix.with_suffix('.stdout'), prefix.with_suffix('.stderr')
    process = None
    try:
        with outpath.open('wb') as out, errpath.open('wb') as err:
            process = subprocess.Popen(command, cwd=cwd, env=env, stdin=subprocess.DEVNULL, stdout=out, stderr=err)
            while process.poll() is None:
                require(time.monotonic()-start < timeout, '实时构建超时')
                if observed is None:
                    # 仅枚举本应用普通AOT的固定层级，不递归SDK或读取历史attempt。
                    paths = list((Path(output)/(stem+'.build')).glob('attempt-*/ryupy-aot-0/publish.stdout.log'))
                    for path in paths:
                        sdk = path.read_bytes(); cli = errpath.read_bytes()
                        begin, logs = events(cli, 'begin'), events(cli, 'logs')
                        if (sdk and begin and logs and not events(cli, 'end') and not events(cli, 'error')
                                and process.poll() is None):
                            observed = {'sdk_stdout_path': str(path), 'wall_time_ns': time.time_ns(),
                                        'elapsed_seconds': time.monotonic()-start, 'cli_pid': process.pid,
                                        'cli_poll_after_reads': None, 'sdk_bytes_observed': len(sdk),
                                        'begin_lines': begin, 'logs_lines': logs}
                            prefix.with_name(prefix.name+'-live-sdk.stdout').write_bytes(sdk)
                            prefix.with_name(prefix.name+'-live-cli.stderr').write_bytes(cli)
                            evidence['observed'] = observed
                            save(prefix.with_name(prefix.name+'-live.json'), evidence)
                            break
                time.sleep(0.1)
            row['exit'] = process.wait()
        actual = subprocess.CompletedProcess(command, row['exit'], outpath.read_bytes(), errpath.read_bytes())
        require(actual.returncode == 0, '真实项目构建失败，见原CLI双流')
        require(observed is not None, '未在CLI仍运行时看到非空SDK流及begin/logs')
        require(len(events(actual.stderr, 'begin')) == len(events(actual.stderr, 'end')) == 1,
                'SDK publish begin/end应各一次')
        require(len(events(actual.stderr, 'logs')) == 1 and not events(actual.stderr, 'error'), 'SDK publish日志事件或错误异常')
        return actual, evidence
    finally:
        if process is not None and process.poll() is None:
            process.kill(); process.wait()
        save(prefix.with_suffix('.json'), row)
        save(prefix.with_name(prefix.name+'-live.json'), evidence)


def validate_layout(evidence, layout, stderr, prefix):
    observed = evidence['observed']; actual = Path(observed['sdk_stdout_path'])
    require(os.path.samefile(actual, layout.sdk_root/'publish.stdout.log'), '采样日志不属本次报告SDK根')
    line = events(stderr, 'logs')[0]
    match = re.search(r' stdout=("(?:[^"\\]|\\.)*") stderr=("(?:[^"\\]|\\.)*") elapsed_us=', line)
    require(match is not None, '日志路径事件格式改变')
    for encoded, name in zip(match.groups(), ['publish.stdout.log', 'publish.stderr.log']):
        require(os.path.samefile(Path(json.loads(encoded)), layout.sdk_root/name), '公开日志路径不属本次layout')
    require(actual.read_bytes().startswith(prefix.with_name(prefix.name+'-live-sdk.stdout').read_bytes()),
            '最终SDK流未保留实时观察前缀')
    evidence['final_layout_checked'] = True
    evidence['sdk_root'] = str(layout.sdk_root)
    save(prefix.with_name(prefix.name+'-live.json'), evidence)
