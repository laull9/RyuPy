"""默认CLI可用性及原流合同；显式NI观察不伪装CP字节等价。"""
from pathlib import Path
import hashlib
import json


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def parse(raw):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, '重复JSON键')
            result[key] = value
        return result
    return json.loads(raw, object_pairs_hook=unique)


def build_argv(cli, entry, out, source_root, aot):
    # 不传诊断功能或stdlib根，证明普通公开入口本身承担来源规划。
    values = [str(cli), 'build', str(entry), '--out-dir', str(out), '-I', str(source_root)]
    if aot:
        values.append('--aot')
    return values


def positive(record, stdout, stderr, golden):
    require(record.get('exit') == 0 and record.get('failure') is None and
            record.get('raw_streams_complete') is True, '非正常完成或原流缺失')
    require(stdout == golden and stderr == b'', '双流与冻结预期不符')


def boundary_expected(cp):
    # 只有预先声明的四行不同，其余包括副作用顺序均逐字固定。
    required = (b'cold 2 []\ndeferred 11\noutside 13\nempty probe_pkg.path_empty False\n'
                b'changed missing\nfinder finder-called\nevents [\'deferred\', \'outside\', \'path_empty\']\n')
    require(cp == required, '不是已冻结CP边界观察')
    return (b'cold 2 []\ndeferred NI\noutside NI\nempty probe_pkg.path_empty False\n'
            b'changed NI\nfinder NI\nevents [\'path_empty\']\n')
