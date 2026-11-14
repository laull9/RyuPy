//! 字符串累加展开：将候选循环改写为精确字符串 holder 的延迟追加与退出物化。
//!
//! 1. 前驱块（preheader）：
//!    - 提升右操作数字面量装箱，整个循环只装箱一次；
//!    - 使用 preheader 中已证明的确凿初值操作数直接调用 NumericAccumulatorCreate 初始化 holder；
//! 2. 循环体（body）：
//!    - 移除原本二次方分配的 LoadName / DynBinary / SetGlobal 切片；
//!    - 替换为单条 Eval(NumericAccumulatorAdd(holder, rhs_boxed))；
//! 3. 循环出口（exit）：
//!    - 在 exit 首部调用 NumericAccumulatorValue(holder) 物化不可变字符串；
//!    - 立即通过 SetGlobal 写回目标全局槽位。在零次迭代（range(0)）下保留原身份与绑定。

use super::string_accumulator_match::Plan;
use crate::mir::*;

pub(super) fn expand(function: &mut Function, plan: Plan) {
    // 1. 分配内部 holder 与前驱块装箱的右操作数
    let holder = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let rhs_boxed = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });

    let preheader_stmts = &mut function.body.blocks[plan.preheader].stmts;

    // 前驱块单次完成字面量装箱
    preheader_stmts.push(Statement::Assign {
        dest: rhs_boxed,
        value: Rvalue::Box {
            value: Operand::Const(Constant::Str(plan.rhs_literal)),
            from: MirType::Str,
        },
    });

    // 使用已证明的初始操作数创建 holder；未绑定与 NameError 在 preheader 已经由确凿初值证明消除
    preheader_stmts.push(Statement::Assign {
        dest: holder,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::NumericAccumulatorCreate),
            args: vec![plan.init_operand],
        },
    });

    // 2. 循环体：将 LoadName / DynBinary / SetGlobal 切片替换为单次无额外分配的追加
    let body_stmts = &mut function.body.blocks[plan.body].stmts;
    body_stmts.splice(
        plan.update_start..plan.update_start + plan.update_len,
        [Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
            args: vec![Operand::Local(holder), Operand::Local(rhs_boxed)],
        })],
    );

    // 3. 循环出口：物化不可变字符串并写回模块槽位
    let final_val = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });

    let exit_stmts = &mut function.body.blocks[plan.exit].stmts;
    exit_stmts.insert(
        0,
        Statement::Assign {
            dest: final_val,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorValue),
                args: vec![Operand::Local(holder)],
            },
        },
    );
    exit_stmts.insert(
        1,
        Statement::SetGlobal {
            dest: plan.target_id,
            value: Operand::Local(final_val),
        },
    );
}
