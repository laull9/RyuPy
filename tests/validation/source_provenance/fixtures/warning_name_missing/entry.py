# 真实Instance缺模块名观察；只使用内建warning，无用户类或特殊基类。
import warnings
import subject

with warnings.catch_warnings(record=True) as records:
    subject.events.clear()
    subject.remove_name()
    subject.issue('missing-default', DeprecationWarning)
    warnings.simplefilter('always')
    subject.issue('missing-always', UserWarning)
    print('name-missing', len(records), subject.events)

print('WN done')
