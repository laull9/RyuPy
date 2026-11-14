"""构建期按完整版本准备官方 PBS：缓存安装树与真实 full 元数据、全部组件许可。

只在构建期联网。归档在临时目录下载并清理，成功缓存以 rename 发布；
系统前缀从不直接递归复制，BUILD 仅用于定位待核实的官方发行。
"""
import ast
from concurrent.futures import ThreadPoolExecutor
from contextlib import contextmanager
import errno
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import uuid
from urllib.parse import unquote

API = 'https://api.github.com/repos/astral-sh/python-build-standalone/releases'
RELEASES = 'https://github.com/astral-sh/python-build-standalone/releases/'


def metadata_options(metadata):
    schema = metadata.get('version')
    if schema == '7':
        return metadata.get('optimizations')
    if schema == '8':
        return metadata.get('build_options')
    raise ValueError('不支持的官方PBS元数据schema：' + str(schema))


def license_paths(metadata):
    paths = set()
    def visit(value):
        if isinstance(value, dict):
            paths.update(value.get('license_paths', []))
            if value.get('license_path'):
                paths.add(value['license_path'])
            for child in value.values():
                visit(child)
        elif isinstance(value, list):
            for child in value:
                visit(child)
    visit(metadata)
    for name in paths:
        if not isinstance(name, str) or any(part in ('', '.', '..') for part in name.split('/')) or '\\' in name or ':' in name:
            raise ValueError('官方元数据许可路径越界')
    return paths


def missing_licenses(root, metadata):
    return [name for name in license_paths(metadata)
            if not (root / name).is_file() or (root / name).stat().st_size == 0]


def cache_ready(root, version, triple, options):
    """发布前检查文件齐备；实际解释器、头文件与ABI仍由Rust探针完整验收。"""
    try:
        metadata = json.loads((root / 'PYTHON.json').read_text(encoding='utf-8'))
        if (metadata.get('python_version') != version or metadata.get('target_triple') != triple
                or metadata_options(metadata) != options or metadata.get('libpython_link_mode') != 'shared'):
            return False
        series = '.'.join(version.split('.')[:2])
        if triple.endswith('windows-msvc'):
            files = ['python.exe', 'include/Python.h', 'python' + series.replace('.', '') + '.dll']
        else:
            suffix = '.dylib' if triple.endswith('apple-darwin') else '.so.1.0'
            files = ['bin/python' + series, 'include/python' + series + '/Python.h', 'lib/libpython' + series + suffix]
        files += ['PYTHON.json', 'distribution-source.json']
        return (all((root / name).is_file() and (root / name).stat().st_size > 0 for name in files)
                and bool(license_paths(metadata)) and (root / 'licenses').is_dir()
                and not missing_licenses(root, metadata))
    except (OSError, ValueError, KeyError, TypeError):
        return False


def publish_cache(tree, destination, version, triple, options):
    """完整候选一次rename发布；并发赢家复用，不完整旧缓存保留在隔离目录。"""
    if not cache_ready(tree, version, triple, options):
        raise ValueError('基础发行候选尚未齐备：' + str(tree))
    with publication_lock(destination):
        publish_locked(tree, destination, version, triple, options)


@contextmanager
def publication_lock(destination):
    """仅锁定毫秒级目录提交；内核在进程退出时释放锁，无中断残留锁占用。"""
    with destination.with_name(destination.name + '.lock').open('a+b') as lock:
        if lock.seek(0, 2) == 0:
            lock.write(b'0')
            lock.flush()
        lock.seek(0)
        if os.name == 'nt':
            import msvcrt
            msvcrt.locking(lock.fileno(), msvcrt.LK_LOCK, 1)
        else:
            import fcntl
            fcntl.flock(lock.fileno(), fcntl.LOCK_EX)
        try:
            yield
        finally:
            lock.seek(0)
            if os.name == 'nt':
                msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(lock.fileno(), fcntl.LOCK_UN)


def publish_locked(tree, destination, version, triple, options):
    if cache_ready(destination, version, triple, options):
        return
    try:
        tree.rename(destination)
    except OSError as error:
        if error.errno not in (errno.EEXIST, errno.ENOTEMPTY):
            raise
        if cache_ready(destination, version, triple, options):
            return
        backup = destination.with_name('incomplete-' + destination.name + '-' + uuid.uuid4().hex)
        destination.rename(backup)
        try:
            tree.rename(destination)
        except OSError:
            if not destination.exists():
                backup.rename(destination)
            raise


