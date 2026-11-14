"""组合本地计算与标签模块，产出确定性的业务收据。"""
import json
from .pricing import calculate
from .labels import heading


def render(customer, configuration):
    total = calculate(configuration)
    return json.dumps({"customer": customer, "heading": heading(), "total": total},
                      ensure_ascii=False, sort_keys=True)
