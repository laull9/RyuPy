"""独立Linux交付符号审计：读原始ELF工具输出核身份，搬迁程序用保留符号真实断点回溯。

本门在binaryornot部署门通过后运行，不重编译、不修改产品原件；调试器wall不是性能数据。
"""
import argparse
import copy
import json
from pathlib import Path
import re
import shutil
import sys
import zlib

HERE = Path(__file__).resolve().parent
sys.path[:0] = [str(HERE.parent), str(HERE.parent/'standalone_app/benchmark')]
from aot_layout import reported_aot
from manifest_files import check_artifacts, plain_stat, relative_path
from manifest_verify import MANIFEST_NAME, image_header, no_duplicate_keys
from measure import capture, inventory, require, save, sha

SYMBOL = 'RyuPy_Runtime_RyuPy_Runtime_PyBinaryDecode__Invoke'
LIMIT = 512 * 1024 * 1024
# UTF16 BOM命中真实decode；ASCII在上游明确跳过decode，不能验证该断点。
DEBUG_INPUT = 'utf16.txt'


def read_json(path):
    require(plain_stat(path).st_size <= 32 * 1024 * 1024, '审计JSON超限')
    return json.loads(path.read_text(encoding='utf-8'), object_pairs_hook=no_duplicate_keys)


def safe_child(root, name):
    """路径解释只允许普通本轮树；不能用resolve把符号链接悄悄接受下来。"""
    relative_path(name)
    current = root
    plain_stat(current, directory=True)
    parts = name.split('/')
    for part in parts[:-1]:
        current = current/part
        plain_stat(current, directory=True)
    result = current/parts[-1]
    plain_stat(result)
    require(result.resolve().is_relative_to(root.resolve()), '符号记录逃逸attempt')
    return result


def debuglink_from_readelf(text):
    """readelf -x的十六进制列按地址连续重建，不把ASCII列当作数据。"""
    data = bytearray()
    for line in text.splitlines():
        match = re.match(r'^\s*0x([0-9a-fA-F]+)\s+(.*)$', line)
        if not match:
            continue
        require(int(match[1], 16) == len(data), 'debuglink十六进制地址不连续')
        # GNU readelf数据区为4个8字符组及3个分隔空格；其后的ASCII列不可解析。
        columns = match[2][:35].split()
        require(1 <= len(columns) <= 4, 'debuglink十六进制列数错误')
        for column in columns:
            require(re.fullmatch(r'(?:[0-9a-fA-F]{2}){1,4}', column), 'debuglink非十六进制数据')
            data.extend(bytes.fromhex(column))
    require(data and b'\0' in data, '缺少debuglink内容/终止符')
    end = data.index(0)
    offset = (end + 1 + 3) & ~3
    require(len(data) == offset + 4, 'debuglink长度不符合basename+CRC')
    require(all(x == 0 for x in data[end:offset]), 'debuglink填充非零')
    return bytes(data[:end]), int.from_bytes(data[offset:], 'little')


def build_id_from_readelf(text):
    matches = re.findall(r'^\s*Build ID:\s*([0-9a-fA-F]+)\s*$', text, re.M)
    require(len(matches) == 1 and len(matches[0]) % 2 == 0, '必须恰一非空GNU build-id')
    return matches[0].lower()


def quote_gdb(path):
    value = str(path)
    require(not any(ord(c) < 32 or ord(c) == 127 for c in value), 'gdb路径含控制符')
    return '"' + value.replace('\\', '\\\\').replace('"', '\\"') + '"'


