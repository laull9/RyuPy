"""第三方依赖只在深层本地模块导入，验收传递扫描。"""
import tomli


def calculate(configuration):
    order = tomli.loads(configuration)["order"]
    return order["units"] * order["price"]
