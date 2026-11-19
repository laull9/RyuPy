"""固定Python的静态安装闭包工具：读AST、METADATA、RECORD，不导入应用与扩展。"""
import ast
import csv
import io
import importlib.metadata as metadata
import json
from pathlib import Path, PurePosixPath
import re
import sys
import tokenize


def fail(message):
    raise ValueError(message)


def record_files(dist, *, strict=True):
    # importlib.metadata.files会静默过滤缺失文件；直接读RECORD才能在构建时发现破损安装。
    text = dist.read_text('RECORD') or ''
    files = []
    seen = set()
    for row in csv.reader(io.StringIO(text)):
        if not row:
            continue
        if len(row) != 3:
            fail(f'{dist.metadata["Name"]} 的RECORD格式错误')
        # 保留既有Windows RECORD斜杠兼容；点分量/重复斜杠归一后仍禁止单发行内部重复。
        logical = str(PurePosixPath(row[0].replace("\\", "/")))
        if logical in seen:
            if strict:
                fail(f'{dist.metadata["Name"]} 的RECORD包含重复安装目标：{logical}')
            continue
        seen.add(logical)
        files.append(logical)
    return files


def canonical_target(logical):
    """沿既有分发规则规范相对目标；不按包名、扩展名或文件内容分类。"""
    path = PurePosixPath(logical)
    if (not logical or '\\' in logical or path.is_absolute() or not path.parts
            or '..' in path.parts or any(ord(char) < 32 or ord(char) == 127 for char in logical)):
        fail(f'RECORD路径越界或不是规范相对目标：{logical!r}')
    return str(path)


def validated_source(site, logical, owner):
    """保留site内安全链接的物化语义；缺失、链接环、外逃及非普通文件明确拒绝。"""
    logical = canonical_target(logical)
    try:
        boundary = site.resolve(strict=True)
        source = (site / logical).resolve(strict=True)
    except (OSError, RuntimeError) as error:
        fail(f'{owner} 的RECORD文件缺失或符号链接循环：{logical}；{error}')
    if not source.is_relative_to(boundary) or not source.is_file():
        fail(f'{owner} 的RECORD缺少普通文件或符号链接越界：{logical}')
    return source


class SharedFiles:
    """以规范目标与真实源身份合并跨发行载荷，逐owner保留审计，禁止同owner重复。"""
    def __init__(self):
        self.payloads = {}
        self.owners = {}

    def add(self, logical, source, owner):
        logical = canonical_target(logical)
        source = str(source.resolve(strict=True))
        previous = self.payloads.get(logical)
        if previous is not None:
            if owner in self.owners[logical]:
                fail(f'{owner} 重复登记同一安装目标：{logical}')
            if previous != source:
                fail(f'不同源文件争用同一安装目标：{logical}；{previous}；{source}')
        else:
            self.payloads[logical] = source
            self.owners[logical] = set()
        self.owners[logical].add(owner)

    def shared_files(self):
        return [{'destination': logical, 'source': self.payloads[logical], 'owners': sorted(owners)}
                for logical, owners in sorted(self.owners.items()) if len(owners) > 1]


def source_imports(tree):
    """仅明确捕获导入错误的 try 主体允许缺包；函数调用不继承定义处保护。

    不求值条件、不执行源码；handler/else/finally 使用进入 try 前的保护状态。
    同源码绑定过异常名时保守退回必需导入，避免把用户自定义异常当成内建异常。
    """
    exception_names = {'ImportError', 'ModuleNotFoundError'}
    shadowed = {node.id for node in ast.walk(tree)
                if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Store)}
    for node in ast.walk(tree):
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            shadowed.add(node.name)
        elif isinstance(node, ast.arg):
            shadowed.add(node.arg)
        elif isinstance(node, ast.alias):
            shadowed.add(node.asname or node.name.split('.')[0])
        elif isinstance(node, ast.ExceptHandler) and node.name:
            shadowed.add(node.name)

    def catches(node):
        if isinstance(node, ast.Tuple):
            return any(catches(item) for item in node.elts)
        return (isinstance(node, ast.Name) and node.id in exception_names
                and node.id not in shadowed)

    class Imports(ast.NodeVisitor):
        def __init__(self):
            self.optional = False
            self.items = []

        def visit_Import(self, node):
            self.items.append((node, self.optional))

        visit_ImportFrom = visit_Import

        def visit_Try(self, node):
            previous = self.optional
            self.optional = previous or any(catches(h.type) for h in node.handlers)
            for item in node.body:
                self.visit(item)
            self.optional = previous
            for item in [*node.handlers, *node.orelse, *node.finalbody]:
                self.visit(item)

        def visit_FunctionDef(self, node):
            previous = self.optional
            self.optional = False
            self.generic_visit(node)
            self.optional = previous

        visit_AsyncFunctionDef = visit_FunctionDef

        def visit_TryStar(self, node):
            # except* 可能把单个导入异常包装成 ExceptionGroup；外层 ImportError 也未必接住。
            previous = self.optional
            self.optional = False
            self.generic_visit(node)
            self.optional = previous

    visitor = Imports()
    visitor.visit(tree)
    return visitor.items


