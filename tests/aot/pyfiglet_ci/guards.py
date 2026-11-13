"""原 pyfiglet 输入、污染、图与资源安全校验；不导入目标包，不执行产品。"""
import hashlib
import json
from pathlib import Path
import tarfile
import zipfile
from manifest_files import plain_stat, relative_path, files_under
from runtime_dependencies import validate_runtime_dependencies

ARCHIVE_SHA = '758788018ab8faaddc0984e1ea05ff330d3c64be663c513cc1f105f6a3066dab'
ENTRY_SHA = '8cea52bf78c0c504dc2b7b5638c709d906ab240469c8d991d357828dd5baa2e5'
FONT_SHA = '398a52d40da3ec217ce3673f532eb8e2303023369cfa0dc27b11ff43cc306a4f'
RESOURCE_SHA = 'f7c61139b7c3ebfcbc8b97c6f330d92ded75096a409024408e469aedbf4bb8ad'
GOLDEN_SHA = '0334adb06d9ec5d7fb71b2c3a2b7609bcd2a67ab320b3f0995a85d770ec2ecd8'


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    plain_stat(path)
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')


def exists(path):
    # 只把真实ENOENT视为不存在；权限错误和悬空链接不能悄悄通过。
    try:
        path.lstat()
        return True
    except FileNotFoundError:
        return False


def extract(archive, destination):
    require(sha(archive) == ARCHIVE_SHA, '官方归档SHA不符')
    records, seen, total = [], set(), 0
    with tarfile.open(archive) as stream:
        for item in stream.getmembers():
            name = relative_path(item.name.rstrip('/'))
            require(name not in seen and (item.isfile() or item.isdir())
                    and not item.mode & 0o6000, '归档重复/特殊成员')
            seen.add(name)
            total += item.size
            require(total <= 100 * 1024**2, '归档解包预算超限')
            if item.isfile():
                data = stream.extractfile(item).read()
                records.append({'path': name, 'size': len(data),
                                'sha256': hashlib.sha256(data).hexdigest()})
        destination.mkdir()
        stream.extractall(destination, filter='data')
    return records


def source_unchanged(destination, records):
    require(all(sha(destination / row['path']) == row['sha256'] for row in records),
            '归档解包输入已变化')


def pollution(cwd, entry, root, roots, shared=Path('/usr/local/share/pyfiglet'), *, report_path=None):
    # 所有观测先落盘再统一拒绝；单项不可读不阻止保留其它项的真实状态。
    report = {'phase': 'pollution-preflight', 'observations': [], 'errors': []}

    def observe(kind, path, inspect):
        row = {'kind': kind, 'path': str(path)}
        report['observations'].append(row)
        try:
            inspect(row)
        except Exception as error:
            row['error'] = repr(error)
            report['errors'].append({'kind': kind, 'path': str(path), 'error': repr(error)})

    def absent(path, row):
        row['exists'] = exists(path)
        require(not row['exists'], '发现污染路径')

    def empty(row):
        plain_stat(cwd, directory=True)
        row['entries'] = sorted(p.name for p in cwd.iterdir())
        require(not row['entries'], '运行cwd不为空')

    def site(path, row):
        row['exists'] = exists(path)
        row['target_hits'] = []
        if row['exists']:
            if path.is_dir():
                row['target_hits'] = sorted(str(p) for p in path.iterdir()
                                            if p.name.lower().startswith('pyfiglet'))
            elif zipfile.is_zipfile(path):
                with zipfile.ZipFile(path) as archive:
                    row['target_hits'] = [p for p in archive.namelist()
                                         if p.split('/')[0].lower().startswith('pyfiglet')]
            else:
                raise RuntimeError('无法检查可见根')
        require(not row['target_hits'], 'site/stdlib目标包污染')

    def font_identity(row):
        row['size'] = plain_stat(font).st_size
        row['sha256'] = sha(font)
        require(row['size'] == 28504 and row['sha256'] == FONT_SHA, '源字体不符')

    def package_identity(row):
        package = font.parent / '__init__.py'
        row['size'] = plain_stat(package).st_size
        row['sha256'] = sha(package)
        require(package.read_bytes() == b'\n', 'fonts不是原普通空包')

    observe('cwd', cwd, empty)
    checks = [cwd / name for name in ('standard.flf', 'standard.tlf')]
    checks += [shared / name for name in ('standard.flf', 'standard.tlf')]
    checks += [entry.parent / 'pyfiglet', entry.parent / 'pyfiglet.py']
    font = root / 'pyfiglet/fonts/standard.flf'
    checks.append(font.with_suffix('.tlf'))
    for path in checks:
        observe('absent', path, lambda row, path=path: absent(path, row))
    for name in roots:
        path = Path(name)
        observe('site', path, lambda row, path=path: site(path, row))
    observe('font', font, font_identity)
    observe('package', font.parent / '__init__.py', package_identity)
    report['passed'] = not report['errors']
    if report_path is not None:
        save(report_path, report)
    require(report['passed'], '污染预检失败；见逐项观测：' + str(report_path))
    return report


