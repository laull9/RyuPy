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
