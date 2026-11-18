"""两个独立CP扩展模型的真实universal输入；fat32/fat64、签名授权与资源数据均不按库名特化。"""
import importlib.util
from pathlib import Path
import plistlib
import platform
import struct
import shutil
import zipfile
import subprocess

SOURCE = Path(__file__).resolve().parent
BASE = SOURCE.parent/'interface_exports'
spec = importlib.util.spec_from_file_location('interface_export_fixture_base', BASE/'fixtures.py')
common = importlib.util.module_from_spec(spec)
spec.loader.exec_module(common)
CASES = common.CASES
HERE = BASE
RESOURCE = 'preserved-fat.bin'


def make_fat(arm, x64, target, wide):
    """包裹实际clang输出而非伪造内层；fat64使用同一公开Mach-O表格式。"""
    first = 16384
    second = (first + len(x64) + 16383) & -16384
    magic = 0xcafebabf if wide else 0xcafebabe
    header = struct.pack('>II', magic, 2)
    if wide:
        header += struct.pack('>IIQQII', 0x1000007, 3, first, len(x64), 14, 0)
        header += struct.pack('>IIQQII', 0x100000c, 0, second, len(arm), 14, 0)
    else:
        header += struct.pack('>IIIII', 0x1000007, 3, first, len(x64), 14)
        header += struct.pack('>IIIII', 0x100000c, 0, second, len(arm), 14)
    with target.open('wb') as output:
        output.write(header); output.seek(first); output.write(x64); output.seek(second); output.write(arm)
    target.chmod(0o755)


def configuration(python, root, env, run):
    import json
    result = run([python, '-I', '-B', '-c', 'import sysconfig,sys,json;print(json.dumps(dict(suffix=sysconfig.get_config_var("EXT_SUFFIX"),include=sysconfig.get_config_var("INCLUDEPY"),tag="cp%d%d"%sys.version_info[:2])))'], root, env, 'fat-configuration')
    return json.loads(result.stdout)


def entitlements(run, path, root, env, label):
    result = run(['codesign', '-d', '--entitlements', '-', '--xml', path], root, env, label)
    # codesign状态写stderr，XML/DER解码后的plist在stdout。
    return plistlib.loads(result.stdout) if result.stdout.strip() else {}