def main(request):
    if sys.version_info < (3, 9):
        fail(f'依赖收集要求 CPython 3.9+，当前为 {sys.version}')
    environment = Path(request['environment']).resolve()
    py_dir = f'lib/python{sys.version_info.major}.{sys.version_info.minor}/site-packages'
    site = environment / ('Lib/site-packages' if sys.platform == 'win32' else py_dir)
    if not site.is_dir():
        fail(f'构建环境缺少site-packages：{site}')
    # packaging是明确的构建工具依赖；隔离模式不执行site初始化和.pth。
    sys.path.insert(0, prepare_build_tools() if request.get('auto_tools') else str(site))
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
        # 未选中的发行只提供名称归属线索；新文件验证必须限于本次冻结闭包。
        for file in record_files(dist, strict=False):
            top = str(file).split('/')[0]
            module = top[:-3] if top.endswith('.py') else top.split('.')[0]
            if ((len(Path(str(file)).parts) > 1 and '.' not in top)
                    or top.endswith(('.py', '.so', '.pyd'))):
                if module.isidentifier() and module != '__pycache__':
                    owners.setdefault(module, set()).add(name)
    requirements = list(request['requirements'])
    modules = set(request['dynamic_modules'])
    optional_imports = []
    entry = Path(request['entry'])
    project = entry.parent
    while (project / "__init__.py").is_file():
        project = project.parent
    stdlib = set(request['stdlib'])
    provided = set(request['provided_modules'])
    seen = set()

    roots = list(dict.fromkeys([project.resolve(), *(Path(value).resolve() for value in request.get('search_paths', []))]))

    def installed_without_metadata(top):
        # 可选仅允许确实缺席；已有未登记源码/扩展不能静默漏包，仍走原 metadata 错误。
        return ((site / top).exists() or (site / (top + '.py')).exists()
                or (site / (top + '.pyc')).exists()
                or (site / (top + '.so')).exists() or any(site.glob(top + '.*.so'))
                or (site / (top + '.pyd')).exists() or any(site.glob(top + '.*.pyd')))

    def follow_local(root, target):
        # 只跟随实际导入链及父包初始化，不把同目录测试/开发文件当应用依赖。
        current = root
        for part in target.relative_to(root).parts:
            current = current / part
            if current.is_dir():
                initializer = current / '__init__.py'
                if initializer.is_file():
                    scan(initializer, root)
            elif current.with_suffix('.py').is_file():
                scan(current.with_suffix('.py'), root)
                break
            else:
                break

    def scan(path, root):
        # macOS 的 /var 与 /private/var 指向同一目录，边界比较须采用同一真实路径。
        path = path.resolve()
        identity = (path.resolve(), root)
        if identity in seen:
            return
        seen.add(identity)
        with tokenize.open(path) as source:
            tree = ast.parse(source.read(), filename=str(path))
        for node, optional in source_imports(tree):
            if isinstance(node, ast.ImportFrom) and node.level:
                base = path.parent
                for _ in range(node.level - 1):
                    base = base.parent
                if not base.is_relative_to(root):
                    fail('相对导入越过项目源码根：' + str(path))
                target = base.joinpath(*(node.module or '').split('.')) if node.module else base
                follow_local(root, target)
                for alias in node.names:
                    if alias.name != '*':
                        follow_local(root, target.joinpath(*alias.name.split('.')))
                continue
            if isinstance(node, ast.Import):
                names = [alias.name for alias in node.names]
            elif isinstance(node, ast.ImportFrom) and node.module:
                names = [node.module]
            else:
                continue
            for name in names:
                top = name.split('.')[0]
                local_root = next((candidate for candidate in roots
                                   if (candidate / top).is_dir() or (candidate / top).with_suffix('.py').is_file()), None)
                if local_root is not None:
                    target = local_root.joinpath(*name.split('.'))
                    follow_local(local_root, target)
                    if isinstance(node, ast.ImportFrom):
                        for alias in node.names:
                            if alias.name != '*':
                                follow_local(local_root, target.joinpath(*alias.name.split('.')))
                elif top not in stdlib:
                    if optional:
                        optional_imports.append({'module': name, 'file': str(path),
                                                 'line': node.lineno, 'installed': top in owners})
                        if top in owners or installed_without_metadata(top):
                            modules.add(top)
                    else:
                        modules.add(top)

    scan(entry, roots[0])
    for name in request.get('include_modules', []):
        if not all(part.isidentifier() for part in name.split('.')):
            fail('显式本地模块名非法：' + name)
        top = name.split('.')[0]
        local_root = next((root for root in roots if (root / top).is_dir() or (root / top).with_suffix('.py').is_file()), None)
        if local_root is None:
            fail('显式包含的本地模块未找到源码：' + name)
        follow_local(local_root, local_root.joinpath(*name.split('.')))
    for name in request.get('delegated_modules', []):
        if not all(part.isidentifier() for part in name.split('.')):
            fail('显式委派本地模块名非法：' + name)
        top = name.split('.')[0]
        local_root = next((root for root in roots if (root / top).is_dir() or (root / top).with_suffix('.py').is_file()), None)
        if local_root is None:
            fail('显式委派的本地模块未找到源码：' + name)
        target = local_root.joinpath(*name.split('.'))
        follow_local(local_root, target)
        if target.is_dir() and (target / '__init__.py').is_file():
            for py_file in sorted(target.rglob('*.py')):
                if '__pycache__' not in py_file.parts:
                    scan(py_file, local_root)
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
    shared = SharedFiles()
    reports = []
    selected_modules = set()
    for name in sorted(selected):
        dist = distributions[name]
        python_requirement = dist.metadata.get('Requires-Python')
        curr_ver = f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}"
        if python_requirement and not SpecifierSet(python_requirement).contains(curr_ver):
            fail(f'{name} 的Requires-Python不接受{curr_ver}：{python_requirement}')
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
        build_headers = []
        for file in sorted(files, key=str):
            logical = str(file)
            parts = Path(logical).parts
            # wheel的命令行脚本不属于嵌入解释器载荷；其余越界记录必须拒绝。
            if logical.startswith(('../../../bin/', '../../bin/', '../../Scripts/')):
                continue
            # wheel 的 headers 安装方案位于环境 include 内；它们仅供扩展构建，
            # 不进入运行期 site-packages。只接受精确方案和真实存在的 C/C++ 头文件。
            header_prefix = '../' * len(site.relative_to(environment).parts) + 'include/'
            if logical.lower().startswith(header_prefix.lower()):
                tail = Path(logical[len(header_prefix):])
                include = (environment / 'include').resolve()
                header = (site / logical).resolve()
                if (not tail.parts or '..' in tail.parts or '\\' in logical
                        or tail.suffix.lower() not in ('.h', '.hpp', '.hh', '.hxx')
                        or not include.is_relative_to(environment)
                        or not header.is_relative_to(include) or not header.is_file()):
                    fail(f'{name} 的RECORD头文件路径越界或缺失：{logical}')
                build_headers.append(logical)
                continue
            if '..' in parts or Path(logical).is_absolute() or '\\' in logical:
                fail(f'{name} 的RECORD路径越界：{logical}')
            if '__pycache__' in parts or logical.endswith(('.pyc', '.pyo')):
                continue
            if logical.endswith(('.pth', '.egg-link')):
                fail(f'{name} 依赖site启动钩子 {logical}，请改用普通wheel和显式模块')
            source = validated_source(site, logical, name)
            shared.add(logical, source, name)
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
                        'files': collected, 'build_headers': build_headers})
    return {'modules': sorted(selected_modules),
            'payloads': [{'source': source, 'destination': destination}
                         for destination, source in sorted(shared.payloads.items())],
            'report': {'collector': 'static_metadata_record', 'packaging_version': packaging.__version__,
                       'environment': str(environment), 'entry': str(entry),
                       'search_paths': list(map(str, roots)),
                       'source_includes': request.get('include_modules', []),
                       'dynamic_modules': request['dynamic_modules'],
                       'optional_imports': optional_imports,
                       'shared_files': shared.shared_files(),
                       'requirements': request['requirements'], 'distributions': reports,
                       'marker_environment': marker_environment}}


if __name__ == '__main__':
    try:
        print(json.dumps(main(json.load(sys.stdin)), ensure_ascii=False))
    except Exception as error:
        print(f'依赖收集：{error}', file=sys.stderr)
        sys.exit(1)
