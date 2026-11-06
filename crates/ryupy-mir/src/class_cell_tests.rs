//! 真实 class cell MIR 的正反例：每次定义的新cell、同源Env、填充相位与暂停恢复。
use crate::*;
const SIMPLE: &str = "class C:\n    def f(self):\n        return __class__\n";
fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("cell.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_globals: true,
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    program
}
fn plan(program: &Program) -> (ClassDefId, ClassCellDefinition) {
    program
        .classes
        .iter()
        .find_map(|(id, class)| class.class_cell.clone().map(|cell| (id, cell)))
        .unwrap()
}
fn errors(program: &Program) -> Vec<String> {
    validate::validate(program).expect_err("损坏的类cell来源必须拒绝")
}
fn is_call(statement: &Statement, runtime: RuntimeFn) -> bool {
    matches!(statement, Statement::Eval(Rvalue::Call { callee: Callee::Runtime(found), .. }) if *found == runtime)
}
#[test]
fn class_cell_pipeline_keeps_definition_and_nested_generator_sources() {
    for source in [
        SIMPLE,
        "class C:\n    f = lambda: __class__\n    def read(self, x=lambda: __class__):\n        return lambda: __class__\n",
        "for index in range(2):\n    class C:\n        def read(self):\n            return __class__\n",
        "class C:\n    def gen(self):\n        yield __class__\n        yield lambda: __class__\n",
        "class C:\n    def gen(self):\n        def inner():\n            return __class__\n        yield inner\n        yield inner\n",
        "class C:\n    def gen(self):\n        return (__class__ for value in [1, 2])\n",
        "if False:\n    class C:\n        def read(self):\n            return __class__\n",
    ] {
        let mut program = compile(source);
        passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
        validate::validate(&program).unwrap();
        assert!(
            program
                .classes
                .iter()
                .any(|(_, class)| class.class_cell.is_some())
        );
    }
}
#[test]
fn class_cell_plain_class_adds_no_cell_or_plan() {
    let program = compile("class C:\n    def f(self):\n        return 1\n");
    assert!(
        program
            .classes
            .iter()
            .all(|(_, class)| class.class_cell.is_none())
    );
    assert!(!pretty::dump(&program).contains("CellNew"));
}
#[test]
fn class_cell_rejects_missing_plan_and_capture_context_substitution() {
    let mut program = compile(SIMPLE);
    let (class, cell) = plan(&program);
    program.classes[class].class_cell = None;
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("未登记来源"))
    );
    let mut program = compile(SIMPLE);
    let context = program.functions[program.entry].module_state.unwrap();
    for (_, block) in program.functions[program.entry].body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign {
                value: Rvalue::MakeFunction { env, .. },
                ..
            } = statement
            {
                env[0] = Operand::Local(context);
            }
        }
    }
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("同源 Env"))
    );
    assert_ne!(context, cell.cell);
}
#[test]
fn class_cell_rejects_hoisted_cell_even_if_it_dominates_captures() {
    let mut program = compile(
        "for index in range(2):\n    class C:\n        def f(self):\n            return __class__\n",
    );
    let (class, cell) = plan(&program);
    let owner = &mut program.functions[cell.owner];
    let position = owner.body.blocks[cell.block]
        .stmts
        .iter()
        .position(
            |statement| matches!(statement, Statement::Assign { dest, .. } if *dest == cell.cell),
        )
        .unwrap();
    let statement = owner.body.blocks[cell.block].stmts.remove(position);
    owner.body.blocks[owner.body.entry]
        .stmts
        .insert(0, statement);
    program.classes[class].class_cell.as_mut().unwrap().block = owner.body.entry;
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("唯一 CellNew"))
    );
}
#[test]
fn class_cell_rejects_early_fill_across_class_body_branch() {
    let mut program = compile(
        "class C:\n    if flag:\n        def f(self):\n            return __class__\n    else:\n        def g(self):\n            return __class__\n",
    );
    let (_, cell) = plan(&program);
    let owner = &mut program.functions[cell.owner];
    let (fill_block, position) = owner
        .body
        .blocks
        .iter()
        .find_map(|(id, block)| {
            block
                .stmts
                .iter()
                .position(|statement| is_call(statement, RuntimeFn::FinalizeOrdinaryClass))
                .map(|index| (id, index))
        })
        .unwrap();
    let finalize = owner.body.blocks[fill_block].stmts.remove(position);
    let guard = owner.body.blocks[fill_block].stmts.remove(position);
    let fill = owner.body.blocks[fill_block].stmts.remove(position);
    let after_new = owner.body.blocks[cell.block]
        .stmts
        .iter()
        .position(
            |statement| matches!(statement, Statement::Assign { dest, .. } if *dest == cell.cell),
        )
        .unwrap()
        + 1;
    owner.body.blocks[cell.block]
        .stmts
        .insert(after_new, finalize);
    owner.body.blocks[cell.block]
        .stmts
        .insert(after_new + 1, guard);
    owner.body.blocks[cell.block]
        .stmts
        .insert(after_new + 2, fill);
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("填充之前捕获"))
    );
}
#[test]
fn class_cell_rejects_raw_array_write_and_alias_escape() {
    for alias in [false, true] {
        let mut program = compile(SIMPLE);
        let (_, cell) = plan(&program);
        let owner = &mut program.functions[cell.owner];
        let target = if alias {
            let temp = owner.body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            owner.body.blocks[cell.block].stmts.push(Statement::Assign {
                dest: temp,
                value: Rvalue::Use(Operand::Local(cell.cell)),
            });
            temp
        } else {
            cell.cell
        };
        owner.body.blocks[cell.block]
            .stmts
            .push(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ArraySet),
                args: vec![
                    Operand::Local(target),
                    Operand::Const(Constant::I64(0)),
                    Operand::Local(cell.original_class),
                ],
            }));
        assert!(
            errors(&program)
                .iter()
                .any(|error| error.contains("raw cell"))
        );
    }
}
#[test]
fn class_cell_rejects_wrong_class_and_missing_fill() {
    for missing in [false, true] {
        let mut program = compile(SIMPLE);
        let (_, cell) = plan(&program);
        let owner = &mut program.functions[cell.owner];
        for (_, block) in owner.body.blocks.iter_mut() {
            if missing {
                block
                    .stmts
                    .retain(|statement| !is_call(statement, RuntimeFn::CellSet));
            } else {
                for statement in &mut block.stmts {
                    if let Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CellSet),
                        args,
                    }) = statement
                    {
                        args[1] = Operand::Local(owner.module_state.unwrap());
                    }
                }
            }
        }
        assert!(
            errors(&program)
                .iter()
                .any(|error| error.contains("唯一 CellNew"))
        );
    }
}
#[test]
fn class_cell_rejects_omitted_generator_resume_plan() {
    let mut program = compile("class C:\n    def gen(self):\n        yield __class__\n");
    let (class, _) = plan(&program);
    program.classes[class]
        .class_cell
        .as_mut()
        .unwrap()
        .captures
        .retain(|capture| matches!(capture.source, ClassCellEnvSource::Parameter));
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("登记同源恢复环境"))
    );
}
#[test]
fn class_cell_rejects_forged_generator_environment_and_second_factory() {
    for duplicate in [false, true] {
        let mut program = compile("class C:\n    def gen(self):\n        yield __class__\n");
        let (_, cell) = plan(&program);
        let capture = cell
            .captures
            .iter()
            .find(|capture| matches!(capture.source, ClassCellEnvSource::GeneratorFrame { .. }))
            .unwrap();
        let ClassCellEnvSource::GeneratorFrame { factory, slot, .. } = capture.source else {
            unreachable!()
        };
        let function = &mut program.functions[factory];
        let mut forged = None;
        for (_, block) in function.body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if let Statement::Assign {
                    dest,
                    value:
                        Rvalue::MakeGenerator {
                            resume,
                            frame,
                            origin,
                        },
                } = statement
                {
                    if duplicate {
                        let mut frame = frame.clone();
                        frame[slot] = Operand::Local(function.module_state.unwrap());
                        forged = Some(Statement::Assign {
                            dest: *dest,
                            value: Rvalue::MakeGenerator {
                                resume: *resume,
                                frame,
                                origin: origin.clone(),
                            },
                        });
                    } else {
                        frame[slot] = Operand::Local(function.module_state.unwrap());
                    }
                }
            }
        }
        if let Some(statement) = forged {
            function.body.blocks[function.body.entry]
                .stmts
                .push(statement);
        }
        assert!(
            errors(&program)
                .iter()
                .any(|error| error.contains("类 cell"))
        );
    }
}

