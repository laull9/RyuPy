# 通用字段解析：首/末匹配、重叠和缺失分隔符；直接、保存与根入口共用同一合同。
def check(label, source, sep, first, last):
    direct = (source.partition(sep), source.rpartition(sep))
    saved_first = source.partition
    saved_last = source.rpartition
    saved = (saved_first(sep), saved_last(sep))
    roots = (str.partition(source, sep), str.rpartition(source, sep))
    assert direct == (first, last) and saved == direct and roots == direct
    for result in direct:
        assert type(result) is tuple and len(result) == 3
        assert all(type(part) is str for part in result)
        assert ''.join(result) == source
    print(label, direct, saved == direct, roots == direct)


# 配置只分开第一个等号，路径扩展名只分开最后一个点；没有分隔符时保留原字段。
check('config', 'TOKEN=alpha=beta', '=', ('TOKEN', '=', 'alpha=beta'), ('TOKEN=alpha', '=', 'beta'))
check('option', '--mode=fast', '=', ('--mode', '=', 'fast'), ('--mode', '=', 'fast'))
check('extension', 'archive.tar.gz', '.', ('archive', '.', 'tar.gz'), ('archive.tar', '.', 'gz'))
check('missing', 'README', '.', ('README', '', ''), ('', '', 'README'))
check('repeated', 'aa::bb::cc', '::', ('aa', '::', 'bb::cc'), ('aa::bb', '::', 'cc'))
check('overlap', 'ababa', 'aba', ('', 'aba', 'ba'), ('ab', 'aba', ''))
check('overlap-even', 'aaaa', 'aa', ('', 'aa', 'aa'), ('aa', 'aa', ''))
check('adjacent', '::::', '::', ('', '::', '::'), ('::', '::', ''))
check('leading', '::tail', '::', ('', '::', 'tail'), ('', '::', 'tail'))
check('trailing', 'head::', '::', ('head', '::', ''), ('head', '::', ''))
check('whole', '::', '::', ('', '::', ''), ('', '::', ''))
check('empty-source', '', 'x', ('', '', ''), ('', '', ''))
check('long-separator', 'abc', 'abcdef', ('abc', '', ''), ('', '', 'abc'))
check('nul', 'a\x00b\x00c', '\x00', ('a', '\x00', 'b\x00c'), ('a\x00b', '\x00', 'c'))
print('fields-ok')
