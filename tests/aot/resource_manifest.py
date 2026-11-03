"""schema2显式资源集合校验；不推测运行时全部资源闭包。"""
from pathlib import PurePosixPath
import re
from manifest_files import ManifestError, relative_path

PREFIX = '_internal/resources/v1/'


def require(value, message):
    if not value:
        raise ManifestError(message)


def portable(value):
    relative_path(value)
    require(value.isascii() and len(value) <= 1024, '首切资源相对名仅支持ASCII且总长不超过1024；宿主目录不受此限制')
    for part in value.split('/'):
        require(len(part) <= 255 and not part.endswith((' ', '.')), '资源目标分量过长或尾空格/点')
        stem = part.split('.')[0].rstrip(' ').upper()
        require(not re.fullmatch(r'CON|PRN|AUX|NUL|CONIN\$|CONOUT\$|CLOCK\$|COM[1-9]|LPT[1-9]', stem), '资源目标设备名')
        require(not any(c in '<>"|?*' for c in part), '资源目标非portable字符')
    return value


def path_key(path):
    return path.translate(str.maketrans('ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'))


def native_basename(value):
    # 唯一顶层native例外，不给资源路径Unicode权限；与Rust validate_name保持一致。
    relative_path(value)
    require('/' not in value and not value.endswith((' ', '.')), 'native必须是安全顶层分量')
    require(not any(c in '<>"|?*' for c in value), 'native非portable字符')
    stem = path_key(value.split('.')[0].rstrip(' '))
    require(not re.fullmatch(r'con|prn|aux|nul|conin\$|conout\$|clock\$|com[1-9¹²³]|lpt[1-9¹²³]', stem), 'native设备名')
    require(path_key(value) not in ('_internal', 'ryupy-artifact.json', '.ryupy-artifact.json.tmp'), 'native保留名')
    return value


def check_targets(artifacts, anchors):
    # 文件和隐式父目录同一表；保留原写法，禁止ASCII大小写别名。
    table = {}
    def insert(path, kind, native=False):
        parts = (native_basename(path) if native else portable(path)).split('/')
        for count in range(1, len(parts)+1):
            original = '/'.join(parts[:count])
            current_kind = kind if count == len(parts) else 'directory'
            key = path_key(original)
            previous = table.get(key)
            if previous is not None:
                require(previous == (original, current_kind), '资源目标ASCII大小写或文件目录冲突')
                require(current_kind in ('directory', 'anchor'), '重复发布目标')
            else:
                table[key] = (original, current_kind)
    for path in anchors:
        insert(path, 'anchor')
    require(sum(entry.get('role') == 'native_executable' for entry in artifacts) <= 1, '重复native角色')
    for entry in artifacts:
        insert(entry['relative_path'], 'file', entry.get('role') == 'native_executable')
    insert('ryupy-artifact.json', 'file')
    insert('.ryupy-artifact.json.tmp', 'file')


def validate_resources(data):
    from manifest_verify import shape, integer, digest_text
    sources = {x['module_id']: x for x in data['sources']}
    locations = data['module_locations']
    resources = data['resources']
    require(isinstance(locations, list) and len(locations) <= 256, '资源模块集合/预算非法')
    require(isinstance(resources, list) and len(resources) <= 1024, '资源集合/预算非法')
    selected = {}
    for location in locations:
        shape(location, 'module_id module_name kind relative_file')
        identity = location['module_id']
        integer(identity)
        require(identity in sources and identity not in selected, '资源模块身份未知/重复')
        require(location['module_name'] == sources[identity]['module_name'], '资源模块名称不匹配')
        require(location['kind'] == 'executable_relative', '非AOT资源定位')
        relative = portable(location['relative_file'])
        require(relative == PREFIX + sources[identity]['logical_source'], '虚拟锚点与实际模块来源不一致')
        selected[identity] = relative
    declared = {}
    for entry in data['artifacts']:
        if path_key(entry['relative_path']).startswith(path_key(PREFIX)):
            require(entry['role'] == 'declared_resource', '保留资源树含未声明项')
        if entry['role'] == 'declared_resource':
            require(entry['relative_path'] not in declared, '重复资源artifact')
            declared[entry['relative_path']] = entry
    seen, total = set(), 0
    for resource in resources:
        shape(resource, 'relative_path size sha256 consumer_module_ids')
        path = portable(resource['relative_path'])
        integer(resource['size'])
        digest_text(resource['sha256'])
        require(path.startswith(PREFIX) and path not in seen, '资源目标未知/重复')
        seen.add(path)
        require(PurePosixPath(path).suffix.lower() not in ('.py', '.pyc', '.pyo'), '不部署Python源码/缓存')
        require(resource['size'] <= 16*1024*1024, '单资源超预算')
        total += resource['size']
        consumers = resource['consumer_module_ids']
        require(isinstance(consumers, list) and bool(consumers), '资源没有consumer')
        require(all(type(x) is int and x in selected for x in consumers), 'consumer不是实际选中模块')
        require(len(set(consumers)) == len(consumers), '重复consumer')
        for identity in consumers:
            directory = str(PurePosixPath(selected[identity]).parent) + '/'
            require(path.startswith(directory), '资源不在consumer锚点目录下')
        artifact = declared.get(path)
        require(artifact is not None and artifact['size'] == resource['size']
                and artifact['sha256'] == resource['sha256'], '资源与artifact不一一同字节')
    require(total <= 64*1024*1024, '资源总量超预算')
    require(seen == set(declared), '资源与artifact集合不一致')
    require(data['scope']['resources_included'] is bool(resources), '资源存在声明不符')
    require(data['scope']['declared_resources_complete'] is True, '未完整处理声明集合')
    check_targets(data['artifacts'], selected.values())
