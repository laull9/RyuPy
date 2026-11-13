# M3.4：系统、流与路径 API 实现清单

本页列出当前原生实现的 API 与参数边界，不代表整个 `sys`、`io`、`os` 或 `pathlib` 兼容。
实现路径为 C# 对象协议与 .NET 底层流/文件系统适配，无 CPython 桥接、逐对象反射或动态程序集生成。
总体验收与固定项目通过率以 [stdlib.md](stdlib.md)、[compatibility.md](compatibility.md) 和 TODO 为准。

## 平台与错误约定

- **已实际验证**：Linux x86_64 / POSIX，固定 CPython 3.12.13；文件副作用用例只使用框架隔离目录或自建临时目录并清理。
- **Windows 词法路径有限实现候选**（[ADR0187](adr/0187-windows-path-lexical-model.md)）：共享 drive/root/tail 模型覆盖普通相对、盘符相对/绝对、当前盘根及完整 UNC；Path 保留 `..`，`is_absolute` 与 CPython 3.12 `os.path.isabs` 分开。
  `Path`/`WindowsPath` 具有宿主身份，异宿主具体类拒绝；设备命名空间、残缺 UNC、非 ASCII 路径比较/哈希及依赖当前盘上下文的 `absolute()` 明确拒绝。`os.path.normpath/abspath` 的 Windows 规则仍未实现。
  固定 CPython 词法矩阵和 Linux 纯算法测试不等于 Windows 文件/Native 验收；三项 Windows 宿主测试与完整 AOT 应用仍需 CI 实跑。无跨平台 PurePath 模拟。
- `os` 的原始字符串文件系统调用、标准流及换行分支使用宿主平台；Windows 原生行为尚未实测，不据 .NET 支持推定兼容。
- 文件路径接受 `str`、本实现的 `Path` 及返回 `str` 的 `__fspath__`；bytes 路径明确拒绝。
  文件描述符、`dir_fd`、自定义 opener、底层句柄和完整特殊文件协议不在本批范围。
- 已映射常用 `FileNotFoundError`、`FileExistsError`、`PermissionError`、`IsADirectoryError`、`NotADirectoryError`、
  `OSError`、关闭后的 `ValueError` 及参数类型错误。宿主错误消息/errno/filename 等完整异常属性不保证逐字一致。
- **`io.UnsupportedOperation` 的有限双父适配已实现**：既有不可读/不可写能力错误、文本文件非零相对定位及内存流 `fileno()` 使用该类型；
  `OSError` / `ValueError` 两父捕获与用户派生类已验收。`StringIO` 非零相对定位仍为普通 `OSError`。
  仅支持无参/单参构造和稳定原生导入身份，不开放一般多重继承、动态 except 或完整文件描述符；见 [具体范围与成本](io-unsupported-operation.md)。

## `sys` 与进程入口

| API | 当前实现 | 限制与错误 | 验证 |
| --- | --- | --- | --- |
| `argv` | 可变列表，首项为编译入口源码标识，后续为生成程序 `Main(string[])` 收到的参数 | 不把 dotnet 宿主路径当脚本名；不提供解释器启动参数解析 | `cases/phase3/m3_4_system`；入口 CIL/退出码回归 |
| `stdin/stdout/stderr` | 可重绑定；`input`、静态/动态 `print` 与错误报告读取共享系统状态 | 默认包装文本流，不开放 `.buffer`、文件描述符等完整 `TextIOWrapper` 接口 | 系统目录用例、`m3_4_print_target.py` |
| `__stdin__/__stdout__/__stderr__` | 保留初始包装对象，可用于恢复重绑定流 | 不是独立复制的底层句柄 | 系统与输出目标差分 |
| 标准流方法 | `read(size=-1)`、`readline(size=-1)`、`write(str)`、`flush/close`、能力查询、`isatty`、迭代、`with` | 输入按码点与通用换行读取；关闭标记阻止后续 I/O，不主动销毁进程拥有的底层流；不支持定位 | `PySystemTests`、系统目录用例 |
| 标准流属性 | `name/mode/encoding/errors/closed` | 当前声明 UTF-8 / strict；不是完整 locale 配置接口 | 运行时测试及使用路径 |
| `exit(status=None)` | 抛 `SystemExit`，保留原对象 `.code`；整型/非整型退出与错误报告由统一入口处理 | 不在构造异常时提前 `str(code)`；退出刷新失败受入口错误边界管理 | 系统目录用例、`system_exit_big`、`system_exit_bad_str`、入口测试 |
| `modules` | 受控实时缓存查询：下标、`in/len/iter`、`get/keys/values/items` | 仅ADR0131的空闲顶层真实模块新别名可写；已有键替换/删除及整体替换拒绝，不是真dict。集合方法仍返回快照列表 | 系统目录用例；缓存与导入回归 |
| `path` | 入口目录的只读 tuple 视图 | 替换明确拒绝；不允许借它动态改变编译模块图/导入搜索，不等价 CPython 的可变搜索列表 | 系统边界检查 |
| `platform/byteorder/maxsize` | 宿主平台名、字节序和本实现的最大有符号索引提示 | 不提供 `version_info`、解释器帧/引用计数、import hook 等内部接口 | 模块属性实现；未列属性报 `AttributeError` |

