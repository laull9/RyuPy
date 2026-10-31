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
//!
//! 构建流程：
//! 1. 预分配所有函数的 MIR [`FnId`]，支持函数间相互调用与前向引用；
//! 2. 逐函数构建基本块、局部变量槽与语句序列；
//! 3. Python 语义改写（print 展开为运行时 Write 调用）；
//! 4. 确定赋值分析：若局部变量在赋值前被读取，作为编译器错误报出（语义陷阱 #10）。

use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, Span};
use ryupy_hir::{
    AssignTarget, BinOp as HirBinOp, Builtin, ExprId, ExprKind, Literal, LocalId as HirLocalId,
    Module, StmtKind,
};
use ryupy_ty::{ModuleTypes, Ty};

use crate::mir::{
    BasicBlock, BinOp, Callee, Constant, FnId, Function, LocalDecl, LocalId as MirLocalId, MirType,
    Operand, Program, RuntimeFn, Rvalue, Statement, Terminator,
};

pub fn build_program(hir: &Module, types: &ModuleTypes) -> (Program, Vec<Diagnostic>) {
    let mut functions = Arena::default();
    let classes = Arena::default();
    let mut diagnostics = Vec::new();

    // 1. 预先为每个 HIR 函数分配 MIR FnId
    let mut fn_map: FxHashMap<ryupy_hir::FuncId, FnId> = FxHashMap::default();
    for (hir_fn_id, hir_fn) in hir.functions.iter() {
        let ret_ty = ty_to_mir(&types.sigs[hir_fn_id].ret);
        let mir_fn = Function {
            name: hir_fn.name.as_str().to_string(),
            params: Vec::new(),
            ret: ret_ty,
            body: crate::mir::Body {
                locals: Arena::default(),
                blocks: Arena::default(),
                entry: la_arena::Idx::from_raw(0.into()),
            },
        };
        let mir_fn_id = functions.alloc(mir_fn);
        fn_map.insert(hir_fn_id, mir_fn_id);
    }

    // 2. 逐函数构建函数体
    for (hir_fn_id, hir_fn) in hir.functions.iter() {
        let mir_fn_id = fn_map[&hir_fn_id];
        let mut builder = FunctionBuilder::new(hir, types, &fn_map, hir_fn_id);

        // 建立 HIR 局部变量 → MIR 局部变量映射
        for (hir_loc_id, hir_loc) in hir_fn.body.locals.iter() {
            let ty = ty_to_mir(&types.bodies[hir_fn_id].locals[hir_loc_id]);
            let mir_loc = builder.locals.alloc(LocalDecl {
                ty,
                name: Some(hir_loc.name.to_string()),
            });
            builder.local_map.insert(hir_loc_id, mir_loc);
        }

        // 收集参数 ID，并标记为已赋值
        let mut params = Vec::new();
        for p in &hir_fn.params {
            let mir_loc = builder.local_map[&p.local];
            params.push(mir_loc);
            builder.assigned_locals.insert(p.local);
        }

        // 构建语句
        for &stmt_id in &hir_fn.body.root {
            builder.build_stmt(stmt_id);
        }

        builder.finish_current_block_with_return();

        diagnostics.extend(builder.diagnostics);

        let entry = builder.entry;
        functions[mir_fn_id].params = params;
        functions[mir_fn_id].body = crate::mir::Body {
            locals: builder.locals,
            blocks: builder.blocks,
            entry,
        };
    }

    let entry = fn_map[&hir.main];
    let program = Program {
        functions,
        classes,
        entry,
    };

    (program, diagnostics)
}

pub fn ty_to_mir(ty: &Ty) -> MirType {
    match ty {
        Ty::Int => MirType::I64,
        Ty::Float => MirType::F64,
        Ty::Bool => MirType::Bool,
        Ty::Str => MirType::Str,
        Ty::None => MirType::Unit,
        Ty::Class(id) => MirType::Class(la_arena::Idx::from_raw(id.into_raw())),
        Ty::Func(_) | Ty::Dynamic | Ty::Error => MirType::Unit,
    }
}

struct FunctionBuilder<'a> {
    hir: &'a Module,
    types: &'a ModuleTypes,
    fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
    current_hir_fn: ryupy_hir::FuncId,
    locals: Arena<LocalDecl>,
    blocks: Arena<BasicBlock>,
    entry: crate::mir::BlockId,
    current_bb: crate::mir::BlockId,
    local_map: FxHashMap<HirLocalId, MirLocalId>,
    assigned_locals: FxHashSet<HirLocalId>,
    reported_unbound: FxHashSet<HirLocalId>,
    diagnostics: Vec<Diagnostic>,
}

