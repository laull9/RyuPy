//! 五处局部查询的边界、原扫描对照和完整诊断顺序回归；不改变生产校验路径。
use crate::mir::*;
use crate::validate::{local_decl, operand_type, validate, validate_operand, validate_rvalue};
use la_arena::{Arena, Idx, RawIdx};

fn local(raw: u32) -> LocalId {
    Idx::from_raw(RawIdx::from_u32(raw))
}

fn declaration(ty: MirType) -> LocalDecl {
    LocalDecl {
        ty,
        name: None,
        is_cell: false,
    }
}

/// 仅测试保留原实现，对照结果和引用身份，不在生产中双执行。
fn scan(locals: &Arena<LocalDecl>, id: LocalId) -> Option<&LocalDecl> {
    locals
        .iter()
        .find(|(candidate, _)| *candidate == id)
        .map(|(_, value)| value)
}

fn compare(locals: &Arena<LocalDecl>, id: LocalId) {
    let old = scan(locals, id);
    let new = local_decl(locals, id);
    assert_eq!(old.map(|v| &v.ty), new.map(|v| &v.ty));
    assert_eq!(old.map(std::ptr::from_ref), new.map(std::ptr::from_ref));
}

fn function(name: &str) -> Function {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    Function {
        module_state: None,
        module_state_source: None,
        name: name.into(),
        codegen_name: name.into(),
        source_file: "lookup.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    }
}

fn program(f: Function) -> Program {
    let mut functions = Arena::default();
    let entry = functions.alloc(f);
    Program {
        functions,
        entry,
        classes: Arena::default(),
        globals: Arena::default(),
        source_name: "lookup.py".into(),
    }
}

#[test]
fn checked_lookup_matches_scan_at_all_boundaries_and_after_clear() {
    let mut locals = Arena::default();
    for id in [local(0), local(u32::MAX)] {
        compare(&locals, id);
    }
    let first = locals.alloc(declaration(MirType::I64));
    let last = locals.alloc(declaration(MirType::Bool));
    for id in [first, last, local(locals.len() as u32), local(u32::MAX)] {
        compare(&locals, id);
    }
    let mut foreign = Arena::default();
    let foreign_first = foreign.alloc(declaration(MirType::Dynamic));
    foreign.alloc(declaration(MirType::F64));
    let foreign_outside = foreign.alloc(declaration(MirType::Str));
    compare(&locals, foreign_first);
    compare(&locals, foreign_outside);
    assert_eq!(
        local_decl(&locals, foreign_first).map(|v| &v.ty),
        Some(&MirType::I64)
    );
    assert!(local_decl(&locals, foreign_outside).is_none());
    locals.clear();
    compare(&locals, first);
    assert!(local_decl(&locals, first).is_none());
    locals.alloc(declaration(MirType::Str));
    compare(&locals, first);
    assert_eq!(
        local_decl(&locals, first).map(|v| &v.ty),
        Some(&MirType::Str)
    );
}

#[test]
fn operand_type_and_operand_validation_preserve_missing_and_foreign_ids() {
    let mut f = function("types");
    let empty = program(function("empty"));
    assert_eq!(operand_type(&empty, &f, &Operand::Local(local(0))), None);
    assert_eq!(
        operand_type(&empty, &f, &Operand::Local(local(u32::MAX))),
        None
    );
    let first = f.body.locals.alloc(declaration(MirType::I64));
    let last = f.body.locals.alloc(declaration(MirType::Bool));
    let p = program(f);
    let f = &p.functions[p.entry];
    for (id, expected) in [
        (first, Some(MirType::I64)),
        (last, Some(MirType::Bool)),
        (local(2), None),
        (local(u32::MAX), None),
    ] {
        assert_eq!(operand_type(&p, f, &Operand::Local(id)), expected);
    }
    let mut foreign = Arena::default();
    let foreign_id = foreign.alloc(declaration(MirType::Dynamic));
    assert_eq!(
        operand_type(&p, f, &Operand::Local(foreign_id)),
        Some(MirType::I64)
    );
    let mut errors = vec![];
    for id in [first, last, foreign_id] {
        validate_operand(&p, f, f.body.entry, &Operand::Local(id), &mut errors);
    }
    assert!(errors.is_empty());
    for id in [local(2), local(u32::MAX)] {
        validate_operand(&p, f, f.body.entry, &Operand::Local(id), &mut errors);
    }
    assert_eq!(
        errors,
        vec![
            "函数 `types` 基本块 Idx::<BasicBlock>(0) 引用的局部变量 Idx::<LocalDecl>(2) 不在 locals arena 中",
            "函数 `types` 基本块 Idx::<BasicBlock>(0) 引用的局部变量 Idx::<LocalDecl>(4294967295) 不在 locals arena 中",
        ]
    );
}

#[test]
fn valid_first_and_last_parameters_and_assignments_still_pass() {
    let mut f = function("valid");
    let first = f.body.locals.alloc(declaration(MirType::I64));
    let last = f.body.locals.alloc(declaration(MirType::Bool));
    f.params = vec![first, last];
    f.body.blocks[f.body.entry].stmts = vec![
        Statement::Assign {
            dest: first,
            value: Rvalue::Use(Operand::Const(Constant::I64(1))),
        },
        Statement::Assign {
            dest: last,
            value: Rvalue::Use(Operand::Local(last)),
        },
    ];
    assert_eq!(validate(&program(f)), Ok(()));
}

#[test]
fn invalid_ids_keep_complete_error_order_and_unreachable_return_rule() {
    let mut f = function("invalid");
    f.body.locals.alloc(declaration(MirType::I64));
    f.body.locals.alloc(declaration(MirType::Bool));
    f.params = vec![local(2), local(u32::MAX)];
    f.body.blocks[f.body.entry].stmts = vec![
        Statement::Assign {
            dest: local(2),
            value: Rvalue::Use(Operand::Local(local(u32::MAX))),
        },
        Statement::Eval(Rvalue::Binary {
            op: BinOp::Add,
            lhs: Operand::Const(Constant::I64(1)),
            rhs: Operand::Const(Constant::Bool(true)),
        }),
    ];
    f.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Eval(Rvalue::Use(Operand::Local(local(
            u32::MAX,
        ))))],
        // 原实现不检查不可达 Return；仍检查同块中的普通语句。
        terminator: Terminator::Return(Some(Operand::Local(local(u32::MAX)))),
    });
    assert_eq!(validate(&program(f)), Err(vec![
        "函数 `invalid` 的参数 Idx::<LocalDecl>(2) 不在 locals arena 中".into(),
        "函数 `invalid` 的参数 Idx::<LocalDecl>(4294967295) 不在 locals arena 中".into(),
        "函数 `invalid` 基本块 Idx::<BasicBlock>(0) 中的赋值目标 Idx::<LocalDecl>(2) 不在 locals arena 中".into(),
        "函数 `invalid` 基本块 Idx::<BasicBlock>(0) 引用的局部变量 Idx::<LocalDecl>(4294967295) 不在 locals arena 中".into(),
        "函数 `invalid` 基本块 Idx::<BasicBlock>(0) 二元运算两侧操作数类型不一致：I64 vs Bool".into(),
        "函数 `invalid` 基本块 Idx::<BasicBlock>(1) 引用的局部变量 Idx::<LocalDecl>(4294967295) 不在 locals arena 中".into(),
    ]));
}

