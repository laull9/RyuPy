//! 旧数组后备路径的只读形参合同；默认字段状态机由generator_state_tests单独验证。
//!
//! 验证：
//! 1. 结构断言：原始 count_up(n) 的 Dynamic 形参 n 命中只读形参集合，在 Suspend 时消除冗余 ArraySet 写回；
//! 2. 结构断言：恢复时入口 ArrayGet 保持完整加载，非参数局部（如 i）与控制状态槽维持正常写回；
//! 3. 边界拒绝：形参重赋值、send 恢复定义、静态 Typed 形参、闭包 cell 形参、异常保护区 (EH)、
//!    product 模式均严格拒绝，退回全量写回；
//! 4. 可变对象别名跨暂停：对象内容被就地修改但引用不变的参数正常命中只读优化；
//! 5. 降低后全量 MIR 验证 (crate::validate::validate) 均通过。

use crate::generator_readonly_params::find_readonly_params;
use crate::mir::*;
use rustc_hash::FxHashSet;

fn compile(source: &str) -> (Program, FnId, FnId) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("test_generator.py", source);
    let (parsed, diags) = ryupy_syntax::parse(file, source);
    assert!(diags.is_empty(), "语法解析失败: {diags:?}");
    let (hir, diags) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diags.is_empty(), "HIR 降低失败: {diags:?}");
    let (types, diags) = ryupy_ty::check_module(&hir);
    assert!(diags.is_empty(), "类型检查失败: {diags:?}");
    let (program, _, diags) = crate::build::build_program_with_origins(&hir, &types, None);
    assert!(diags.is_empty(), "MIR 构建失败: {diags:?}");

    let mut factory_id = None;
    let mut resume_id = None;
    for (id, func) in program.functions.iter() {
        if func.codegen_name.contains("$resume") {
            resume_id = Some(id);
        } else if func.name != "__module_main" && factory_id.is_none() {
            factory_id = Some(id);
        }
    }
    let factory_id = factory_id.expect("未找到生成器工厂函数");
    let resume_id = resume_id.expect("未找到生成器恢复函数");
    (program, factory_id, resume_id)
}

fn has_array_set_to_slot(block: &BasicBlock, frame: LocalId, slot_index: i64) -> bool {
    block.stmts.iter().any(|stmt| {
        if let Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArraySet),
            args,
        }) = stmt
        {
            args.len() == 3
                && args[0] == Operand::Local(frame)
                && args[1] == Operand::Const(Constant::I64(slot_index))
        } else {
            false
        }
    })
}

fn has_array_get_from_slot(block: &BasicBlock, frame: LocalId, slot_index: i64) -> bool {
    block.stmts.iter().any(|stmt| {
        if let Statement::Assign {
            value:
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ArrayGet),
                    args,
                },
            ..
        } = stmt
        {
            args.len() == 2
                && args[0] == Operand::Local(frame)
                && args[1] == Operand::Const(Constant::I64(slot_index))
        } else {
            false
        }
    })
}

#[test]
fn test_count_up_n_hit_and_lowered_mir_validates() {
    let source = "def count_up(n):\n    i = 0\n    while i < n:\n        yield i\n        i += 1\n";
    let (program, factory_id, resume_id) = compile(source);

    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    let factory = &program.functions[factory_id];
    let resume = &program.functions[resume_id];

    // 工厂函数创建时，帧槽位包含 n 的初始实参
    let make_gen_stmt = factory.body.blocks[factory.body.entry]
        .stmts
        .iter()
        .find_map(|s| match s {
            Statement::Assign {
                value: Rvalue::MakeGenerator { frame, .. },
                ..
            } => Some(frame),
            _ => None,
        })
        .expect("工厂函数必须调用 MakeGenerator");

    // 帧槽 0..3 为状态与异常，槽 4 为首个参数 n
    assert!(make_gen_stmt.len() >= 5);
    assert_eq!(
        make_gen_stmt[4],
        Operand::Local(LocalId::from_raw(0.into()))
    );

    let frame = resume.params[0];
    let n_slot = 4i64;
    let i_slot = 5i64;

    // 恢复入口必须完整执行 ArrayGet 加载 n 和 i
    let entry_block = &resume.body.blocks[resume.body.entry];
    assert!(
        has_array_get_from_slot(entry_block, frame, n_slot),
        "恢复入口必须有 n 的 ArrayGet"
    );
    assert!(
        has_array_get_from_slot(entry_block, frame, i_slot),
        "恢复入口必须有 i 的 ArrayGet"
    );

    // 查找挂起块（设置非 -1 状态并返回 yield 值的块）
    // 查找包含挂起局部写回的块（包含对局部变量 i 的写回）
    let suspend_blocks: Vec<_> = resume
        .body
        .blocks
        .iter()
        .filter(|(_, b)| has_array_set_to_slot(b, frame, i_slot))
        .collect();

    assert_eq!(
        suspend_blocks.len(),
        1,
        "应恰好有一个包含局部写回的 Suspend 挂起块"
    );
    let (_, suspend_block) = suspend_blocks[0];

    // 只读形参 n 的 ArraySet 写回被成功消除！
    assert!(
        !has_array_set_to_slot(suspend_block, frame, n_slot),
        "只读形参 n 的 Suspend ArraySet 写回必须已被消除"
    );

    // 局部变量 i 的 ArraySet 写回必须保留！
    assert!(
        has_array_set_to_slot(suspend_block, frame, i_slot),
        "非参数局部变量 i 的 Suspend ArraySet 写回必须保留"
    );
}

