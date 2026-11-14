# dict派生之前，先让更新按顺序发生

docopt已经越过list基类阻断，但这不意味着再添加一个dict载体便能正确运行。
固定CP合同先揭示dict.__init__是合并，不像list.__init__先清空。进一步用精确dict实测，
旧更新提前耗尽generator，结果丢掉失败前应写入的前缀，也会在坏pair后执行多余副作用。

修复将外流与内流分开：外流逐项取值，内pair完整物化并验证，然后马上提交该项。
不能为了“流式”而只读inner前三项；CP在检查pair长度前会完整耗尽它，提示/异常都可观察。
mapping又有不同规则：先keys，keys一般先物化，但exact list须活读取。先getter后验证键，
才能保留用户异常的优先级。源是iter(user)时，内部适配器不能隐藏原对象的keys属性。

异常也不是仅复制一段消息。inner TypeError被替换不代表把调用者正在处理的异常清掉；
新的错误仍以那个ambient异常为context。用完整链和用户帧回归比只断言最后一行可靠。

本切不重写哈希表。新generic路径的安全键守卫与旧exact/native raw路径分别说明，不能
将一次成功外推成自定义键stored-hash/重入正确。原生快路也不能因新增回调lambda，在提前
返回之前就分配闭包。表示保持不变后，仍需用同CIL公共入口检查时间和实际分配。

完整范围、保留失败、性能与门禁见[ADR0105](../adr/0105-dict-update-protocol.md)。
