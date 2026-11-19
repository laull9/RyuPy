import optparse

p = optparse.OptionParser(prog='sample', version='%prog 1.0')
print('version-value', p.get_version())
p.print_usage()
p.parse_args(['--version'])
print('unreachable')
