# ryupy: pending
"""TreeBuilder 六组同源正域；all 顺序执行同六函数，供唯一 Native 发布。"""
import sys
import xml.etree.ElementTree as ET
from xml.etree.ElementTree import ParseError as ImportedParseError


def core():
    left = ET.TreeBuilder()
    right = ET.TreeBuilder()
    attrs = {'id': '1'}
    root = left.start('root', attrs)
    other = right.start('other', {})
    left.data('alpha')
    right.data('independent')
    child = left.start('child', {})
    left.data('inner')
    assert left.end('ignored') is child
    left.data('tail')
    assert right.end('other') is other and right.close() is other
    assert left.end('root') is root and left.close() is root
    assert root[0] is child and type(child) is ET.Element
    assert root.attrib is attrs
    attrs['outside'] = 'shared'
    assert root.attrib['outside'] == 'shared'
    assert (root.text, child.text, child.tail, other.text) == ('alpha', 'inner', 'tail', 'independent')
    print('core', root.tag, root.text, child.text, child.tail, other.text)
    print('identity', left.close() is root, root[0] is child, root.attrib is attrs)


def flush():
    builder = ET.TreeBuilder()
    assert builder.close() is None
    assert builder.data(object()) is None
    root = builder.start('root', {})
    builder.data('a'); builder.data('b')
    assert builder.close() is root and root.text is None
    child = builder.start('child', {})
    assert root.text == 'ab'
    child.text = 'prefix'
    builder.data('C'); builder.data('D')
    assert builder.end(object()) is child and child.text == 'prefixCD'
    child.tail = 'tail-prefix'
    builder.data('E')
    assert builder.close() is root and child.tail == 'tail-prefix'
    builder.comment('not inserted')
    assert child.tail == 'tail-prefixE'
    builder.data('F'); assert builder.end('wrong') is root
    builder.data('after')
    assert builder.close() is root and root.tail is None
    try:
        builder.end('extra')
    except IndexError as error:
        assert str(error) == 'pop from empty stack'
    else:
        raise AssertionError('empty end accepted')
    assert root.tail == 'after'
    try:
        builder.start('second', {})
    except ET.ParseError as error:
        assert str(error) == 'multiple elements on top level'
    else:
        raise AssertionError('second root accepted')
    builder.data('more')
    try:
        builder.end('again')
    except IndexError:
        pass
    else:
        raise AssertionError('recovery empty end accepted')
    # 本新差分明确补观测 text；不冒称旧 oracle 已打印此字段。
    print('flush', root.text, root.tail, child.text, child.tail, builder.close() is root)


def comments():
    for insert in (False, True):
        builder = ET.TreeBuilder(insert_comments=insert, insert_pis=insert)
        outside = builder.comment('outside')
        outside_pi = builder.pi('before')
        assert outside.tag is ET.Comment and outside_pi.tag is ET.PI
        assert builder.close() is None
        root = builder.start('r', {})
        builder.data('A')
        comment = builder.comment('note')
        builder.data('B')
        pi = builder.pi('target', 'value')
        builder.data('C')
        assert builder.end('r') is root
        if insert:
            assert len(root) == 2 and root[0] is comment and root[1] is pi
            assert (root.text, comment.tail, pi.tail) == ('A', 'B', 'C')
        else:
            assert len(root) == 0 and root.text == 'ABC'
        builder.comment('after'); builder.pi('after')
        assert builder.close() is root
        print('comments', insert, root.text, len(root), comment.text, comment.tail, pi.text, pi.tail)
    # 原工厂已有任意Comment.text及PI假值合同，不被新builder窄域倒退。
    marker = object()
    assert ET.Comment(marker).text is marker
    for value in (None, False, 0, ''):
        assert ET.ProcessingInstruction('target', value).text == 'target'
    print('old-factories', True)


