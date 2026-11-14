//! 列表推导式私有 collector ListAppend 直调优化回归与 Guard 判定测试。
//!
//! 形式化验证目标：
//! 1. 真实 pipeline（parse -> lower -> check -> build -> validate）完整打通；
//! 2. 原始推导式基准（comprehensions.py）中的全部三个 `<listcomp>` 均成功应用 ListAppend 直调，
//!    私有函数内部的 DynCallMethod("append") 彻底消除；
//! 3. 伴随的 dict 推导式与 set 推导式（.add / 下标赋值）不受影响，保持原路径；
//! 4. 用户级普通 `list.append` 与自定义类的 `append` 方法调用严格排除，不误入私有直调；
//! 5. 负例 Guard：非位置参数、重赋值、cell 逃逸或伪造初始化的 local 严格拒绝直调，并保留有效 MIR。

use ryupy_base::TextRange;
use ryupy_hir::{AssignTarget, ExprKind, Module, Name, Stmt, StmtKind};
use ryupy_ty::ModuleTypes;

use crate::mir::*;

fn compile_to_mir(source: &str) -> (Program, Module, ModuleTypes) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("test_comp.py", source);
    let (parsed, parse_diags) = ryupy_syntax::parse(file, source);
    assert!(parse_diags.is_empty(), "解析错误: {parse_diags:?}");
    let (hir, hir_diags) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(hir_diags.is_empty(), "HIR lower 错误: {hir_diags:?}");
    let (types, ty_diags) = ryupy_ty::check_module(&hir);
    assert!(ty_diags.is_empty(), "类型检查错误: {ty_diags:?}");
    let (program, _, mir_diags) = crate::build_program(&hir, &types);
    assert!(mir_diags.is_empty(), "MIR build 错误: {mir_diags:?}");
    crate::validate::validate(&program).expect("MIR 校验失败");
    (program, hir, types)
}

/// 检查给定函数体内是否有指定的 RuntimeFn 调用
fn count_runtime_calls(func: &Function, target: RuntimeFn) -> usize {
    let mut count = 0;
    for (_, bb) in func.body.blocks.iter() {
        for stmt in &bb.stmts {
            if let Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(rt),
                ..
            })
            | Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(rt),
                        ..
                    },
                ..
            } = stmt
            {
                if *rt == target {
                    count += 1;
                }
            }
        }
    }
    count
}

/// 检查给定函数体内是否存在 DynCallMethod
fn count_dyn_method_calls(func: &Function, method_name: &str) -> usize {
    let mut count = 0;
    for (_, bb) in func.body.blocks.iter() {
        for stmt in &bb.stmts {
            if let Statement::Assign {
                value: Rvalue::DynCallMethod { name, .. },
                ..
            } = stmt
            {
                if name == method_name {
                    count += 1;
                }
            }
        }
    }
    count
}

#[test]
fn test_original_benchmark_comprehensions_list_comp_direct_append() {
    // 直接使用固定原始输入，避免手写副本与基准内容漂移。
    let source = include_str!("../../../tests/benchmarks/comprehensions.py");
    let (program, _, _) = compile_to_mir(source);

    // 查找所有 <listcomp> 函数
    let listcomps: Vec<&Function> = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .filter(|f| f.name.as_str() == "<listcomp>")
        .collect();

    // 源码中共有三个列表推导式：squares, evens, grid
    assert_eq!(listcomps.len(), 3, "应当提取出 3 个 <listcomp> 独立函数");

    for comp_fn in listcomps {
        // 1. 每个 <listcomp> 内部必须包含 ListAppend 直调
        let append_calls = count_runtime_calls(comp_fn, RuntimeFn::ListAppend);
        assert_eq!(
            append_calls, 1,
            "<listcomp> 内部必须恰好有 1 处 ListAppend 直调"
        );

        // 2. 动态方法分派 DynCallMethod("append") 必须从推导式函数中彻底消失
        let dyn_append_calls = count_dyn_method_calls(comp_fn, "append");
        assert_eq!(
            dyn_append_calls, 0,
            "<listcomp> 内部不应存在任何 DynCallMethod('append')"
        );
    }

    // 3. 检查集合推导式 <setcomp> 依然存在且保持原有的 add 路径（未被误拦截）
    let setcomps: Vec<&Function> = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .filter(|f| f.name.as_str() == "<setcomp>")
        .collect();
    assert_eq!(setcomps.len(), 1, "应当存在 1 个 <setcomp>");
    let set_add_calls = count_dyn_method_calls(setcomps[0], "add");
    assert_eq!(
        set_add_calls, 1,
        "<setcomp> 内部应保留 DynCallMethod('add')"
    );
}

