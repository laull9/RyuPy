"""两套独立名字/目录/ABI版本的原生组件输入；只在仓外编译，不执行消费者或修改系统。"""
import json
from pathlib import Path
import subprocess
import struct
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT))
from tests.general_projects.shared_ownership.fixtures import wheel

CASES = [
    dict(name='ridge_engine', module='_ridge_stream', library='granite_math', major=3, offset=37,
         symbol='ridge_offset', destination='native_layers/alpha/libgranite_math.3.dylib', custom=False),
    dict(name='vale_runtime', module='_vale_catalog', library='marsh_counter', major=7, offset=83,
         symbol='vale_adjust', destination='vendor_space/deep/beta/libmarsh_counter.7.dylib', custom=True),
]


def prepare(case, root, python, env):
    prefix = root / (case['name'] + '-prefix')
    library_dir = prefix / 'lib'
    library_dir.mkdir(parents=True)
    (prefix / 'LICENSE').write_text(case['name'] + ' 手写测试输入，MIT许可。\n')
    query = subprocess.run([python, '-I', '-c',
        'import json,sysconfig; print(json.dumps({"suffix":sysconfig.get_config_var("EXT_SUFFIX"),"include":sysconfig.get_config_var("INCLUDEPY"),"platform":sysconfig.get_platform()}))'],
        capture_output=True, check=True, env=env, timeout=30)
    config = json.loads(query.stdout)
    leaf = 'lib' + case['library'] + '.' + str(case['major']) + '.dylib'
    identity = '/legacy/source/' + case['name'] + '/' + leaf
    requested = '/different/developer/prefix/' + leaf if case['custom'] else identity
    current, compatibility = f'{case["major"]}.2.0', f'{case["major"]}.0.0'
    source = root / (case['name'] + '.c')
    source.write_text('/* 手写组件：不同符号和常量使两组ABI消费者相互独立。 */\n'
                      f'long {case["symbol"]}(long value) {{ return value * 3 + {case["offset"]}; }}\n')
    library = library_dir / leaf
    subprocess.run(['cc', '-arch', 'arm64', '-dynamiclib', '-mmacosx-version-min=11.0', '-Wl,-headerpad_max_install_names',
                    source, '-install_name', identity, '-compatibility_version', compatibility,
                    '-current_version', current, '-o', library], env=env, capture_output=True, timeout=60, check=True)
    (library_dir / ('lib' + case['library'] + '.dylib')).symlink_to(leaf)
    extension_source = root / (case['module'] + '.c')
    extension_source.write_text(Path(__file__).with_name('extension.c.in').read_text()
                               .replace('@SYMBOL@', case['symbol']).replace('@MODULE@', case['module']))
    extension = root / (case['module'] + config['suffix'])
    subprocess.run(['cc', '-arch', 'arm64', '-bundle', '-undefined', 'dynamic_lookup',
                    '-mmacosx-version-min=11.0', '-Wl,-headerpad_max_install_names', '-I', config['include'], extension_source,
                    '-L', library_dir, '-l' + case['library'], '-o', extension], env=env,
                   capture_output=True, timeout=60, check=True)
    if requested != identity:
        subprocess.run(['install_name_tool', '-change', identity, requested, extension], check=True,
                       capture_output=True, env=env, timeout=30)
    # 显式未选组件携带真实缺失依赖；默认完整闭包必须失败，选择以后仍审计这条加载边。
    auxiliary_leaf = 'libextra_' + case['library'] + '.dylib'
    auxiliary = library_dir / auxiliary_leaf
    auxiliary_identity = '/legacy/optional/' + case['name'] + '/' + auxiliary_leaf
    missing = '/unavailable/dependency/libabsent_' + case['library'] + '.dylib'
    optional_source = root / (case['name'] + '-optional.c')
    optional_source.write_text(f'extern long {case["symbol"]}(long);\nlong optional_entry(long v) {{ return {case["symbol"]}(v); }}\n')
    subprocess.run(['cc', '-arch', 'arm64', '-dynamiclib', '-mmacosx-version-min=11.0', '-Wl,-headerpad_max_install_names', optional_source,
                    '-L', library_dir, '-l' + case['library'], '-install_name', auxiliary_identity,
                    '-compatibility_version', compatibility, '-current_version', current, '-o', auxiliary],
                   capture_output=True, env=env, timeout=60, check=True)
    subprocess.run(['install_name_tool', '-change', identity, missing, auxiliary],
                   capture_output=True, env=env, timeout=30, check=True)
    auxiliary_target = str(Path(case['destination']).parent / auxiliary_leaf)
    tag = 'cp312-cp312-' + config['platform'].replace('-', '_').replace('.', '_')
    # 同ID、同叶名的另一部署位置制造真实全局歧义；LC_RPATH必须在全局候选前精确命中。
    runtime_load = '@rpath/' + leaf
    subprocess.run(['install_name_tool', '-change', requested, runtime_load, '-add_rpath',
                    '@loader_path/' + str(Path(case['destination']).parent), extension],
                   capture_output=True, env=env, timeout=30, check=True)
    case['extension_load'] = runtime_load
    decoy = bytearray(library.read_bytes())
    cursor = 32
    for _ in range(struct.unpack_from('<I', decoy, 16)[0]):
        command, size = struct.unpack_from('<II', decoy, cursor)
        if command == 0xd: struct.pack_into('<I', decoy, cursor + 16, 0)
        cursor += size
    case['decoy_target'] = 'independent_decoy/' + case['name'] + '/' + leaf
    package = wheel(root, case['name'], {extension.name: extension.read_bytes(),
                    case['decoy_target']: bytes(decoy)}, tag)
    declaration = {'schema': 1, 'components': [{
        'name': case['name'], 'version': '1.0', 'source_url': 'handwritten-fixture://' + case['name'],
        'root': str(prefix), 'libraries': [{'path': 'lib/' + leaf, 'destination': case['destination'],
            'install_name': identity, 'compatibility_version': compatibility, 'current_version': current,
            'architecture': 'osx-arm64', 'load_names': [requested]}],
        'licenses': [{'path': 'LICENSE', 'destination': 'native_licenses/' + case['name'] + '/LICENSE'}]}]}
    declaration['components'][0]['libraries'].append({
        'path': 'lib/' + auxiliary_leaf, 'destination': auxiliary_target,
        'install_name': auxiliary_identity, 'compatibility_version': compatibility,
        'current_version': current, 'architecture': 'osx-arm64', 'load_names': []})
    declaration['payload_selection'] = [{
        'name': case['name'] + '-application-capability', 'scope': str(Path(case['destination']).parent),
        'members': [case['destination'], auxiliary_target], 'selected': [case['destination']],
        'reason': '此手写产品调用主库；附属独立功能未部署，依赖仍完整审计。'}]
    return prefix, extension, package, declaration
