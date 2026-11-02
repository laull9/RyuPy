# 显式 AOT 资源：三平台有限验收

实测源码 `e7c54d375d90ff63b8519552feb5fccd63f49609`。

- [Linux x64 glibc：资源、无运行时容器与原八项验证](https://github.com/laull9/RyuPy/actions/runs/41451385685)
- [macOS arm64 / Windows x64 MSVC：资源与原四模式](https://github.com/laull9/RyuPy/actions/runs/41451385625)
- 本地完整门禁：523 C#、228固定CPython3.12.13基线、202e2e+30ignored，fmt/clippy/Release通过。
- 新Windows词法50项、模块位置17项及IO异常帧2项共69专项；Windows真平台执行同筛选。

## 真实交付

使用 `ryupy build app/main.py --aot --resources app/resources.json --out-dir out`；
精确配置与边界见[资源入口](../../../tests/aot/RESOURCES.md)。本例静态条件选择两个已编译
模块，共用30字节数据，各平台仅一次资源发布。数据冻结到 `_internal/resources/v1`，
manifest schema2绑定真实模块ID、文件大小与SHA；没有部署Python源码。

CP/JIT原数据及修改后的数据分别对照；删除本轮源码/托管输入后，搬迁native仍读冻结的
原值，切换cwd不失效。外部 `__file__ = None/list` 与模块内全局保持同槽身份。缺数据的
发布树先被独立清单校验拒绝，实际程序再退出1，保 main11→english.load_text14 两帧与
FileNotFoundError；没有把失败改为成功或删除帧断言。成功模式逐字比较实际双流。

完整结果摘要见 `summary.json`；SDK/双流/二进制仅留上述CI artifact（14天），不入Git。
清单/hash不是签名或安全证明。CLI/托管输入已删除者仅保原记录，不冒独立重读验证。

## 保留的失败历史

1. 首轮 `d65f99c`：Linux/macOS资源ILC与成功搬迁已完成，缺数据暴露PyIO宿主异常映射
   未附Python帧；只在真正映射抛出点PrepareRaise，纯mapper与原Python异常rethrow不变。
2. 同轮Windows在fixture SHA检查失败，尚无资源发布。当轮原文件字节未保存，不能唯一
   归因换行；固定资源fixture LF checkout、保持原SHA并补完整差异诊断。
3. 布局改 `_internal` 时本地位置专项18/19，一处分段Path.Combine期望漏改；修后19/19。
4. 第二轮 `9d5ee8c`：Linux/macOS资源全部通过，Windows在JIT首次dirname遇原有明确
   Windows词法拒绝。本次仅实现dirname/join两个冷分支，不改fixture绕过真实路径。

上述每轮CI链接与专项历史保存在[资源记录](../../../tests/aot/RESOURCES.md)。

## 仍未承诺

- 资源必须显式选择当前编译图内模块及精确文件；无glob/自动扫描、任意动态import或
  importlib.resources兼容。部署相对名限portable ASCII，宿主中文/空格/&路径已真验证。
- Windows同目标只复用同来源记录/活句柄快照，不合并另一拼写/hardlink别名；设备路径
  和需非ASCII drive等价比较的join明确未支持，未开放完整ntpath。
- Linux在只读、无网络、无dotnet/Python容器运行；Mac/Windows仍安装SDK，不能称完全
  干净目标机。仍依赖系统库，支持仅上述三种同宿主RID，不等于可随意交叉编译。
- 静态优先、必要外置使用 `_internal`；未实现第三方native依赖自动收集/静态链接，未
  为单文件强加自解包。完整发布目录包含符号和审计清单，不能把本轮称作裸exe验收。
- 无性能采样或新性能收益声明；原八项回归通过也不证明全部语言或动态闭包。
