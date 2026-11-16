"""显式super三分合同的纯检查：严格原负例、最小caller AST与具名测试计数。"""
import ast
from collections import Counter
import re

BOUNDARY = '实例模块首切暂不支持非模块/global裸名del目标'
CALLER_FUNCTIONS = {'canonical', 'construct', 'direct', 'invoke_read', 'invoke_callback'}


def audit_caller(path):
    """窄白名单而非模拟完整HIR；源码预检仍由真实driver独立执行。"""
    tree = ast.parse(path.read_text(), filename=str(path))
    allowed = {ast.Module, ast.Assign, ast.Name, ast.Load, ast.Store, ast.FunctionDef,
               ast.arguments, ast.arg, ast.Return, ast.Call, ast.Attribute}
    nodes = list(ast.walk(tree))
    if any(type(node) not in allowed for node in nodes):
        raise RuntimeError('caller超出已盘点AST子集')
    functions = [node for node in tree.body if isinstance(node, ast.FunctionDef)]
    if {node.name for node in functions} != CALLER_FUNCTIONS or len(functions) != 5:
        raise RuntimeError('caller函数集合不符')
    if len(tree.body) != 6 or ast.unparse(tree.body[0]) != 'alias = super':
        raise RuntimeError('canonical别名必须在Instance模块初始化中读取')
    expected = {
        'canonical': ([], 'alias'),
        'construct': (['start', 'receiver'], 'alias(start, receiver)'),
        'direct': (['start', 'receiver'], 'super(start, receiver)'),
        'invoke_read': (['proxy'], 'proxy.read()'),
        'invoke_callback': (['proxy', 'callback'], 'proxy.call(callback())'),
    }
    for function in functions:
        params, expression = expected[function.name]
        if (function.decorator_list or function.returns or function.type_params
                or function.args.posonlyargs or function.args.kwonlyargs
                or function.args.vararg or function.args.kwarg or function.args.defaults
                or [arg.arg for arg in function.args.args] != params
                or any(arg.annotation for arg in function.args.args)
                or len(function.body) != 1 or not isinstance(function.body[0], ast.Return)
                or ast.unparse(function.body[0].value) != expression):
            raise RuntimeError('caller语义形状改变：' + function.name)
    return {'nodes': dict(sorted(Counter(type(n).__name__ for n in nodes).items())),
            'functions': sorted(CALLER_FUNCTIONS), 'scope': 'AST静态盘点，非实际HIR通过证明',
            'precheck': '无class/del/handler/import/annotation/async/generator/comprehension/closure/敏感环境名'}


def check_rejection(exit_code, stdout, stderr, output):
    """只接受原两位置的E0100；额外诊断、崩溃、超时或陈旧PE不能算边界成功。"""
    headers = re.findall(r'^\[([^\]]+)\] (Error|Warning): (.*)$', stderr, re.M)
    if re.findall(r'\[([EW]\d{4})\]', stderr) != ['E0100', 'E0100']:
        raise RuntimeError('存在其它诊断或缺失诊断')
    locations = re.findall(r'subject\.py:(\d+):(\d+)\s*\]', stderr)
    if exit_code != 1 or stdout or headers != [('E0100', 'Error', BOUNDARY)] * 2:
        raise RuntimeError('不是精确预期的两个Instance语法诊断')
    if locations != [('58', '9'), ('96', '9')]:
        raise RuntimeError('Instance语法诊断位置不符')
    if any(word in stderr.lower() for word in ['panicked', 'backtrace', 'fatal']):
        raise RuntimeError('语法诊断伴随崩溃')
    if output.exists() and any(output.rglob('*')):
        raise RuntimeError('预检拒绝后不应有可消费产物或报告')


def check_test_count(text, name):
    """cargo具名ignored门必须真实运行唯一指定测试，不能接受零覆盖。"""
    if not re.search(r'^test (?:\w+::)*' + re.escape(name) + r' \.\.\. ok$', text, re.M):
        raise RuntimeError('缺少指定实际测试成功记录：' + name)
    if 'test result: ok. 1 passed; 0 failed; 0 ignored;' not in text:
        raise RuntimeError('具名门必须恰好执行一个测试')


CALLER_SOURCES = {'__main__': 'entry.py', 'caller': 'caller.py',
                  'model': 'model.py', 'other_model': 'other_model.py'}


def check_caller_plan(plan):
    modules = plan['modules']
    if len(modules) != 4 or {m['module'] for m in modules} != set(CALLER_SOURCES):
        raise RuntimeError('必须恰好编译四个固定源模块')
    for module in modules:
        expected = 'InstanceGlobals' if module['module'] == 'caller' else 'LegacySingleton'
        if module['compiled'] is not True or module['execution'] != expected:
            raise RuntimeError('caller真实Instance及其余Legacy编译合同不符')


