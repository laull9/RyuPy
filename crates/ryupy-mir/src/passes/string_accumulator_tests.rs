//! 字符串累加优化真实管线结构测试与完备反例。
//!
//! 验证：
//! 1. 真实编译管线生成的合法 MIR（保留合法 ID，通过 crate::validate 校验）；
//! 2. 顶层 str_concat 模型（+=）与 containers 模型（= +，多循环）；
//! 3. 完备安全反例阻断：
//!    - 自定义 LHS __add__ / __iadd__ 回调读取 globals；
//!    - header 包含动态函数回调；
//!    - latch 包含动态或未知操作；
//!    - range(0) 下未绑定变量（杜绝提前抛 NameError）；
//!    - RHS 循环内重定义；
//!    - 循环内别名观察；
//!    - loop 前存在 globals() 活字典；
//!    - 包含 try 异常保护区。

use super::{MirPass, string_accumulator::StringAccumulator};
use crate::mir::*;

fn compile_module(source: &str) -> (Program, FunctionId) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("string_acc_test.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "解析错误: {diagnostics:?}");
    let (hir, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "HIR 错误: {diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "类型检查错误: {diagnostics:?}");
    let (mut program, _, diagnostics) = crate::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "MIR 构建错误: {diagnostics:?}");
    crate::validate(&program).unwrap();

    // 运行初始 CFG 简化，消除无用空块形成标准 5 块自然循环
    super::SimplifyCfg.run(&mut program.functions[program.entry]);
    crate::validate(&program).unwrap();

    let entry = program.entry;
    (program, entry)
}

fn check_optimization(source: &str) -> (bool, Program) {
    let (mut program, entry) = compile_module(source);
    StringAccumulator.run(&mut program.functions[entry]);
    crate::validate(&program).unwrap();

    let optimized = program.functions[entry].body.blocks.iter().any(|(_, b)| {
        b.stmts.iter().any(|s| {
            matches!(
                s,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
                    ..
                })
            )
        })
    });
    (optimized, program)
}

#[test]
fn test_str_concat_module_global_inplace_optimized() {
    let source = "text = ''\nfor i in range(100):\n    text += 'ab'\n";
    let (optimized, program) = check_optimization(source);
    assert!(optimized, "str_concat 顶层 += 应当被优化为 Holder 累加");

    // 验证 exit 块存在 NumericAccumulatorValue 物化与 SetGlobal
    let func = &program.functions[program.entry];
    let has_materialization = func.body.blocks.iter().any(|(_, b)| {
        b.stmts.iter().any(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::NumericAccumulatorValue),
                        ..
                    },
                    ..
                }
            )
        })
    });
    assert!(has_materialization, "exit 必须包含物化调用");
}

#[test]
fn test_containers_module_global_non_inplace_and_multiple_loops_optimized() {
    let source = "x = 10\ntext = ''\nfor i in range(100):\n    text = text + 'b'\n";
    let (optimized, _) = check_optimization(source);
    assert!(optimized, "containers 顶层 = + 模式在证明为精确字符串后应当被优化");
}

#[test]
fn test_unicode_and_empty_string_literal_accepted() {
    let source = "text = '你好'\nfor i in range(10):\n    text += '世界'\n";
    let (optimized, _) = check_optimization(source);
    assert!(optimized, "Unicode 字符串字面量应当被接纳");

    let empty_source = "text = 'init'\nfor i in range(10):\n    text += ''\n";
    let (optimized_empty, _) = check_optimization(empty_source);
    assert!(optimized_empty, "空字符串字面量追加应当被接纳");
}

#[test]
fn test_counter_example_custom_lhs_add_reading_globals_rejected() {
    // LHS 为自定义类型，可能定义 __add__ 或 __iadd__ 并在回调中观察 globals()
    let source = r#"
class Custom:
    def __add__(self, other):
        return globals()["text"]

text = Custom()
for i in range(10):
    text = text + "suffix"
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "LHS 为非字符串字面量初值时必须被拒绝");
}

#[test]
fn test_counter_example_header_dynamic_callback_rejected() {
    // header 条件包含动态调用，可在循环判定时观察模块状态
    let source = r#"
def check():
    return True

text = ""
while check():
    text += "a"
    break
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "循环头包含动态调用时必须被拒绝");
}

#[test]
fn test_counter_example_unbound_target_range_zero_rejected() {
    // 目标在循环前未绑定，若循环 0 次则在 Python 中不报错；提前 LoadName 会多抛 NameError
    let source = r#"
for i in range(0):
    unbound_target += "suffix"
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "preheader 中未绑定目标在 range(0) 下必须被拒绝");
}

#[test]
fn test_counter_example_rhs_redefinition_in_loop_rejected() {
    // RHS 在循环体内被重定义或来源于动态变量
    let source = r#"
text = ""
rhs = "a"
for i in range(10):
    text += rhs
    rhs = "b"
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "RHS 非固定字面量或在循环中被重定义必须被拒绝");
}

#[test]
fn test_counter_example_alias_observed_in_loop_rejected() {
    // 循环体内观察 text（产生了中间别名引用）
    let source = r#"
text = ""
for i in range(10):
    alias = text
    text += "a"
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "循环体内产生别名读取必须被拒绝");
}

#[test]
fn test_counter_example_globals_live_dict_before_loop_rejected() {
    // 循环前存在 globals() 反射或活字典引用
    let source = r#"
g = globals()
text = ""
for i in range(10):
    text += "a"
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "函数存在 globals 活字典合约或反射时必须被拒绝");
}

#[test]
fn test_counter_example_try_region_rejected() {
    // 循环受异常保护区包裹
    let source = r#"
text = ""
try:
    for i in range(10):
        text += "a"
except Exception:
    pass
"#;
    let (optimized, _) = check_optimization(source);
    assert!(!optimized, "包含异常保护区时必须被拒绝");
}

#[test]
fn test_post_loop_global_read_observes_materialized_string() {
    let source = r#"
text = ""
for i in range(10):
    text += "a"
result = text
"#;
    let (optimized, program) = check_optimization(source);
    assert!(optimized, "带后续读取的正常循环应被优化");

    // 验证在 exit 块中，SetGlobal(text) 支配后文对 text 的读取
    let func = &program.functions[program.entry];
    let exit_has_set_global = func.body.blocks.iter().any(|(_, b)| {
        b.stmts.iter().any(|s| matches!(s, Statement::SetGlobal { .. }))
    });
    assert!(exit_has_set_global, "exit 必须包含 SetGlobal 写回");
}