## `open`、`io` 与文件流

### 构造与配置

| API / 参数 | 当前实现 | 未支持或明确错误 |
| --- | --- | --- |
| `open` / `io.open` | `file, mode='r', buffering=-1, encoding=None, errors=None, newline=None, closefd=True, opener=None` | 非路径文件描述符未开放；未知参数/重复绑定为 `TypeError` |
| `mode` | 一个 `r/w/a/x`，可配 `+`、`b` 或 `t`；`x` 保留独占创建错误，`a` 每次写前定位末尾 | 重复字符、多个主模式、`b+t` 等非法组合 `ValueError`；不承诺多进程并发追加原子性 |
| `buffering` | `-1` 默认；`0` 二进制无缓冲；`1` 文本行缓冲，遇 CR/LF 刷新 | 文本无缓冲 `ValueError`；其它缓冲大小、二进制行缓冲明确拒绝 |
| `encoding` | 默认 UTF-8；UTF-8 / ASCII / Latin-1 及源码列出的规范化别名 | 不按 locale 隐式切换默认编码；其它编码 `LookupError`；有状态编码未实现 |
| `errors` | `None` / `'strict'` | 其它策略明确拒绝；编码/解码错误保留相应 Python 异常类型，不保证完整错误字段/消息 |
| `newline` | `None`、`''`、`'\n'`、`'\r'`、`'\r\n'` | 其它值 `ValueError`；二进制模式不能带非 None 的编码/errors/newline |
| `closefd/opener` | 路径打开且 `closefd=True`、`opener=None` | 路径配 `closefd=False` 为 `ValueError`；自定义 opener 明确拒绝 |
| `io.StringIO(initial_value='', newline='\n')` | 码点游标的可变文本内存流，支持 `getvalue()` | 不开放完整 CPython 内部缓冲/编码属性；宿主字符串的既有代理项表示边界仍适用 |
| `io.BytesIO(initial_bytes=None)` | bytes / bytearray 初始化；可变字节内存流，支持 `getvalue()` | memoryview / 可导出缓冲区协议未实现 |
| 类型对象 | `StringIO/BytesIO` 为真实原生类型；`IOBase/TextIOBase/BufferedIOBase/TextIOWrapper` 有明确支持的类型关系 | 基类与 `TextIOWrapper` 不能任意构造；未列出的 `RawIOBase/FileIO/BufferedReader/BufferedWriter/BufferedRandom` 模块构造 API 未开放；不支持继承原生流类型 |

默认 UTF-8 读写无 BOM 自动插入；`newline=None` 读取通用换行并归一成 LF，写入将 LF 转宿主行尾；
`newline=''` 读取识别通用行尾但保留原字符，写入不转换；明确行尾值按该终止符处理。
文件构造成功后才转移底层句柄所有权，初始化失败会关闭已打开流；`with` 在正常/异常退出均关闭，不抑制原异常。

### 流操作

| 操作 | 当前实现与边界 |
| --- | --- |
| `read(size=-1)` / `readline(size=-1)` | 文本按码点、二进制按字节；负数及 None 表示不限；EOF 返回空值，不与空行混同 |
| `readlines(hint=-1)` | 逐行读取，正 hint 达到累计长度界限后停止；None 不限；保留参数转换/关闭错误顺序的已测范围 |
| `write(data)` / `writelines(lines)` | 类型必须匹配文本/字节流；write 返回输入码点/字节数，writelines 不添加换行并逐项消费，失败前写入前缀保留 |
| `seek(offset, whence=0)` / `tell()` | 二进制使用字节位置；StringIO 使用码点位置。文本磁盘流仅接受 0 或本实例 tell 返回的 cookie，whence 1/2 仅允许零偏移；其它合法但未实现文本 cookie 路径明确拒绝 |
| `truncate(size=None)` | 默认当前位置，不改变游标；磁盘扩展补零；StringIO/BytesIO 请求更大长度不扩展内容 |
| `flush/close/closed` | 重复 close 安全；关闭后操作按已测顺序报错；刷新失败仍尝试释放底层资源 |
| `readable/writable/seekable/isatty` | 文件/内存能力查询；本批普通文件/内存流可定位且非终端；默认标准流单独判断 |
| 迭代 / `with` | 返回流自身、逐行消费；正常耗尽为 StopIteration；异常离开关闭流 |
| 只读属性 | 文件 `name/mode`，文本文件 `encoding/errors/line_buffering`；内存流 `closed`，StringIO `line_buffering=False` |

