# 配置解析：tomli 读取数组表与错误路径。
import tomli

data = tomli.loads('name = "pkg"\n[[items]]\nid = 1\n[[items]]\nid = 2\n')
print(data["name"], [item["id"] for item in data["items"]])
try:
    tomli.loads("bad =")
except tomli.TOMLDecodeError as error:
    print("decode error", type(error).__name__)