#[test]
fn test_ordinary_user_append_is_not_intercepted() {
    let source = r#"
def user_func():
    items = []
    items.append(100)
    return items

class CustomCollector:
    def append(self, val):
        pass

c = CustomCollector()
c.append(200)
"#;
    let (program, _, _) = compile_to_mir(source);

    // 用户函数内普通的 items.append 绝不能走 ListAppend 直调
    let user_fn = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .find(|f| f.name.as_str() == "user_func")
        .expect("找不到 user_func");

    assert_eq!(
        count_runtime_calls(user_fn, RuntimeFn::ListAppend),
        0,
        "用户函数内的普通 append 绝不能被优化为 ListAppend"
    );
    assert_eq!(
        count_dyn_method_calls(user_fn, "append"),
        1,
        "用户函数内的普通 append 应保留 DynCallMethod"
    );

    // 模块顶层 c.append(200) 同样保留 DynCallMethod
    let main_fn = &program.functions[program.entry];
    assert_eq!(
        count_runtime_calls(main_fn, RuntimeFn::ListAppend),
        0,
        "主函数内的自定义对象 append 绝不能被优化为 ListAppend"
    );
    assert_eq!(
        count_dyn_method_calls(main_fn, "append"),
        1,
        "主函数内的自定义对象 append 应保留 DynCallMethod"
    );
}

#[test]
fn test_guard_rejections_and_edges() {
    // 嵌套推导式与多重 for / if 过滤
    let source = r#"
matrix = [[x * y for x in range(5) if x > 1] for y in range(4) if y % 2 == 0]
funcs = [lambda: item for item in range(3)]
"#;
    let (program, _, _) = compile_to_mir(source);

    let listcomps: Vec<&Function> = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .filter(|f| f.name.as_str() == "<listcomp>")
        .collect();
    assert_eq!(listcomps.len(), 3, "应当包含 3 个 <listcomp>");

    for comp_fn in listcomps {
        assert_eq!(
            count_runtime_calls(comp_fn, RuntimeFn::ListAppend),
            1,
            "推导式内部均应走 ListAppend 直调"
        );
        assert_eq!(count_dyn_method_calls(comp_fn, "append"), 0);
    }
}

#[test]
fn test_hir_guard_rejection_negative_fake_init() {
    // 构造合法 HIR 负例：伪造初始化（root 首条语句给 .result 赋非空列表）
    let source = "squares = [x for x in range(5)]";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("fake_init.py", source);
    let (parsed, _) = ryupy_syntax::parse(file, source);
    let (mut hir, _) = ryupy_hir::lower_module(&parsed, &Default::default(), source);

    // 定位到 <listcomp>
    let comp_id = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "<listcomp>")
        .map(|(id, _)| id)
        .expect("应存在 <listcomp>");

    let comp_fn = &mut hir.functions[comp_id];
    let first_stmt_id = comp_fn.body.root[0];

    // 篡改 root[0] 的 value 为非空列表 [ExprId(0)]
    let dummy_expr = comp_fn.body.exprs.alloc(ryupy_hir::Expr {
        kind: ExprKind::Literal(ryupy_hir::Literal::Int(1)),
        range: TextRange::default(),
    });
    let non_empty_list = comp_fn.body.exprs.alloc(ryupy_hir::Expr {
        kind: ExprKind::List(vec![dummy_expr]),
        range: TextRange::default(),
    });
    let local_id = match comp_fn.body.stmts[first_stmt_id].kind {
        StmtKind::Assign {
            target: AssignTarget::Local(loc),
            ..
        } => loc,
        _ => panic!("首条语句必须为 Local 赋值"),
    };
    comp_fn.body.stmts[first_stmt_id].kind = StmtKind::Assign {
        target: AssignTarget::Local(local_id),
        value: non_empty_list,
    };

    let (types, _) = ryupy_ty::check_module(&hir);
    let (program, _, mir_diags) = crate::build_program(&hir, &types);
    assert!(mir_diags.is_empty());
    crate::validate::validate(&program).expect("负例仍必须为有效 MIR");

    let comp_mir = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .find(|f| f.name.as_str() == "<listcomp>")
        .unwrap();

    // 负例由于没有通过空列表初始化验证，必须拒绝 ListAppend 直调，回退为 DynCallMethod
    assert_eq!(
        count_runtime_calls(comp_mir, RuntimeFn::ListAppend),
        0,
        "非空初始化必须拒绝 ListAppend 直调"
    );
    assert_eq!(
        count_dyn_method_calls(comp_mir, "append"),
        1,
        "拒绝后必须回退为 DynCallMethod('append')"
    );
}

