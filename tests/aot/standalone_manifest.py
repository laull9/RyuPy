"""schema4显式资源组合消费者；保原清单，不降schema，不推断任意包资源完整。"""
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent / 'default_source_aot'))
from schema4 import _validate_schema4, bind_candidate
from manifest_verify import require


def validate_standalone_schema4(data, rid, sdk, *, expected_automatic,
                                metadata_report, expected_resources):
    """调用者从锁定请求/源生成独立资源记录；空列表表示真实显式空声明。"""
    require(type(expected_resources) is list, '需要独立声明资源集合')
    _validate_schema4(data, rid, sdk, expected_automatic=expected_automatic,
                     metadata_report=metadata_report, declared_request=True, expected_policy='explicit-only')
    validate_default_reasons(data['modules'], expected_automatic)
    # 与artifact自洽不足以证明没漏请求资源：再比调用者独立输入，包含consumer身份。
    require(data['resources'] == expected_resources, '实际资源与独立锁定声明不一致')


def validate_default_reasons(modules, expected_automatic):
    # ExplicitOnly优先于AOT迁移选择；不得错用pilot的aot-not-migrated理由。
    require(expected_automatic == () and all(m['environment_reason'] == 'policy-disabled'
        for m in modules), '默认app必须保持全Legacy及原选择理由')
