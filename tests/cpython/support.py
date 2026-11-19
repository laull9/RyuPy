"""CPython provider 验收共用版本、环境、进程及分发声明合同。"""
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile
from typing import NamedTuple

ROOT = Path(__file__).resolve().parents[2]
VERSION = '3.12.13'
NUMPY_VERSION = '2.3.5'


class ProductLayout(NamedTuple):
    """公开入口与唯一数据锚点；严格消费marker，不从后缀或递归搜索猜测产物根。"""
    entry: Path
    root: Path
    data: Path
    kind: str

    @property
    def provider_report(self):
        return self.data / 'cpython-provider.json'

    @property
    def provider_config(self):
        return self.data / 'ryupy-cpython.json'


def generated_entry(stderr):
    """CLI唯一生成记录必须指向实际公开入口，JIT也通过apphost直接启动。"""
    generated = [Path(line[len('已生成 '):]) for line in (stderr.decode('utf-8') if isinstance(stderr, bytes) else stderr).splitlines()
                 if line.startswith('已生成 ')]
    assert len(generated) == 1, ('公开入口报告不是唯一记录', generated)
    return generated[0].resolve()


def product_layout(root):
    """严格canonical-v1门：根仅入口/internal，所有依赖及配置留在同一真实internal。"""
    root = Path(root).resolve()
    data = root / 'internal'
    marker = data / '.ryupy-layout.json'
    assert root.is_dir() and not data.is_symlink() and data.is_dir(), ('缺少exact internal目录', root)
    assert marker.is_file() and not marker.is_symlink(), ('缺少canonical布局marker', marker)
    # marker只含五个短字段；限定读取并拒绝重复键，避免不同消费者选择不同事实。
    with marker.open('rb') as stream:
        encoded = stream.read(4097)
    assert len(encoded) <= 4096, ('布局marker超过4KiB', marker)
    def unique_object(pairs):
        value = {}
        for key, item in pairs:
            assert key not in value, ('布局marker含重复键', key)
            value[key] = item
        return value
    value = json.loads(encoded, object_pairs_hook=unique_object)
    assert isinstance(value, dict), ('布局marker不是对象', marker)
    assert set(value) == {'schema_version', 'kind', 'payload_mode', 'entry', 'entry_kind'}, value
    assert type(value['schema_version']) is int and value['schema_version'] == 1, value
    assert value['kind'] == 'ryupy-application-layout' and value['payload_mode'] == 'canonical-v1', value
    name = value['entry']
    assert isinstance(name, str) and name not in ('', '.', '..', 'internal') and '/' not in name and '\\' not in name, value
    entry = root / name
    assert entry.is_file() and not entry.is_symlink(), ('marker入口不是普通文件', entry)
    assert value['entry_kind'] in ('apphost', 'native_aot'), value
    assert {path.name for path in root.iterdir()} == {name, 'internal'}, ('公开根含额外依赖/遗留flat文件', root)
    return ProductLayout(entry, root, data, value['entry_kind'])


def built_product(stderr, output, stem, mode):
    """本次非OneFile实际构建的公开目录，校验CLI记录与marker为同一入口。"""
    assert mode in ('jit', 'standalone', 'aot'), mode
    root = Path(output) if mode == 'jit' else Path(output) / (stem + '.dist')
    layout = product_layout(root)
    assert layout.kind == ('apphost' if mode == 'jit' else 'native_aot'), layout
    assert generated_entry(stderr) == layout.entry, ('CLI入口与marker不一致', layout)
    return layout


def provider_data(output, stem, mode):
    """OneFile私有AOT树本轮明确flat/no marker；其它应用必须消费canonical marker。"""
    if mode == 'onefile':
        root = Path(output) / (stem + '.dist')
        assert not (root / 'internal/.ryupy-layout.json').exists(), ('OneFile私有布局合同已变化', root)
        assert (root / 'cpython-provider.json').is_file(), root
        return root
    root = Path(output) if mode == 'jit' else Path(output) / (stem + '.dist')
    return product_layout(root).data


class StandaloneLayout(NamedTuple):
    """现代CLI公开原生入口及构建/发布根；数据目录由marker严格确定。"""
    native: Path
    build: Path
    dist: Path

    @property
    def data(self):
        return self.dist / 'internal'


