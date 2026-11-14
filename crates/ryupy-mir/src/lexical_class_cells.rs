//! 新词法类cell的冷发布来源。局部、外cell、模块槽和外namespace分别保留真实身份。
use crate::*;

#[derive(Debug, Clone, PartialEq)]
pub struct LexicalClassCell {
    pub namespace: LocalId,
    pub publication: LexicalClassPublication,
}
#[derive(Debug, Clone, PartialEq)]
pub enum LexicalClassPublication {
    Local(LocalId),
    Cell(LocalId),
    FreeCell {
        cell: LocalId,
        index: usize,
    },
    Global(GlobalId),
    Namespace {
        namespace: LocalId,
        name: String,
        lexical: bool,
    },
    InstanceName {
        context: LocalId,
        name: String,
    },
}

pub(crate) fn is_publication(statement: &Statement, cell: &ClassCellDefinition) -> bool {
    let Some(plan) = &cell.lexical else {
        return false;
    };
    let published = cell.published_class;
    match (&plan.publication, statement) {
        (
            LexicalClassPublication::Local(target),
            Statement::Assign {
                dest,
                value: Rvalue::Use(Operand::Local(value)),
            },
        ) => dest == target && *value == published,
        (
            LexicalClassPublication::Global(target),
            Statement::SetGlobal {
                dest,
                value: Operand::Local(value),
            },
        ) => dest == target && *value == published,
        (
            LexicalClassPublication::Cell(target)
            | LexicalClassPublication::FreeCell { cell: target, .. },
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellSet),
                args,
            }),
        ) => {
            matches!(args.as_slice(), [Operand::Local(dest), Operand::Local(value)] if dest == target && *value == published)
        }
        (
            LexicalClassPublication::Namespace {
                namespace,
                name,
                lexical,
            },
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(runtime),
                args,
            }),
        ) => {
            *runtime
                == if *lexical {
                    RuntimeFn::LexicalNamespaceSet
                } else {
                    RuntimeFn::SetAttr
                }
                && matches!(args.as_slice(), [Operand::Local(target), Operand::Const(Constant::Str(binding)), Operand::Local(value)]
                if target == namespace && binding == name && *value == published)
        }
        (
            LexicalClassPublication::InstanceName { context, name },
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceStoreName),
                args,
            }),
        ) => {
            matches!(args.as_slice(), [Operand::Local(target), Operand::Const(Constant::Str(binding)), Operand::Local(value)]
                if target == context && binding == name && *value == published)
        }
        _ => false,
    }
}

pub(crate) fn valid_source(function: &Function, cell: &ClassCellDefinition) -> bool {
    let Some(plan) = &cell.lexical else {
        return false;
    };
    let prepares = crate::validate_class_cells::assignments(function, plan.namespace);
    let original = crate::validate_class_cells::assignments(function, cell.original_class);
    let created = crate::validate_class_cells::assignments(function, cell.cell);
    let prepared = matches!((prepares.as_slice(), created.as_slice()),
        ([((block, index), Rvalue::Call { callee: Callee::Runtime(RuntimeFn::LexicalClassPrepare), .. })],
         [((cell_block, cell_index), _)]) if block == cell_block && index + 1 == *cell_index);
    let finished = matches!(original.as_slice(),
        [((block, index), Rvalue::Call { callee: Callee::Runtime(RuntimeFn::LexicalClassFinish), args })]
        if matches!(args.as_slice(), [Operand::Local(namespace)] if *namespace == plan.namespace)
        && crate::validate_instance_module::context_dominates(function, plan.namespace, *block, *index));
    let publication = match plan.publication {
        LexicalClassPublication::FreeCell {
            cell: target,
            index,
        } => matches!(crate::validate_class_cells::assignments(function, target).as_slice(),
                [(_, Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args })]
                if matches!(args.as_slice(), [Operand::Local(env), Operand::Const(Constant::I64(slot))]
                    if Some(*env) == function.env && *slot == index as i64)),
        LexicalClassPublication::Local(local) => {
            crate::validate::local_decl(&function.body.locals, local)
                .is_some_and(|decl| !decl.is_cell && decl.ty == MirType::Dynamic)
        }
        LexicalClassPublication::Cell(local) => {
            crate::validate::local_decl(&function.body.locals, local)
                .is_some_and(|decl| decl.is_cell && decl.ty == MirType::Dynamic)
        }
        _ => true,
    };
    prepared && finished && publication
}

/// 现DeadValues只移除完全未读取的Use/Box发布。该局部无任何观察者时允许删掉死写，不能借此放过活绑定。
pub(crate) fn dead_local_publication(function: &Function, cell: &ClassCellDefinition) -> bool {
    let Some(LexicalClassCell {
        publication: LexicalClassPublication::Local(target),
        ..
    }) = &cell.lexical
    else {
        return false;
    };
    let mut reads = rustc_hash::FxHashSet::default();
    // 与DeadValues一致：EH隐式写入槽也保守作为活读取，不能放宽死发布豁免。
    reads.extend(
        function
            .body
            .regions
            .iter()
            .filter_map(|region| region.exc_local),
    );
    for (_, block) in function.body.blocks.iter() {
        for statement in &block.stmts {
            match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => {
                    crate::passes::dead_values::read_value(value, &mut reads)
                }
                Statement::SetGlobal { value, .. } => {
                    crate::passes::dead_values::read_operand(value, &mut reads)
                }
                Statement::SetField { obj, value, .. } => {
                    crate::passes::dead_values::read_operand(obj, &mut reads);
                    crate::passes::dead_values::read_operand(value, &mut reads);
                }
                Statement::Line(_) => {}
            }
        }
        match &block.terminator {
            Terminator::Return(Some(value)) => {
                crate::passes::dead_values::read_operand(value, &mut reads);
            }
            Terminator::Branch { cond, .. } => {
                crate::passes::dead_values::read_operand(cond, &mut reads)
            }
            Terminator::Raise { value, cause } => {
                for value in value.iter().chain(cause.iter()) {
                    crate::passes::dead_values::read_operand(value, &mut reads);
                }
            }
            Terminator::Suspend { .. } => return false,
            _ => {}
        }
    }
    !reads.contains(target)
}