def deployment_files(dist, manifest):
    """核实际声明集及已知源码/托管格式；不声称识别任意伪装文本。"""
    from manifest_verify import image_header
    actual = files_under(dist)
    records = {row['relative_path']: row for row in manifest['artifacts']}
    require(len(records) == len(manifest['artifacts']) and
            set(actual) == set(records) | {'ryupy-artifact.json'}, '部署文件与manifest集合不符')
    require(sum(row['role'] == 'native_executable' for row in records.values()) == 1,
            '部署必须包含唯一native')
    require(sum(row['role'] == 'unclassified_sidecar' for row in records.values()) <= 1,
            '本Linux部署域最多一个ELF调试sidecar')
    observed = []
    validate_runtime_dependencies(manifest['artifacts'], 'linux-x64')
    for name, path in sorted(actual.items()):
        if name == 'ryupy-artifact.json':
            continue
        row = records[name]
        require(row['role'] in ('native_executable', 'declared_resource', 'unclassified_sidecar',
                                'runtime_native_dependency', 'runtime_dependency_license'),
                '未知部署角色')
        require(plain_stat(path).st_size == row['size'] and sha(path) == row['sha256'],
                '部署核验期间文件改变')
        lower = name.lower()
        require(not lower.endswith(('.py', '.pyc', '.pyo', '.dll', '.runtimeconfig.json', '.deps.json')),
                'dist含已知Python/托管部署文件：' + name)
        with path.open('rb') as stream:
            header = stream.read(4)
        # 本窗口唯一目标为Linux ELF；PE无论扩展名/CLR目录如何都不属于此部署。
        require(not header.startswith(b'MZ'), 'Linux dist含PE/可能托管映像：' + name)
        if row['role'] == 'declared_resource':
            require(row['sha256'] == FONT_SHA and row['size'] == 28504, '资源不是冻结字体')
        elif row['role'] == 'runtime_dependency_license':
            require(name == 'ThirdPartyNotices.sqlite3.txt', '未知运行时许可证')
        else:
            if row['role'] == 'unclassified_sidecar':
                require('/' not in name and name.endswith('.dbg'),
                        '陌生sidecar布局，停止，不扩大部署白名单')
            # 已有同SDK真实.dbg为ELF64/x64 ET_DYN，复用同一严格头校验。
            image_header(path, 'linux-x64')
        observed.append(dict(row, header_hex=header.hex()))
    return {'files': observed, 'scope': 'native、固定SQLite库和许可证、冻结字体及至多一个顶层ELF调试sidecar；不检查ELF内嵌内容'}


def graph(path, explicit):
    data = json.loads(path.read_text())
    expected = {'__main__', 'pyfiglet', 'pyfiglet.version', 'pyfiglet.fonts'}
    require(data['compiled_feature'] is True and
            data['session_policy'] == 'ordinary-dynamic-module-pilot', 'CLI实际feature/policy不符')
    require(len(data['modules']) == len(expected) and
            {m['module'] for m in data['modules']} == expected, '实际模块图不符')
    require(all(m['compiled'] is True and m['execution'] == 'LegacySingleton'
                and not m['ordinary_name_handlers'] and not m['relative_from_import']
                for m in data['modules']), '实际模块执行策略改变')
    discovered = {m['module']: m['resource_discovery'] for m in data['modules'] if 'resource_discovery' in m}
    expected_discovery = {} if explicit else {'pyfiglet.fonts': {
        'source': 'pyfiglet', 'anchor': 'pyfiglet.fonts', 'reason': 'literal-files-empty-direct-package'}}
    require(discovered == expected_discovery, '默认发现与显式图的实际来源理由不符')
    return data


def reported_managed(stderr, output):
    paths = [Path(line.removeprefix('已生成 ')) for line in stderr.decode().splitlines()
             if line.startswith('已生成 ')]
    require(len(paths) == 1, 'JIT CLI必须返回唯一PE')
    path = paths[0]
    require(path.is_absolute() and path.resolve().is_relative_to(output.resolve())
            and path.suffix == '.dll', 'CLI PE不属于独占输出')
    plain_stat(path)
    require(path.read_bytes()[:2] == b'MZ', 'CLI产物不是PE')
    return path


def resource_record(manifest):
    resources = manifest['resources']
    require(len(resources) == 1, '只准部署一个显式字体')
    resource = resources[0]
    locations = {row['module_id']: row for row in manifest['module_locations']}
    require(len(resource['consumer_module_ids']) == 1, '字体consumer不唯一')
    location = locations[resource['consumer_module_ids'][0]]
    require(location['module_name'] == 'pyfiglet.fonts' and
            location['kind'] == 'executable_relative', '字体不是Kind2资源锚点')
    require(resource['size'] == 28504 and resource['sha256'] == FONT_SHA, '字体身份不符')
    matches = [row for row in manifest['artifacts']
               if row['relative_path'] == resource['relative_path']]
    require(len(matches) == 1 and matches[0]['role'] == 'declared_resource'
            and matches[0]['size'] == resource['size'] and matches[0]['sha256'] == FONT_SHA,
            '资源record与artifact不一致')
    relative_path(resource['relative_path'])
    return resource


def remove_copy_font(copy, original, source_font, resource):
    # 调用前已校验完整副本manifest；逐级lstat，拒绝链接、硬链接及目录包含别名。
    for other in (original, source_font.parent):
        require(not copy.resolve().is_relative_to(other.resolve()) and
                not other.resolve().is_relative_to(copy.resolve()), '负测目录不是独立副本')
    name = relative_path(resource['relative_path'])
    target, protected = copy / name, original / name
    current = copy
    plain_stat(current, directory=True)
    for part in Path(name).parts[:-1]:
        current /= part
        plain_stat(current, directory=True)
    plain_stat(target)
    require(target.resolve().is_relative_to(copy.resolve()), '删除目标越界')
    require(not target.samefile(protected) and not target.samefile(source_font), '拒绝硬链接源字体')
    for path in (target, protected, source_font):
        require(path.stat().st_size == 28504 and sha(path) == FONT_SHA, '删除前字体身份不符')
    target.unlink()
    require(not exists(target), '删除后字体仍存在')
    require(sha(protected) == sha(source_font) == FONT_SHA, '源/原dist字体改变')
    return name
