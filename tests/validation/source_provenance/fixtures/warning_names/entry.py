# None先类别校验再抑制；其它非str和缺键按<string>，str子类读取载体内容。
import warnings
import subject

existing = subject.Stringified('existing')
with warnings.catch_warnings(record=True) as records:
    subject.events.clear()
    subject.__name__ = None
    subject.issue('new', subject.Constructed)
    subject.issue(existing, UserWarning)
    print('none-suppression', len(records), subject.events)
    try:
        subject.issue('bad-category', int)
    except TypeError:
        print('none-category', 'TypeError', subject.events)

for label, value in (('object', subject.NameObject()), ('integer', 7),
                     ('list', []), ('string-subclass', subject.NameString('__main__'))):
    with warnings.catch_warnings(record=True) as records:
        subject.events.clear()
        subject.__name__ = value
        subject.issue('default-check', DeprecationWarning)
        print('name-default', label, len(records), subject.events)
        warnings.simplefilter('always')
        subject.issue('always-check', UserWarning)
        print('name-always', label, len(records), subject.events)

with warnings.catch_warnings(record=True) as records:
    subject.events.clear()
    subject.remove_name()
    subject.issue('missing-default', DeprecationWarning)
    warnings.simplefilter('always')
    subject.issue('missing-always', UserWarning)
    print('name-missing', len(records), subject.events)

for label, message, category in (('constructor', 'new', subject.Constructed),
                                  ('existing-str', existing, UserWarning)):
    with warnings.catch_warnings(record=True) as records:
        subject.events.clear()
        subject.__name__ = '__main__'
        subject.issue(message, category)
        print('context-before-message', label, len(records), subject.events)
print('WN done')