#[test]
fn test_rebound_param_rejected() {
    let source = "def rebind(n):\n    yield 1\n    n = 42\n    yield n\n";
    let (program, _, resume_id) = compile(source);

    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    let resume = &program.functions[resume_id];
    let frame = resume.params[0];
    let n_slot = 4i64;

    // 挂起块中，由于 n 被重新赋值，必须保留 ArraySet 写回
    let has_suspend_with_n_spill = resume
        .body
        .blocks
        .iter()
        .any(|(_, b)| has_array_set_to_slot(b, frame, n_slot));

    assert!(
        has_suspend_with_n_spill,
        "重新赋值的形参 n 必须在挂起块中执行 ArraySet 写回"
    );
}

#[test]
fn test_suspend_input_param_rejected() {
    let source = "def recv(n):\n    n = yield 1\n    yield n\n";
    let (program, _, resume_id) = compile(source);

    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    let resume = &program.functions[resume_id];
    let frame = resume.params[0];
    let n_slot = 4i64;

    let has_suspend_with_n_spill = resume
        .body
        .blocks
        .iter()
        .any(|(_, b)| has_array_set_to_slot(b, frame, n_slot));

    assert!(
        has_suspend_with_n_spill,
        "通过 yield 接收 send 输入的形参 n 必须在挂起块中执行 ArraySet 写回"
    );
}

#[test]
fn test_typed_param_rejected() {
    // 构造包含静态类型 (MirType::I64) 形参的生成器 MIR
    let mut locals = la_arena::Arena::default();
    let n_typed = locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: Some("n".into()),
        is_cell: false,
    });
    let mut blocks = la_arena::Arena::default();
    let resume_bb = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    let entry_bb = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Suspend {
            value: Operand::Const(Constant::None),
            resume: resume_bb,
            input: None,
            delegated: false,
        },
    });

    let mut functions = la_arena::Arena::default();
    let gen_id = functions.alloc(Function {
        core: Default::default(),
        is_comprehension: false,
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "typed_gen".into(),
        codegen_name: "typed_gen".into(),
        source_file: "typed_test.py".into(),
        definition: None,
        params: vec![n_typed],
        ret: MirType::Dynamic,
        body: Body {
            direct_globals_contracts: Vec::new(),
            source_handler_contracts: Vec::new(),
            locals,
            blocks,
            entry: entry_bb,
            regions: Vec::new(),
        },
        param_meta: Vec::new(),
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });

    let mut program = Program {
        functions,
        classes: la_arena::Arena::default(),
        globals: la_arena::Arena::default(),
        entry: gen_id,
        source_name: "typed_test.py".into(),
    };

    crate::generators::lower(&mut program, &[gen_id], &Default::default());
    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    // 找到 resume 函数
    let resume = program
        .functions
        .iter()
        .find_map(|(_, f)| {
            if f.codegen_name.contains("$resume") {
                Some(f)
            } else {
                None
            }
        })
        .expect("必须生成 resume 函数");

    let frame = resume.params[0];
    let n_slot = 4i64;

    // 静态类型的形参 n 不属于 Dynamic 候选，在挂起时必须执行 ArraySet 写回（装箱保存）
    let has_suspend_with_n_spill = resume
        .body
        .blocks
        .iter()
        .any(|(_, b)| has_array_set_to_slot(b, frame, n_slot));

    assert!(
        has_suspend_with_n_spill,
        "静态类型形参 n 不属于 Dynamic 候选，必须保留写回"
    );
}

#[test]
fn test_cell_param_rejected() {
    let source = "def cell_param(n):\n    def inner():\n        return n\n    yield inner()\n";
    let (program, _, resume_id) = compile(source);

    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    let resume = &program.functions[resume_id];
    let frame = resume.params[0];
    let n_slot = 4i64;

    let has_suspend_with_n_spill = resume
        .body
        .blocks
        .iter()
        .any(|(_, b)| has_array_set_to_slot(b, frame, n_slot));

    assert!(
        has_suspend_with_n_spill,
        "被闭包捕获的 cell 形参 n 必须保留写回"
    );
}

