//! HIR + 类型 → MIR。
//!
//! 核心是一个「当前基本块」游标的 builder：
//! - 表达式降级返回 `Operand`，复杂子表达式先存进临时局部变量；
//! - `if` / `while` / `for range` / 短路 `and` `or` 需要新建基本块并连接 terminator；
//! - `break` / `continue` 需要一个「循环目标栈」记录 (continue_bb, break_bb)；
//! - `return` 之后的代码落在一个新的不可达块里，最后由清理 Pass 删除；
//! - 函数末尾没有显式 `return` 时补 `Return(None)`。
//!
//! Python 语义改写在这里完成：
//! - `a // b`、`a % b`、`a ** b` → `Callee::Runtime(..)`；
//! - `int` 与 `float` 混合 → 先插入 `Cast { I64ToF64 }`；
//! - `print(a, b)` → 按各参数类型依次 `WriteI64` / `WriteF64` / ...，中间 `WriteSep`，末尾 `WriteNewline`；
//! - `str + str` → `StrConcat`；
//! - `for i in range(a, b, s)` → 计数循环（注意 `s` 为负数时比较方向相反，`s == 0` 要抛 `ValueError`）。

use la_arena::Arena;
use ryupy_hir::{BinOp as HirBinOp, Builtin, ExprId, ExprKind, Literal, Module, StmtKind};
use ryupy_ty::ModuleTypes;

use crate::mir::{
    BasicBlock, BinOp, Callee, Constant, Function, LocalDecl, MirType, Operand, Program, RuntimeFn,
    Rvalue, Statement, Terminator,
};

pub fn build_program(hir: &Module, _types: &ModuleTypes) -> Program {
    let mut functions = Arena::default();
    let classes = Arena::default();

    let main_hir_fn = &hir.functions[hir.main];
    let mut builder = BodyBuilder::new();

    for &stmt_id in &main_hir_fn.body.root {
        builder.build_stmt(stmt_id, &main_hir_fn.body);
    }

    builder.finish_current_block_with_return();

    let entry_bb = builder.entry;
    let main_mir_fn = Function {
        name: main_hir_fn.name.as_str().to_string(),
        params: Vec::new(),
        ret: MirType::Unit,
        body: crate::mir::Body {
            locals: builder.locals,
            blocks: builder.blocks,
            entry: entry_bb,
        },
    };

    let entry_fn_id = functions.alloc(main_mir_fn);

    Program {
        functions,
        classes,
        entry: entry_fn_id,
    }
}

struct BodyBuilder {
    locals: Arena<LocalDecl>,
    blocks: Arena<BasicBlock>,
    entry: crate::mir::BlockId,
    current_bb: crate::mir::BlockId,
}

impl BodyBuilder {
    fn new() -> Self {
        let mut blocks = Arena::default();
        let entry = blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        });
        Self {
            locals: Arena::default(),
            blocks,
            entry,
            current_bb: entry,
        }
    }

    #[allow(dead_code)]
    fn new_block(&mut self) -> crate::mir::BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        })
    }

    fn push_stmt(&mut self, stmt: Statement) {
        self.blocks[self.current_bb].stmts.push(stmt);
    }

    fn finish_current_block_with_return(&mut self) {
        if matches!(
            self.blocks[self.current_bb].terminator,
            Terminator::Unreachable
        ) {
            self.blocks[self.current_bb].terminator = Terminator::Return(None);
        }
    }

    fn build_stmt(&mut self, stmt_id: ryupy_hir::StmtId, hir_body: &ryupy_hir::Body) {
        let stmt = &hir_body.stmts[stmt_id];
        if let StmtKind::Expr(expr_id) = stmt.kind {
            self.build_expr_stmt(expr_id, hir_body);
        }
    }

    fn build_expr_stmt(&mut self, expr_id: ExprId, hir_body: &ryupy_hir::Body) {
        let expr = &hir_body.exprs[expr_id];
        match &expr.kind {
            ExprKind::Call { callee, args } => {
                let callee_expr = &hir_body.exprs[*callee];
                if matches!(callee_expr.kind, ExprKind::Builtin(Builtin::Print)) {
                    for (i, &arg) in args.iter().enumerate() {
                        if i > 0 {
                            self.push_stmt(Statement::Eval(Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::WriteSep),
                                args: Vec::new(),
                            }));
                        }
                        let op = self.build_expr(arg, hir_body);
                        self.push_stmt(Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::WriteI64),
                            args: vec![op],
                        }));
                    }
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::WriteNewline),
                        args: Vec::new(),
                    }));
                } else {
                    let _ = self.build_expr(expr_id, hir_body);
                }
            }
            _ => {
                let _ = self.build_expr(expr_id, hir_body);
            }
        }
    }

    fn build_expr(&mut self, expr_id: ExprId, hir_body: &ryupy_hir::Body) -> Operand {
        let expr = &hir_body.exprs[expr_id];
        match &expr.kind {
            ExprKind::Literal(Literal::Int(val)) => Operand::Const(Constant::I64(*val)),
            ExprKind::Binary { op, lhs, rhs } => {
                let lhs_op = self.build_expr(*lhs, hir_body);
                let rhs_op = self.build_expr(*rhs, hir_body);
                let mir_op = match op {
                    HirBinOp::Add => BinOp::Add,
                    _ => BinOp::Add,
                };
                let dest = self.locals.alloc(LocalDecl {
                    ty: MirType::I64,
                    name: None,
                });
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: mir_op,
                        lhs: lhs_op,
                        rhs: rhs_op,
                    },
                });
                Operand::Local(dest)
            }
            _ => Operand::Const(Constant::I64(0)),
        }
    }
}
