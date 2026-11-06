# 真实文件模块导出、setter、global与循环导入。

import binding_provider as provider
from binding_provider import public as frozen
first = [provider.read() is provider.public, provider.alias is frozen, frozen(3)]
def replacement(value=9):
    return ['replacement', value]
provider.public = replacement
second = [provider.read() is replacement, provider.public(4), frozen(5)]
provider.mutate('after global write')
result = [first, second, provider.public, provider.read(), frozen is provider.alias]

print('exports', result)

import binding_cycle_a as first
import binding_cycle_b as second
result = [first.observed_before, second.seen, first.public(), callable(first.public)]

print('cycle', result)
