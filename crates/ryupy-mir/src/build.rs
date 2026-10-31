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
    AssignTarget, Builtin, ExprId, ExprKind, LocalId as HirLocalId, Module, StmtId, StmtKind,
};
use ryupy_ty::{ModuleTypes, Ty, type_ref_to_ty};

use crate::expr_builder::ExprBuilder;
use crate::mir::{
    BasicBlock, BinOp, BlockId, Callee, Constant, FnId, Function, LocalDecl, LocalId as MirLocalId,
    MirType, Operand, Program, RuntimeFn, Rvalue, Statement, Terminator, UnaryOp,
};

pub fn build_program(hir: &Module, types: &ModuleTypes) -> (Program, Vec<Diagnostic>) {
    let mut functions = Arena::default();
    let mut classes = Arena::default();
    let mut diagnostics = Vec::new();

    // 0. 构建 MIR 类的类型与字段定义
    for (_cls_id, cls) in hir.classes.iter() {
        let fields = cls
            .fields
            .iter()
            .map(|f| {
                let ty = type_ref_to_ty(&f.annotation);
                (f.name.as_str().to_string(), ty_to_mir(&ty))
            })
            .collect();
        classes.alloc(crate::mir::ClassDef {
            name: cls.name.as_str().to_string(),
            fields,
            is_value_type: cls.frozen,
        });
    }

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
            let hir_ty = types
                .body(hir_fn_id)
                .map(|b| b.local_ty(hir_loc_id))
                .unwrap_or(Ty::Dynamic);
            let ty = ty_to_mir(&hir_ty);
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
        Ty::Dynamic => MirType::Dynamic,
        // 函数对象与毒化类型都不该走到 codegen；用 Dynamic 兜底而不是 Unit，
        // 避免把「有值」错当成 void。
        Ty::Func(_) | Ty::Error => MirType::Dynamic,
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
                match target {
                    AssignTarget::Local(loc) => {
                        let dest = self.local_map[loc];
                        let val_ty = self.expr_builder().ty_of(*value);
                        self.expr_builder().assign_value(dest, val_op, &val_ty);
                        self.assigned_locals.insert(*loc);
                    }
                    AssignTarget::Attribute { base, name } => {
                        let base_op = self.expr_builder().build_expr(*base);
                        let base_ty = self.expr_builder().ty_of(*base);
                        if let Ty::Class(cls_id) = base_ty {
                            let cls = &self.hir.classes[cls_id];
                            if let Some((field_idx, _)) =
                                cls.fields.iter().enumerate().find(|(_, f)| f.name == *name)
                            {
                                self.push_stmt(Statement::SetField {
                                    obj: base_op,
                                    field: field_idx as u32,
                                    value: val_op,
                                });
                            }
                        }
                    }
                }
            }
            StmtKind::Return(opt_expr) => {
                let ret_ty = ty_to_mir(&self.types.sigs[self.current_hir_fn].ret);
                let op = opt_expr.map(|e| {
                    let value_ty = self.expr_builder().ty_of(e);
                    let raw = self.expr_builder().build_expr(e);
                    self.expr_builder().coerce(raw, &value_ty, &ret_ty)
                });
                self.terminate_current(Terminator::Return(op));
            }
            StmtKind::If { cond, then_, else_ } => {
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = self.expr_builder().ty_of(*cond);
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, &cond_ty);

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
                let cond_ty = self.expr_builder().ty_of(*cond);
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, &cond_ty);
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
            StmtKind::ForRange {
                var,
                start,
                stop,
                step,
                body,
            } => {
                self.build_for_range(*var, *start, *stop, *step, body);
            }
            StmtKind::Pass => {}
        }
    }

    /// 动态函数里的 `for x in range(...)`：边界先拆箱成 i64，循环变量仍是动态值。
    ///
    /// 好处是保留计数循环的快路径，不让 `range` 退化成迭代器；代价是边界必须是真整数，
    /// 与 CPython 的 `TypeError: 'float' object cannot be interpreted as an integer` 一致。
    fn build_for_range_dynamic(
        &mut self,
        var: HirLocalId,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: &[StmtId],
    ) {
        let start_op = self.expr_builder().build_expr(start);
        let stop_op = self.expr_builder().build_expr(stop);
        let step_op = match step {
            Some(s) => self.expr_builder().build_expr(s),
            None => Operand::Const(Constant::I64(1)),
        };
        let start_ty = self.expr_builder().ty_of(start);
        let stop_ty = self.expr_builder().ty_of(stop);
        let step_ty = step
            .map(|s| self.expr_builder().ty_of(s))
            .unwrap_or(Ty::Int);

        // 边界拆箱到 i64；步长额外检查 0（Python 在 range 构造时抛 ValueError）
        let start_i = self
            .expr_builder()
            .coerce(start_op, &start_ty, &MirType::I64);
        let stop_i = self.expr_builder().coerce(stop_op, &stop_ty, &MirType::I64);
        let step_i = if step.is_some() {
            let raw = self
                .expr_builder()
                .coerce(step_op, &step_ty, &MirType::Dynamic);
            let dest = self.expr_builder().alloc_temp(MirType::I64);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::RangeStep),
                    args: vec![raw],
                },
            });
            Operand::Local(dest)
        } else {
            Operand::Const(Constant::I64(1))
        };

        // counter = start；循环变量在每轮开头装箱赋值
        let counter = self.expr_builder().alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: counter,
            value: Rvalue::Use(start_i),
        });
        let stop_local = self.expr_builder().alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: stop_local,
            value: Rvalue::Use(stop_i),
        });
        let step_local = self.expr_builder().alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: step_local,
            value: Rvalue::Use(step_i),
        });

        let var_mir = self.local_map[&var];
        self.assigned_locals.insert(var);

        let cond_bb = self.new_block();
        let body_bb = self.new_block();
        let step_bb = self.new_block();
        let exit_bb = self.new_block();
        self.terminate_current(Terminator::Goto(cond_bb));

        // cond: (step > 0 && counter < stop) || (step < 0 && counter > stop)
        self.current_bb = cond_bb;
        let step_pos = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: step_pos,
            value: Rvalue::Binary {
                op: BinOp::Gt,
                lhs: Operand::Local(step_local),
                rhs: Operand::Const(Constant::I64(0)),
            },
        });
        let lt = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: lt,
            value: Rvalue::Binary {
                op: BinOp::Lt,
                lhs: Operand::Local(counter),
                rhs: Operand::Local(stop_local),
            },
        });
        let gt = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: gt,
            value: Rvalue::Binary {
                op: BinOp::Gt,
                lhs: Operand::Local(counter),
                rhs: Operand::Local(stop_local),
            },
        });
        let positive = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: positive,
            value: Rvalue::Binary {
                op: BinOp::BitAnd,
                lhs: Operand::Local(step_pos),
                rhs: Operand::Local(lt),
            },
        });
        let negative_step = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: negative_step,
            value: Rvalue::Unary {
                op: UnaryOp::Not,
                operand: Operand::Local(step_pos),
            },
        });
        let negative = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: negative,
            value: Rvalue::Binary {
                op: BinOp::BitAnd,
                lhs: Operand::Local(negative_step),
                rhs: Operand::Local(gt),
            },
        });
        let cond = self.expr_builder().alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: cond,
            value: Rvalue::Binary {
                op: BinOp::BitOr,
                lhs: Operand::Local(positive),
                rhs: Operand::Local(negative),
            },
        });
        self.terminate_current(Terminator::Branch {
            cond: Operand::Local(cond),
            then_: body_bb,
            else_: exit_bb,
        });

        // 循环体：先把 counter 装箱写进用户变量
        self.current_bb = body_bb;
        let boxed = self.expr_builder().alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: boxed,
            value: Rvalue::Box {
                value: Operand::Local(counter),
                from: MirType::I64,
            },
        });
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Use(Operand::Local(boxed)),
        });
        self.loop_stack.push((step_bb, exit_bb));
        for &s in body {
            self.build_stmt(s);
        }
        self.loop_stack.pop();
        if self.is_current_unterminated() {
            self.terminate_current(Terminator::Goto(step_bb));
        }

        // 步长块
        self.current_bb = step_bb;
        self.push_stmt(Statement::Assign {
            dest: counter,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(counter),
                rhs: Operand::Local(step_local),
            },
        });
        self.terminate_current(Terminator::Goto(cond_bb));

        self.current_bb = exit_bb;
    }

    fn build_for_range(
        &mut self,
        var: HirLocalId,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: &[StmtId],
    ) {
        if self.types.is_dynamic(self.current_hir_fn) {
            self.build_for_range_dynamic(var, start, stop, step, body);
            return;
        }

        // 1. 求值起始、终止和步长操作数
        let start_op = self.expr_builder().build_expr(start);
        let stop_op = self.expr_builder().build_expr(stop);
        let step_op = match step {
            Some(s) => self.expr_builder().build_expr(s),
            None => Operand::Const(Constant::I64(1)),
        };

        // 步长为常量 0 时，编译期报错（语义陷阱 #9）
        if let Operand::Const(Constant::I64(0)) = step_op {
            let range = self.hir.functions[self.current_hir_fn].body.exprs[step.unwrap()].range;
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.hir.file, range),
                    "range() arg 3 must not be zero",
                )
                .with_code("E0302")
                .with_primary_label("步长不能为 0"),
            );
        }

        let var_mir = self.local_map[&var];
        self.assigned_locals.insert(var);

        // var = start
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Use(start_op),
        });

        // 2. 基本块创建
        let cond_bb = self.new_block();
        let body_bb = self.new_block();
        let step_bb = self.new_block();
        let exit_bb = self.new_block();

        self.terminate_current(Terminator::Goto(cond_bb));

        // 3. 条件块 (cond_bb)
        self.current_bb = cond_bb;
        // 条件判断：
        // 若 step 为常量 > 0：var < stop
        // 若 step 为常量 < 0：var > stop
        // 若 step 为非常量：(step > 0 && var < stop) || (step < 0 && var > stop)
        let cond_local = self.expr_builder().alloc_temp(MirType::Bool);
        match step_op {
            Operand::Const(Constant::I64(s)) if s > 0 => {
                self.push_stmt(Statement::Assign {
                    dest: cond_local,
                    value: Rvalue::Binary {
                        op: BinOp::Lt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op,
                    },
                });
            }
            Operand::Const(Constant::I64(s)) if s < 0 => {
                self.push_stmt(Statement::Assign {
                    dest: cond_local,
                    value: Rvalue::Binary {
                        op: BinOp::Gt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op,
                    },
                });
            }
            _ => {
                // 动态步长：判断 step > 0 ? var < stop : var > stop
                let is_pos_temp = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: is_pos_temp,
                    value: Rvalue::Binary {
                        op: BinOp::Gt,
                        lhs: step_op.clone(),
                        rhs: Operand::Const(Constant::I64(0)),
                    },
                });
                let cond_lt = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: cond_lt,
                    value: Rvalue::Binary {
                        op: BinOp::Lt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op.clone(),
                    },
                });
                let cond_gt = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: cond_gt,
                    value: Rvalue::Binary {
                        op: BinOp::Gt,
                        lhs: Operand::Local(var_mir),
                        rhs: stop_op,
                    },
                });
                let pos_cond = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: pos_cond,
                    value: Rvalue::Binary {
                        op: BinOp::BitAnd,
                        lhs: Operand::Local(is_pos_temp),
                        rhs: Operand::Local(cond_lt),
                    },
                });
                let not_pos = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: not_pos,
                    value: Rvalue::Unary {
                        op: UnaryOp::Not,
                        operand: Operand::Local(is_pos_temp),
                    },
                });
                let neg_cond = self.expr_builder().alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest: neg_cond,
                    value: Rvalue::Binary {
                        op: BinOp::BitAnd,
                        lhs: Operand::Local(not_pos),
                        rhs: Operand::Local(cond_gt),
                    },
                });
                self.push_stmt(Statement::Assign {
                    dest: cond_local,
                    value: Rvalue::Binary {
                        op: BinOp::BitOr,
                        lhs: Operand::Local(pos_cond),
                        rhs: Operand::Local(neg_cond),
                    },
                });
            }
        }

        self.terminate_current(Terminator::Branch {
            cond: Operand::Local(cond_local),
            then_: body_bb,
            else_: exit_bb,
        });

        // 4. 循环体 (body_bb)
        self.current_bb = body_bb;
        // 关键：continue 跳到 step_bb，break 跳到 exit_bb
        self.loop_stack.push((step_bb, exit_bb));
        for &s in body {
            self.build_stmt(s);
        }
        self.loop_stack.pop();
        if self.is_current_unterminated() {
            self.terminate_current(Terminator::Goto(step_bb));
        }

        // 5. 步长递增块 (step_bb)
        self.current_bb = step_bb;
        self.push_stmt(Statement::Assign {
            dest: var_mir,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(var_mir),
                rhs: step_op,
            },
        });
        self.terminate_current(Terminator::Goto(cond_bb));

        // 6. 退出块 (exit_bb)
        self.current_bb = exit_bb;
    }

    fn build_expr_stmt(&mut self, expr_id: ExprId) {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        if let ExprKind::Call { callee, args } = &expr.kind {
            let callee_expr = &hir_body.exprs[*callee];
            if matches!(callee_expr.kind, ExprKind::Builtin(Builtin::Print)) {
                let dynamic = self.types.is_dynamic(self.current_hir_fn);
                for (i, &arg) in args.iter().enumerate() {
                    if i > 0 {
                        self.push_stmt(Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::WriteSep),
                            args: Vec::new(),
                        }));
                    }
                    let op = self.expr_builder().build_expr(arg);
                    let arg_ty = self.expr_builder().ty_of(arg);
                    // 动态函数里编译期不知道类型，统一交给 WriteObject（等价 print(str(v))）
                    let rt_fn = if dynamic {
                        RuntimeFn::WriteObject
                    } else {
                        match arg_ty {
                            Ty::Int => RuntimeFn::WriteI64,
                            Ty::Float => RuntimeFn::WriteF64,
                            Ty::Bool => RuntimeFn::WriteBool,
                            Ty::Str => RuntimeFn::WriteStr,
                            _ => RuntimeFn::WriteI64,
                        }
                    };
                    let op = if dynamic {
                        self.expr_builder().coerce(op, &arg_ty, &MirType::Dynamic)
                    } else {
                        op
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
