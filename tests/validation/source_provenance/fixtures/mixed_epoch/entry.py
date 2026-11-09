# 同epoch按真实源行选择module/once，二元altkey必须共享，两顺序各仅一条。
import warnings

def first():
    warnings.warn('shared-alt', UserWarning)

def second():
    warnings.warn('shared-alt', UserWarning)

for order in (0, 1):
    with warnings.catch_warnings(record=True) as records:
        warnings.simplefilter('module', lineno=5)
        warnings.simplefilter('once', lineno=8)
        if order == 0:
            first()
            second()
        else:
            second()
            first()
        print('mixed-epoch', order, len(records))
        for record in records:
            print('event', str(record.message), record.lineno)
print('ME done')
