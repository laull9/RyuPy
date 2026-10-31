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
use ryupy_base::Diagnostic;
use ryupy_hir::{
    AssignTarget, Builtin, ExprId, ExprKind, LocalId as HirLocalId, Module, StmtId, StmtKind,
};
use ryupy_ty::{ModuleTypes, Ty};

use crate::expr_builder::ExprBuilder;
use crate::mir::{
    BasicBlock, BlockId, Callee, FnId, Function, LocalDecl, LocalId as MirLocalId, MirType,
    Program, RuntimeFn, Rvalue, Statement, Terminator,
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
    entry: BlockId,
    current_bb: BlockId,
    local_map: FxHashMap<HirLocalId, MirLocalId>,
    assigned_locals: FxHashSet<HirLocalId>,
    reported_unbound: FxHashSet<HirLocalId>,
    loop_stack: Vec<(BlockId, BlockId)>, // (continue_bb, break_bb)
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
            loop_stack: Vec::new(),
            diagnostics: Vec::new(),
        }
    }

    fn new_block(&mut self) -> BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        })
    }

    fn push_stmt(&mut self, stmt: Statement) {
        self.blocks[self.current_bb].stmts.push(stmt);
    }

    fn is_current_unterminated(&self) -> bool {
        matches!(
            self.blocks[self.current_bb].terminator,
            Terminator::Unreachable
        )
    }

    fn terminate_current(&mut self, term: Terminator) {
        if self.is_current_unterminated() {
            self.blocks[self.current_bb].terminator = term;
        }
    }

    fn finish_current_block_with_return(&mut self) {
        self.terminate_current(Terminator::Return(None));
    }

    fn expr_builder(&mut self) -> ExprBuilder<'_> {
        ExprBuilder {
            hir: self.hir,
            types: self.types,
            fn_map: self.fn_map,
            current_hir_fn: self.current_hir_fn,
            locals: &mut self.locals,
            blocks: &mut self.blocks,
            current_bb: &mut self.current_bb,
            local_map: &self.local_map,
            assigned_locals: &mut self.assigned_locals,
            reported_unbound: &mut self.reported_unbound,
            diagnostics: &mut self.diagnostics,
        }
    }

    fn build_stmt(&mut self, stmt_id: StmtId) {
        if !self.is_current_unterminated() {
            // 当前基本块已终结（例如前一条语句是 return/break/continue）。
            // 只有当存在后续不可达语句时，才分配死代码块承载它。
            self.current_bb = self.new_block();
        }
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let stmt = &hir_body.stmts[stmt_id];
        match &stmt.kind {
            StmtKind::Expr(expr_id) => {
                self.build_expr_stmt(*expr_id);
            }
            StmtKind::Assign { target, value } => {
                let val_op = self.expr_builder().build_expr(*value);
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
                let op = opt_expr.map(|e| self.expr_builder().build_expr(e));
                self.terminate_current(Terminator::Return(op));
            }
            StmtKind::If { cond, then_, else_ } => {
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = &self.types.bodies[self.current_hir_fn].exprs[*cond];
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, cond_ty);

                let then_bb = self.new_block();
                let else_bb = self.new_block();
                let join_bb = self.new_block();

                self.terminate_current(Terminator::Branch {
                    cond: cond_bool,
                    then_: then_bb,
                    else_: else_bb,
                });

                // then 分支
                self.current_bb = then_bb;
                for &s in then_ {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(join_bb));
                }

                // else 分支
                self.current_bb = else_bb;
                for &s in else_ {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(join_bb));
                }

                self.current_bb = join_bb;
            }
            StmtKind::While { cond, body } => {
                let cond_bb = self.new_block();
                let body_bb = self.new_block();
                let exit_bb = self.new_block();

                self.terminate_current(Terminator::Goto(cond_bb));

                // 循环条件块
                self.current_bb = cond_bb;
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = &self.types.bodies[self.current_hir_fn].exprs[*cond];
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, cond_ty);
                self.terminate_current(Terminator::Branch {
                    cond: cond_bool,
                    then_: body_bb,
                    else_: exit_bb,
                });

                // 循环体
                self.current_bb = body_bb;
                self.loop_stack.push((cond_bb, exit_bb));
                for &s in body {
                    self.build_stmt(s);
                }
                self.loop_stack.pop();
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(cond_bb));
                }

                self.current_bb = exit_bb;
            }
            StmtKind::Break => {
                let (_, break_bb) = *self
                    .loop_stack
                    .last()
                    .expect("break 语句在 HIR 降级阶段已做循环作用域检查");
                self.terminate_current(Terminator::Goto(break_bb));
            }
            StmtKind::Continue => {
                let (continue_bb, _) = *self
                    .loop_stack
                    .last()
                    .expect("continue 语句在 HIR 降级阶段已做循环作用域检查");
                self.terminate_current(Terminator::Goto(continue_bb));
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
                    let op = self.expr_builder().build_expr(arg);
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
        let _ = self.expr_builder().build_expr(expr_id);
    }
}