#[test]
fn test_hir_guard_rejection_negative_multiple_writes() {
    // 构造合法 HIR 负例：局部变量 .result 在函数体内有第二次赋值
    let source = "squares = [x for x in range(5)]";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("reassign.py", source);
    let (parsed, _) = ryupy_syntax::parse(file, source);
    let (mut hir, _) = ryupy_hir::lower_module(&parsed, &Default::default(), source);

    let comp_id = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "<listcomp>")
        .map(|(id, _)| id)
        .expect("应存在 <listcomp>");

    let comp_fn = &mut hir.functions[comp_id];
    let first_stmt_id = comp_fn.body.root[0];
    let local_id = match comp_fn.body.stmts[first_stmt_id].kind {
        StmtKind::Assign {
            target: AssignTarget::Local(loc),
            ..
        } => loc,
        _ => panic!("首条语句必须为 Local 赋值"),
    };

    // 在 body.stmts 中插入额外的向 .result 赋值的语句
    let dummy_expr = comp_fn.body.exprs.alloc(ryupy_hir::Expr {
        kind: ExprKind::Literal(ryupy_hir::Literal::Int(0)),
        range: TextRange::default(),
    });
    let reassign_stmt = comp_fn.body.stmts.alloc(Stmt {
        kind: StmtKind::Assign {
            target: AssignTarget::Local(local_id),
            value: dummy_expr,
        },
        range: TextRange::default(),
        line: 1,
    });
    // 将其追加进 root
    comp_fn.body.root.insert(1, reassign_stmt);

    let (types, _) = ryupy_ty::check_module(&hir);
    let (program, _, mir_diags) = crate::build_program(&hir, &types);
    assert!(mir_diags.is_empty());
    crate::validate::validate(&program).expect("负例仍必须为有效 MIR");

    let comp_mir = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .find(|f| f.name.as_str() == "<listcomp>")
        .unwrap();

    // 负例因为存在多处赋值，无法证明单态类型，拒绝 ListAppend
    assert_eq!(
        count_runtime_calls(comp_mir, RuntimeFn::ListAppend),
        0,
        "重赋值必须拒绝 ListAppend 直调"
    );
    assert_eq!(
        count_dyn_method_calls(comp_mir, "append"),
        1,
        "拒绝后必须回退为 DynCallMethod('append')"
    );
}

#[test]
fn test_hir_guard_rejection_negative_cell_capture() {
    // 构造合法 HIR 负例：.result 局部被记录为 cell
    let source = "squares = [x for x in range(5)]";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("cell_escape.py", source);
    let (parsed, _) = ryupy_syntax::parse(file, source);
    let (mut hir, _) = ryupy_hir::lower_module(&parsed, &Default::default(), source);

    let comp_id = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "<listcomp>")
        .map(|(id, _)| id)
        .expect("应存在 <listcomp>");

    let comp_fn = &mut hir.functions[comp_id];
    // 将 ".result" 伪装加入 closure.cells
    comp_fn.closure.cells.push(Name::from(".result"));

    let (types, _) = ryupy_ty::check_module(&hir);
    let (program, _, mir_diags) = crate::build_program(&hir, &types);
    assert!(mir_diags.is_empty());
    crate::validate::validate(&program).expect("负例仍必须为有效 MIR");

    let comp_mir = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .find(|f| f.name.as_str() == "<listcomp>")
        .unwrap();

    assert_eq!(
        count_runtime_calls(comp_mir, RuntimeFn::ListAppend),
        0,
        "cell 逃逸必须拒绝 ListAppend 直调"
    );
    assert_eq!(
        count_dyn_method_calls(comp_mir, "append"),
        1,
        "拒绝后必须回退为 DynCallMethod('append')"
    );
}

#[test]
fn private_collector_deletion_rejects_specialization() {
    let (_, mut hir, _) = compile_to_mir("result = [x for x in range(3)]");
    let comp = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "<listcomp>")
        .map(|(id, _)| id)
        .unwrap();
    let body = &mut hir.functions[comp].body;
    let local = match body.stmts[body.root[0]].kind {
        StmtKind::Assign {
            target: AssignTarget::Local(local),
            ..
        } => local,
        _ => panic!("私有收集器缺少初始赋值"),
    };
    let target = body.exprs.alloc(ryupy_hir::Expr {
        kind: ExprKind::Local(local),
        range: TextRange::default(),
    });
    let delete = body.stmts.alloc(Stmt {
        kind: StmtKind::Delete(vec![target]),
        range: TextRange::default(),
        line: 1,
    });
    body.root.insert(1, delete);
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = crate::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    crate::validate::validate(&program).expect("删除负例仍须为有效MIR");
    let function = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .find(|f| f.name == "<listcomp>")
        .unwrap();
    assert_eq!(count_runtime_calls(function, RuntimeFn::ListAppend), 0);
    assert_eq!(count_dyn_method_calls(function, "append"), 1);
}

#[test]
fn private_collector_keyword_call_rejects_specialization() {
    let (_, mut hir, _) = compile_to_mir("result = [x for x in range(3)]");
    let comp = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "<listcomp>")
        .map(|(id, _)| id)
        .unwrap();
    let function = &mut hir.functions[comp];
    let mut changed = false;
    for (_, expression) in function.body.exprs.iter_mut() {
        if let ExprKind::Call { args, .. } = &mut expression.kind {
            if let [ryupy_hir::CallArg::Positional(value)] = args.as_slice() {
                let value = *value;
                *args = vec![ryupy_hir::CallArg::Keyword(Name::from("value"), value)];
                changed = true;
            }
        }
    }
    assert!(changed, "须修改到私有append调用");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = crate::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    crate::validate::validate(&program).expect("关键字负例仍须为有效MIR");
    let function = program
        .functions
        .iter()
        .map(|(_, f)| f)
        .find(|f| f.name == "<listcomp>")
        .unwrap();
    assert_eq!(count_runtime_calls(function, RuntimeFn::ListAppend), 0);
    assert_eq!(
        count_runtime_calls(function, RuntimeFn::CallCapturedMethod),
        1
    );
}
