import optparse

p = optparse.OptionParser(prog='sample', usage='%prog [options] FILE', version='%prog 1.0', description='A small parser.')
p.add_option('-n', '--number', type='int', default=7, metavar='N', help='Count (default: %default).')
p.add_option('-v', '--verbose', action='store_true', help='Show details.')
p.parse_args(['--help'])
print('unreachable')
