# Python 3.9+ 自动版本验收

产品入口为 `ryupy --cpython-provider auto build app.py`。产品自行探测项目 `.venv`、激活环境和PATH，
不要求用户指定版本。测试驱动的 `--python` 和 `--python-version` 用来准备、核对不同用户环境，
不会作为版本参数传给产品。

`version_run.py` 对每个实际解释器创建真实项目venv，严格安装匹配的NumPy wheel与packaging，
同次执行CPython oracle，再实际执行生成的JIT和NativeAOT产物。stdout、stderr、退出码及默认
中文文件IO副作用逐字节比较。业务覆盖真实NumPy数组/视图别名、计算、ValueError、SQLite与SSL。
构建报告的冻结版本必须等于用户解释器的完整版本；AOT搬迁前删除项目、venv、JIT及所有构建目录。
Linux CI另在无Python/.NET/SDK、断网、空PATH且污染Python变量的容器中执行，确保宿主输入不可见。

`--priority` 额外创建三个真实venv，安装不同数据的同名手写包，再实际编译运行验证
项目环境优先、激活环境次之、PATH兜底。没有使用打印假版本的shell替身。
生产探测器的错版本/实现/ABI拒绝由核心的定向测试和真实运行分别登记，驱动不将未执行内容报告为通过。

## 固定回归依赖

| Python | NumPy |
| --- | --- |
| 3.9 | 1.26.4 |
| 3.10 | 2.2.3 |
| 3.11–3.13 | 2.3.5 |
| 3.14–3.15 | 2.5.4 |

这些是固定回归版本，安装器须找到适配实际解释器的二进制wheel；不推断“最高兼容版本”。
发布资料：[NumPy 2.5.4](https://pypi.org/project/numpy/2.5.4/)、
[Python 当前版本状态](https://devguide.python.org/versions/)。

## 命令与状态

```sh
python3 -B tests/cpython/version_run.py --python /path/to/python --python-version 3.12 --priority
```

所有输入、产物和结果放仓库外。成功清理；失败保留并输出诊断目录。
CI按3.9–3.15逐项真实运行，结果待主代理收齐登记；不得以编译成功或接口帮助文本代替实际执行。