def cached_distribution(cache, version, triple, options):
    """仅复用带官方来源记录且精确匹配的完整缓存；后续Rust仍核对实际ABI和所有许可。"""
    for root in sorted(cache.glob(f'cpython-{version}+*-{triple}'), reverse=True):
        if not root.is_dir():
            continue
        source = root / 'distribution-source.json'
        metadata_path = root / 'PYTHON.json'
        if not source.is_file() or not metadata_path.is_file():
            continue
        provenance = json.loads(source.read_text(encoding='utf-8'))
        metadata = json.loads(metadata_path.read_text(encoding='utf-8'))
        release = provenance.get('release', '')
        build = release.removeprefix(RELEASES + 'tag/')
        if (not re.fullmatch(r'\d{8}', build)
                or root.name != f'cpython-{version}+{build}-{triple}'
                or provenance.get('project') != 'python-build-standalone'
                or metadata.get('python_version') != version
                or metadata.get('target_triple') != triple
                or metadata_options(metadata) != options
                or metadata.get('libpython_link_mode') != 'shared'):
            raise ValueError('已有发行缓存的来源或元数据不匹配：' + str(root))
        prefix = RELEASES + f'download/{build}/cpython-{version}+{build}-{triple}'
        stems = [prefix, prefix + '-shared'] if triple.endswith('windows-msvc') else [prefix]
        expected = {'install_archive': [stem + '-install_only.tar.gz' for stem in stems],
                    'metadata_archive': [stem + f'-{options}-full.tar.zst' for stem in stems]}
        for field, urls in expected.items():
            if unquote(provenance.get(field, '')) not in urls:
                raise ValueError('已有发行缓存缺少匹配官方归档来源：' + str(root))
        if not cache_ready(root, version, triple, options):
            continue
        return {'root': str(root), 'url': release, 'build': build}
    return None


def release_assets(release, version, triple, options):
    """匹配新旧官方动态发行命名，完整版本、构建日期和目标必须同时一致。"""
    prefix = f'cpython-{version}+{release["tag_name"]}'
    suffixes = [(f'-{triple}-{options}-full.tar.zst', f'-{triple}-install_only.tar.gz')]
    if triple.endswith('windows-msvc'):
        suffixes.append((f'-{triple}-shared-{options}-full.tar.zst', f'-{triple}-shared-install_only.tar.gz'))
    assets = {asset['name']: asset for asset in release['assets']}
    return next(((assets[prefix + full], assets[prefix + install])
                 for full, install in suffixes if prefix + full in assets and prefix + install in assets), None)


def fetch(url, output=None):
    cmd = ['curl', '--fail', '--location', '--silent', '--show-error',
           '--retry', '3', '--connect-timeout', '30', '--max-time', '600', url]
    # 仅官方API请求使用可选凭据；通过stdin传curl配置，失败诊断与进程参数都不含token。
    config = None
    if url.startswith(API):
        cmd += ['--compressed']
        token = os.environ.get('GH_TOKEN') or os.environ.get('GITHUB_TOKEN')
        if token:
            if any(char in token for char in '\r\n'):
                raise ValueError('GitHub API凭据格式无效')
            config = ('header = ' + json.dumps('Authorization: Bearer ' + token) + '\n').encode()
            cmd += ['--config', '-']
    if output:
        subprocess.run(cmd + ['--output', str(output)], input=config, check=True)
    else:
        return json.loads(subprocess.check_output(cmd, input=config))


def fetch_asset(asset, output):
    """已知官方资产长度的大文件并行取段；逐段长度核对，成功后按偏移合并。"""
    size = asset['size']
    if size < 20 * 1024 * 1024:
        fetch(asset['browser_download_url'], output)
        if output.stat().st_size != size:
            raise ValueError('官方资产下载长度不完整')
        return
    with tempfile.TemporaryDirectory(prefix='parts-', dir=output.parent) as temporary:
        parts = []
        for index in range(4):
            start, end = size * index // 4, size * (index + 1) // 4 - 1
            parts.append((start, end, Path(temporary) / str(index)))
        def download(part):
            start, end, path = part
            subprocess.run(['curl', '--fail', '--location', '--silent', '--show-error',
                            '--retry', '3', '--connect-timeout', '30', '--max-time', '600',
                            '--range', f'{start}-{end}', '--dump-header', str(path) + '.headers',
                            asset['browser_download_url'],
                            '--output', str(path)], check=True)
            headers = Path(str(path) + '.headers').read_text(errors='replace')
            ranges = re.findall(r'(?im)^content-range:\s*bytes\s+(\d+)-(\d+)/(\d+)', headers)
            if not ranges or tuple(map(int, ranges[-1])) != (start, end, size):
                raise ValueError('官方资产 HTTP Content-Range 与请求偏移不匹配')
            if path.stat().st_size != end - start + 1:
                raise ValueError('官方资产 HTTP Range 响应长度不匹配')
        with ThreadPoolExecutor(max_workers=4) as pool:
            list(pool.map(download, parts))
        with output.open('wb') as target:
            for _, _, path in parts:
                with path.open('rb') as source:
                    shutil.copyfileobj(source, target)
    if output.stat().st_size != size:
        raise ValueError('官方资产下载长度不完整')


