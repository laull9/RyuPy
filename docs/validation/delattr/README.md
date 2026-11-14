# delattr 有限公开入口验证

基线HEAD `58b8595cd37d4b22458bfdb7d23a988e08317638`，Linux x86_64，CPython3.12.13、
.NET SDK10.0.403、Rust1.99.1。范围及共享守卫成本见[ADR0100](../../adr/0100-delattr-public-entry.md)。

- reference.py/results.txt/stderr.txt为一次独立CP参考，包含未支持域，不代表生产通过。
- 初次公开入口C#18/18。审源后补冻结、保留名property、native数据分类及用户异常descriptor锚点。
- 新公开fixture的前两次CP冻结在动态property错误文本断言失败，未跑JIT；独立审查后仅改类体@property，
  保完整错误文本。第三次CP冻结全部七标签及完整cause/context、六用户帧通过，exit1是预期未捕获链。
- 三个CP-first/JIT应用：新公开合同、原slice delattr失败源、原typing.IO delattr失败源。旧源不改字节。
- 原slice源SHA256：`758b02ccb591159016d3b789a206002a2e1fc58eb1be81bf857e1394607b88ae`。
- 原typing.IO源SHA256：`09a3d7f131daed0c2942bf50c130f7cb51a0196e323fba65c1cce7cd621bb781`。
- 独立MIR结构不算应用，验证保存builtin、完整实参副作用、用户shadow及暂停callee。

增补23项首21/23，两宿主setup/身份断言经窄修后定向2/2；原流保留。
最新Release下三应用首2/3，两原pending通过，新应用遇既有未绑定builtin fallback NI。
原源SHA `1350316b12ec849c9342d7a14408e12669297b1b3c73f657471faea764462c7b`逐字保pending；
仅改已绑定模块别名，保yield后重绑和callee捕获观察，随后定向重验。
暖态direct/saved各1000次set+delete暂实测32,000字节，包含设置与调用器，不作新guard归因。
后续正常门禁及控制路线结果待补，不预先宣称通过。

首次正常门禁C#1814/1815失败：IO派生__module__既有TypeError被新NI盖住；旧测试未改，
守卫修正后重新验证。首check原流保check-first，最终check另存。
控制路线实测0字节，公开direct/saved各32,000字节/1000次，结果JSON和组合操作范围随证据归档。

IO守卫修后24项相关定向通过。第二次正常make check exit0：fmt、严格clippy、Release、workspace通过，1815项C#零失败跳过、固定CP基线228、e2e202及原30 ignored全部通过。三项新公开差分及结构在门禁中亦通过。最终日志SHA256 `13bd3f9487fee86a66ecdf804639a328a2d9f3cd613b97206e1bd59e74736865`。
源码/测试/日志及分配JSON摘要见summary.json；六件恢复包保完整原流与产物哈希，不复制DLL。
