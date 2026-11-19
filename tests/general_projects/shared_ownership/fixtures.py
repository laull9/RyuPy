"""两组互不相关的共享发行手写输入；wheel/RECORD/本地扩展只在仓外同次生成。"""
import csv
import io
import json
from pathlib import Path
import subprocess
import sys
import zipfile

CASES = [
    {'id': 'source', 'module': 'granite_mesh', 'owners': ['copper_gate', 'harbor_widget']},
    {'id': 'native', 'module': 'cobalt_basin', 'owners': ['quartz_relay', 'marsh_engine']},
]


def wheel(root, name, files, tag='py3-none-any', requirements=()):
    """RECORD不计算普通文件摘要，完整列举手写metadata/license与真实载荷。"""
    files = dict(files)
    info = name + '-1.0.dist-info'
    files[info + '/METADATA'] = ('Metadata-Version: 2.3\nName: ' + name + '\nVersion: 1.0\nLicense: MIT\n'
                               + ''.join('Requires-Dist: ' + item + '\n' for item in requirements))
    files[info + '/WHEEL'] = ('Wheel-Version: 1.0\nGenerator: ryupy-shared-contract\n'
                            'Root-Is-Purelib: ' + ('true' if tag == 'py3-none-any' else 'false') + '\nTag: ' + tag + '\n')
    files[info + '/licenses/LICENSE'] = name + ' 的手写 MIT 测试许可\n'
    stream = io.StringIO()
    writer = csv.writer(stream)
    writer.writerows((file, '', '') for file in [*files, info + '/RECORD'])
    files[info + '/RECORD'] = stream.getvalue()
    path = root / (name + '-1.0-' + tag + '.whl')
    with zipfile.ZipFile(path, 'w') as archive:
        for file, content in files.items():
            archive.writestr(file, content)
    return path


def prepare(root, python, env):
    """同一源/数据/扩展分别被两个发行登记，另有各自独立文件和许可。"""
    query = subprocess.run([str(python), '-I', '-c',
        'import json,sysconfig; print(json.dumps({k:sysconfig.get_config_var(k) for k in ("EXT_SUFFIX", "INCLUDEPY")} | {"platform":sysconfig.get_platform()}))'],
        capture_output=True, env=env, timeout=30, check=True)
    config = json.loads(query.stdout)
    source = Path(__file__).with_name('pulse.c')
    native = root / ('_pulse' + config['EXT_SUFFIX'])
    if sys.platform == 'darwin':
        flags = ['-dynamiclib', '-undefined', 'dynamic_lookup']
    elif sys.platform == 'linux':
        flags = ['-shared', '-fPIC']
    else:
        raise RuntimeError('本手写扩展驱动暂验 macOS/Linux；Windows 原生编译门独立待验')
    subprocess.run(['cc', *flags, '-I', config['INCLUDEPY'], str(source), '-o', str(native)],
                   capture_output=True, env=env, timeout=60, check=True)
    tag = 'cp312-cp312-' + config['platform'].replace('-', '_').replace('.', '_')
    packages = []
    for case in CASES:
        package, owners = case['module'], case['owners']
        code = ('from importlib.metadata import version, distribution\n'
                'from importlib.resources import files\n'
                'def run():\n'
                '    owners = ' + repr(owners) + '\n'
                '    assert all(distribution(name).read_text("licenses/LICENSE") for name in owners)\n'
                '    data = files(__package__).joinpath("assets/data.bin").read_bytes().decode("utf-8")\n'
                '    return data + ":" + ",".join(name + "=" + version(name) for name in owners)')
        if case['id'] == 'native':
            code += ' + ":" + str(__import__(__package__ + "._pulse", fromlist=["add"]).add(17, 25))'
        code += '\n'
        shared = {package + '/__init__.py': code, package + '/assets/data.bin': case['id'] + ' 共享资源',
                  package + '/assets/data.body': case['id'] + ' 共享资源'}
        if case['id'] == 'native':
            shared[package + '/' + native.name] = native.read_bytes()
        wheels = []
        for index, owner in enumerate(owners):
            own = {**shared, package + '/member_' + str(index) + '.py': 'VALUE = ' + repr(owner) + '\n'}
            wheels.append(wheel(root, owner, own, tag if case['id'] == 'native' else 'py3-none-any',
                                [owners[1] + '==1.0'] if index == 0 else []))
        packages.append((case, wheels, sorted(shared)))
    return packages