def prepare(root, python, env, run):
    config = configuration(python, root, env, run)
    packages = []
    for case in CASES:
        wide = case['id'] == 'multiphase'
        extension = root/(case['extension']+config['suffix'])
        ordinary = root/('ordinary_image-'+case['id']+config['suffix'])
        for source, output in ((HERE/case['source'], extension), (HERE/'ordinary.c', ordinary)):
            thin = []
            expected = {'org.ryupy.fixture':case['id'], 'com.apple.security.cs.disable-library-validation':True}
            plist = root/(case['id']+'-entitlements.plist'); plist.write_bytes(plistlib.dumps(expected))
            native_source=root/(case['id']+'-'+source.name);shutil.copy2(source,native_source)
            bundle=wide and source.name==case['source']
            for architecture in ('arm64', 'x86_64'):
                target = root/(case['id']+'-'+output.name+'-'+architecture)
                command=['clang','-arch',architecture,'-bundle' if bundle else '-dynamiclib','-undefined','dynamic_lookup','-I',config['include']]
                if not bundle:command.append('-Wl,-install_name,@rpath/'+output.name)
                run([*command,native_source,'-o',target],root,env,case['id']+'-'+architecture+'-compile')
                # codesign将fat64识别为generic；逐真实slice先签，避免给外层数据blob签名冒充内层授权。
                run(['codesign', '--force', '--sign', '-', '--identifier', 'org.ryupy.fixture.'+case['id'],
                     '--options', 'runtime', '--entitlements', plist,
                     *(['--force-library-entitlements'] if int(platform.mac_ver()[0].split('.')[0])>=15 else []), target], root, env, case['id']+'-slice-sign')
                run(['codesign','--verify','--strict',target],root,env,case['id']+'-slice-signature')
                assert entitlements(run,target,root,env,case['id']+'-slice-entitlements')==expected
                metadata=signature_metadata(target)
                assert metadata['flags']&0x10000 and metadata['identifier']=='org.ryupy.fixture.'+case['id']
                thin.append(target.read_bytes())
            make_fat(thin[0], thin[1], output, wide)
            metadata=signature_metadata(output)
            assert metadata['flags']&0x10000 and metadata['identifier']=='org.ryupy.fixture.'+case['id']
        helper=root/(case['id']+'-native-helper')
        helper_source=root/(case['id']+'-helper.c');shutil.copy2(SOURCE/'native_helper.c',helper_source)
        helper_slices=[]
        for architecture in ('arm64','x86_64'):
            target=root/(case['id']+'-helper-'+architecture)
            run(['clang','-arch',architecture,helper_source,'-o',target],root,env,case['id']+'-helper-compile')
            helper_slices.append(target.read_bytes())
        # 无扩展名沿原collector的fat32拥有入口；不借收敛步骤扩大fat64资源准入。
        make_fat(helper_slices[0],helper_slices[1],helper,False)
        run(['codesign','--force','--sign','-','--identifier','org.ryupy.fixture.helper.'+case['id'],
             '--options','runtime',helper],root,env,case['id']+'-helper-sign')
        directory = case['directory']
        files = {case['package']+'/__init__.py':'"""实际双架构CP扩展，业务回调由CIL执行。"""\n',
                 directory+'/'+extension.name:extension.read_bytes(), directory+'/native_helper':helper.read_bytes(), directory+'/'+case['extension']+'.pyi':(HERE/case['stub']).read_bytes(),
                 directory+'/ordinary_image'+config['suffix']:ordinary.read_bytes(),
                 directory+'/ordinary_image.pyi':'def ordinary_scale(value: int, /) -> int: ...\n',
                 directory+'/stub_only.pyi':'def absent(value: bytes, /) -> str: ...\nraise RuntimeError("声明不执行")\n'}
        if directory != case['package']: files[directory+'/__init__.py']='"""嵌套扩展布局。"""\n'
        tag = config['tag']+'-'+config['tag']+'-macosx_11_0_universal2'
        package=common.wheel(root,case['distribution'],files,tag)
        # PEP427的Unix可执行权限随wheel安装保留；原生helper必须由实际exec消费。
        with zipfile.ZipFile(package) as archive:
            members=[(info,archive.read(info.filename)) for info in archive.infolist()]
        with zipfile.ZipFile(package,'w') as archive:
            for info,data in members:
                if info.filename.endswith('/native_helper'):info.external_attr=(0o100755<<16)
                archive.writestr(info,data)
        packages.append((case,package))
    # 真实fat映像同时作为显式原始资源；它位于原重定位集合之外，必须保持完整数据。
    raw = root/RESOURCE
    make_fat(thin[0], thin[1], raw, True)
    return packages


def signature_metadata(path):
    """只读CodeDirectory的identifier/flags；不读取或计算页摘要。"""
    with path.open('rb') as file:
        header=file.read(8);base=0
        if header[:4] in (bytes.fromhex('cafebabe'),bytes.fromhex('cafebabf')):
            wide=header[:4]==bytes.fromhex('cafebabf')
            entries=[]
            for _ in range(struct.unpack('>I',header[4:])[0]):
                row=file.read(32 if wide else 20)
                values=struct.unpack('>IIQQII' if wide else '>IIIII',row)
                entries.append(values)
            base=next(row[2] for row in entries if row[0]==0x100000c and row[1]==0)
        file.seek(base);header=file.read(32);assert header[:4]==bytes.fromhex('cffaedfe')
        ncmds,size=struct.unpack_from('<II',header,16);commands=file.read(size);at=0;signature=None
        for _ in range(ncmds):
            command,length=struct.unpack_from('<II',commands,at)
            if command==0x1d:signature=base+struct.unpack_from('<I',commands,at+8)[0]
            at+=length
        assert signature is not None
        file.seek(signature);magic,length,count=struct.unpack('>III',file.read(12));assert magic==0xfade0cc0
        slots=[struct.unpack('>II',file.read(8)) for _ in range(count)]
        directory=signature+next(offset for kind,offset in slots if kind==0)
        file.seek(directory);head=file.read(24);assert struct.unpack_from('>I',head)[0]==0xfade0c02
        flags=struct.unpack_from('>I',head,12)[0];identifier=struct.unpack_from('>I',head,20)[0]
        file.seek(directory+identifier);name=bytearray()
        for _ in range(256):
            value=file.read(1)
            if value==b'\0':break
            assert value;name.extend(value)
        return {'flags':flags,'identifier':name.decode()}
