"""采样专用副本显式恢复已保留符号，不把调试旁件放回产品dist。"""
import json
from pathlib import Path
import shutil
from measure import require,sha


def prepare_profile_symbols(layout, profile_dist):
    record_path=layout.attempt/'debug-symbols.json'
    require(record_path.is_file() and not record_path.is_symlink(),'缺本次符号记录')
    record=json.loads(record_path.read_text())
    require(record['schema_version']==1 and record['platform']=='linux-x64','符号记录版本/平台不符')
    require(record['status']=='separated' and record['reason']=='verified_debuglink_crc_and_build_id','未验证关联的符号不能用于热点归因')
    def retained(name):
        relative=Path(record[name]);require(not relative.is_absolute() and '..' not in relative.parts,'符号保留路径逃逸')
        path=layout.attempt/relative
        require(path.is_file() and not path.is_symlink(),'符号记录文件非法')
        require(path.parent.resolve()==(layout.sdk_root/'publish').resolve(),'符号不在本次publish')
        return path
    native=retained('native_relative_path');symbols=retained('symbols_relative_path')
    require(symbols.name==native.name+'.dbg','不是精确host旁件')
    require(sha(native)==record['native_sha256']==sha(layout.native),'native与符号记录不匹配')
    require(sha(symbols)==record['symbols_sha256'],'符号摘要不符')
    target=profile_dist/symbols.name
    if record['status']=='separated':
        require(not target.exists(),'采样副本意外已有符号')
        shutil.copy2(symbols,target)
    require(target.is_file() and not target.is_symlink() and sha(target)==sha(symbols),'采样符号复制不符')
    return target,{'record':str(record_path),'retained_symbols':str(symbols),'profile_symbols':str(target),
                   'scope':'只给采样副本加符号；不改变产品dist或native字节','product_status':record['status']}
