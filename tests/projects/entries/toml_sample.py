# 配置解析：TOML 往返。
import toml

document = toml.loads('title = "demo"\n[server]\nport = 8080\n')
print(document["title"], document["server"]["port"])
print(toml.dumps({"a": 1, "b": {"c": [1, 2]}}).strip())