#[test]
fn invalid_dest_and_operand_on_empty_arena_keep_duplicate_errors() {
    let mut f = function("empty");
    f.params = vec![local(0)];
    f.body.blocks[f.body.entry].stmts = vec![Statement::Assign {
        dest: local(u32::MAX),
        value: Rvalue::Binary {
            op: BinOp::Add,
            lhs: Operand::Local(local(0)),
            rhs: Operand::Local(local(0)),
        },
    }];
    assert_eq!(validate(&program(f)), Err(vec![
        "函数 `empty` 的参数 Idx::<LocalDecl>(0) 不在 locals arena 中".into(),
        "函数 `empty` 基本块 Idx::<BasicBlock>(0) 中的赋值目标 Idx::<LocalDecl>(4294967295) 不在 locals arena 中".into(),
        "函数 `empty` 基本块 Idx::<BasicBlock>(0) 引用的局部变量 Idx::<LocalDecl>(0) 不在 locals arena 中".into(),
        "函数 `empty` 基本块 Idx::<BasicBlock>(0) 引用的局部变量 Idx::<LocalDecl>(0) 不在 locals arena 中".into(),
    ]));
}

#[test]
fn generator_uses_resume_arena_and_preserves_parameter_short_circuit() {
    for (params, ty, valid) in [
        (vec![], MirType::Dynamic, false),
        (vec![local(0), local(0)], MirType::Dynamic, false),
        (vec![local(1)], MirType::Dynamic, false),
        (vec![local(u32::MAX)], MirType::Dynamic, false),
        (vec![local(0)], MirType::I64, false),
        (vec![local(0)], MirType::Dynamic, true),
    ] {
        let mut caller = function("caller");
        // 与恢复函数同raw但类型相反，防止错误查询当前函数arena。
        caller
            .body
            .locals
            .alloc(declaration(if ty == MirType::Dynamic {
                MirType::I64
            } else {
                MirType::Dynamic
            }));
        let mut p = program(caller);
        let mut resume = function("resume");
        resume.ret = MirType::Dynamic;
        resume.body.locals.alloc(declaration(ty));
        resume.params = params;
        let resume = p.functions.alloc(resume);
        let caller = &p.functions[p.entry];
        let value = Rvalue::MakeGenerator {
            resume,
            frame: vec![Operand::Const(Constant::None); 4],
            origin: None,
        };
        let mut errors = vec![];
        validate_rvalue(caller, caller.body.entry, &value, &p, &mut errors);
        let expected: Vec<String> = if valid {
            vec![]
        } else {
            vec!["函数 `caller` 的生成器恢复入口非法".into()]
        };
        assert_eq!(errors, expected);
    }
}
