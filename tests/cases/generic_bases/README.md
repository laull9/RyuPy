# Generic 参数化单基类合同

当前10项全部 pending。5项主候选：construction、definition_order、cached_metadata、
orig_entries、bare_failures；5项边界：unsupported_original_core、unsupported_hooks、
unsupported_dynamic_orig、unsupported_user_alias、unsupported_body_raise。主候选尚未经过编译/JIT，不能当已支持。

core 原源含普通实例 __dict__ 与类体 locals 反射缺口，完整复制到
unsupported_original_core，不删除断言、不覆盖其oracle。其它主候选是独立合同。
cached_metadata、orig_entries、hooks、dynamic_orig 直接保原冻结CP源。
未知hook的C# NI专项与CP真实执行结果分开验收，不能拿NI作逐字通过。

全部使用固定CP3.12.13的 -I -S -B 双跑，stdout/stderr/exit逐字一致，均exit0、stderr空。
证据在独立generic-bases-validation/test-contract，代码与golden原样固定。
G3用户类alias保留替换、调用后orig_class及Concrete继承，不只测试一次订阅。

首轮bare_failures因类体直接raise旧语法E0100失败，完整原源与golden保留为
unsupported_body_raise；主例用类外body_failure函数抛同一异常，类体调用它。
新CP双跑stdout仍等于原golden，不代表直接类体raise已经支持。
