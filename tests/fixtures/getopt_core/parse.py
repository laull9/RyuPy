# 有限getopt解析与绑定差分；输入与尾身份均可观察。
import getopt

def report(label, fn):
    try:
        value = fn()
    except Exception as error:
        print(label, 'error', type(error).__name__,
              error.args, str(error), getattr(error, 'msg', None),
              getattr(error, 'opt', None))
    else:
        print(label, 'ok', value)


def parse(label, args, shortopts, longopts=()):
    before = args[:]
    try:
        opts, tail = getopt.getopt(args, shortopts, longopts)
    except Exception as error:
        print(label, 'error', type(error).__name__, error.args,
              str(error), getattr(error, 'opt', None), args == before)
    else:
        print(label, 'ok', opts, tail, type(tail).__name__, tail is args,
              args == before, type(opts).__name__)


def matrix():
    # 首个普通参数、单独减号、双减号；不消费与消费的列表/元组身份。
    for label, args in [('empty-list', []), ('empty-tuple', ()),
                        ('word-list', ['word', '-a']), ('dash', ['-', '-a']),
                        ('word-tuple', ('word', '-a')),
                        ('separator-list', ['--', '-a']),
                        ('separator-tuple', ('--', '-a')),
                        ('consume-tuple', ('-a', 'rest'))]:
        parse(label, args, 'a')
    parse('mixed-order', ['-aabVALUE', '--beta=', '--alpha', 'file', '-a'],
          'ab:', ['alpha', 'beta='])
    parse('separate-arg', ['-b', '--', 'tail'], 'b:')
    parse('short-bmp', ['-éx'], 'éx')
    parse('short-astral', ['-😀x'], '😀x')
    parse('short-astral-value', ['-😀payload'], '😀:')
    parse('short-combining', ['-é'], 'é')
    parse('short-colon', ['-:'], ':')
    parse('short-first-repeat', ['-a', 'tail'], 'aa:')
    parse('unknown-short', ['-z'], 'a')
    parse('missing-short', ['-b'], 'b:')
    parse('unknown-long', ['--z'], '', ['alpha'])
    parse('missing-long', ['--alpha'], '', ['alpha='])
    parse('forbidden-long-value', ['--alpha=x'], '', ['alpha'])
    parse('ambiguous-long', ['--al'], '', ['alpha', 'alpine'])
    parse('long-string', ['--alpha'], '', 'alpha')
    parse('long-tuple', ['--alpha=x=y'], '', ('alpha=',))
    parse('unique-prefix', ['--alp', 'x'], '', ['alpha='])
    parse('duplicate-exact', ['--alpha'], '', ['alpha', 'alpha'])
    parse('duplicate-prefix', ['--al'], '', ['alpha', 'alpha'])
    parse('exact-over-required', ['--alpha=x'], '', ['alpha=', 'alpha'])
    parse('required-exact', ['--alpha', 'x'], '', ['alphabet', 'alpha='])
    parse('long-late-validation-empty', [], '', [1])
    parse('long-late-validation-short', ['-a'], 'a', [1])
    parse('long-element-used', ['--alpha'], '', [1])
    parse('long-scan-after-exact', ['--alpha'], '', ['alpha', 1])
    parse('short-false-value', ['-b', None], 'b:')
    parse('long-false-value', ['--beta', None], '', ['beta='])
    # 绑定器只用有限静态调用，不引入任意对象测试框架。
    report('bind-keywords', lambda: getopt.getopt(args=['-a'], shortopts='a', longopts=[]))
    report('bind-zero', lambda: getopt.getopt())
    report('bind-one', lambda: getopt.getopt([]))
    report('bind-four', lambda: getopt.getopt([], '', [], 0))
    report('bind-unknown', lambda: getopt.getopt([], '', unknown=1))
    report('bind-duplicate', lambda: getopt.getopt([], '', args=[]))
    report('shortopts-lazy', lambda: getopt.getopt([], None))
    report('longopts-before-empty', lambda: getopt.getopt([], '', None))
    report('longopts-before-bad-arg', lambda: getopt.getopt([1], '', None))
    report('args-element-used', lambda: getopt.getopt([1], '', []))


matrix()