def check_caller_sources(sources, expected_hashes):
    if len(sources) != 4 or {s['module_name'] for s in sources} != set(CALLER_SOURCES):
        raise RuntimeError('同次driver必须记录完整四源身份')
    if set(expected_hashes) != set(CALLER_SOURCES):
        raise RuntimeError('当前四源哈希不完整')
    for source in sources:
        if source['compiled_text_sha256'] != expected_hashes[source['module_name']]:
            raise RuntimeError('driver编译文本不等于固定源：' + source['module_name'])


def audit_bindings(root):
    """仅盘点固定四源的词法作用域；不模拟完整HIR或承诺未执行的编译会成功。"""
    class Scope(ast.NodeVisitor):
        def __init__(self, name, kind):
            self.name, self.kind = name, kind
            self.bindings, self.children, self.mutations, self.reads = {}, [], [], set()

        def bind(self, name, kind, line):
            self.bindings.setdefault(name, []).append({'kind': kind, 'line': line})

        def visit_Name(self, node):
            if isinstance(node.ctx, ast.Load):
                self.reads.add(node.id)
            else:
                self.bind(node.id, type(node.ctx).__name__, node.lineno)

        def visit_Attribute(self, node):
            if isinstance(node.ctx, (ast.Store, ast.Del)):
                self.mutations.append({'target': ast.unparse(node), 'line': node.lineno,
                                       'kind': type(node.ctx).__name__})
            self.generic_visit(node)

        def visit_Import(self, node):
            for alias in node.names:
                self.bind(alias.asname or alias.name.split('.')[0], 'import', node.lineno)

        def visit_ExceptHandler(self, node):
            if node.name:
                self.bind(node.name, 'exception-target', node.lineno)
            self.generic_visit(node)

        def visit_FunctionDef(self, node):
            if self.kind == 'function':
                raise RuntimeError('固定模型不应新增嵌套def')
            self.bind(node.name, 'function', node.lineno)
            child = Scope(self.name + '.' + node.name, 'function')
            for arg in node.args.posonlyargs + node.args.args + node.args.kwonlyargs:
                child.bind(arg.arg, 'parameter', node.lineno)
            for statement in node.body:
                child.visit(statement)
            self.children.append(child.result())

        def visit_ClassDef(self, node):
            if self.kind != 'module' or node.keywords or node.decorator_list or len(node.bases) > 1:
                raise RuntimeError('固定模型只允许模块级普通单继承类')
            if any(not isinstance(base, ast.Name) for base in node.bases):
                raise RuntimeError('固定模型基类必须是先定义Name')
            for base in node.bases:
                if not any(b['kind'] == 'class' for b in self.bindings.get(base.id, [])):
                    raise RuntimeError('固定模型基类必须先定义')
            self.bind(node.name, 'class', node.lineno)
            child = Scope(self.name + '.' + node.name, 'class')
            for statement in node.body:
                child.visit(statement)
            result = child.result()
            result['bases'] = [base.id for base in node.bases]
            self.children.append(result)

        def result(self):
            for name, bindings in self.bindings.items():
                definitions = [b for b in bindings if b['kind'] in {'class', 'function'}]
                if definitions and len(bindings) != 1:
                    raise RuntimeError('固定模型定义碰撞：' + self.name + '.' + name)
            return {'scope': self.name, 'kind': self.kind, 'bindings': self.bindings,
                    'reads': sorted(self.reads), 'attribute_mutations': self.mutations,
                    'children': self.children}

    results = {}
    forbidden = (ast.AsyncFunctionDef, ast.Await, ast.Yield, ast.YieldFrom, ast.AnnAssign,
                 ast.TypeAlias, ast.Global, ast.Nonlocal, ast.ListComp, ast.SetComp,
                 ast.DictComp, ast.GeneratorExp, ast.Lambda, ast.ImportFrom)
    for module, filename in CALLER_SOURCES.items():
        tree = ast.parse((root / filename).read_text(), filename=filename)
        for node in ast.walk(tree):
            if isinstance(node, forbidden) or getattr(node, 'type_params', None):
                raise RuntimeError('固定模型超出已盘点语法域')
            if isinstance(node, ast.FunctionDef):
                if node.returns or any(arg.annotation for arg in node.args.args):
                    raise RuntimeError('固定模型不应新增参数或返回注解')
                if any(not isinstance(d, ast.Name) or d.id not in {'property', 'classmethod', 'staticmethod'}
                       for d in node.decorator_list):
                    raise RuntimeError('固定模型不应新增自定义decorator')
        scope = Scope(module, 'module')
        scope.visit(tree)
        results[module] = scope.result()
    return {'scope': '完整词法绑定盘点；不是实际HIR编译证明', 'modules': results}
