"""本验收的身份错误与完整输入清单；仅支持已登记metadata根的有限rename隔离。"""
from contextlib import contextmanager
import os
from pathlib import Path
import stat
import typing_noreturn as old


class IdentityError(ValueError):
    """身份失效必须粘性停止后续产品，不能作为普通语义失败吞掉。"""


def require(value, message):
    if not value: raise IdentityError(message)


def mapped(path, mapping):
    path = Path(path)
    for original, destination in mapping.items():
        original = Path(original)
        if path == original or original in path.parents:
            return Path(destination) / path.relative_to(original)
    return path


def validate_mapping(mapping):
    for original, destination in mapping.items():
        original, destination = Path(original), Path(destination)
        require(destination == original.with_name(original.name + '.isolated'), '隔离映射目的地不符')
        require(not os.path.lexists(original), '隔离时原路径重新出现')
        require(destination.is_dir() and not destination.is_symlink(), '隔离目录消失/类型改变')


def inventory(root, mapping=None):
    """包含root、空目录、文件类型/bytes/hash；新成员、缺失或链接一律可见。"""
    mapping = mapping or {}
    validate_mapping(mapping)
    root = Path(root); rows = {}
    def visit(logical):
        physical = mapped(logical, mapping)
        info = physical.lstat()
        relative = '.' if logical == root else logical.relative_to(root).as_posix()
        if stat.S_ISDIR(info.st_mode):
            rows[relative] = {'type': 'directory'}
            children = {child.name: logical / child.name for child in physical.iterdir()}
            # 只有原parent还在原位时才折回已登记rename；不忽略任意未知目录。
            if physical == logical:
                for original, destination in mapping.items():
                    original, destination = Path(original), Path(destination)
                    if original.parent == logical:
                        require(destination.name in children and original.name not in children, '隔离目录成员不符')
                        del children[destination.name]
                        children[original.name] = original
            for child in sorted(children.values()): visit(child)
        elif stat.S_ISREG(info.st_mode):
            rows[relative] = {'type': 'file', 'bytes': info.st_size, 'sha256': old.sha(physical)}
        else:
            raise IdentityError('输入出现链接/特殊对象：' + str(logical))
    visit(root)
    require(rows['.']['type'] == 'directory', '清单根不是目录')
    return rows


@contextmanager
def isolated(runner, roots, label):
    """原authority不改，只在本作用域登记精确路径换算；finally复原并重新全核。"""
    roots = list(map(Path, roots)); changed = []
    require(not runner.isolation, '不接受嵌套或恢复遗留隔离')
    runner.barrier(label + '-before-isolation')
    try:
        def setup():
            require(roots and len(set(roots)) == len(roots), '隔离根空或重复')
            for root in roots:
                require(any(root == Path(base) or Path(base) in root.parents for base in runner.input_inventories),
                        '隔离根不在冻结输入域')
                require(not any(other != root and (root in other.parents or other in root.parents) for other in roots),
                        '隔离根重叠')
                destination = root.with_name(root.name + '.isolated')
                require(not os.path.lexists(destination), '隔离目的地已存在')
                root.rename(destination)
                changed.append((root, destination))
                runner.isolation[str(root)] = str(destination)
            old.save(runner.out / (label + '-isolation.json'), {'mapping': dict(runner.isolation), 'status': 'renamed'})
        runner.audit(setup)
        runner.barrier(label + '-after-isolation')
        yield
    finally:
        failures = []
        for original, destination in reversed(changed):
            try:
                require(not os.path.lexists(original) and os.path.lexists(destination), '隔离恢复路径冲突/缺失')
                destination.rename(original)
                del runner.isolation[str(original)]
            except Exception as error: failures.append(str(error))
        old.save(runner.out / (label + '-restoration.json'), {'mapping_remaining': dict(runner.isolation), 'failures': failures})
        if failures:
            runner.invalidate(IdentityError('; '.join(failures)))
            runner.persist()
        # 即使先前已经失败也记录全核结果，但绝不把身份恢复成通过。
        runner.barrier(label + '-restored', cleanup=True)
