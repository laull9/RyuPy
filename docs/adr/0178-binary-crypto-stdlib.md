# ADR0178：Base64、二进制转换与 BCL 摘要认证

日期：2026-11-13。状态：逐 API 差分、C# 合同及本机 NativeAOT 搬迁通过。

## 范围与分层

当前 dev 的这四个模块缺少原生实现。本切只增加 `base64`、`binascii`、`hashlib`、
`hmac` 的常用合同；压缩由独立切片负责，联合分发由主线 ADR0175 统一说明。
不改 driver、发布流程、catalog、公共 TODO、roadmap 或 stdlib 清单。

沿用 `PyNativeModules`、`PyNativeFunctions`、`PyNativeObject` 的静态绑定及动态协议。
新增的 `binascii.Error` 经 HIR 导入身份、MIR 异常名称、CIL 引用和 CLR 继承链贯通；
只在原有表中追加静态条目，保留既有 token 次序，无新 RuntimeFn、导入桩或反射发射。
用户同名源码的既有解析优先级不变。

公开 CLR ABI 仅新增 `PyBinasciiError` 类型、公开 message 构造器和受保护的派生构造器
三项，总量由 1,737 变为 1,740。独立 `PyBinaryCryptoAbiContractTests` 手写精确许可；
既有各层合成合同同步追加这组记录，保留逐项缺失、改签、重复及未知项的失败检验。
ADR0173 的十五条 C ABI 桥接签名表保持原样。

## API 与验收映射

| 模块 | 已验收 API / 参数 | CPython 差分用例 |
| --- | --- | --- |
| base64 | `b64encode(s, altchars=None)`、`b64decode(s, altchars=None, validate=False)` | `stdlib_binary_codecs.py`、`stdlib_binary_edges.py` |
| base64 | `standard_b64encode/decode`、`urlsafe_b64encode/decode` | `stdlib_binary_codecs.py` |
| base64 | `b16encode/decode(casefold)`、`b32encode/decode(casefold, map01)` | `stdlib_binary_codecs.py`、`stdlib_binary_edges.py` |
| binascii | `hexlify/b2a_hex(data, sep, bytes_per_sep)`、`unhexlify/a2b_hex` | `stdlib_binary_codecs.py`、`stdlib_binary_edges.py` |
| binascii | `crc32(data, crc=0)`、`b2a_base64(newline=...)`、`a2b_base64(strict_mode=...)`、`Error` | `stdlib_binary_codecs.py`、`stdlib_binary_edges.py` |
| 编码回调 | 可变 buffer 在 `__bool__` / `__index__` 中修改、resize 失败、view release 和失败后租约释放 | `stdlib_binary_callbacks.py` |
| hashlib | `sha256/sha512/sha1/md5`、`new(name, data, usedforsecurity=...)` | `stdlib_crypto_hashes.py` |
| hashlib | `update/digest/hexdigest/copy`、`name/digest_size/block_size`、支持算法集合的成员关系 | `stdlib_crypto_hashes.py` |
| hmac | `new`、`HMAC`、`digest`、`compare_digest`；对象增量、复制与同组属性 | `stdlib_crypto_hashes.py` |

四个用例均先登记 pending，经固定 CPython 3.12.13 的退出码和标准输出差分通过后转正。
短 Base64 输入的五字符集合及边界串共进行 7,826 次 strict/permissive 解码，异常类型、
消息逐字比较。编码另覆盖 RFC 向量、完整 256 字节、替代字母表、正负 hex 分组、
非 ASCII bytes 分隔符、CRC 分段与任意精度初值掩码。密码对象覆盖四种已知向量、
长 key、空消息、55/56/63/64/65/127/128/129 等块边界、非破坏性摘要及分叉更新。

## BCL 选择与状态模型

- Base64 与 Base16 的实际转换使用 `Convert`。Base64 在进入 BCL 前用小状态机区分
  CPython 的宽松垃圾跳过、过量/不连续 padding 和 strict 错误优先级；禁止直接把
  `Convert.FromBase64String` 的接受域当作 Python 合同。
- BCL 没有 Base32 转换，本切仅实现 RFC4648 字母表与 padding 的文本位编排。
- CRC32 是非密码校验和，采用 256 项 IEEE 查表，不增加依赖；未引入第三方压缩库。
- 所有密码运算复用 `System.Security.Cryptography.IncrementalHash`。
  `GetCurrentHash` 提供非破坏性摘要，`Clone` 提供独立复制；状态大小与消息累计长度无关，
  不缓存明文历史、不手写 SHA/MD5/HMAC、不直接调用 OpenSSL。
- 四算法名字、BCL 身份、摘要长度和块长来自同一静态表。构造器身份使用弱键表关联，
  HMAC 接受真实登记的 hashlib 构造器，不按函数名字猜测算法。
- `CreateHMAC` 消费 key 快照；临时 key 数组在 finally 中 `ZeroMemory`。
  HMAC 对象自身仍由 BCL/GC 管理，没有承诺密钥立即销毁或整个托管堆的零副本。

## buffer、编码和错误边界

只接受当前运行时支持的真实 `bytes`、`bytearray`、连续 `memoryview` 原始字节。
不把 list、迭代器或仅实现 `__bytes__` 的对象自动转为 buffer。released view 保留
ValueError；连续 B/c 视图读原始字节。非连续/其他格式的 view 受既有 memoryview
构造合同约束，未在这些模块另造类型或静默格式转换。

