import subject
assert subject.outer_iterable.__builtins__ is subject.first
assert subject.generator_case.__builtins__ is subject.first
assert subject.__builtins__ is subject.third
assert subject.events == [('iterable-before-rebind', 'token1'), ('iterable-after-rebind', 'token1')]
assert next(subject.result) == 'token2'
assert subject.events == [('iterable-before-rebind', 'token1'), ('iterable-after-rebind', 'token1'), ('element', 'token2')]
assert next(subject.result) == 'token2'
assert subject.events == [('iterable-before-rebind', 'token1'), ('iterable-after-rebind', 'token1'), ('element', 'token2'), ('element', 'token2')]
print('genexpr captures before outer iterable and defers element execution')