def verify(evidence):
    evidence = evidence.resolve()
    out = evidence/'symbol-delivery'
    out.mkdir(exist_ok=False)
    result = {'passed': False, 'scope': 'Linux x64交付体积与保留符号调试验证；非性能测量',
              'clean_machine_claim': False, 'debugger': {'passed': False}}
    frozen = None
    roots = {}
    files = {}
    clean = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C', 'LANG': 'C', 'TZ': 'UTC',
             'DEBUGINFOD_URLS': '', 'HOME': str(out)}

    def command(name, argv):
        record = capture(out/name, argv, out, clean, 120)
        require(record['exit'] == 0 and record['failure'] is None
                and record['raw_streams_complete'], name+'失败或原流不完整')
        return (out/(name+'.stdout')).read_text(encoding='utf-8', errors='strict')

    def snapshot():
        return {'trees': {name: inventory(path) for name, path in roots.items()},
                'files': {name: sha(path) for name, path in files.items()}}

    try:
        require(read_json(evidence/'result.json')['passed'], '部署门未通过')
        stderr = (evidence/'public-build.stderr').read_bytes()
        layout = reported_aot(stderr, evidence/'build', 'scanner', 'linux-x64')
        record_path = layout.attempt/'debug-symbols.json'
        reports = [line.removeprefix('AOT 调试符号清单：') for line in stderr.decode().splitlines()
                   if line.startswith('AOT 调试符号清单：')]
        require(len(reports) == 1 and Path(reports[0]) == record_path, 'CLI符号记录不是本次唯一记录')
        record = read_json(record_path)
        require(record['schema_version'] == 1 and record['platform'] == 'linux-x64'
                and record['status'] == 'separated', '产品没有本次已分离符号记录')
        source = layout.sdk_root/'publish'
        original = safe_child(layout.attempt, record['native_relative_path'])
        debug = safe_child(layout.attempt, record['symbols_relative_path'])
        require(original.parent == source and debug.parent == source, '记录不在本次SDK publish')
        require(debug.name == original.name+'.dbg', '非精确本次host.dbg')
        relocated = evidence/'relocated-dist'
        moved = relocated/'scanner'
        for path in [original, debug, layout.native, moved]:
            require(plain_stat(path).st_size <= LIMIT, '二进制审计超过512MiB')
            image_header(path, 'linux-x64')
        tools = {name: Path(shutil.which(name) or '') for name in ['readelf', 'nm', 'gdb']}
        for name, path in tools.items():
            require(path.is_absolute() and path.is_file(), '缺少独立工具：'+name)
        roots = {'source': source, 'dist': layout.dist, 'relocated': relocated}
        files = {'symbol_record': record_path, **tools}
        frozen = snapshot()
        save(out/'before.json', frozen)
        require(sha(original) == sha(layout.native) == sha(moved) == record['native_sha256'], 'native字节改变')
        require(sha(debug) == record['symbols_sha256'], '保留符号hash错误')
        source_manifest = read_json(source/MANIFEST_NAME)
        dist_manifest = read_json(layout.manifest)
        require(source_manifest['schema_version'] == dist_manifest['schema_version'] == 4, '必须真实schema4')
        check_artifacts(source, source_manifest['artifacts'], MANIFEST_NAME)
        check_artifacts(layout.dist, dist_manifest['artifacts'], MANIFEST_NAME)
        check_artifacts(relocated, read_json(relocated/MANIFEST_NAME)['artifacts'], MANIFEST_NAME)
        require(read_json(relocated/MANIFEST_NAME) == dist_manifest, '搬迁清单变化')
        expected = copy.deepcopy(source_manifest)
        symbols = [row for row in expected['artifacts'] if row['relative_path'] == debug.name]
        require(len(symbols) == 1 and symbols[0]['role'] == 'unclassified_sidecar'
                and symbols[0]['sha256'] == sha(debug), '原清单符号归属错误')
        expected['artifacts'] = [row for row in expected['artifacts'] if row['relative_path'] != debug.name]
        natives = [row for row in expected['artifacts'] if row['role'] == 'native_executable']
        require(len(natives) == 1 and natives[0]['relative_path'] == original.name, '原清单native归属错误')
        natives[0]['relative_path'] = layout.native.name
        expected['artifacts'].sort(key=lambda row: row['relative_path'])
        require(expected == dist_manifest, '除一个已确认符号与入口改名外清单发生改变')
        for root in [layout.dist, relocated]:
            require(not any(name.endswith(('.dbg', '.pdb')) or '.dSYM/' in name
                            for name in inventory(root)['files']), '本应用dist仍带符号')
        receipt = read_json(evidence/'receipt/receipt.json')
        require(receipt['structure_validated'] and receipt['manifest_sha256'] == sha(layout.manifest)
                and receipt['native_sha256'] == sha(layout.native), '已通过schema4 receipt非当前产物')
        link = command('readelf-debuglink', [tools['readelf'], '-x', '.gnu_debuglink', original])
        basename, crc = debuglink_from_readelf(link)
        require(basename == debug.name.encode(), '独立debuglink basename错误')
        require(crc == zlib.crc32(debug.read_bytes()) == record['gnu_debuglink_crc32'], '独立CRC错误')
        native_id = build_id_from_readelf(command('readelf-native-notes', [tools['readelf'], '-n', original]))
        debug_id = build_id_from_readelf(command('readelf-symbol-notes', [tools['readelf'], '-n', debug]))
        require(native_id == debug_id == record['build_id'], '独立build-id不相等')
        symbols_text = command('nm-symbols', [tools['nm'], '--defined-only', debug])
        require(re.search(r'\b'+re.escape(SYMBOL)+r'$', symbols_text, re.M), '缺少真实断点符号')
        command('gdb-version', [tools['gdb'], '--version'])
        # -iex在加载ELF前禁止自动加载和联网符号发现；显式symbol-file使用保留的精确原件。
        argv = [tools['gdb'], '-nx', '-nh', '--batch',
                '-iex', 'set auto-load off', '-iex', 'set debuginfod enabled off',
                '-ex', 'set pagination off', '-ex', 'set confirm off',
                '-ex', 'set breakpoint pending off', '-ex', 'set print demangle off',
                '-ex', 'file '+quote_gdb(moved), '-ex', 'symbol-file '+quote_gdb(debug),
                '-ex', 'break '+SYMBOL, '-ex', 'run '+quote_gdb(evidence/'user-inputs'/DEBUG_INPUT),
                '-ex', 'bt', '-ex', 'disable breakpoints', '-ex', 'continue']
        transcript = command('gdb-relocated', argv)
        require(re.search(r'^Breakpoint \d+,.*'+re.escape(SYMBOL), transcript, re.M), '断点未实际命中已知函数')
        require(re.search(r'^#0\s+.*'+re.escape(SYMBOL), transcript, re.M), '回溯顶帧非已知runtime函数')
        require('exited normally' in transcript and re.search(r'^False\s*$', transcript, re.M), '被调试程序未正常完成')
        result['debugger'] = {'passed': True, 'symbol': SYMBOL, 'native': str(moved),
                              'symbols': str(debug), 'scope': '显式保留符号下实际断点和回溯；非自动发现保证'}
        publish_bytes = frozen['trees']['source']['logical_bytes']
        dist_bytes = frozen['trees']['dist']['logical_bytes']
        result.update({'publish_logical_bytes': publish_bytes, 'dist_logical_bytes': dist_bytes,
                       'deploy_logical_bytes_reduction': publish_bytes-dist_bytes,
                       'retained_symbols_bytes': debug.stat().st_size,
                       'native_sha256': sha(original), 'symbols_sha256': sha(debug),
                       'build_id': native_id, 'debuglink_crc32': crc,
                       'cost_scope': '符号仍占.build磁盘；部署减少包括清单大小变化，不是总磁盘节省',
                       'unknown_sidecars_and_resources_preserved': True})
        require(snapshot() == frozen, '独立审计期间产物/工具发生变化')
        result['passed'] = True
    except BaseException as error:
        result['error'] = str(error)
        raise
    finally:
        try:
            if frozen is not None:
                after = snapshot()
                save(out/'after.json', after)
                if after != frozen:
                    result['passed'] = False
                    result['freeze_error'] = '原件或工具前后哈希不一致'
                    raise ValueError(result['freeze_error'])
        except BaseException as error:
            result['passed'] = False
            result['freeze_error'] = str(error)
            raise
        finally:
            save(out/'result.json', result)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', required=True, type=Path)
    verify(parser.parse_args().evidence)


if __name__ == '__main__':
    main()
