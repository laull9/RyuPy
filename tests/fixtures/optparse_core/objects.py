import optparse

p = optparse.OptionParser(prog='sample', add_help_option=False)
option = optparse.Option('-n', '--number', type='int', default='7', help='number')
print('option', type(option) is optparse.Option, p.add_option(option) is option, option.container is p)
print('fields', option.dest, option.type, option.action, option.default)
a, remaining = p.parse_args([])
b, remaining = p.parse_args([])
print('fresh', a is b, a.number, type(a.number) is int, p.defaults['number'])
option.default = 99
print('option-default', p.get_default_values().number, option.default)
p.set_default('number', '8')
print('set-default', p.get_default_values().number, p.defaults['number'])
p.set_defaults(number='9', extra='kept')
print('set-defaults', p.get_default_values().number, p.get_default_values().extra)
p.defaults['number'] = '10'
print('live-defaults', p.get_default_values().number)
seed = optparse.Values({'number': 11, 'extra': 'original'})
v, remaining = p.parse_args(['-n12'], seed)
print('provided', v is seed, v.number, v.extra, remaining)
p.add_option('--new', default='not-added-to-seed')
v, remaining = p.parse_args([], seed)
print('no-fill', v is seed, hasattr(v, 'new'))
seed.marker = []
print('dict', seed.__dict__ is seed.__dict__, seed.__dict__['marker'] is seed.marker)
del seed.marker
print('delete', hasattr(seed, 'marker'))
print('equality', optparse.Values({'x': 1}) == optparse.Values({'x': 1}))
