# 错误传播到模块后捕获；完整比较三段上下文/原因及所有用户帧。
import getopt


def fail():
    import getopt as options
    options.getopt(['--bad'], '', ['alpha'])


try:
    raise ValueError('outer-context')
except ValueError:
    try:
        fail()
    except getopt.GetoptError as error:
        raise RuntimeError('outer-wrapper') from error
