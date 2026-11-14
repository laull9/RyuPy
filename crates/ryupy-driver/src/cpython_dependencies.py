"""固定Python的静态安装闭包工具：读AST、METADATA、RECORD，不导入应用与扩展。"""
import ast
import csv
import io
import importlib.metadata as metadata
import json
from pathlib import Path
import re
import sys
import tokenize


def fail(message):
    raise ValueError(message)


def record_files(dist):
    # importlib.metadata.files会静默过滤缺失文件；直接读RECORD才能在构建时发现破损安装。
    text = dist.read_text('RECORD') or ''
    files = []
    for row in csv.reader(io.StringIO(text)):
        if not row:
            continue
        if len(row) != 3:
            fail(f'{dist.metadata["Name"]} 的RECORD格式错误')
        files.append(row[0].replace("\\", "/"))
    return files


def main(request):
    if sys.version_info[:3] != (3, 12, 13):
        fail('依赖收集固定使用CPython 3.12.13')
    environment = Path(request['environment']).resolve()
    site = environment / ('Lib/site-packages' if sys.platform == 'win32' else 'lib/python3.12/site-packages')
    if not site.is_dir():
        fail(f'构建环境缺少site-packages：{site}')
    # packaging是明确的构建工具依赖；隔离模式不执行site初始化和.pth。
    sys.path.insert(0, str(site))
    try:
        import packaging
        from packaging.requirements import Requirement
        from packaging.markers import default_environment
        from packaging.utils import canonicalize_name
        from packaging.specifiers import SpecifierSet
        from packaging.tags import sys_tags, parse_tag
    except ImportError:
        fail('请在dependencies.environment中安装构建工具 packaging==25.0')
    if packaging.__version__ != '25.0':
        fail('依赖收集工具固定为 packaging==25.0，请更新构建环境')
    marker_environment = default_environment()
    distributions = {}
    owners = {}
    for dist in metadata.distributions(path=[str(site)]):
        name = canonicalize_name(dist.metadata['Name'])
        if name in distributions:
            fail(f'构建环境含重复发行：{name}')
        distributions[name] = dist
        # 从RECORD识别导入族，覆盖无top_level.txt的wheel和PEP420命名空间。
        for file in record_files(dist):
            top = str(file).split('/')[0]
            module = top[:-3] if top.endswith('.py') else top.split('.')[0]
            if ((len(Path(str(file)).parts) > 1 and '.' not in top)
                    or top.endswith(('.py', '.so', '.pyd'))):
                if module.isidentifier() and module != '__pycache__':
                    owners.setdefault(module, set()).add(name)
    requirements = list(request['requirements'])
    modules = set(request['dynamic_modules'])
    entry = Path(request['entry'])
    project = entry.parent
    while (project / "__init__.py").is_file():
        project = project.parent
    stdlib = set(request['stdlib'])
    provided = set(request['provided_modules'])
    seen = set()

    def scan(path):
        path = path.resolve()
        if path in seen:
            return
        seen.add(path)
        with tokenize.open(path) as source:
            tree = ast.parse(source.read(), filename=str(path))
        for node in ast.walk(tree):
            names = []
            if isinstance(node, ast.Import):
                names = [alias.name for alias in node.names]
            elif isinstance(node, ast.ImportFrom) and node.level:
                # 相对导入只解析当前源码包树，不执行模块或site初始化。
                base = path.parent
                for _ in range(node.level - 1):
                    base = base.parent
                local_names = [node.module] if node.module else [alias.name for alias in node.names]
                for local_name in local_names:
                    local = base.joinpath(*local_name.split('.'))
                    if local.is_dir():
                        for source in sorted(local.rglob('*.py')):
                            scan(source)
                    elif local.with_suffix('.py').is_file():
                        scan(local.with_suffix('.py'))
            elif isinstance(node, ast.ImportFrom) and node.module:
                names = [node.module]
            for name in names:
                top = name.split('.')[0]
                local = project / top
                if local.is_dir():
                    for source in sorted(local.rglob('*.py')):
                        scan(source)
                elif local.with_suffix('.py').is_file():
                    scan(local.with_suffix('.py'))
                elif top not in stdlib:
                    modules.add(top)

    scan(entry)
    for module in sorted(modules):
        if not all(part.isidentifier() for part in module.split('.')):
            fail(f'动态导入模块名非法：{module}')
        top = module.split('.')[0]
        if top in stdlib or top in provided:
            continue
        if top not in owners:
            fail(f'找不到导入模块 {module} 的安装metadata；请安装依赖或补充payloads/modules')
        requirements.extend(sorted(owners[top]))
    selected = {}
    processed = set()
    queue = [(text, '入口或显式声明') for text in requirements]
    while queue:
        text, reason = queue.pop(0)
        requirement = Requirement(text)
        if requirement.marker and not requirement.marker.evaluate(dict(marker_environment, extra='')):
            continue
        name = canonicalize_name(requirement.name)
        dist = distributions.get(name)
        if dist is None:
            fail(f'缺少传递依赖 {text}，来源：{reason}；请安装到 {environment}')
        if requirement.specifier and not requirement.specifier.contains(dist.version, prereleases=True):
            fail(f'依赖版本冲突：{text}，已安装 {dist.version}，来源：{reason}')
        if requirement.url:
            direct = json.loads(dist.read_text('direct_url.json') or '{}')
            if direct.get('url') != requirement.url:
                fail(f'直接URL依赖来源不符：{text}')
        extras = selected.setdefault(name, set())
        extras.update(canonicalize_name(extra) for extra in requirement.extras)
        state = (name, tuple(sorted(extras)))
        if state in processed:
            continue
        processed.add(state)
        provided = {canonicalize_name(extra) for extra in dist.metadata.get_all('Provides-Extra') or []}
        if not extras <= provided:
            fail(f'{name} 缺少声明的extras：{sorted(extras - provided)}')
        for dependency in dist.requires or []:
            parsed = Requirement(dependency)
            if parsed.marker and not any(parsed.marker.evaluate(dict(marker_environment, extra=extra))
                                         for extra in [''] + sorted(extras)):
                continue
            parsed.marker = None
            queue.append((str(parsed), f'{name}=={dist.version}'))
    supported_tags = set(sys_tags())
    payloads = {}
    reports = []
    selected_modules = set()
    for name in sorted(selected):
        dist = distributions[name]
        python_requirement = dist.metadata.get('Requires-Python')
        if python_requirement and not SpecifierSet(python_requirement).contains('3.12.13'):
            fail(f'{name} 的Requires-Python不接受3.12.13：{python_requirement}')
        wheel_metadata = dist.read_text('WHEEL') or ''
        tags = set()
        for line in wheel_metadata.splitlines():
            if line.startswith('Tag: '):
                tags.update(parse_tag(line[5:].strip()))
        if not tags.intersection(supported_tags):
            fail(f'{name} 的wheel ABI/平台不匹配当前固定发行，请重新安装目标wheel')
        files = record_files(dist)
        if not files or not dist.read_text('RECORD'):
            fail(f'{name} 缺少完整RECORD，请使用wheel重新安装，editable安装需先构建wheel')
        direct = json.loads(dist.read_text('direct_url.json') or '{}')
        if direct.get('dir_info', {}).get('editable'):
            fail(f'{name} 为editable安装，请先构建并安装wheel')
        metadata_roots = {str(Path(str(file)).parent) for file in files
                          if str(file).endswith('.dist-info/METADATA')}
        if len(metadata_roots) != 1:
            fail(f'{name} 的RECORD缺少唯一METADATA文件')
        metadata_root = next(iter(metadata_roots))
        recorded = set(map(str, files))
        if not all(metadata_root + '/' + required in recorded for required in ('METADATA', 'WHEEL', 'RECORD')):
            fail(f'{name} 的RECORD未收齐METADATA/WHEEL/RECORD')
        collected = []
        for file in sorted(files, key=str):
            logical = str(file)
            parts = Path(logical).parts
            # wheel的命令行脚本不属于嵌入解释器载荷；其余越界记录必须拒绝。
            if logical.startswith(('../../../bin/', '../../bin/', '../../Scripts/')):
                continue
            if '..' in parts or Path(logical).is_absolute() or '\\' in logical:
                fail(f'{name} 的RECORD路径越界：{logical}')
            if '__pycache__' in parts or logical.endswith(('.pyc', '.pyo')):
                continue
            if logical.endswith(('.pth', '.egg-link')):
                fail(f'{name} 依赖site启动钩子 {logical}，请改用普通wheel和显式模块')
            source = (site / logical).resolve()
            if not source.is_relative_to(site.resolve()) or not source.is_file():
                fail(f'{name} 缺少文件或符号链接越界：{logical}')
            if logical in payloads:
                fail(f'多个发行拥有同一文件：{logical}')
            payloads[logical] = str(source)
            collected.append(logical)
        licenses = [file for file in collected
                    if re.search(r'(^|/)(licenses?|copying|notice)([./_-]|$)', file, re.I)]
        license_text = dist.metadata.get('License-Expression') or dist.metadata.get('License')
        if not licenses and not license_text:
            fail(f'{name} 缺少许可证信息，请补齐wheel metadata与许可证')
        module_names = sorted(module for module, names in owners.items() if name in names)
        if set(module_names).intersection(stdlib):
            fail(f'{name} 的导入族遮蔽固定标准库：{sorted(set(module_names).intersection(stdlib))}')
        selected_modules.update(module_names)
        reports.append({'name': name, 'version': dist.version, 'extras': sorted(selected[name]),
                        'requires_dist': dist.requires or [], 'modules': module_names,
                        'source': {'kind': 'installed_distribution', 'environment': str(environment), 'direct_url': direct,
                                   'wheel_tags': sorted(map(str, tags)),
                                   'installer': dist.read_text('INSTALLER') or '未登记安装器'},
                        'licenses': licenses, 'license_expression': license_text,
                        'files': collected})
    return {'modules': sorted(selected_modules),
            'payloads': [{'source': source, 'destination': destination}
                         for destination, source in sorted(payloads.items())],
            'report': {'collector': 'static_metadata_record', 'packaging_version': packaging.__version__,
                       'environment': str(environment), 'entry': str(entry),
                       'dynamic_modules': request['dynamic_modules'],
                       'requirements': request['requirements'], 'distributions': reports,
                       'marker_environment': marker_environment}}


try:
    print(json.dumps(main(json.load(sys.stdin)), ensure_ascii=False))
except Exception as error:
    print(f'依赖收集：{error}', file=sys.stderr)
    sys.exit(1)
