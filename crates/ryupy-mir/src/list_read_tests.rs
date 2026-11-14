//! 受控读取的证明负例：同一pair、条件失效、潜在回调、合流与回边都必须保守。
use crate::mir::*;
use la_arena::Arena;

fn sample() -> (Program, [BlockId; 3], [LocalId; 5]) {
    let mut locals = Arena::default();
    let mut local = |ty| {
        locals.alloc(LocalDecl {
            ty,
            name: None,
            is_cell: false,
        })
    };
    let list = local(MirType::Dynamic);
    let index = local(MirType::Dynamic);
    let other = local(MirType::Dynamic);
    let condition = local(MirType::Bool);
    let result = local(MirType::F64);
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: condition,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericListCanReadF64),
                args: vec![Operand::Local(list), Operand::Local(index)],
            },
        }],
        terminator: Terminator::Unreachable,
    });
    let yes = blocks.alloc(BasicBlock {
        stmts: vec![read(result, list, index)],
        terminator: Terminator::Return(Some(Operand::Local(result))),
    });
    let no = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::F64(0.0)))),
    });
    blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Local(condition),
        then_: yes,
        else_: no,
    };
    let mut functions = Arena::default();
    let main = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "checked_read".into(),
        codegen_name: "checked_read".into(),
        source_file: "checked.py".into(),
        definition: None,
        params: vec![list, index, other],
        ret: MirType::F64,
        body: Body {
            direct_globals_contracts: Vec::new(),
            source_handler_contracts: Vec::new(),
            locals,
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
    });
    (
        Program {
            functions,
            classes: Arena::default(),
            globals: Arena::default(),
            entry: main,
            source_name: "checked.py".into(),
        },
        [entry, yes, no],
        [list, index, other, condition, result],
    )
}
fn read(dest: LocalId, list: LocalId, index: LocalId) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::NumericListReadCheckedF64),
            args: vec![Operand::Local(list), Operand::Local(index)],
        },
    }
}
fn reject(program: &Program) {
    assert!(
        crate::validate::validate(program)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("受控列表读取"))
    );
}
fn callback(callee: LocalId) -> Statement {
    Statement::Eval(Rvalue::DynCallValue {
        callee: Operand::Local(callee),
        args: vec![],
    })
}
#[test]
fn same_pair_guard_and_pure_reads_preserve_proof() {
    let (mut p, [_, yes, _], [list, index, _, _, result]) = sample();
    crate::validate::validate(&p).unwrap();
    p.functions[p.entry].body.blocks[yes].stmts.insert(
        0,
        Statement::Assign {
            dest: result,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericListReadF64),
                args: vec![Operand::Local(list), Operand::Local(index)],
            },
        },
    );
    crate::validate::validate(&p).unwrap();
}
#[test]
fn different_receiver_or_index_does_not_reuse_proof() {
    for wrong_receiver in [true, false] {
        let (mut p, [_, yes, _], [list, index, other, _, result]) = sample();
        p.functions[p.entry].body.blocks[yes].stmts[0] = if wrong_receiver {
            read(result, other, index)
        } else {
            read(result, list, other)
        };
        reject(&p);
    }
}
#[test]
fn pair_and_condition_writes_invalidate_proof() {
    for which in 0..3 {
        let (mut p, [entry, yes, _], [list, index, other, condition, _]) = sample();
        if which < 2 {
            p.functions[p.entry].body.blocks[yes].stmts.insert(
                0,
                Statement::Assign {
                    dest: if which == 0 { list } else { index },
                    value: Rvalue::Use(Operand::Local(other)),
                },
            );
        } else {
            p.functions[p.entry].body.blocks[entry]
                .stmts
                .push(Statement::Assign {
                    dest: condition,
                    value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
                });
        }
        reject(&p);
    }
}
#[test]
fn callback_clears_fact_and_old_bool_cannot_reestablish_it() {
    for before_branch in [true, false] {
        let (mut p, [entry, yes, _], [_, _, other, _, _]) = sample();
        let block = if before_branch { entry } else { yes };
        let position = usize::from(before_branch);
        p.functions[p.entry].body.blocks[block]
            .stmts
            .insert(position, callback(other));
        reject(&p);
    }
}
#[test]
fn unguarded_diamond_and_mutating_backedge_reject() {
    let (mut p, [_, yes, no], _) = sample();
    p.functions[p.entry].body.blocks[no].terminator = Terminator::Goto(yes);
    reject(&p);
    let (mut p, [_, yes, no], [_, index, other, condition, _]) = sample();
    let f = &mut p.functions[p.entry];
    let back = f.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: index,
            value: Rvalue::Use(Operand::Local(other)),
        }],
        terminator: Terminator::Goto(yes),
    });
    f.body.blocks[yes].terminator = Terminator::Branch {
        cond: Operand::Local(condition),
        then_: back,
        else_: no,
    };
    reject(&p);
}
#[test]
fn trusted_read_requires_f64_result_consumption_and_dynamic_pair() {
    for case in 0..4 {
        let (mut p, [_, yes, _], [list, index, _, _, result]) = sample();
        let f = &mut p.functions[p.entry];
        match case {
            0 => f.body.locals[result].ty = MirType::Dynamic,
            1 => {
                f.body.blocks[yes].stmts[0] = Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::NumericListReadCheckedF64),
                    args: vec![Operand::Local(list), Operand::Local(index)],
                })
            }
            2 => {
                f.body.blocks[yes].stmts[0] = Statement::Assign {
                    dest: result,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::NumericListReadCheckedF64),
                        args: vec![Operand::Local(list)],
                    },
                }
            }
            _ => f.body.locals[index].ty = MirType::I64,
        }
        assert!(crate::validate::validate(&p).is_err());
    }
}
#[test]
fn writes_and_suspension_reject_trusted_read() {
    let (mut p, [_, yes, _], [list, index, _, _, _]) = sample();
    p.functions[p.entry].body.blocks[yes].stmts.insert(
        0,
        Statement::SetField {
            obj: Operand::Local(list),
            field: 0,
            value: Operand::Local(index),
        },
    );
    reject(&p);
    let (mut p, [entry, yes, _], [list, _, _, _, _]) = sample();
    p.functions[p.entry].body.blocks[entry].terminator = Terminator::Suspend {
        value: Operand::Local(list),
        resume: yes,
        input: None,
        delegated: false,
    };
    reject(&p);
}

