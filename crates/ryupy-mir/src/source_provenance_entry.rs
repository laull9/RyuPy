//! 来源准入的有限Legacy入口直线检查：真实name槽、guard发布与marker顺序；不做CFG固定点。
//! 仅由source_provenance统一准入调用，不按模块名字或文件名恢复缺失来源。
use crate::*;

// 公开模块name必须读写同一已发布槽；只核本入口直线前言，不再建立CFG固定点。
pub(super) fn name_export(
    program: &LinkedProgram,
    module: &ModuleDef,
    initial: &LegacySourceEntry,
) -> bool {
    let names: Vec<_> = module
        .exports
        .iter()
        .filter(|e| e.name == "__name__")
        .collect();
    let [export] = names.as_slice() else {
        return false;
    };
    if export.value != initial.name_global {
        return false;
    }
    let Some((_, function)) = program.functions.iter().find(|(id, _)| *id == module.init) else {
        return false;
    };
    let Some((_, block)) = function
        .body
        .blocks
        .iter()
        .find(|(id, _)| *id == function.body.entry)
    else {
        return false;
    };
    let Some(store) = entry_name(function, initial) else {
        return false;
    };
    let marker = block.stmts[..store].iter().position(|s| {
        matches!(
            s,
            Statement::Assign {
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                    ..
                },
                ..
            }
        )
    });
    let Some(marker) = marker else {
        return false;
    };
    let (reset, value) = match export.guard {
        ExportGuard::ValueSentinel => (export.value, Constant::Null),
        ExportGuard::BoundFlag(flag)
            if block.stmts.get(store + 1)
                == Some(&Statement::SetGlobal {
                    dest: flag,
                    value: Operand::Const(Constant::Bool(true)),
                }) =>
        {
            (flag, Constant::Bool(false))
        }
        _ => return false,
    };
    block.stmts[..marker].iter().any(|s| {
        s == &Statement::SetGlobal {
            dest: reset,
            value: Operand::Const(value.clone()),
        }
    })
}

// 只沿本入口块的已执行纯Use/Box链读取字面量；不执行用户协议或扫描注册名。
pub(super) fn entry_name(function: &Function, initial: &LegacySourceEntry) -> Option<usize> {
    let (_, entry) = function
        .body
        .blocks
        .iter()
        .find(|(id, _)| *id == function.body.entry)?;
    let mut phase = 0;
    for (at, statement) in entry.stmts.iter().enumerate() {
        if let Statement::SetGlobal { dest, value } = statement
            && *dest == initial.name_global
            && !matches!(value, Operand::Const(Constant::Null))
        {
            return (phase == 2
                && text_before(&entry.stmts[..at], value) == Some(initial.initial_name.as_str()))
            .then_some(at);
        }
        // 合成name发布之前只能重置槽、准备纯值和压帧；不能夹入任何用户回调再冒称入口值。
        match statement {
            Statement::Line(_)
            | Statement::Assign {
                value: Rvalue::Use(_) | Rvalue::Box { .. },
                ..
            } => {}
            Statement::SetGlobal {
                value: Operand::Const(Constant::Null | Constant::Bool(false)),
                ..
            } if phase == 0 => {}
            Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                        ..
                    },
                ..
            } if phase == 0 => phase = 1,
            Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                        ..
                    },
                ..
            } if phase == 1 => phase = 2,
            _ => return None,
        }
    }
    None
}

fn text_before<'a>(statements: &'a [Statement], value: &'a Operand) -> Option<&'a str> {
    let mut value = value;
    let mut before = statements;
    for _ in 0..32 {
        match value {
            Operand::Const(Constant::Str(text)) => return Some(text),
            Operand::Local(local) => {
                let (at, statement) =
                    before.iter().enumerate().rev().find(
                        |(_, s)| matches!(s, Statement::Assign { dest, .. } if dest == local),
                    )?;
                let Statement::Assign {
                    value: Rvalue::Use(operand) | Rvalue::Box { value: operand, .. },
                    ..
                } = statement
                else {
                    return None;
                };
                value = operand;
                before = &before[..at];
            }
            _ => return None,
        }
    }
    None
}
