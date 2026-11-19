"""两种不同名称/布局/扩展模型的手写发布输入；所有二进制、wheel及RECORD在仓外生成。"""
import csv
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

HERE = Path(__file__).resolve().parent
CASES = (
    {'id': 'legacy', 'package': 'cedar_dock', 'directory': 'cedar_dock',
     'extension': '_atlas', 'source': 'atlas.c', 'stub': 'atlas.pyi',
     'distribution': 'cedar_export_fixture', 'entry': 'legacy_entry.py'},
    {'id': 'multiphase', 'package': 'violet_ridge', 'directory': 'violet_ridge/engine',
     'extension': '_nacre', 'source': 'nacre.c', 'stub': 'nacre.pyi',
     'distribution': 'violet_export_fixture', 'entry': 'multiphase_entry.py'},
)


def wheel(root, name, files, tag):
    """只列RECORD路径和许可；普通文件不计算摘要。"""
    files = dict(files)
    info = name + '-1.0.dist-info'
    files[info+'/METADATA'] = f'Metadata-Version: 2.3\nName: {name}\nVersion: 1.0\nLicense: MIT\n'
    files[info+'/WHEEL'] = f'Wheel-Version: 1.0\nGenerator: ryupy-interface-fixture\nRoot-Is-Purelib: false\nTag: {tag}\n'
    files[info+'/licenses/LICENSE'] = name + '的手写MIT测试许可\n'
    record = io.StringIO()
    csv.writer(record).writerows((file, '', '') for file in [*files, info+'/RECORD'])
    files[info+'/RECORD'] = record.getvalue()
    target = root/f'{name}-1.0-{tag}.whl'
    with zipfile.ZipFile(target, 'w') as archive:
        for path, data in files.items():
            archive.writestr(path, data)
    return target


def prepare(root, python, env, run):
    result = run([python, '-I', '-B', '-c',
        'import json,sysconfig,sys; print(json.dumps({"suffix":sysconfig.get_config_var("EXT_SUFFIX"),'
        '"include":sysconfig.get_config_var("INCLUDEPY"),"platform":sysconfig.get_platform(),'
        '"tag":"cp%d%d"%sys.version_info[:2]}))'], root, env, 'configuration')
    config = json.loads(result.stdout)
    flags = ['-dynamiclib', '-undefined', 'dynamic_lookup'] if sys.platform == 'darwin' else ['-shared', '-fPIC']
    if sys.platform not in ('darwin', 'linux'):
        raise RuntimeError('此手写原生编译门验macOS/Linux，Windows另验')
    packages = []
    ordinary_source = root/'ordinary.c'
    shutil.copy2(HERE/'ordinary.c', ordinary_source)
    for case in CASES:
        extension = root/(case['extension']+config['suffix'])
        ordinary = root/('ordinary_image'+config['suffix'])
        source = root/case['source']
        shutil.copy2(HERE/case['source'], source)
        run(['cc', *flags, '-I', config['include'], source, '-o', extension], root, env, case['id']+'-compile')
        run(['cc', *flags, ordinary_source, '-o', ordinary], root, env, case['id']+'-ordinary')
        directory = case['directory']
        files = {case['package']+'/__init__.py': '"""测试原生接口包，无Python业务函数。"""\n',
                 directory+'/'+extension.name: extension.read_bytes(),
                 directory+'/'+case['extension']+'.pyi': (HERE/case['stub']).read_bytes(),
                 directory+'/'+ordinary.name: ordinary.read_bytes(),
                 directory+'/ordinary_image.pyi': 'def ordinary_scale(value: int, /) -> int: ...\n',
                 directory+'/stub_only.pyi': 'def absent(value: bytes, /) -> str: ...\nraise RuntimeError("stub不得求值")\n'}
        if directory != case['package']:
            files[directory+'/__init__.py'] = '"""多阶段扩展的独立嵌套布局。"""\n'
        tag = config['tag']+'-'+config['tag']+'-'+config['platform'].replace('-', '_').replace('.', '_')
        packages.append((case, wheel(root, case['distribution'], files, tag)))
    return packages
