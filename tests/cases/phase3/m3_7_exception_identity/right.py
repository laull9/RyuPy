class ValueError(Exception):
    pass


def run(callback):
    try:
        callback()
    except ValueError:
        print('错误：捕获别模块同名类')
    except Exception as error:
        print('右拒绝左类', str(error))
    try:
        raise ValueError('右')
    except ValueError as error:
        print('右捕获', str(error))


def throw_error(generator):
    try:
        generator.throw(ValueError('右注入'))
    except ValueError as error:
        print('右注入未被左捕获', str(error))