#[test]
fn class_cell_rejects_environment_alias_and_raw_forwarding_escape() {
    let source = "class C:\n    def f(self):\n        return lambda: __class__\n";
    for alias in [false, true] {
        let mut program = compile(source);
        let (_, cell) = plan(&program);
        let id = cell
            .captures
            .iter()
            .find(|capture| program.functions[capture.function].name == "f")
            .unwrap()
            .function;
        let function = &mut program.functions[id];
        let raw = if alias {
            function.env.unwrap()
        } else {
            function
                .body
                .blocks
                .iter()
                .flat_map(|(_, block)| &block.stmts)
                .find_map(|statement| {
                    if let Statement::Assign {
                        value: Rvalue::MakeFunction { env, .. },
                        ..
                    } = statement
                        && let Operand::Local(local) = env[0]
                    {
                        Some(local)
                    } else {
                        None
                    }
                })
                .unwrap()
        };
        if alias {
            let dest = function.body.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            });
            function.body.blocks[function.body.entry]
                .stmts
                .push(Statement::Assign {
                    dest,
                    value: Rvalue::Use(Operand::Local(raw)),
                });
        } else {
            function.body.blocks[function.body.entry].terminator =
                Terminator::Return(Some(Operand::Local(raw)));
        }
        assert!(errors(&program).iter().any(|error| error.contains("逃逸")));
    }
}

