"""C0 手写执行链：直接对象调用、原生反向回调、新线程及关闭回调均留下具名帧。"""
import atexit
import threading


def direct_leaf():
    return 21


def direct_outer():
    return direct_leaf() * 2


def callback_leaf():
    return 7


def raising_leaf():
    raise ValueError('audit exception preserved')


def resumed_generator():
    yield 1
    yield 2


def thread_leaf():
    assert direct_leaf() == 21


def shutdown_leaf():
    print('audit shutdown observed', flush=True)


def run_calls():
    assert direct_outer() == 42
    assert native_callback(callback_leaf) == 7
    try:
        native_callback(raising_leaf)
    except ValueError as error:
        assert error.args == ('audit exception preserved',)
    else:
        raise AssertionError('native callback exception was lost')
    assert list(resumed_generator()) == [1, 2]
    worker = threading.Thread(target=thread_leaf)
    worker.start()
    worker.join()
    atexit.register(shutdown_leaf)
