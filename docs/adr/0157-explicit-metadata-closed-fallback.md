# ADR-0157：显式枚举与自动候选的有限权威分离

状态：未验收候选。Rust/C#/原产品与保护门尚待同源hosted实测，不增加项目通过数。

## 原因

ADR0150显式closed_snapshot只固定发行目录内容，Load按编译注册域导入；ADR0155声明显式
接口不变，却把显式未选目标与自动未选目标都归Outside。受控A/B两输入同红仅表明这些
观察在A已存在，不足单独归因类工厂，也不使原期望失效。

## 有限实现

driver保留显式来源标记，literal-group已选目标继续使用原候选队列、固定点、强度、
预算与延期规则。group累积后从closed/outside同时移除已选项，最终移除真实图节点。
自动未选仍Outside；显式未选为closed_unplanned。预算Outside优先，不变成缺失。

仅新标签非空发optional schema2，追加closed_unplanned；否则仍v1。运行时v1解析与
历史接受规则不变。v2先验证全部字段、名字、重复、预算及分类关系，再一次发布。
父前缀可交叉，但Outside优先；ActiveUnavailable、已注册/缓存身份不被closed遮蔽。

Import仍先Cache、候选激活、递归父与Cache复查、Registry及其旧path约束。仅最终无注册
fallback命中新标签且名字无点时，要求缓存sys仍是首次真实初始化对象，原path身份保留，
无sticky finder配置且实际hooks字段不存在。检查只读内部字典/身份，无getter/import/IO。
缺失、替换或改动sys/path均NI。首次初始化才记录path，重复Initialize不能签发新权限。

无点顶层可回原ModuleNotFoundError；请求p.q时无注册顶层p也可如此。已注册p而q未纳入
仍NI；不扩dotted finder、可变sys协议、Load/Discover、resolver、RuntimeFn、热ABI或AOT。
Load继续实时读取用户重绑callee；发现不会执行目标。cache/registry真实命中不受新guard
阻断，但仍保各自旧约束。

## 不声称什么

closed_unplanned表示显式编译注册/候选域未纳入，**不是globalAbsent或磁盘缺失证书**。
源物理存在却未include，与真实fixture无源，须在原argv/目录审计中分开记录。运行时不
查文件是否随后出现，也不偷加载它。自动缺失域没有因此修复。

原synthetic必须完整506B、原missing_include必须完整54B且严格CP/退出/流匹配才算恢复。
原golden、collector与autoempty独立红不改。新增显式literal-group无include正例保护既有
候选能力；自动同输入与Outside、late-hooks、重绑、缓存/注册、坏包及前缀交叉均保反例。
固定20逐ID、双fullcheck、source-import/ABI/既有Native-AOT及默认Markdown77B待同源复验。

## 停止条件

若需撤selected能力、放宽预算/错误、改变Cache/Registry优先、宿主缺失扫描、全finder、
改Load或扩AOT才能满足原oracle，则停止并重审合同，不改原期望。
