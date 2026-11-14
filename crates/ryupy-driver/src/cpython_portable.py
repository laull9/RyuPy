"""构建期按完整版本准备官方 PBS：缓存安装树与真实 full 元数据、全部组件许可。

只在构建期联网。归档在临时目录下载并清理，成功缓存以 rename 发布；
系统前缀从不直接递归复制，BUILD 仅用于定位待核实的官方发行。
"""
import ast
from concurrent.futures import ThreadPoolExecutor
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
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
            if not destination.is_dir():
                with tempfile.TemporaryDirectory(prefix='local-', dir=cache) as temporary:
                    tree = Path(temporary) / 'python'
                    shutil.copytree(local, tree, symlinks=True)
                    shutil.copy2(metadata_root / 'PYTHON.json', tree / 'PYTHON.json')
                    shutil.copytree(metadata_root / 'licenses', tree / 'licenses', dirs_exist_ok=True)
                    tree.rename(destination)
            return {'root': str(destination), 'url': str(local), 'build': build,
                    'project': 'cpython-local-metadata'}
    def releases():
        if build:
            yield fetch(API + '/tags/' + build)
        else:
            for page in range(1, 41):
                batch = fetch(API + f'?per_page=5&page={page}')
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
        if destination.is_dir():
            (destination / 'distribution-source.json').write_text(json.dumps(provenance, indent=2))
            return {'root': str(destination), 'url': release['html_url'], 'build': release['tag_name']}
        with tempfile.TemporaryDirectory(prefix='prepare-', dir=cache) as temporary:
            work = Path(temporary)
            archive = work / 'install.tar.gz'
            fetch_asset(install, archive)
            subprocess.run(['tar', '-xf', str(archive), '-C', str(work)], check=True)
            archive.unlink()
            archive = work / 'full.tar.zst'
            fetch_asset(full, archive)
            meta = work / 'meta'
            meta.mkdir()
            subprocess.run(['tar', '-xf', str(archive), '-C', str(meta),
                            'python/PYTHON.json', 'python/licenses'], check=True)
            tree = work / 'python'
            shutil.copy2(meta / 'python/PYTHON.json', tree / 'PYTHON.json')
            shutil.copytree(meta / 'python/licenses', tree / 'licenses', dirs_exist_ok=True)
            # install_only 未必含 BUILD；来源来自实际 API 资产 URL，绝不伪造 BUILD。
            if (tree / 'BUILD').is_file() and (tree / 'BUILD').read_text(encoding="utf-8").strip() != release['tag_name']:
                raise ValueError('官方归档 BUILD 与 release 不一致')
            try:
                tree.rename(destination)
            except FileExistsError:
                pass
        (destination / 'distribution-source.json').write_text(json.dumps(provenance, indent=2))
        return {'root': str(destination), 'url': release['html_url'], 'build': release['tag_name']}
    raise ValueError(f'官方 PBS 缺少匹配完整版本/目标/常规 GIL 的发行：{version} {triple} {options}；构建停止')


def complete_licenses(result):
    """按发行 tag 的组件源版本补齐上游 full 遗漏许可，并保存真实来源记录。"""
    root = Path(result['root'])
    metadata = json.loads((root / 'PYTHON.json').read_text(encoding="utf-8"))
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
    missing = [p for p in paths if not (root / p).is_file()]
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
        definitions = next(ast.literal_eval(n.value) for n in tree.body
                           if isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id == 'DOWNLOADS' for t in n.targets))
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
            fetch(url, destination)
            sources[relative] = url
    (root / 'license-sources.json').write_text(json.dumps(sources, indent=2))
    return result


if __name__ == '__main__':
    try:
        print(json.dumps(complete_licenses(prepare(json.load(sys.stdin)))))
    except Exception as error:
        sys.exit('自动准备匹配基础发行失败：' + str(error))