上述文件/内存流方法采用已声明的位置参数入口；不将完整 Python 各 I/O 类型的关键字签名视为已兼容。
额外缓冲方法（`readinto/read1/peek/getbuffer/detach/fileno` 等）未开放；缺失属性明确失败。

## `print` 与 `input`

| API | 当前实现 | 关键边界 |
| --- | --- | --- |
| `print(*objects, sep=' ', end='\n', file=None, flush=False)` | sep/end 的 None 使用默认值；file=None 使用当前 stdout；显式 file 使用其 `write`/`flush`；返回 None | 参数先求值，本次调用只固定一次目标，write/str 副作用重绑定 stdout 不把后续片段转给另一条流；None stdout 忽略输出；未知关键字拒绝 |
| 静态 print 路径 | 使用同一固定目标，保留原类型格式化入口 | 原始标准输出仍有缓冲优化；自定义流走显式协议，不绕过用户 write 副作用 |
| `input(prompt)` | 0 或 1 个位置参数，先输出提示并刷新，从当前 stdin.readline 读取；只剥去末尾一个 LF | 无关键字参数；EOF 为 EOFError；stdin丢失为 RuntimeError；自定义 readline 的 CR 不再次归一 |
| 退出刷新 / 错误输出 | 正常/SystemExit/异常均经过统一入口；尝试刷新不同输出目标，错误报告保护用户 `str`/write 失败 | 不承诺完整交互式终端、信号、颜色或编辑器行为；详见入口和错误退出测试 |

## `os` 与 `os.path`

| API | 支持参数 / 行为 | 限制 |
| --- | --- | --- |
| `getcwd/chdir` | 返回/设置进程工作目录 | 仅路径；不支持目录描述符 |
| `listdir(path='.')` | path=None 同当前目录；返回名称列表，保留系统枚举顺序 | 不偷偷排序；不是迭代器；不支持 fd/bytes 路径 |
| `mkdir(path, mode=0o777)` | 非递归，父目录必须存在；已存在报 FileExistsError | 仅默认 mode，宿主 umask 仍生效；不覆盖权限；其它 mode 明确拒绝 |
| `makedirs(name, mode=0o777, exist_ok=False)` | 递归创建与已存在目录选项 | 同默认 mode 限制；不支持 dir_fd |
| `remove/unlink/rmdir` | 删除文件/链接或空目录；缺失与文件/目录类型错误保留 | rmdir 不递归删除目录树；不提供 shutil/rmtree |
| `rename/replace(src,dst)` | 文件移动/覆盖；POSIX rename 覆盖文件；同一路径无操作 | 覆盖已存在目录明确拒绝；不承诺跨设备移动回退 |
| `fspath(path)` | str、Path、返回str的 `__fspath__` | bytes 路径明确拒绝，不等价完整 os.fspath |
| `environ` | 字符串键/值，下标读写删除、in/len/iter、get/keys/values/items/copy；显式映射写入同步进程环境 | keys/values/items为快照列表；非完整MutableMapping；不提供environb |
| `getenv(key, default=None)` | 读取已缓存 environ 视图 | 不是每次重新读取外部进程环境 |
| `putenv/unsetenv` | 只改进程环境，不反向更新 environ 视图；非法名称/空字节明确报错 | 不提供子进程环境管理或线程安全更新承诺 |
| 路径词法 | `join(*parts)`、`basename/dirname/split/splitext/normpath/abspath/isabs` | POSIX已验；normpath消除词法 `..`，不解析链接，恰好双前导斜杠保留；Windows词法边界见开头 |
| 路径探测 | `exists/isfile/isdir` | 基于宿主存在性查询；与 pathlib 保留权限异常的探针不同；不提供完整stat/lstat |
| 常量 | `os.name/sep/linesep/pathsep/curdir/pardir`；os.path另有 `altsep/extsep` | 不提供完整操作系统能力常量集合 |

## `pathlib`

