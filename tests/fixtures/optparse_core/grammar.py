import optparse

p = optparse.OptionParser(prog='sample', add_help_option=False)
p.add_option('-v', '--verbose', action='store_true', default=False)
p.add_option('-q', '--quiet', action='store_false', dest='verbose')
p.add_option('-n', '--number', type='int', default='7')
p.add_option('-s', '--string', type='string', default='base')
p.add_option('--color', type='choice', choices=['red', 'blue'], default='red')
args = ['first', '-vn3', '--str=hello', '--col', 'blue', '--', '-q']
v, remaining = p.parse_args(args)
print('types', type(p) is optparse.OptionParser, type(v) is optparse.Values)
print('grammar', v.verbose, v.number, v.string, v.color, remaining)
print('original', args)
for args in (['-vqn-010', '-', 'tail'], ['-n', '--'], ['--string', '--color'], ['--number=010']):
    try:
        v, remaining = p.parse_args(args)
        print('tokens', v.verbose, v.number, v.string, remaining)
    except SystemExit as error:
        print('token-error', error.code)
p.disable_interspersed_args()
v, remaining = p.parse_args(['first', '-v'])
print('stop', v.verbose, remaining)
p.enable_interspersed_args()
v, remaining = p.parse_args(['first', '-v'])
print('resume', v.verbose, remaining)
