//! 词法类 cell 的冷来源计划：运行值仍只用 CellNew/CellSet 和普通闭包 Env。
//! 计划供 MIR 校验与 link/生成器变换使用，不改变 PyFunction 布局或代码后端协议。
use crate::build::FunctionBuilder;
use crate::{BlockId, Callee, FnId, LocalId, MirType, RuntimeFn, Rvalue, Statement};

#[derive(Debug, Clone, PartialEq)]
pub struct ClassCellDefinition {
    /// 当前支持模块级 class；owner 明确指向实际模块执行函数，而非注册名称。
    pub owner: FnId,
    pub block: BlockId,
    pub cell: LocalId,
    pub original_class: LocalId,
    /// 类体合流后实际完成/发布块；发布值可以是class decorator替换后的对象。
    pub completion: BlockId,
    pub published_class: LocalId,
    /// 真实捕获或转发该 cell 的函数及环境槽，不包含同名普通局部 cell。
    pub captures: Vec<ClassCellCapture>,
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct ClassCellCapture {
    pub function: FnId,
    pub index: usize,
    pub source: ClassCellEnvSource,
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub enum ClassCellEnvSource {
    /// 函数对象的纯 cell Env 隐藏参数。
    Parameter,
    /// 暂停恢复只复用原 Env 引用；factory 是本恢复函数唯一可信的帧创建者。
    GeneratorFrame {
        factory: FnId,
        environment: LocalId,
        frame: LocalId,
        slot: usize,
    },
}

impl FunctionBuilder<'_> {
    /// 在每次实际类体执行前分配空 cell；不能在模块初始化或 MakeClass 里缓存它。
    pub(crate) fn begin_class_cell(
        &mut self,
        class: ryupy_hir::ClassId,
        original_class: LocalId,
    ) -> Option<LocalId> {
        if !self.hir.classes[class].has_class_cell {
            return None;
        }
        let cell = self.alloc_temp(MirType::Dynamic);
        self.locals[cell].is_cell = true;
        self.push_stmt(Statement::Assign {
            dest: cell,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellNew),
                args: Vec::new(),
            },
        });
        let captures = self
            .hir
            .functions
            .iter()
            .filter_map(|(function, definition)| {
                (definition.closure.class_cell == Some(class))
                    .then(|| {
                        definition
                            .closure
                            .free
                            .iter()
                            .position(|name| name.as_str() == "__class__")
                            .map(|index| ClassCellCapture {
                                function: self.fn_map[&function],
                                index,
                                source: ClassCellEnvSource::Parameter,
                            })
                    })
                    .flatten()
            })
            .collect();
        self.class_cells.push((
            self.class_map[&class],
            ClassCellDefinition {
                owner: self.fn_map[&self.current_hir_fn],
                block: self.current_bb,
                cell,
                original_class,
                completion: self.current_bb,
                published_class: original_class,
                captures,
            },
        ));
        Some(cell)
    }
}

/// 生成器恢复入口同样转发这个环境；显式记录帧来源，不按函数名后缀猜测。
pub(crate) fn register_resume(
    classes: &mut la_arena::Arena<crate::ClassDef>,
    factory: FnId,
    resume: FnId,
    environment: Option<LocalId>,
    frame: LocalId,
) {
    let Some(environment) = environment else {
        return;
    };
    for (_, class) in classes.iter_mut() {
        let Some(cell) = &mut class.class_cell else {
            continue;
        };
        let Some(capture) = cell
            .captures
            .iter()
            .find(|capture| capture.function == factory)
            .copied()
        else {
            continue;
        };
        cell.captures.push(ClassCellCapture {
            function: resume,
            index: capture.index,
            source: ClassCellEnvSource::GeneratorFrame {
                factory,
                environment,
                frame,
                slot: environment.into_raw().into_u32() as usize + 4,
            },
        });
    }
}