#[test]
fn global_write_unknown_call_bool_alias_and_eh_cannot_preserve_proof() {
    for case in 0..4 {
        let (mut p, [entry, yes, no], [list, _, _, condition, _]) = sample();
        if case == 0 {
            let global = p.globals.alloc(GlobalDecl {
                storage: crate::GlobalStorage::LegacyStatic,
                name: "changed".into(),
                ty: MirType::Dynamic,
            });
            p.functions[p.entry].body.blocks[yes].stmts.insert(
                0,
                Statement::SetGlobal {
                    dest: global,
                    value: Operand::Local(list),
                },
            );
        } else if case == 1 {
            p.functions[p.entry].body.blocks[yes].stmts.insert(
                0,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TraceCheckDepth),
                    args: vec![],
                }),
            );
        } else if case == 2 {
            let f = &mut p.functions[p.entry];
            let alias = f.body.locals.alloc(LocalDecl {
                ty: MirType::Bool,
                name: None,
                is_cell: false,
            });
            f.body.blocks[entry].stmts.push(Statement::Assign {
                dest: alias,
                value: Rvalue::Use(Operand::Local(condition)),
            });
            f.body.blocks[entry].terminator = Terminator::Branch {
                cond: Operand::Local(alias),
                then_: yes,
                else_: no,
            };
        } else {
            p.functions[p.entry].body.regions.push(Region {
                source_handler_chain: None,
                kind: RegionKind::CatchAll,
                try_start: entry,
                try_len: 1,
                handler: no,
                exc_local: None,
            });
        }
        reject(&p);
    }
}

#[test]
fn packed_call_cannot_bypass_trusted_abi_or_dominance() {
    for eval in [true, false] {
        let (mut p, [entry, yes, _], [list, index, _, _, result]) = sample();
        let value = Rvalue::CallWithArgs {
            callee: RuntimeFn::NumericListReadCheckedF64,
            prefix: vec![Operand::Local(list)],
            args: vec![Operand::Local(index)],
        };
        p.functions[p.entry].body.blocks[yes].stmts[0] = if eval {
            Statement::Eval(value)
        } else {
            Statement::Assign {
                dest: result,
                value,
            }
        };
        // 即使无普通Call受控读节点、且绕过CanRead分支，仍必须拒绝另一调用表示。
        p.functions[p.entry].body.blocks[entry].terminator = Terminator::Goto(yes);
        reject(&p);
    }
}