def standalone_layout(stderr, output, stem, target_rid):
    """只接受本次发布唯一入口；托管中间产物属于构建细节。"""
    aot_tools = str(ROOT / 'tests/aot')
    if aot_tools not in sys.path:
        sys.path.insert(0, aot_tools)
    from manifest_files import plain_stat
    from manifest_verify import image_header
    output = Path(output).resolve()
    build, dist = output / (stem + '.build'), output / (stem + '.dist')
    for directory in (output, build, dist):
        plain_stat(directory, directory=True)
    layout = built_product(stderr, output, stem, 'standalone')
    plain_stat(layout.entry)
    image_header(layout.entry, target_rid)
    return StandaloneLayout(layout.entry, build, dist)


def execute(command, *, cwd, env, timeout=240):
    """全部进程有界执行，保留退出码和完整输出参与当次比较。"""
    return subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                          capture_output=True, timeout=timeout)


def checked(result, label):
    assert result.returncode == 0, (label, result.returncode,
                                  result.stdout[:1600].decode(errors='replace'),
                                  result.stderr[:5000].decode(errors='replace'))
    return result


def environment(product_root=None):
    """消除Python/加载器开发环境污染，固定可控locale与时区。"""
    env = {key: value for key, value in os.environ.items()
           if not key.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0', TZ='UTC',
               LC_ALL='C.UTF-8', DOTNET_CLI_UI_LANGUAGE='en-US',
               NO_PROXY='127.0.0.1,localhost', no_proxy='127.0.0.1,localhost')
    if product_root:
        env['RYUPY_RUNTIME_DIR'] = str(Path(product_root).resolve() / 'runtime')
    return env


def require_python(python, cwd, env):
    result = checked(execute([python, '-I', '-c',
                             'import platform; print(platform.python_version())'],
                            cwd=cwd, env=env), '固定 CPython')
    assert result.stdout.decode().strip() == VERSION, result.stdout


def external_workspace(prefix, parent=None):
    """拒绝将日志、数据库和分发产物放回源码仓库。"""
    directory = Path(parent or tempfile.gettempdir()).resolve()
    assert not directory.is_relative_to(ROOT), '验收输出必须位于仓库外'
    directory.mkdir(parents=True, exist_ok=True)
    return Path(tempfile.mkdtemp(prefix=prefix, dir=directory))


def rid():
    system = {'darwin': 'osx', 'linux': 'linux', 'win32': 'win'}[sys.platform]
    machine = platform.machine().lower()
    assert machine in ('arm64', 'aarch64', 'x86_64', 'amd64'), machine
    return system + ('-arm64' if machine in ('arm64', 'aarch64') else '-x64')