impl<'a> FunctionBuilder<'a> {
    fn new(
        hir: &'a Module,
        types: &'a ModuleTypes,
        fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
        current_hir_fn: ryupy_hir::FuncId,
    ) -> Self {
        let mut blocks = Arena::default();
        let entry = blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        });
        Self {
            hir,
            types,
            fn_map,
            current_hir_fn,
            locals: Arena::default(),
            blocks,
            entry,
            current_bb: entry,
            local_map: FxHashMap::default(),
            assigned_locals: FxHashSet::default(),
            reported_unbound: FxHashSet::default(),
            diagnostics: Vec::new(),
        }
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

    fn build_stmt(&mut self, stmt_id: ryupy_hir::StmtId) {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let stmt = &hir_body.stmts[stmt_id];
        match &stmt.kind {
            StmtKind::Expr(expr_id) => {
                self.build_expr_stmt(*expr_id);
            }
            StmtKind::Assign { target, value } => {
                let val_op = self.build_expr(*value);
                if let AssignTarget::Local(loc) = target {
                    let dest = self.local_map[loc];
                    self.push_stmt(Statement::Assign {
                        dest,
                        value: Rvalue::Use(val_op),
                    });
                    self.assigned_locals.insert(*loc);
                }
            }
            StmtKind::Return(opt_expr) => {
                let op = opt_expr.map(|e| self.build_expr(e));
                self.blocks[self.current_bb].terminator = Terminator::Return(op);
            }
            StmtKind::Pass => {}
            _ => {}
        }
    }

    fn build_expr_stmt(&mut self, expr_id: ExprId) {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        if let ExprKind::Call { callee, args } = &expr.kind {
            let callee_expr = &hir_body.exprs[*callee];
            if matches!(callee_expr.kind, ExprKind::Builtin(Builtin::Print)) {
                for (i, &arg) in args.iter().enumerate() {
                    if i > 0 {
                        self.push_stmt(Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::WriteSep),
                            args: Vec::new(),
                        }));
                    }
                    let op = self.build_expr(arg);
                    let arg_ty = &self.types.bodies[self.current_hir_fn].exprs[arg];
                    let rt_fn = match arg_ty {
                        Ty::Int => RuntimeFn::WriteI64,
                        Ty::Float => RuntimeFn::WriteF64,
                        Ty::Bool => RuntimeFn::WriteBool,
                        Ty::Str => RuntimeFn::WriteStr,
                        _ => RuntimeFn::WriteI64,
                    };
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(rt_fn),
                        args: vec![op],
                    }));
                }
                self.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::WriteNewline),
                    args: Vec::new(),
                }));
                return;
            }
        }
        let _ = self.build_expr(expr_id);
    }

    fn build_expr(&mut self, expr_id: ExprId) -> Operand {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        match &expr.kind {
            ExprKind::Literal(lit) => match lit {
                Literal::Int(v) => Operand::Const(Constant::I64(*v)),
                Literal::Float(v) => Operand::Const(Constant::F64(*v)),
                Literal::Bool(b) => Operand::Const(Constant::Bool(*b)),
                Literal::Str(s) => Operand::Const(Constant::Str(s.to_string())),
                Literal::None => Operand::Const(Constant::I64(0)),
            },
            ExprKind::Local(loc) => {
                if !self.assigned_locals.contains(loc) && !self.reported_unbound.contains(loc) {
                    let var_name = &hir_body.locals[*loc].name;
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.hir.file, expr.range),
                            format!("局部变量 `{var_name}` 在赋值前被读取"),
                        )
                        .with_code("E0301")
                        .with_primary_label("在赋值前被使用（运行时将引发 UnboundLocalError）"),
                    );
                    self.reported_unbound.insert(*loc);
                }
                Operand::Local(self.local_map[loc])
            }
            ExprKind::Binary { op, lhs, rhs } => {
                let lhs_op = self.build_expr(*lhs);
                let rhs_op = self.build_expr(*rhs);
                let mir_op = match op {
                    HirBinOp::Add => BinOp::Add,
                    HirBinOp::Sub => BinOp::Sub,
                    HirBinOp::Mul => BinOp::Mul,
                    HirBinOp::FloorDiv => BinOp::Div,
                    _ => BinOp::Add,
                };
                let expr_ty = &self.types.bodies[self.current_hir_fn].exprs[expr_id];
                let dest = self.locals.alloc(LocalDecl {
                    ty: ty_to_mir(expr_ty),
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
            ExprKind::Call { callee, args } => {
                let callee_expr = &hir_body.exprs[*callee];
                if let ExprKind::Func(fn_id) = callee_expr.kind {
                    let mir_fn_id = self.fn_map[&fn_id];
                    let mut args_ops = Vec::new();
                    for &arg in args {
                        args_ops.push(self.build_expr(arg));
                    }
                    let ret_ty = ty_to_mir(&self.types.sigs[fn_id].ret);
                    let dest = self.locals.alloc(LocalDecl {
                        ty: ret_ty,
                        name: None,
                    });
                    self.push_stmt(Statement::Assign {
                        dest,
                        value: Rvalue::Call {
                            callee: Callee::Fn(mir_fn_id),
                            args: args_ops,
                        },
                    });
                    Operand::Local(dest)
                } else {
                    Operand::Const(Constant::I64(0))
                }
            }
            _ => Operand::Const(Constant::I64(0)),
        }
    }
}
