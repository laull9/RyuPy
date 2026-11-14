# A1受控hosted原流、身份与产物读取；不重试、不从观察结果生成期望。
import hashlib
import json
import os
from pathlib import Path
import signal
import subprocess
import xml.etree.ElementTree as ET


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def save(path, value):
    Path(path).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def require(ok, message):
    if not ok:
        raise RuntimeError(message)


def snapshot(root):
    names = subprocess.check_output(['git', 'ls-files', '-z'], cwd=root).split(b'\0')
    return {n.decode(): sha(root / n.decode()) for n in names if n}


def capture(out, label, argv, env, cwd, timeout=300, expected=0):
    """先落argv，始终先存双流/退出再判断；超时杀整个子进程组并回收。"""
    argv = [str(x) for x in argv]
    save(out / (label + '.command.json'), {'argv': argv, 'cwd': str(cwd),
         'env': {k: env[k] for k in ('DOTNET_ROOT', 'RYUPY_RUNTIME_DIR', 'LC_ALL', 'TZ') if k in env}})
    record = {'exit': None, 'timeout': False, 'launch_error': None}
    stdout = stderr = b''
    try:
        process = subprocess.Popen(argv, cwd=cwd, env=env, stdout=subprocess.PIPE,
                                   stderr=subprocess.PIPE, start_new_session=True)
        try:
            stdout, stderr = process.communicate(timeout=timeout)
        except subprocess.TimeoutExpired:
            record['timeout'] = True
            os.killpg(process.pid, signal.SIGKILL)
            stdout, stderr = process.communicate()
        record['exit'] = process.returncode
    except OSError as error:
        record['launch_error'] = type(error).__name__
        stderr = str(error).encode()
    (out / (label + '.stdout')).write_bytes(stdout)
    (out / (label + '.stderr')).write_bytes(stderr)
    record.update(stdout_sha256=hashlib.sha256(stdout).hexdigest(),
                  stderr_sha256=hashlib.sha256(stderr).hexdigest())
    save(out / (label + '.result.json'), record)
    require(not record['timeout'] and record['launch_error'] is None, label + '未正常完成')
    if expected is not None:
        require(record['exit'] == expected, label + '退出不符')
    return record


def read_artifacts(out):
    result = {}
    for key in ('dll', 'native', 'manifest'):
        path = out / ('artifact-' + key + '.txt')
        if path.exists():
            actual = Path(path.read_text().strip()).resolve(strict=True)
            require(actual.is_relative_to(out.resolve()), '产物越过独占输出根')
            result[key] = actual
    require('dll' in result, '缺真实Artifacts.dll')
    return result


def verify_five_trx(path):
    rows = ET.parse(path).findall('.//{*}UnitTestResult')
    names = {'CanonicalCallReturnsOneRealMutableDictionary',
             'DefaultDeletionIsNameErrorAndStableIdentitySurvivesRebinding',
             'ExactColdAdditionPreservesAll1716PriorRecords',
             'ColdAdditionRejectsMissingChangedFlagsSignaturesAndOverloads',
             'CandidateMatchingForgedPermissionCannotCrossGroupDuplicate'}
    require(len(rows) == 5 and {x.get('testName').split('.')[-1] for x in rows} == names
            and all(x.get('outcome') == 'Passed' for x in rows), 'C#五具名TRX不符')


def negative_result(folder, record):
    require(record['exit'] == 1, 'driver负例只接受自然exit1')
    require((folder / 'result-kind.txt').read_text().strip() == 'has-errors', '非typed HasErrors')
    require('[E0100]' in (folder / 'diagnostics.txt').read_text(), '非E0100')
    require(not list(folder.rglob('*.dll')) and not (folder / 'artifact-dll.txt').exists(), '拒绝已生成业务DLL')


def aot_origin(manifest, executable):
    data = json.loads(Path(manifest).read_text())
    rows = [x for x in data['module_locations'] if x['module_name'] == 'subject']
    require(len(rows) == 1 and rows[0]['kind'] == 'executable_relative', 'subject来源锚不唯一/错误kind')
    rel = Path(rows[0]['relative_file'])
    require(not rel.is_absolute() and '..' not in rel.parts, '非法虚拟来源锚')
    return str((Path(executable).parent / rel).resolve())