def factories():
    canonical = ET.Element
    for factory in (None, canonical):
        for attrs in ({}, {'a': 'original'}):
            has_attrs = bool(attrs)
            builder = ET.TreeBuilder(element_factory=factory)
            root = builder.start('r', attrs)
            assert type(root) is canonical
            assert (root.attrib is attrs) == (factory is None and has_attrs)
            attrs['later'] = 'source'
            assert ('later' in root.attrib) == (factory is None and has_attrs)
            print('factory', factory is None, has_attrs, root.attrib is attrs, root.attrib)
    saved_comment, saved_pi = ET.Comment, ET.ProcessingInstruction
    builder = ET.TreeBuilder(insert_comments=True, insert_pis=True)
    root = builder.start('r', {})
    try:
        ET.Comment = 'new-comment-tag'
        ET.ProcessingInstruction = 'new-pi-tag'
        # 保存原默认函数，但函数仍读取自己的定义globals。
        comment = builder.comment('C'); pi = builder.pi('P')
        assert (comment.tag, pi.tag) == ('new-comment-tag', 'new-pi-tag')
        assert root[0] is comment and root[1] is pi
        ET.Element = 'rebound-element'
        child = builder.start('child', {})
        assert type(child) is canonical
        print('saved-factories', comment.tag, pi.tag, comment.text, pi.text, type(child) is canonical)
    finally:
        ET.Element = canonical
        ET.Comment = saved_comment
        ET.ProcessingInstruction = saved_pi
    assert ET.Element is canonical and ET.Comment is saved_comment and ET.ProcessingInstruction is saved_pi


def binding():
    calls = [lambda: ET.TreeBuilder(None, None), lambda: ET.TreeBuilder(unknown=True),
             lambda: ET.TreeBuilder(None, element_factory=None),
             lambda: ET.TreeBuilder().start(tag='r', attrs={}),
             lambda: ET.TreeBuilder().end(tag='r'), lambda: ET.TreeBuilder().data(data='x'),
             lambda: ET.TreeBuilder().comment(text='x'),
             lambda: ET.TreeBuilder().pi(target='p'), lambda: ET.TreeBuilder().close(1)]
    for index, operation in enumerate(calls):
        try:
            operation()
        except TypeError as error:
            print('binding', index, str(error))
        else:
            raise AssertionError('binding accepted')


def errors():
    for family in (SyntaxError, ImportedParseError):
        empty = family()
        assert empty.args == () and empty.msg is None
        error = family('message')
        assert error.args == ('message',) and error.msg == 'message'
        assert error.filename is None and error.lineno is None and error.offset is None
        assert error.text is None and error.end_lineno is None and error.end_offset is None
        assert not hasattr(error, 'code') and not hasattr(error, 'position')
        print('error-text', family.__name__, str(empty), repr(empty), str(error), repr(error))
        print('error-fields', family.__module__, family.__name__, error.args, error.msg,
              error.filename, error.lineno, error.offset, error.text, error.end_lineno, error.end_offset)
    direct = SyntaxError('direct')
    assert type(direct) is SyntaxError and direct.args == ('direct',)
    canonical = ET.ParseError
    assert canonical is ImportedParseError
    ET.ParseError = canonical
    assert ET.ParseError is canonical
    assert issubclass(ET.ParseError, SyntaxError) and issubclass(SyntaxError, Exception)
    builder = ET.TreeBuilder(); root = builder.start('r', {}); builder.end('r')
    try:
        builder.start('second', {})
    except ET.ParseError as error:
        assert type(error) is canonical and isinstance(error, SyntaxError)
        print('static-catch', str(error), error.args)
    try:
        raise canonical('imported')
    except ImportedParseError as error:
        print('import-catch', error.msg)
    dynamic = canonical
    try:
        raise canonical('dynamic')
    except dynamic as error:
        print('dynamic-catch', error.msg)
    try:
        raise canonical('base')
    except SyntaxError as error:
        print('base-catch', error.msg, isinstance(error, Exception))
    assert builder.close() is root


selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
if selected in ('core', 'all'): core()
if selected in ('flush', 'all'): flush()
if selected in ('comments', 'all'): comments()
if selected in ('factories', 'all'): factories()
if selected in ('binding', 'all'): binding()
if selected in ('errors', 'all'): errors()
assert selected in ('core', 'flush', 'comments', 'factories', 'binding', 'errors', 'all')
