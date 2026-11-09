# 普通Import路径早期hosted门（产品待执行）

`gate.py`复用现有hosted准备、Recorder、实际PE路径解析、白名单环境及全源码身份工具，不另装本地SDK。

固定顺序：Release runtime/tests build一次 → C#定向测试一次（新44条、旧49方法58条精确case计数，全部Passed零skip）→ 同源pilot CLI build一次 → CP6、PE build7、JIT7，各label一次，无重试。完整make check仍在原job执行。

旧parent_path_first三源逐字保留原已实测反例，fixtures.json锁输入/golden。原图恰main+pkg，显式`--include-module pkg.data`图恰main+pkg+pkg.data；两者必须正常exit1、原stdout和终结MNFE。信号/abort/启动失败/NI/成功一律失败。负臂stderr只验终行，完整traceback双流保留供独核，不声称CP/JIT traceback逐字相同。

四个路径正案直接对tests/cases/source_import_paths原样entry build并运行实际报告DLL。empty_from_import独立三源只提取既有四臂CP的empty子案，父/child源逐字保留，entry仅包含该臂和等值缓存观察。既有四臂其余三正文臂仅保原CP事实与12条caller C#覆盖，本轮没有编译Python JIT/AOT实测，不以host测试冒领。

普通内建MNFE的name关键字构造及.name赋值都是旧未支持域，失败的四臂派生方案未执行并移出active fixtures；不扩constructor/setter。CI CP与JIT读取同一独立三源，不能称其entry已在本地CP实测。

五个正案忽略pending只为定向真实执行，不把skipped算通过，不提前转正。CP/JIT exit/stdout/stderr精确验；empty-from预期由原CP empty臂提取，仅展开当前源根绝对路径，不改写实际输出，CI须先验本案CP。错误类型、name/path/context及cache均须符合。case原流、退出/启动/超时、图、Runtime与真实PE/runtimeconfig均保全。

源码/GITHUB_SHA/实际工具前后锁定，Release测试输入与编译Runtime身份相同，测试前保存两托管输入；不上传Cargo/SDK/obj大树。结果不是完整20/fullcheck/Native通过；原jobs仅增加早门依赖，原断言继续保护，未弱化。

kind0/2/native的路径域、任意finder/spec圆环/可写name-file-spec诊断、多成员fromlist ABI和来源消费者不扩展。caller补实际单成员入口，不能宣称完整importlib兼容。

本地只做静态检查及不启动子进程的纯合成判定器测试；产品预算尚未执行。hosted通过后须核完整原流、全部测试计数、实际模块图/产物身份，才可决定pending转正。