def extract_archive(archive, destination, members=()):
    """归档工具失败保留原始诊断，明确区分构建机tar/zstd条件。"""
    try:
        result = subprocess.run(['tar', '-xf', str(archive), '-C', str(destination), *members],
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    except FileNotFoundError as error:
        raise ValueError('构建期准备发行需要tar解包工具') from error
    if result.returncode:
        detail = result.stderr.decode('utf-8', errors='replace')
        requirement = '；构建机需要支持Zstandard的tar或PATH中的zstd' if archive.suffix == '.zst' else ''
        raise ValueError('官方发行归档解包失败' + requirement + '：' + detail)


def prepare(req):
    version, triple, options = req['version'], req['triple'], req['options']
    local = Path(req['local']).resolve()
    if local in map(Path, ('/', '/usr', '/usr/local', '/opt', '/opt/homebrew')):
        # 系统前缀只允许自动获取匹配 portable，禁止本地目录整体冻结。
        local = Path(tempfile.gettempdir()) / 'ryupy-no-local-distribution'
    cache = Path.home() / '.cache/ryupy/cpython-auto'
    cache.mkdir(parents=True, exist_ok=True)
    existing = cached_distribution(cache, version, triple, options)
    if existing:
        return existing
    # 元数据完整的本地发行由 Rust 再核对字段与原生 ABI。
    if (local / 'PYTHON.json').is_file() and (local / 'licenses').is_dir():
        metadata = json.loads((local / 'PYTHON.json').read_text(encoding='utf-8'))
        if not missing_licenses(local, metadata):
            return {'root': str(local), 'url': str(local), 'build': (local / 'BUILD').read_text(encoding="utf-8").strip() if (local / 'BUILD').is_file() else version, 'project': 'cpython-local-metadata'}
    build = (local / 'BUILD').read_text(encoding="utf-8").strip() if (local / 'BUILD').is_file() else None
    if build and not (len(build) == 8 and build.isdigit()):
        raise ValueError('本地 BUILD 不是可核实的官方发行日期')
    # 复用用户已准备的真实 full 元数据时，如实标记为本地观察来源。
    metadata_root = Path.home() / f'.cache/ryupy/cpython-provider/pbs-{build}-metadata/python'
    if build and (metadata_root / 'PYTHON.json').is_file():
        metadata = json.loads((metadata_root / 'PYTHON.json').read_text(encoding="utf-8"))
        if metadata.get('python_version') == version and metadata.get('target_triple') == triple:
            destination = cache / f'local-{version}-{build}-{triple}'
            if not cache_ready(destination, version, triple, options):
                with tempfile.TemporaryDirectory(prefix='local-', dir=cache) as temporary:
                    tree = Path(temporary) / 'python'
                    shutil.copytree(local, tree, symlinks=True)
                    shutil.copy2(metadata_root / 'PYTHON.json', tree / 'PYTHON.json')
                    shutil.copytree(metadata_root / 'licenses', tree / 'licenses', dirs_exist_ok=True)
                    (tree / 'distribution-source.json').write_text(json.dumps({'project': 'cpython-local-metadata', 'source': str(local)}))
                    complete_licenses({'root': str(tree), 'url': str(local), 'build': build, 'project': 'cpython-local-metadata'})
                    publish_cache(tree, destination, version, triple, options)
            return {'root': str(destination), 'url': str(local), 'build': build,
                    'project': 'cpython-local-metadata'}
    def releases():
        if build:
            yield fetch(API + '/tags/' + build)
        else:
            # 100条携带全部资产列表会令部分网关504；20条压缩分页保持请求量与响应大小可控。
            for page in range(1, 21):
                batch = fetch(API + f'?per_page=20&page={page}')
                if not batch:
                    break
                yield from batch
    for release in releases():
        prefix = f'cpython-{version}+{release["tag_name"]}'
        selected = release_assets(release, version, triple, options)
        if not selected:
            continue
        full, install = selected
        destination = cache / (prefix + '-' + triple)
        provenance = {'project': 'python-build-standalone', 'release': release['html_url'],
                      'install_archive': install['browser_download_url'],
                      'metadata_archive': full['browser_download_url']}
        if cache_ready(destination, version, triple, options):
            return {'root': str(destination), 'url': release['html_url'], 'build': release['tag_name']}
        with tempfile.TemporaryDirectory(prefix='prepare-', dir=cache) as temporary:
            work = Path(temporary)
            archive = work / 'install.tar.gz'
            fetch_asset(install, archive)
            extract_archive(archive, work)
            archive.unlink()
            archive = work / 'full.tar.zst'
            fetch_asset(full, archive)
            meta = work / 'meta'
            meta.mkdir()
            extract_archive(archive, meta, ['python/PYTHON.json', 'python/licenses'])
            tree = work / 'python'
            shutil.copy2(meta / 'python/PYTHON.json', tree / 'PYTHON.json')
            shutil.copytree(meta / 'python/licenses', tree / 'licenses', dirs_exist_ok=True)
            # install_only 未必含 BUILD；来源来自实际 API 资产 URL，绝不伪造 BUILD。
            if (tree / 'BUILD').is_file() and (tree / 'BUILD').read_text(encoding="utf-8").strip() != release['tag_name']:
                raise ValueError('官方归档 BUILD 与 release 不一致')
            (tree / 'distribution-source.json').write_text(json.dumps(provenance, indent=2))
            complete_licenses({'root': str(tree), 'url': release['html_url'], 'build': release['tag_name']})
            publish_cache(tree, destination, version, triple, options)
        return {'root': str(destination), 'url': release['html_url'], 'build': release['tag_name']}
    raise ValueError(f'官方 PBS 缺少匹配完整版本/目标/常规 GIL 的发行：{version} {triple} {options}；构建停止')


def complete_licenses(result):
    """按发行 tag 的组件源版本补齐上游 full 遗漏许可，并保存真实来源记录。"""
    root = Path(result['root'])
    metadata = json.loads((root / 'PYTHON.json').read_text(encoding="utf-8"))
    missing = missing_licenses(root, metadata)
    if not missing:
        return result
    # 本地来源不可就地改动；补充发生在仓库外私有缓存。
    if result.get('project') == 'cpython-local-metadata' and not str(root).startswith(str(Path.home() / '.cache/ryupy/cpython-auto')):
        raise ValueError('本地发行缺少组件许可证：' + ', '.join(missing))
    sources = {}
    with tempfile.TemporaryDirectory(prefix='licenses-') as temporary:
        downloads = Path(temporary) / 'downloads.py'
        fetch(f'https://raw.githubusercontent.com/astral-sh/python-build-standalone/{result["build"]}/pythonbuild/downloads.py', downloads)
        tree = ast.parse(downloads.read_text(encoding="utf-8"))
        definitions = download_definitions(tree)
        if definitions is None:
            data = Path(temporary) / 'downloads.json'
            fetch(f'https://raw.githubusercontent.com/astral-sh/python-build-standalone/{result["build"]}/pythonbuild/downloads.json', data)
            definitions = json.loads(data.read_text(encoding='utf-8'))
        for relative in missing:
            if relative != 'licenses/LICENSE.zlib-ng.txt':
                raise ValueError('上游组件许可缺失且无已审查补充规则：' + relative)
            component = definitions['zlib-ng']
            version = component['version']
            expected = f'https://github.com/python/cpython-source-deps/archive/refs/tags/zlib-ng-{version}.tar.gz'
            if component['url'] != expected or component['license_file'] != 'LICENSE.zlib-ng.txt':
                raise ValueError('zlib-ng 上游源身份不匹配')
            url = f'https://raw.githubusercontent.com/python/cpython-source-deps/zlib-ng-{version}/LICENSE.md'
            destination = root / relative
            staged = Path(temporary) / 'component-license.txt'
            fetch(url, staged)
            if not staged.is_file() or staged.stat().st_size == 0:
                raise ValueError('上游组件许可证响应为空：' + relative)
            shutil.copy2(staged, destination)
            sources[relative] = url
    (root / 'license-sources.json').write_text(json.dumps(sources, indent=2))
    return result


def download_definitions(tree):
    """静态支持旧版字面量与新版JSON表；绝不执行上游Python源码。"""
    for node in ast.walk(tree):
        targets = node.targets if isinstance(node, ast.Assign) else [node.target] if isinstance(node, ast.AnnAssign) else []
        if any(isinstance(target, ast.Name) and target.id == 'DOWNLOADS' for target in targets):
            value = node.value
            if (isinstance(value, ast.Call) and isinstance(value.func, ast.Attribute)
                    and isinstance(value.func.value, ast.Name) and value.func.value.id == 'json'
                    and value.func.attr == 'load'):
                return None
            return ast.literal_eval(node.value)
    raise ValueError('官方组件来源表缺少DOWNLOADS字面量定义')


if __name__ == '__main__':
    try:
        print(json.dumps(complete_licenses(prepare(json.load(sys.stdin)))))
    except Exception as error:
        sys.exit('自动准备匹配基础发行失败：' + type(error).__name__ + '：' + str(error))
