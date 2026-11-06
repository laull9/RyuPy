"""T3：动态全部hooks、普通duck和公开chunk别名；有限调用，不隐藏复制或回滚。"""
from textwrap import TextWrapper as TW

events = []
marker = ['custom result']


class Spy(TW):
    def _munge_whitespace(self, text):
        events.append(['munge', text])
        return 'prepared'

    def _split(self, text):
        events.append(['split', text])
        self.saved = ['a', ' ', 'b']
        return self.saved

    def _fix_sentence_endings(self, chunks):
        events.append(['fix', chunks is self.saved, chunks[:]])
        chunks[1] = '  '

    def _wrap_chunks(self, chunks):
        events.append(['wrap', chunks is self.saved, chunks[:]])
        return marker


spy = Spy(width=0, fix_sentence_endings=True, max_lines=1, placeholder='custom')
assert spy.wrap(None) is marker
assert events == [['munge', None], ['split', 'prepared'],
                  ['fix', True, ['a', ' ', 'b']], ['wrap', True, ['a', '  ', 'b']]]
assert spy.saved == ['a', '  ', 'b']
print('T3.pipeline', events, spy.saved, marker)


class SplitChunks(TW):
    def _split_chunks(self, text):
        return ['override']


assert SplitChunks(20).wrap(None) == ['override']
print('T3.split-chunks', SplitChunks(20).wrap(None))


class PublicOverride(TW):
    def wrap(self, text):
        return ['overridden public']

    def _wrap_chunks(self, chunks):
        return ['hook:' + ''.join(chunks)]


public = PublicOverride()
assert public.wrap('ab cd') == ['overridden public']
assert TW.wrap(public, 'ab cd') == ['hook:ab cd']
assert TW.fill(public, 'ignored') == 'overridden public'
print('T3.root-dispatch', public.wrap('ab cd'), TW.wrap(public, 'ab cd'), TW.fill(public, 'ignored'))


class Duck:
    fix_sentence_endings = False

    def _split_chunks(self, text):
        self.saved = ['duck', text]
        return self.saved

    def _wrap_chunks(self, chunks):
        assert chunks is self.saved
        return chunks


duck = Duck()
result = TW.wrap(duck, 'x')
assert result is duck.saved and result == ['duck', 'x'] and not isinstance(duck, TW)
print('T3.duck-wrap', result)


class Late(TW):
    pass


late = Late(3)
before = late.wrap('ab cd')
Late._wrap_chunks = lambda self, chunks: ['class override']
after = late.wrap('ab cd')
late._wrap_chunks = lambda chunks: ['instance override']
assert before == ['ab', 'cd'] and after == ['class override']
assert late.wrap('ab cd') == ['instance override']
# exact root实例也可遮蔽hook，不能因旧资格检查而跳过用户callable。
root = TW(3)
root._wrap_chunks = lambda chunks: ['root instance override']
assert root.wrap('ab cd') == ['root instance override']
print('T3.late-method', before, after, late.wrap('ab cd'), root.wrap('ab cd'))


class Plain(TW):
    pass


plain = Plain(3)
assert plain.wrap('ab cd') == ['ab', 'cd']
plain.width = 8
assert plain.wrap('ab cd') == ['ab cd']
print('T3.late-width', plain.width, plain.wrap('ab cd'))

chunks = ['ab', ' ', 'cd', ' ', 'ef']
alias = chunks
lines = TW(3)._wrap_chunks(chunks)
assert alias is chunks and alias == [] and lines == ['ab', 'cd', 'ef']
print('T3.consume-alias', lines, alias)

chunks, current = ['abcdef'], []
chunks_alias, current_alias = chunks, current
assert TW()._handle_long_word(chunks, current, 0, 2) is None
assert chunks is chunks_alias and current is current_alias
assert chunks_alias == ['cdef'] and current_alias == ['ab']
print('T3.longword-break', chunks_alias, current_alias)
chunks, current = ['abcdef'], []
chunks_alias, current_alias = chunks, current
assert TW(break_long_words=False)._handle_long_word(chunks, current, 0, 2) is None
assert chunks is chunks_alias and current is current_alias
assert chunks_alias == [] and current_alias == ['abcdef']
print('T3.longword-preserve', chunks_alias, current_alias)

events = []
chunks = ['abcdef']


class Long(TW):
    def _handle_long_word(self, reversed_chunks, current, used, width):
        assert reversed_chunks is chunks
        events.append([reversed_chunks[:], current[:], used, width])
        return TW._handle_long_word(self, reversed_chunks, current, used, width)


lines = Long(2)._wrap_chunks(chunks)
assert lines == ['ab', 'cd', 'ef'] and chunks == []
assert events == [[['abcdef'], [], 0, 2], [['cdef'], [], 0, 2]]
print('T3.dynamic-longword', lines, events, chunks)

events = []
chunks = ['ab', ' ', 'cdefgh']
current_aliases = []
hook_marker = RuntimeError('hook stop')


class Raising(TW):
    def _handle_long_word(self, reversed_chunks, current, used, width):
        assert reversed_chunks is chunks
        events.append([reversed_chunks[:], current[:], used, width])
        current_aliases.append(current)
        reversed_chunks.append('seen')
        raise hook_marker


try:
    Raising(3)._wrap_chunks(chunks)
except RuntimeError as error:
    assert error is hook_marker
else:
    raise AssertionError('longword hook exception swallowed')
assert events == [[['cdefgh'], ['ab', ' '], 3, 3]]
assert chunks == ['cdefgh', 'seen'] and current_aliases == [['ab', ' ']]
print('T3.raise-partial', events, chunks, current_aliases, str(hook_marker))

# _munge_whitespace执行期间重绑下一步，_split_chunks必须在回调后再查找_split。
rebind_events = []


class Rebinding(TW):
    def _munge_whitespace(self, text):
        rebind_events.append(['munge', text])

        def replacement(prepared):
            rebind_events.append(['replacement', prepared])
            return [prepared]

        self._split = replacement
        return 'fresh'

    def _split(self, text):
        raise AssertionError('stale split method was captured')


assert Rebinding(20).wrap('original') == ['fresh']
assert rebind_events == [['munge', 'original'], ['replacement', 'fresh']]
print('T3.rebind-next-hook', rebind_events)
