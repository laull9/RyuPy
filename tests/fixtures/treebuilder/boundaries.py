"""明确NI与绑定反例：调用前后marker证明已执行到准确runtime守卫，不属于CP等价。"""
import xml.etree.ElementTree as ET


def reject(label, operation, message):
    print('before', label)
    try:
        operation()
    except NotImplementedError as error:
        assert str(error) == message, (label, str(error))
        print('NI', label, str(error))
    else:
        raise AssertionError(label + ' accepted')


def mark(value):
    print('argument', value)
    return ('file.py', 1, 2, 'line')


def callback(*args):
    raise AssertionError('unsupported callback was invoked')


class Truth:
    def __bool__(self):
        raise AssertionError('unsupported truth protocol was invoked')


for key in ('element_factory', 'comment_factory', 'pi_factory'):
    # 分开显式调用，避免 **mapping 自身准入错误冒充factory守卫。
    if key == 'element_factory':
        reject('custom-element', lambda: ET.TreeBuilder(element_factory=callback),
               'TreeBuilder custom element_factory is not supported')
        reject('noncallable-element', lambda: ET.TreeBuilder(element_factory=7),
               'TreeBuilder custom element_factory is not supported')
    elif key == 'comment_factory':
        reject('custom-comment', lambda: ET.TreeBuilder(comment_factory=callback),
               'TreeBuilder custom comment_factory is not supported')
        reject('noncallable-comment', lambda: ET.TreeBuilder(comment_factory=7),
               'TreeBuilder custom comment_factory is not supported')
    else:
        reject('custom-pi', lambda: ET.TreeBuilder(pi_factory=callback),
               'TreeBuilder custom pi_factory is not supported')
        reject('noncallable-pi', lambda: ET.TreeBuilder(pi_factory=7),
               'TreeBuilder custom pi_factory is not supported')
reject('comments-truth', lambda: ET.TreeBuilder(insert_comments=Truth()),
       'TreeBuilder insert_comments other than bool is not supported')
reject('pis-truth', lambda: ET.TreeBuilder(insert_pis=Truth()),
       'TreeBuilder insert_pis other than bool is not supported')
builder = ET.TreeBuilder(); root = builder.start('root', {})
reject('data-object', lambda: builder.data(object()), 'TreeBuilder data other than exact str is not supported')
reject('attrs-list', lambda: ET.TreeBuilder().start('r', []), 'TreeBuilder attrib other than exact dict is not supported')
reject('attrs-key', lambda: ET.TreeBuilder().start('r', {1: 'value'}), 'TreeBuilder attrib keys other than exact str is not supported')
root.text = object(); builder.data('text')
reject('append-object', lambda: builder.end('root'), 'TreeBuilder text/tail append other than exact str is not supported')
reject('syntax-direct-tuple', lambda: SyntaxError('message', mark('direct')),
       'SyntaxError positional location details is not supported')
Alias = SyntaxError
reject('syntax-alias-tuple', lambda: Alias('message', mark('alias')),
       'SyntaxError positional location details is not supported')
reject('parse-tuple', lambda: ET.ParseError('message', mark('parse')),
       'ParseError positional location details is not supported')
for family in (SyntaxError, ET.ParseError):
    for value in (None, 7, object()):
        reject('message-domain-' + family.__name__, lambda: family(value),
               family.__name__ + ' message other than exact str is not supported')
    try:
        family(msg=mark('keyword'))
    except TypeError as error:
        assert str(error) == family.__name__ + '() takes no keyword arguments'
        print('keyword', str(error))
    else:
        raise AssertionError('keyword accepted')
try:
    SyntaxError(unknown=mark('direct-keyword'))
except TypeError as error:
    assert str(error) == 'SyntaxError() takes no keyword arguments'
    print('direct-keyword', str(error))
else:
    raise AssertionError('direct keyword accepted')
canonical = ET.ParseError
ET.ParseError = canonical
try:
    ET.ParseError = ValueError
except NotImplementedError as error:
    assert str(error) == 'rebinding xml.etree.ElementTree.ParseError is not supported'
    assert ET.ParseError is canonical
    print('rebind-before-write', ET.ParseError is canonical, str(error))
else:
    raise AssertionError('ParseError rebound')
print('boundaries complete')
