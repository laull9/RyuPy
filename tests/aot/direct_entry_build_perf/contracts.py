"""完整构建A/B的冻结顺序、SDK计时分类与输入身份合同；不启动编译。"""
import hashlib
from pathlib import Path
import re
import statistics
import xml.etree.ElementTree as ET
from urllib.parse import unquote


def require(ok, message):
    if not ok:
        raise AssertionError(message)


APPS = ('pyfiglet', 'binaryornot')


def python_command(python, app, entry, argv):
    # 上游pyfiglet列字体会调用site安装的exit；不得用-S破坏真实CP入口再注入替身。
    require(app in APPS, '未知应用')
    return [python, '-B', *(['-S'] if app == 'binaryornot' else []), entry, *argv]


def schedule():
    rows = []
    for app in APPS:
        for arm in ('direct', 'thin'):
            rows.append({'app': app, 'arm': arm, 'warmup': True, 'block': None})
    # 每位置交错两个应用，下一块反转应用顺序；每应用仍严格ABBA/BAAB。
    for block in range(4):
        arms = ('direct', 'thin', 'thin', 'direct') if block % 2 == 0 else ('thin', 'direct', 'direct', 'thin')
        for arm in arms:
            for app in APPS if block % 2 == 0 else tuple(reversed(APPS)):
                rows.append({'app': app, 'arm': arm, 'warmup': False, 'block': block})
    return [{'id': f'{index:03}', **row} for index, row in enumerate(rows)]


def timings(text):
    # 保留SDK原文；target与task是不同层级，绝不能相加或缺值补零。
    section = None; rows = []
    for line in text.splitlines():
        if line.strip() == 'Target Performance Summary:': section = 'target'
        elif line.strip() == 'Task Performance Summary:': section = 'task'
        elif line.strip().endswith('Performance Summary:'): section = None
        match = re.fullmatch(r'\s*(\d+(?:\.\d+)?) ms\s+(\S+)\s+(\d+) calls\s*', line)
        if match and section:
            rows.append({'section': section, 'name': match[2], 'milliseconds': float(match[1]),
                         'calls': int(match[3]), 'raw': line})
    for section, name in [('target', 'IlcCompile'), ('target', 'LinkNative'), ('task', 'Csc')]:
        require(sum(row['section'] == section and row['name'] == name for row in rows) == 1,
                '缺少或重复SDK计时项：'+section+'/'+name)
    return rows


def verify_project(layout, arm, sha):
    text = (layout.host/'Host.csproj').read_text(encoding='utf-8')
    xml = ET.fromstring(text)
    for key in ('PublishAot', 'TreatWarningsAsErrors', 'IlcTreatWarningsAsErrors', 'ILLinkTreatWarningsAsErrors'):
        require(xml.findtext('./PropertyGroup/'+key) == 'true', '严格SDK属性改变：'+key)
    require(not any(x in text for x in ('NoWarn','TrimmerRoot','WarningsNotAsErrors')), '警告/保根合同改变')
    require(not xml.findall('.//OptimizationPreference'), '不是原默认优化配置')
    references = xml.findall('./ItemGroup/Reference')
    require([r.get('Include') for r in references] ==
            (['RyuPy.Runtime'] if arm == 'direct' else [layout.managed.stem, 'RyuPy.Runtime']), '引用改变')
    if arm == 'direct':
        for key, value in {'SkipCompilerExecution':'true','ProduceReferenceAssembly':'false',
                           'GenerateAssemblyInfo':'false','GenerateTargetFrameworkAttribute':'false','UseAppHost':'false'}.items():
            require(xml.findtext('./PropertyGroup/'+key) == value, '直接入口属性改变')
        require(not xml.findall('.//Compile') and not (layout.host/'Program.cs').exists(), '直接入口混入薄宿主')
        targets = [t for t in xml.findall('./Target') if t.get('BeforeTargets') == 'CoreCompile']
        require(len(targets) == 1 and len(list(targets[0])) == 1, 'Copy目标改变')
        copy = targets[0].find('Copy')
        require(copy is not None and copy.get('DestinationFiles') == '@(IntermediateAssembly)' and
                Path(unquote(copy.get('SourceFiles'))).resolve() == layout.managed.resolve(), 'Copy输入不同')
    else:
        require(xml.findtext('./PropertyGroup/SkipCompilerExecution') is None, '对照不应绕过Csc')
        require([n.get('Include') for n in xml.findall('.//Compile')] == ['Program.cs'], '对照编译项改变')
        require((layout.host/'Program.cs').read_bytes() == b'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n', '对照薄宿主改变')
    responses = list(layout.host.rglob('*.ilc.rsp')); require(len(responses) == 1, 'ILC响应不唯一')
    flags = responses[0].read_text(encoding='utf-8-sig').splitlines()
    inputs = [x.strip('"') for x in flags if x and not x.startswith('-')]
    require(len(inputs) == 1, 'ILC主输入不唯一')
    entry = Path(inputs[0]); entry = entry if entry.is_absolute() else layout.host/entry
    require(entry.resolve().is_relative_to(layout.host.resolve()), 'ILC输入逃出本轮根')
    require((sha(entry) == sha(layout.managed)) == (arm == 'direct'), 'ILC入口字节与目标模式不同')
    return {'project_sha256':sha(layout.host/'Host.csproj'), 'ilc_response':str(responses[0]),
            'ilc_main':str(entry),'ilc_main_sha256':sha(entry),'managed_sha256':sha(layout.managed)}


