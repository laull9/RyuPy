import optparse

p = optparse.OptionParser(prog='sample', usage='%prog [options]', add_help_option=False)
p.add_option('-n', '--number', type='int', default=0)
p.add_option('-v', '--verbose', action='store_true')
p.add_option('--color', type='choice', choices=['red', 'blue'])
p.add_option('--collect', action='store_true')
for label, args in (
    ('integer', ['-n', 'bad']),
    ('choice', ['--color', 'pink']),
    ('missing', ['-n']),
    ('flagvalue', ['--verbose=x']),
    ('short', ['-z']),
    ('unknown', ['--unknown']),
    ('ambiguous', ['--col']),
):
    seed = optparse.Values({'number': 0})
    try:
        p.parse_args(['-n4'] + args, seed)
    except SystemExit as error:
        print(label, error.code, seed.number, type(error.__context__).__name__)
        if label == 'integer':
            assert type(error.__context__) is optparse.OptionValueError
            assert type(error.__context__.__context__) is ValueError
        if label == 'choice':
            assert type(error.__context__) is optparse.OptionValueError
            assert error.__context__.__context__ is None