#[test]
fn test_eh_param_rejected() {
    let source =
        "def eh_param(n):\n    try:\n        yield n\n    except Exception:\n        yield 0\n";
    let (program, _, resume_id) = compile(source);

    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    let resume = &program.functions[resume_id];
    let frame = resume.params[0];
    let n_slot = 4i64;

    let has_suspend_with_n_spill = resume
        .body
        .blocks
        .iter()
        .any(|(_, b)| has_array_set_to_slot(b, frame, n_slot));

    assert!(
        has_suspend_with_n_spill,
        "包含异常保护区 (EH) 的生成器必须全量保存局部槽位"
    );
}

#[test]
fn test_mutable_param_modification_hit() {
    let source = "def mutate_param(items):\n    items.append(1)\n    yield len(items)\n    items.append(2)\n    yield len(items)\n";
    let (program, _, resume_id) = compile(source);

    crate::validate::validate(&program).expect("降低后 MIR 必须合法");

    let resume = &program.functions[resume_id];
    let frame = resume.params[0];
    let items_slot = 4i64;

    // items 的引用绑定从未变化（原地修改同一对象），命中只读参数优化，Suspend 时无 ArraySet
    let suspend_blocks: Vec<_> = resume
        .body
        .blocks
        .iter()
        .filter(|(_, b)| {
            b.stmts.iter().any(|s| {
                if let Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ArraySet),
                    args,
                }) = s
                {
                    args.len() == 3
                        && args[0] == Operand::Local(frame)
                        && args[1] == Operand::Const(Constant::I64(0))
                        && args[2] != Operand::Const(Constant::I64(-1))
                } else {
                    false
                }
            })
        })
        .collect();

    assert!(!suspend_blocks.is_empty(), "必须存在 Suspend 挂起块");
    for (_, block) in suspend_blocks {
        assert!(
            !has_array_set_to_slot(block, frame, items_slot),
            "就地修改的可变参数 items 引用未变，其 ArraySet 写回必须已被消除"
        );
    }
}

#[test]
fn test_safety_gates_and_hidden_parameters() {
    let mut locals = la_arena::Arena::default();
    let p_dyn = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("x".into()),
        is_cell: false,
    });
    let p_hidden = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("__env".into()),
        is_cell: false,
    });
    let p_cell = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("c".into()),
        is_cell: true,
    });
    let p_static = locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: Some("i".into()),
        is_cell: false,
    });

    let mut blocks = la_arena::Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Suspend {
            value: Operand::Const(Constant::None),
            resume: BlockId::from_raw(0.into()),
            input: None,
            delegated: false,
        },
    });

    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals,
        blocks,
        entry,
        regions: Vec::new(),
    };

    let function = Function {
        core: Default::default(),
        is_comprehension: false,
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "test_fn".into(),
        codegen_name: "test_fn".into(),
        source_file: "test.py".into(),
        definition: None,
        params: vec![p_dyn, p_hidden, p_cell, p_static],
        ret: MirType::Dynamic,
        body: Body {
            direct_globals_contracts: Vec::new(),
            source_handler_contracts: Vec::new(),
            locals: la_arena::Arena::default(),
            blocks: la_arena::Arena::default(),
            entry: BlockId::from_raw(0.into()),
            regions: Vec::new(),
        },
        param_meta: Vec::new(),
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: Some(p_hidden),
        dynamic_origin: None,
    };

    // 正常命中：仅 p_dyn 命中
    let normal = find_readonly_params(&function, &body, false);
    assert_eq!(normal, FxHashSet::from_iter([p_dyn]));

    // 门禁：product 模式拒绝
    assert!(find_readonly_params(&function, &body, true).is_empty());

    // 门禁：存在异常区域拒绝
    body.regions.push(Region {
        source_handler_chain: None,
        try_start: entry,
        try_len: 1,
        handler: entry,
        kind: RegionKind::CatchAll,
        exc_local: None,
    });
    assert!(find_readonly_params(&function, &body, false).is_empty());
}

#[test]
fn source_captured_param_keeps_spills() {
    let (program, _, resume_id) = compile(
        "def gen(n):\n    def inc():\n        nonlocal n\n        n += 1\n        return n\n    yield inc()\n    yield n\n",
    );
    crate::validate::validate(&program).unwrap();
    let resume = &program.functions[resume_id];
    assert!(
        resume
            .body
            .blocks
            .iter()
            .any(|(_, block)| has_array_set_to_slot(block, resume.params[0], 4))
    );
}
