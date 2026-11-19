# 版本化编译器离线源码 catalog

## 当前版本及准入

当前版本为 `cpython-3.12.13-catalog-2`。`facts-v2.json` 是 schema 2 事实表，
分别记录原源出处、准确版本、准入角色、已验 API 和平台。参考 Python 为 3.12.13，
`complete_stdlib=false`；全标准库接入继续推进。

| 模块 | 原源与角色 | 验收范围 |
| --- | --- | --- |
| `_markupbase`、`html.entities`、`html.parser` | 旧锁定证据包，准入源码 | 原有 HTML 闭包范围 |
| `html` | 同上，dependency_only | 直接导入继续选择 C#，随源码闭包才选原源 |
| `colorsys` | CPython v3.12.13 原源，准入源码 | 六个公开转换 API，CPython 差分与 osx-arm64 AOT 搬迁 |
| `queue` | CPython v3.12.13 原源，detected_only | 静态报告依赖缺口；尚未接入编译或验收线程 API |

新原源在 `cpython-3.12.13/`，以 `include_bytes!` 内嵌；保留上游代码、介绍、注释和行号。
旧四模块的上游 tag 未核实，事实表明确保留该版本缺口，参考解释器版本不能替代来源版本。
`LICENSE.txt` 保留完整 PSF 许可。新增普通文件不引入摘要校验。

`source_catalog::resolve` 仅给准入表中的五模块创建私有 Token；`queue` 只有静态原源，
无法取得 Token。Token 绑定实际部署路径和原字节直接比较，用户同名或同字节文件没有该能力。
实际部署位置是 `compiler-sources/<catalog_version>/`，带当前 `manifest.json`、许可和
`legacy-manifest.json`。入口目录和显式 `-I` 优先，既有策略门、AST/Instance 能力查询保持。

静态报告及 CLI 用法见 [ADR0174](../../../docs/adr/0174-versioned-source-catalog-probe.md)，
逐 API 证据见 [独立验收](../../../docs/validation/source-catalog-v2.md)。

## 旧四模块资源归档合同

`cpython-sources.tar`是旧四模块实际include_bytes的来源单元，含四份原模块、完整
LICENSE与manifest。无宿主Python、无仓库访问、无用户tar入口。移走编译器后仍可
从同一不可变资源单元恢复JIT实际源文件、来源清单与许可；并非旁置的行数规避副本。

来源是锁定CI42932089715证据包`markdown_closure_inputs.tar.gz`，SHA256见manifest。
参考运行基线为CPython3.12.13；源码upstream tag未另作网络核验，以四份原字节SHA256
作为准确不可变来源身份，不能据此声称整个3.12标准库兼容。

固定第三方原样输入按本次资源合同单独登记。`html/entities.py`仍是2513行，另三份
396/132/535行，**未变成≤600行**。为了保持原字节、注释、行号、模块身份，不拆分、
删注释或生成等价代码。RyuPy维护的生产器、reader、部署、manifest接线与测试仍各≤600行。
该范围只适用于这四份已锁上游资源，不是其它vendor代码的一般例外。

重建：在仓库根运行`python3 crates/ryupy-driver/catalog/build_catalog.py`。
生产器只接受固定digest证据包，采用标准未压缩USTAR、固定次序、普通0644文件、
uid/gid/mtime=0、空owner，重复生产逐字节一致。外层SHA256：
`7a6f3d5a28f5bcc7843e81ecb2433867408d61441c53041e1ecf299b1bb9b34b`。
可用标准`tar -tvf`检查及向独立审计目录恢复成员，不需要任何RyuPy私有解码。

driver先核外层digest，再核白名单、头checksum、固定metadata、padding、成员长度/hash、
许可与manifest。没有通用extractall。只将实际需要的源码写到最终JIT输出目录的
`compiler-sources/<catalog_version>/`，随写manifest和许可；已存在文件只认证，不覆盖。
从最终位置有界nofollow读取同批字节，认证后才入SourceDb与SourceAbsolute位置。
普通JIT不承诺任意搬迁。损坏包、成员或许可全部硬停，不软化成NotImplementedError。

私有令牌绑定最终来源字节；入口与-I用户源优先，相同名字或相同字节的用户文件
都不能自动取得catalog能力。候选冷激活是另一独立合同，部署不等于活动注册。

来源认证与ABI能力分别判定：认证源也必须通过共享完整AST能力查询才可自动选择
Instance；包、无函数及能力边界回到原Legacy选择。令牌不豁免self.__class__等限制，
也不扩大普通用户auto的class域。候选可同时含已有Legacy与Instance冷注册形式。