def declaration(distribution, payloads, modules, container_transfer='synchronized', product_root=None):
    """开发机根显式提供；上游发行和许可证随声明进入产品清单。"""
    distribution = Path(distribution).resolve()
    licenses = []
    for candidate in ('lib/python3.12/LICENSE.txt', 'Lib/LICENSE.txt', 'LICENSE.txt'):
        if (distribution / candidate).is_file():
            licenses.append(candidate)
            break
    if not licenses:
        licenses.append('lib/python3.12/LICENSE.txt')
    licenses.extend(str(path.relative_to(distribution)).replace('\\', '/') for path in sorted((distribution / 'licenses').glob('*.txt')))
    result = {'schema': 1, 'python_version': VERSION,
            'distribution_root': str(Path(distribution).resolve()),
            'modules': modules, 'payloads': payloads, 'container_transfer': container_transfer,
            'source': {'project': 'python-build-standalone', 'build_id': '20260623',
                       'source_url': 'https://github.com/astral-sh/python-build-standalone/releases/tag/20260623',
                       'licenses': licenses}}
    if product_root:
        bridge = Path(product_root).resolve() / 'bridge'
        shim_name = {'darwin': 'libryupy_cpython.dylib', 'win32': 'ryupy_cpython.dll'}.get(sys.platform, 'libryupy_cpython.so')
        result.update(bridge_directory=str(bridge), shim=str(bridge / shim_name))
    return result


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def private_distribution(source, workspace, metadata=None):
    """私有发行输入可在搬迁时隐藏，避免修改其他验证进程共用的uv发行。"""
    target = workspace / 'distribution-input'
    shutil.copytree(source, target, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
    if metadata:
        metadata = Path(metadata).resolve()
        shutil.copy2(metadata / 'PYTHON.json', target / 'PYTHON.json')
        shutil.copytree(metadata / 'licenses', target / 'licenses', dirs_exist_ok=True)
    assert (target / 'PYTHON.json').is_file() and (target / 'licenses/LICENSE.openssl-3.txt').is_file(), (
        '基础发行缺少上游完整metadata/第三方许可，请传--distribution-metadata指向固定full archive的python目录')
    return target


def audit_native_paths(dist):
    """逐原生映像审计加载边，禁止依赖源发行/venv的绝对路径。"""
    if sys.platform == 'darwin':
        audit_macho_paths(dist)
    elif sys.platform == 'linux':
        audit_elf_paths(dist)
    elif sys.platform == 'win32':
        audit_pe_paths(dist)


def audit_macho_paths(dist):
    """Mach-O独立审计拆到专属模块，避免公共支持库承载平台解析。"""
    if __package__:
        from .macho_audit import audit_macho_paths as audit
    else:
        from macho_audit import audit_macho_paths as audit
    audit(dist)


def audit_elf_paths(dist):
    """Linux产品载荷独立复核：工具失败、外部rpath和未闭合加载边都使验收失败。"""
    readelf = shutil.which('readelf')
    assert readelf, 'Linux独立审计需要readelf'
    system = {'ld-linux-x86-64.so.2', 'libc.so.6', 'libm.so.6', 'libpthread.so.0',
              'libdl.so.2', 'librt.so.1', 'libresolv.so.2', 'libutil.so.1',
              'libgcc_s.so.1', 'libstdc++.so.6'}
    for path in dist.rglob('*.so*'):
        if not path.is_file():
            continue
        result = subprocess.run([readelf, '-d', str(path)], capture_output=True, check=True, timeout=60)
        for line in result.stdout.decode(errors='strict').splitlines():
            if not any(tag in line for tag in ('(NEEDED)', '(RUNPATH)', '(RPATH)')):
                continue
            assert '[' in line and ']' in line, (path, line)
            value = line.split('[', 1)[1].split(']', 1)[0]
            if '(NEEDED)' in line:
                if value in system:
                    continue
                assert '/' not in value, (path, value)
                matches = list(dist.rglob(value))
                assert len(matches) == 1, ('Linux原生边必须随包唯一解析', path, value, matches)
            else:
                for entry in value.split(':'):
                    assert entry == '$ORIGIN' or entry.startswith('$ORIGIN/'), (path, entry)
                    relative = entry[len('$ORIGIN'):].lstrip('/')
                    target = (path.parent / relative).resolve()
                    assert target.is_relative_to(dist.resolve()) and target.is_dir(), (path, entry)


def audit_pe_paths(dist):
    # Windows 校验所有 .dll 与 .pyd 均为合法 PE 文件
    for path in dist.rglob('*'):
        if path.suffix.lower() not in ('.dll', '.pyd'):
            continue
        with path.open('rb') as stream:
            header = stream.read(2)
            assert header == b'MZ', f"Windows 原生映像不是有效的 PE 文件：{path}"


def inspect_host_report(path, dist, *, closed=True):
    """宿主报告来自真实加载和关闭状态；任何包文件均须位于分发根。"""
    report = json.loads(path.read_text(encoding='utf-8'))
    if closed:
        assert report['state'] == 'closed', report
        assert report['handles'] == report['managed_roots'] == report['pending_releases'] == report['active_leases'] == 0, report
    assert not report['synchronization_failure'], report
    for field in ('python_library', 'shim_library'):
        assert Path(report[field]).resolve().is_relative_to(dist.resolve()), report[field]
    for name, filename in report['module_paths'].items():
        if filename and name != '__main__':
            assert Path(filename).resolve().is_relative_to(dist.resolve()), (name, filename)
    return report