| API | 支持参数 / 行为 | 限制 |
| --- | --- | --- |
| `Path(*segments)` / `PosixPath(*segments)` / `cwd()` | 原生类型对象；稳定身份、type/isinstance/issubclass；构造无文件系统副作用 | 宿主POSIX/Windows对象；异宿主具体类构造拒绝；Windows候选边界见开头，非完整PurePath层次/原生类继承 |
| `str/repr/__fspath__`、`/`、`joinpath`、相等/哈希 | 组合时清除冗余分隔符和`.`，不消除`..`；路径值不可变 | 仅文本PathLike；未声明完整排序/模式匹配协议 |
| `name/suffix/stem/parent/parts/anchor` | 词法组件，点文件与根有专门处理 | 不提供suffixes/parents/with_name等其它属性与转换 |
| `absolute/is_absolute/as_posix` | absolute只增加cwd前缀，**保留`..`**，与os.path.abspath不同 | 不等价resolve；不宣称符号链接解析转换接口已完成 |
| `exists/is_file/is_dir` | 缺失/非目录及空字节返回False，权限错误传播PermissionError；探针跟随链接目标 | 权限分支用注入异常专测，root测试环境不冒称已复现真实无权限目录；不承诺特殊设备/完整stat语义 |
| `mkdir(mode=0o777, parents=False, exist_ok=False)` | 同os目录创建边界 | 自定义mode拒绝 |
| `unlink(missing_ok=False)` / `rmdir` / `rename` / `replace` | 删除与移动；移动返回目标Path | 同os文件系统限制 |
| `iterdir()` | 首次next读取目录快照，返回迭代器 | 不是持续监控；不保留未关闭OS枚举句柄 |
| `glob(pattern, *, case_sensitive=None)` | 单目录 `*` / `?`，按码点匹配；首次推进开始执行 | 路径分隔符、`**`、字符类、显式case_sensitive拒绝；无rglob |
| `read_text(encoding=None, errors=None)` / `read_bytes()` | 借统一open读完并关闭 | 编码/错误策略同open；不提供read_text newline参数 |
| `write_text(data, encoding=None, errors=None, newline=None)` / `write_bytes(data)` | 打开前检查数据类型，避免非法输入截断原文件；返回输入长度 | 接受str或bytes/bytearray对应路径；其它缓冲协议未实现 |
| `open(mode='r', buffering=-1, encoding=None, errors=None, newline=None)` | 转统一open构造 | 不扩closefd/opener或文件描述符 |

## 验证与性能代价

- 编译差分：`tests/e2e/phase3/m3_4_io_paths.py`、`m3_4_streams.py`、`m3_4_io_types.py`、
  `m3_4_paths.py`、`m3_4_native_types.py`、`m3_4_print_target.py`；参数/输入/环境/缓存用 `tests/cases/phase3/m3_4_system`。
- 运行时回归：`PyIOTests`（参数/关闭次序、编码、换行、定位、缓冲可见性）、`PyPathTests`
  （词法、隔离文件、类型身份、延迟枚举、权限映射、Windows拒绝分支）、`PySystemTests`。
  错误退出fixture及driver入口测试验证SystemExit、用户格式化失败与退出刷新，不只比较成功stdout。
- 文件/内存流先保证码点和游标正确：文本文件逐完整码点解码且不跨读取器预取，每次码点处理会有
  解码/字符串分配成本；StringIO以码点字符串列表保存内容，也有额外对象开销。
  文本文件记录本实例已返回的tell位置以校验cookie，记录量随不同位置增长。
- 二进制普通读取按块搬运，逐行读取到行尾停止；返回read/getvalue快照仍会分配与复制。
  目录迭代首次推进快照目录列表；glob对候选名做码点匹配。未提交吞吐加速测量，不声称快于CPython。
- 固定项目需要依赖与真实入口全部通过才计入原生分子；本页API或单个语法用例通过不增加项目覆盖率。

## M3.7：版本与实现标识（已验证子集）

`sys.version_info` 为不可变的原生 tuple 派生协议对象，内容固定为
`(3, 12, 13, 'final', 0)`，表示项目选定的 **Python 语言兼容目标**，不表示已经
实现 Python 3.12.13 的全部能力。支持 major/minor/micro/releaselevel/serial、
整型下标/切片、长度、迭代、与 tuple 的双向比较及相同哈希、tuple 祖先判定、
结构化 repr。字段与下标不可写；不把它伪装成可变 list，也不承诺完整 structseq
构造、元组算术或全部 tuple 方法。

`sys.implementation.name` 明确为 `ryupy`；`sys.version` 为
`3.12.13 (RyuPy; Python compatibility target)`。不伪造 CPython 构建信息、
cache_tag 或原生 ABI。implementation 当前仅公开受控只读 name，不宣称完整
SimpleNamespace 的可写字段/实现元数据协议。

语言版本值通过固定 CPython 3.12.13 的 `m3_7_sys_version.py` 对照；实际实现
名称和 version 字符串由 `PySystemVersionTests` 独立验收，不作假差分。
该差分已通过，专测包含双向不支持比较的 TypeError；不把语言版本声明作为
项目兼容率或全版本实现完成的证据。


受限新模块别名的键域、注册/初始化守卫、原身份与完整pending见[ADR0131](adr/0131-module-cache-aliases.md)和[验收报告](module-cache-alias-slice.md)。