#[test]
fn class_cell_rejects_duplicate_cell_in_other_environment_slot() {
    // 普通局部捕获x仍合法，但不能把同一raw类cell复制到x的槽，供任意nonlocal写入。
    let source =
        "class C:\n    def f(self):\n        x = 1\n        return lambda: (__class__, x)\n";
    let mut program = compile(source);
    let (_, cell) = plan(&program);
    let capture = cell
        .captures
        .iter()
        .find(|capture| program.functions[capture.function].name == "<lambda>")
        .unwrap();
    let target = capture.function;
    let index = capture.index;
    for (_, function) in program.functions.iter_mut() {
        for (_, block) in function.body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if let Statement::Assign {
                    value: Rvalue::MakeFunction { func, env, .. },
                    ..
                } = statement
                    && *func == target
                {
                    let raw = env[index].clone();
                    let other = (0..env.len()).find(|slot| *slot != index).unwrap();
                    env[other] = raw;
                }
            }
        }
    }
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("转发的类 cell"))
    );
}

#[test]
fn class_cell_guard_requires_exact_void_abi() {
    for (assign, array, args) in [
        (false, false, vec![]),
        (false, false, vec![Operand::Const(Constant::I64(1))]),
        (true, false, vec![Operand::Const(Constant::Null)]),
        (false, true, vec![Operand::Const(Constant::Null)]),
    ] {
        let mut program = compile(SIMPLE);
        let function = &mut program.functions[program.entry];
        let value = if array {
            Rvalue::CallWithArgs {
                callee: RuntimeFn::InstanceClassCellGuard,
                prefix: args,
                args: vec![],
            }
        } else {
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard),
                args,
            }
        };
        let statement = if assign {
            Statement::Assign {
                dest: function.module_state.unwrap(),
                value,
            }
        } else {
            Statement::Eval(value)
        };
        function.body.blocks[function.body.entry]
            .stmts
            .push(statement);
        assert!(
            errors(&program)
                .iter()
                .any(|error| error.contains("InstanceClassCellGuard") || error.contains("ABI"))
        );
    }
}

#[test]
fn class_cell_rejects_fill_on_only_one_success_path() {
    let mut program = compile(
        "class C:\n    def f(self):\n        return __class__\n    if flag:\n        x = 1\n    else:\n        x = 2\n",
    );
    let (_, cell) = plan(&program);
    let function = &mut program.functions[cell.owner];
    let then_ = function
        .body
        .blocks
        .iter()
        .find_map(|(_, block)| {
            if let Terminator::Branch { then_, .. } = block.terminator {
                Some(then_)
            } else {
                None
            }
        })
        .unwrap();
    let block = &mut function.body.blocks[cell.completion];
    let start = block
        .stmts
        .iter()
        .position(|statement| is_call(statement, RuntimeFn::FinalizeOrdinaryClass))
        .unwrap();
    let group: Vec<_> = block.stmts.drain(start..start + 3).collect();
    function.body.blocks[then_].stmts.extend(group);
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("正常发布之前"))
    );
}

#[test]
fn class_cell_rejects_fill_after_class_decorator_and_missing_publication() {
    for remove_publication in [false, true] {
        let mut program =
            compile("@decorate\nclass C:\n    def f(self):\n        return __class__\n");
        let (_, cell) = plan(&program);
        let block = &mut program.functions[cell.owner].body.blocks[cell.completion];
        if remove_publication {
            block
                .stmts
                .retain(|statement| !is_call(statement, RuntimeFn::InstanceStoreName));
        } else {
            let start = block
                .stmts
                .iter()
                .position(|statement| is_call(statement, RuntimeFn::FinalizeOrdinaryClass))
                .unwrap();
            let group: Vec<_> = block.stmts.drain(start..start + 3).collect();
            let decorator = block
                .stmts
                .iter()
                .position(|statement| {
                    matches!(
                        statement,
                        Statement::Assign {
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::CallValue),
                                ..
                            },
                            ..
                        }
                    )
                })
                .unwrap();
            block.stmts.splice(decorator + 1..decorator + 1, group);
        }
        assert!(
            errors(&program)
                .iter()
                .any(|error| error.contains(if remove_publication {
                    "发布接点"
                } else {
                    "class decorator"
                }))
        );
    }
}

#[test]
fn class_cell_rejects_generator_frame_alias_before_environment_write() {
    let mut program = compile("class C:\n    def gen(self):\n        yield __class__\n");
    let (_, cell) = plan(&program);
    let capture = cell
        .captures
        .iter()
        .find(|capture| matches!(capture.source, ClassCellEnvSource::GeneratorFrame { .. }))
        .unwrap();
    let ClassCellEnvSource::GeneratorFrame { frame, slot, .. } = capture.source else {
        unreachable!()
    };
    let function = &mut program.functions[capture.function];
    let alias = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    function.body.blocks[function.body.entry].stmts.extend([
        Statement::Assign {
            dest: alias,
            value: Rvalue::Use(Operand::Local(frame)),
        },
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArraySet),
            args: vec![
                Operand::Local(alias),
                Operand::Const(Constant::I64(slot as i64)),
                Operand::Const(Constant::Null),
            ],
        }),
    ]);
    assert!(
        errors(&program)
            .iter()
            .any(|error| error.contains("原始frame不得别名"))
    );
}