def managed_identity(data):
    """仅定位已知非确定COFF时间戳；不改原件，不屏蔽MVID/metadata/IL。"""
    require(len(data) >= 64 and data[:2] == b'MZ', '不是完整MZ头')
    pe = int.from_bytes(data[60:64], 'little')
    require(64 <= pe <= len(data)-24 and data[pe:pe+4] == b'PE\0\0', '无效PE头偏移/签名')
    optional_size = int.from_bytes(data[pe+20:pe+22], 'little')
    require(optional_size >= 2 and pe+24+optional_size <= len(data), '截短PE可选头')
    require(int.from_bytes(data[pe+24:pe+26], 'little') in (0x10b, 0x20b), '未知PE格式')
    offset = pe+8
    equivalent = data[:offset] + bytes(4) + data[offset+4:]
    return {'raw_sha256':hashlib.sha256(data).hexdigest(), 'size':len(data),
            'coff_timestamp_offset':offset, 'coff_timestamp':int.from_bytes(data[offset:offset+4], 'little'),
            'coff_timestamp_zeroed_sha256':hashlib.sha256(equivalent).hexdigest()}


def verify_managed_equivalence(data, baseline):
    actual, expected = managed_identity(data), managed_identity(baseline)
    offset = actual['coff_timestamp_offset']
    require(offset == expected['coff_timestamp_offset'] and len(data) == len(baseline), 'managed PE布局改变')
    require(data[:offset] == baseline[:offset] and data[offset+4:] == baseline[offset+4:],
            'managed除精确COFF时间戳外存在字节差异，不允许忽略MVID/metadata/IL')
    return actual


def summaries(rows):
    result = {}
    for app in APPS:
        measured = [row for row in rows if row['app'] == app and not row['warmup']]
        groups = {arm:[row['wall_ms'] for row in measured if row['arm'] == arm] for arm in ('direct','thin')}
        require(all(len(v) == 8 for v in groups.values()), '样本数量不同，不能输出完成比较')
        blocks = []
        for block in range(4):
            values = {arm:[row['wall_ms'] for row in measured if row['block'] == block and row['arm'] == arm] for arm in groups}
            require(all(len(v) == 2 for v in values.values()), '块内缺样本')
            med = {arm:statistics.median(v) for arm,v in values.items()}
            blocks.append({'block':block,'median_ms':med,'direct_minus_thin_ms':med['direct']-med['thin'],
                           'direct_over_thin':med['direct']/med['thin']})
        result[app] = {'arms':{arm:{'samples_ms':v,'median_ms':statistics.median(v),'min_ms':min(v),'max_ms':max(v)} for arm,v in groups.items()},
                       'paired_blocks':blocks,'inference':'仅描述分布与配对块差，不声明统计显著或跨机提升'}
    return result
