# ADR-0091：默认关闭的编译阶段诊断

状态：已接线，driver三项/CLI四项专项及首个正常完整门禁通过；唯一真实包阶段定位已完成，仍在90秒超时；未发布。

## 背景与边界

chardet 5.2.0 在候选6fe244e的唯一固定入口窗口中CP通过，编译90秒超时且双流为空，未生成产物或启动JIT。不能凭源码需求宣告运行时API首错，也不能凭无输出目录认定前端卡住：原driver直到程序集发射完成才创建输出目录。现dump只处理单文件，不代表真实依赖图构建。

## 决策

仅为build/run增加显式`--diagnostic-stages`，Session默认关闭；dump不接受，build与native-binding组合拒绝。driver记录模块图加载、每模块HIR/类型检查/MIR构建/优化pipeline/最终校验、链接与导出检查、发射、输出写入的begin/end。每轮使用共享单调起点，启用时每条stderr立即flush；模块名最多96字符并转义，不输出IR或源内容。不加逐函数/逐pass标记，不更改优化、校验、实验开关或运行时ABI。

关闭时不读时钟、不格式化、不取得stderr或I/O；仍增加固定阶段处的关闭状态检查，不能称零编译开销。开启时增加计时/有界字符串/I/O成本，不作为速度基准。stderr写入/flush失败会停用后续标记，不改变原编译结果；缺失记录只能视为证据不完整。

end表示对应调用按原边界返回，不普遍等于无Python语法诊断。例如graph-load原本可带parser诊断返回，仍由随后HIR后的原stop_if_errors终止。失败不伪造该阶段end，不移动诊断检查点。标记只能证明最后已完成阶段及已开始未结束区间，不自动证明死循环、唯一根因或回归。

## 验证计划与首轮结果

新增driver三项单测首次3/3：关闭无时钟/sink、转义与flush、写失败不重试。CLI小fixture专项首次4/4，验证默认输出/退出不变、两模块阶段顺序/单调时间、语法错误原诊断、缺失输入无假end及选项限制。仅构建小fixture，runtime复制使用哨兵，不启动dotnet/JIT，不能当运行时验收。

计划随后按同一官方包/入口做唯一一次不超过90秒的诊断编译，首失败停，不运行JIT、不扩大预算或切换release；原失败证据保留。真实定位结果与正常完整门禁分别记终态，未执行前不预称通过。

## 正常完整门禁

首个make check exit0：fmt、严格clippy、workspace及Release构建零warning/error，1647项C#零失败跳过，固定CPython3.12.13基线228通过，e2e202通过+30原ignored。没有快照迁移。日志SHA256：`7043373bb0f65e31d72e0015f85e8abf7fd886a006fe88848fa3dce968e3350e`。此门禁不含新的chardet执行、AOT或性能组；下次诊断仍须单独记录候选及真实原流。

## 唯一同预算诊断结果

候选`89ede2acee53361643a90da296dd526ea47628f6`（tree `6f5f08873df300473d6217dd132a680d6e758033`）完成唯一CLI准备后，仅编译同一锁定chardet5.2.0原入口。501源文件逐hash与此前记录相同，没有修改包/入口、切换release、改变优化开关或跳过校验。此次不执行CP/JIT；CP原golden证据引用原6fe244e窗口，并未冒称重新运行。

编译90.004163秒后timeout/exit -15，stdout空、无out/部分产物。stderr保259条单调阶段标记，129对完整begin/end；唯一尚未结束的区间为`chardet.langbulgarianmodel`的pipeline，在63.556475秒开始。此前该模块HIR/type/MIR构建均已完成。没有进入link、emit或输出写入。

已完成区间中，`chardet.jpcntx` pipeline为15.874278秒，`big5freq`为9.822349秒，`euctwfreq`为9.134622秒；jpcntx单独final-validate为2.341462秒。这些是开启诊断的dev编译区间，不是运行时性能比较。pipeline包含优化pass与每轮验证，当前标记不区分二者，不能据此宣称某个pass/验证器是唯一根因、死循环或回归。源码中线性locals查找的复杂度风险仍只是待核假设。

原result SHA256：`29de3e59c527122d9c7c93690043e3b89a745f06ae3b482216a94105d09d2008`；stderr SHA256：`4ed9144c88e1094e535f5d700cf7a404e8011b980a91cba6aef49b0af0657fcf`。严格一次、90秒加最多2秒终止清理，未重试/加时，未运行AOT/项目矩阵或性能组。结果仍为未完成编译，不是chardet兼容通过。
