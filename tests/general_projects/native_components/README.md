# 显式原生组件与精确标量键产品门

`--cpython-native-components` 用 schema 1 声明固定来源与版本、实际私有根、构建能力、
动态库、部署目标、原始 LC_ID、current/compatibility、目标架构、授权加载名和完整许可证。
默认 provider 无新增必填字段。声明只增加已有 payload，并将审计信息写入 provider 清单；
源码/private prefix 保持原样，重定位、签名只修改部署副本。

当前组件格式实测 macOS arm64。每个库先检查真实架构、LC_ID、声明版本；每条加载边
再要求实际库 current 不低于引用 compatibility。解析依次选择精确声明、当前映像的
`@loader_path` 实际相对目标、当前映像 LC_RPATH 按原顺序实际命中、完全相同 LC_ID、
唯一同叶名候选，最终均核对 ABI 身份。当前 LC_RPATH 子合同支持 loader 相对路径和
部署边界内绝对路径；第一条存在映像若越界或ABI错误直接拒绝。继承runpath stack尚未
推导，`@executable_path` 明确拒绝并要求host上下文，JIT与NativeAOT分别实际验证负例。
找不到、歧义、不同 ABI、越界、重复目标或加载名、缺少许可均精确报错。

声明可带 `payload_selection`：每组给出名称、scope目录、完整members、selected子集与
理由。实际成员必须与冻结全集完全一致，拒绝新成员、缺成员、规范重复和scope重叠；
只可省略真实Mach-O，源码、资源、metadata和许可证继续部署。报告逐文件保存原源/
目标/选择结果/LC_ID/架构/全部加载边，任何仍被选中映像需要的遗漏依赖照常拒绝。
未声明选择时严格检查完整闭包。

```sh
python3 -B tests/general_projects/native_components/run.py \
  --ryupy /absolute/path/to/ryupy \
  --python /absolute/path/to/cpython-3.12.13/bin/python3.12
```

两套手写独立 fixture 使用不同 distribution、扩展模块、库名、符号、ABI 主版本和
部署布局。C 扩展实际调用另一个动态库，提供原生 mapping/index 协议与真实 CP 字典，
入口全部编译为 CIL。第二组同时覆盖没有 dependencies 收集器、只有组件声明的旧显式
provider，确认原基础 site 文件继续部署。

默认 JIT/NativeAOT/OneFile 六产品均先生成同次 CPython oracle，构建后删除源码、wheel、venv、
组件 prefix 和中间构建目录，在中文空格路径、空 PATH 与污染 Python 环境运行。比较
完整 stdout/stderr/退出码，检查依赖重定位、许可审计和原始 prefix 的 LC_ID 保持原值。
每组还有未声明库、缺源、声明版本不符、目标冲突、加载名冲突、缺许可和真实加载边
compatibility 过高等十二个负例，另覆盖完整闭包默认失败、误删必需库、过期选择全集
及JIT/NativeAOT的host路径拒绝。每个负例必须被构建器精确拒绝。

每组wheel另外含独立目录同名/同LC_ID的低ABI候选，当前映像真实LC_RPATH必须先命中
所需目标。旧CLI两组均真实重现全局歧义；选择中另有未选原生库，携带真实缺失依赖，
此库及其加载边写入报告，全部metadata/许可保留。

标量输入覆盖 foreign/local 双向字典覆盖和第一次插入键身份、20000 bit 整数、bool/
int/float 数值相等、None、负零、NaN、emoji、孤立代理项及两个代理码点。内部字典
hash 统一域，键对象保持原身份；foreign str 同时保存实际码点证据，相同 UTF16 内容
允许 hash 碰撞，但 emoji 和两个代理码点仍是不同键。精确 reader 不执行用户转换，
弱缓存命中仍检查 provider 活性。自定义 hash 键的 dict `**` 合同继续在独立 pending
源码保留；本竖切支持精确内建标量，派生类型不进入精确布局快路。

最终六产品和二十四负例通过，证据在仓外：
`~/.cache/ryupy/validation/native-components/20261117T095900.515039Z/`。
最初组件部署成功但 foreign str 键拒绝的真实红证保留于
`~/.cache/ryupy/validation/native-components/20261117T091600.552105Z/`。
前一轮 lone-surrogate 字面量发现常量路径丢码点，独立 pending 保留；当前 reader 门使用
等价 `chr(0xd800) + "x"` 输入，完整源码在产品驱动内可复现。

两独立同LC_ID/叶名歧义红证：
`~/.cache/ryupy/validation/native-components/20261117T095708.283182Z/`。
最初显式选择六产品成功证据（RPATH扩围前）保留于
`~/.cache/ryupy/validation/native-components/20261117T094959.827835Z/`。
