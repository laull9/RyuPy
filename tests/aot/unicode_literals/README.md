# 精确 Unicode 字面量与跨域码点

本门从手写原始源码验证 `\ud800x`、lone low surrogate、连续独立 high/low surrogate、真正 emoji、raw 字符串、反斜杠奇偶、相邻字面量、f-string 片段及格式规格的前端保真。普通非延迟注解和函数 docstring 同样保留原码点。类型与 IR 保持文本语义，CIL 仅静态装载码点数据并调用中性工厂。

CLR 字符串只有 UTF-16 单元；两个独立代理码点与一个补充平面码点会拥有相同单元。Runtime 对需要区分的值分配独立实例，以弱旁表保存边界，绝不给 `ldstr` / interned 实例登记。普通程序在首次出现元数据前跳过弱表查找；字符串常量数据弱池保证同一常量重复求值身份。无须另造 Python 业务体或动态 IL。

已覆盖的值操作包括 `len/ord`、索引/切片/迭代、repr、比较、字典/集合键相等、拼接、重复、join、大小写和strip结果的边界保留，以及严格 UTF-8 和直接 C ABI 借用输入。普通ASCII切片沿原StringBuilder算法，比较先查首个UTF16差异再恢复该处码点。CPython桥使用同一中性码点序列导出，回读仅在UTF8无法表示代理码点时走精确路径；foreign/local字典比较复用该事实。

```bash
cargo test -p ryupy-syntax string_codepoints
# 先构建同版Runtime/provider再构建CLI
dotnet build runtime/RyuPy.CPython -c Release
cargo build -p ryupy-cli
python3.12 -B tests/aot/unicode_literals/run.py \
  --cli target/debug/ryupy --python /absolute/cpython3.12
```

Runner 对普通CIL和provider桥分别运行JIT/NativeAOT四产品；固定CPython同次生成预期，删除源、venv和构建树后搬迁，以空PATH及失效Python环境运行，严格比较退出码/stdout/stderr；桥场景还审计原生路径及终态Host计数。日志在仓库外 `~/.cache/ryupy/validation/unicode-literals`，临时产品自动清理。

字符串方法的完整传播继续逐API推进。`tests/e2e/phase3/m3_8_surrogate_replace_pending.py` 保留 replace 新建结果丢边界的完整差分输入；`m3_8_surrogate_postponed_annotation_pending.py` 保留延迟注解规范字符串化目前明确E0100的输入。现有 partition/regex 相邻独立代理专项也继续pending。上述长尾未扩大调用禁域。
