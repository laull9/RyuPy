"""CPython standalone fixture 专用手写第三方包。"""
from importlib.resources import files


def get_resource_content():
    content = files(__package__).joinpath("资源.txt").read_text(encoding="utf-8").strip()
    return f"包载荷:[{content}]"