解码入口允许 ASCII str；非 ASCII str 报 ValueError。编码、哈希与认证不自动选择文本
编码；hashlib 的 str 使用 `Strings must be encoded before hashing`，其他假 buffer
使用对应 TypeError。HMAC `new/HMAC` 的 key 仅接受 bytes/bytearray，一次性 `digest`
接受通用连续 buffer；`update(msg=...)` 与 hashlib 的仅位置 `update(data, /)` 分开绑定。
未知关键字、参数个数和仅位置/仅关键字约束由统一原生函数绑定器处理。

跨 Python 参数回调时借用现有连续 buffer 导出租约实现（内部名称为
`PyRegexBufferLease`），在回调结束后才重新取得 span：允许等长字节修改，阻止 resize
和当前 view release，成功/异常路径都撤销导出。Python 层 base64 先物化任意 view，
保留 bytes/bytearray，并按 translate/upper 时点复制；不能以提前复制所有输入代替这些
可观察行为。`stdlib_binary_callbacks.py` 同时验证了这两类入口的差别。

`binascii.Error` 是 ValueError 子类，模块名、args、别名 catch、isinstance/issubclass
有直接执行覆盖。重绑定该原生异常身份明确 NotImplementedError，用户派生该原生
异常仍受现有 native-base 编译限制；没有开放通用 CLR 异常继承。

## 明确的有限支持

1. 支持算法固定为 `sha256`、`sha512`、`sha1`、`md5`。`new` 兼容大小写，以及
   `sha-256` / `sha-512` / `sha-1` 拼写。其他名字明确 ValueError；不探测宿主额外算法。
2. `algorithms_available` 与 `algorithms_guaranteed` 都只列实际四种；CPython 的集合更大。
   `usedforsecurity` 消费真值协议，不绕过宿主密码策略。受策略限制时明确 ValueError。
3. HMAC 任意自定义 digest callable 明确 NotImplementedError；固定四种注册构造器与
   字符串名字可用。HMAC 的 `digest_size/block_size` 写入明确 NotImplementedError，
   其他状态属性只读；未开放任意实例字段、内部 `_inner/_outer` 或完整自省。
4. `compare_digest` 接受同类 ASCII 文本或真实连续 buffer。非 ASCII 文本和混合
   文本/buffer 明确 TypeError。最终字节比较调用 `FixedTimeEquals`；同类型同长度的
   比较阶段具有其恒时性质。类型检查、ASCII 验证、转换、分配及不同长度分支都在此
   承诺之外，不能声称整个 API 全路径恒时。
5. 未导出的 API 正常 AttributeError，包括 Base85、Base32hex、旧文件式 Base64 API、
   binascii 的 uu/quoted-printable、hashlib KDF/SHA3/BLAKE2/file_digest 等。
   本切也未导出 CPython 的实现辅助属性 `base64.binascii`。

## 执行证据与复跑

环境：macOS arm64，.NET SDK 10.0.403、net10.0，CPython 3.12.13。
专项 C# 合同 25 项通过；HIR 异常导入身份专项 20 项通过；四项正式 JIT 差分通过。

`tests/aot/crypto_stdlib.py` 对四个正式用例逐个真实 `build --aot`，分别与固定 CPython
比对退出码/stdout/stderr。之后复制整个 dist，删除本轮源码、build 及原 dist，使用新
HOME/CWD、空 PATH、无效 DOTNET_ROOT/RYUPY_RUNTIME_DIR 执行搬迁结果，四项均通过。
每轮独立目录保留发布日志、原始双流、退出码及 `result.json`，不做普通文件摘要校验。
验收范围是这台 macOS arm64 宿主；Linux/Windows、独立干净机器及跨模块联合发布由后续
集成门禁执行。

```sh
export CARGO_TARGET_DIR="$PWD/target/crypto-task/cargo"
export TMPDIR="$PWD/target/crypto-task/tmp"
export RUST_TEST_THREADS=1
export PYTHON="$(uv python find 3.12.13)"
mkdir -p "$TMPDIR"
make runtime
dotnet test runtime/RyuPy.Runtime.Tests -nologo -v q \
  --filter 'FullyQualifiedName~PyBinaryCodecsTests|FullyQualifiedName~PyCryptoTests'
cargo test -p ryupy-hir exception_imports -- --test-threads=1
cargo test -p ryupy-e2e -- stdlib_ --test-threads=1
cargo build -p ryupy-cli
"$PYTHON" -B tests/aot/crypto_stdlib.py \
  --ryupy "$CARGO_TARGET_DIR/debug/ryupy" --python "$PYTHON" \
  --out target/crypto-task/aot-new-run
make check
```

`--out` 必须为新目录。runtime、e2e 与 AOT 命令串行执行；不设置 `FULL_CHECK_EVIDENCE`。

本轮确实执行了 `make check`：fmt/clippy、725 项 CPython 基线和 C# 全套
3,052 通过、2 项既有条件跳过；随后旧 `foreach_reconstructed_semantic_matrix`
读取默认 `python3`（宿主 3.13.13）而中止。该测试还会校验普通源码文件摘要，按本任务
要求跳过，未修改其源码或放宽断言。其余 Rust、用例自检及编译差分以固定解释器串行复跑：

```sh
export RYUPY_FOREACH_PYTHON="$PYTHON"
cargo test --workspace --exclude ryupy-e2e -- \
  --skip foreach_fixture_tests::foreach_reconstructed_semantic_matrix
make cases-selftest e2e
```

复跑结果：fmt/clippy 通过，Rust 各包及文档测试合计 1,230 通过、14 项既有 ignored；
上述 foreach 测试单项过滤；用例自检 728 通过；正式 JIT 448 通过、284 项既有 pending
跳过，其中本切四项全通过。C# 全套最终复跑仍为 3,052 通过、2 项既有条件跳过。
完整 `make check` 留有上述一项既有测试阻断。
