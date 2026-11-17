# ryupy: pending
# 通用外部调用与回读：源码码点、显式参数和本地/foreign混合键共享精确Unicode合同。
import operator
import json

pair = '\ud83d\ude00'
emoji = '😀'
lone = '\ud800x'
print(operator.length_hint(pair), operator.length_hint(emoji), operator.length_hint(lone))
print(operator.eq(pair, emoji), operator.eq(pair, '\ud83d\ude00'))
for text in [pair, emoji, lone]:
    returned = str(operator.add(text, '!'))
    print(repr(returned), len(returned), [ord(part) for part in returned])
    print(json.dumps(text, ensure_ascii=True))
# 外部返回的真实str作为本地key；对照原literal和另一个相同UTF16的码点序列。
foreign_pair = operator.add(pair, '')
foreign_emoji = operator.add(emoji, '')
local = {pair: 'pair', emoji: 'emoji'}
print(local[foreign_pair], local[foreign_emoji], len(local))
print('unicode-bridge-ok')
