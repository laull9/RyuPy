"""构造独立Mach-O闭包；所有编译产物在调用者给定的仓外工作目录，源文件仅手写输入。"""
from pathlib import Path
import subprocess
import sys

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]


def prepare(root, run=None, *, fat=False):
    """返回真实加载器及两种独立布局、同LC_ID多版本和祖先shadow闭包。"""
    root = Path(root).resolve()
    if root == REPO or REPO in root.parents:
        raise ValueError('Mach-O测试产物必须写在仓库外')
    if sys.platform != 'darwin':
        raise RuntimeError('此输入明确要求macOS工具链')
    root.mkdir(parents=True, exist_ok=True)
    if run is None:
        def run(command):
            return subprocess.run([str(x) for x in command], check=True, capture_output=True, text=True)

    def library(directory, name, identity, value):
        directory.mkdir(parents=True, exist_ok=True)
        output = directory / name
        architectures = ['-arch', 'arm64', '-arch', 'x86_64'] if fat else ['-arch', 'arm64']
        run(['clang', *architectures, '-dynamiclib', HERE/'value.c',
             '-DFIXTURE_VALUE='+str(value), '-Wl,-install_name,'+identity,
             '-Wl,-current_version,'+str(value)+'.0.0', '-o', output])
        return output

    def consumer(directory, dependency, relative_rpath, identity):
        directory.mkdir(parents=True, exist_ok=True)
        output = directory/'consumer.dylib'
        run(['clang', '-arch', 'arm64', '-dynamiclib', HERE/'consumer.c', dependency,
             '-Wl,-install_name,'+identity, '-Wl,-rpath,'+relative_rpath, '-o', output])
        return output

    rows = []
    for folder, subfolder, name, value in [
        ('copper', 'libraries', 'liborchid.1.dylib', 11),
        ('quartz/nested', 'vendor/deep', 'libslate.7.dylib', 29),
    ]:
        directory = root/'independent'/folder
        dependency = library(directory/subfolder, name, '@rpath/'+name, value)
        entry = consumer(directory, dependency, '@loader_path/'+subfolder,
                         '@rpath/'+folder.replace('/', '-')+'-consumer.dylib')
        rows.append({'case': 'independent', 'entry': entry, 'dependency': dependency, 'expected': value})
    for folder, value in [('first', 41), ('second/deep', 83)]:
        directory = root/'duplicate'/folder
        dependency = library(directory/'native', 'libshared.1.dylib', '@rpath/libshared.1.dylib', value)
        entry = consumer(directory, dependency, '@loader_path/native',
                         '@rpath/'+folder.replace('/', '-')+'-consumer.dylib')
        rows.append({'case': 'duplicate', 'entry': entry, 'dependency': dependency, 'expected': value})
    # 两个不同身份拥有同一leaf；独立祖先若提供这个runpath，计划器必须选择严格改写。
    directory = root/'ancestor-shadow'
    dependency = library(directory/'own', 'libshadow.dylib', '@rpath/own/libshadow.dylib', 101)
    shadow = library(directory/'ancestor/own', 'libshadow.dylib', '@rpath/other/libshadow.dylib', 199)
    entry = consumer(directory, dependency, '@loader_path', '@rpath/consumer.dylib')
    ancestor = directory/'ancestor.dylib'
    run(['clang', '-arch', 'arm64', '-dynamiclib', HERE/'ancestor.c', entry,
         '-Wl,-install_name,@rpath/ancestor.dylib', '-Wl,-rpath,@loader_path/ancestor',
         '-Wl,-rpath,@loader_path', '-o', ancestor])
    rows.append({'case': 'ancestor-shadow', 'entry': entry, 'dependency': dependency,
                 'shadow': shadow, 'ancestor': ancestor, 'expected': 101})
    loader = root/'loader'
    run(['clang', '-arch', 'arm64', HERE/'loader.c', '-o', loader])
    return loader, rows
