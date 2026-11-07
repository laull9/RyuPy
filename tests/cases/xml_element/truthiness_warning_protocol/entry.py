# ryupy: pending
# CP3.12真值依孩子数量并发DeprecationWarning；过滤为error必须阻止正常返回。
import warnings
import xml.etree.ElementTree as ET

empty = ET.Element('empty')
full = ET.Element('full')
ET.SubElement(full, 'child')
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('always')
    empty_truth = bool(empty)
    full_truth = bool(full)
    assert empty_truth is False and full_truth is True
    assert len(seen) == 2
    assert seen[0].category is DeprecationWarning and seen[1].category is DeprecationWarning
    assert seen[0].filename == __file__ and seen[1].filename == __file__
    assert seen[1].lineno == seen[0].lineno + 1
    print('truth', empty_truth, full_truth)
    print('warnings', [type(item.message).__name__ for item in seen])
    print('message', str(seen[0].message))
    print('locations', seen[0].filename == __file__, seen[1].lineno - seen[0].lineno)
with warnings.catch_warnings():
    warnings.simplefilter('error', DeprecationWarning)
    try:
        bool(empty)
    except DeprecationWarning as error:
        print('as-error', type(error).__name__, str(error))
    else:
        raise AssertionError('warning filter was ignored')
with warnings.catch_warnings(record=True) as ignored:
    warnings.simplefilter('ignore', DeprecationWarning)
    assert bool(empty) is False and bool(full) is True
    assert len(ignored) == 0
print('unchanged', len(empty), len(full))
