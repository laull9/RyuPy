//! 模块捕获双局部的must来源证明；局部写入杀依赖，回边与EH入口不借未初始化事实。
//! 使用到达定义而非全函数唯一def，使生成器入口reload后重新建立的局部pair仍可验证。
use crate::mir::*;
use rustc_hash::FxHashMap;
use std::collections::VecDeque;

#[derive(Clone, PartialEq, Eq)]
enum Fact {
    Frozen,
    Flag(LocalId),
    Target {
        receiver: LocalId,
        flag: LocalId,
        name: String,
    },
}
#[derive(Clone, Default, PartialEq, Eq)]
struct Facts(FxHashMap<LocalId, Fact>);
impl Facts {
    fn kill(&mut self, local: LocalId) {
        self.0.retain(|id, fact| {
            *id != local
                && match fact {
                    Fact::Frozen => true,
                    Fact::Flag(receiver) => *receiver != local,
                    Fact::Target { receiver, flag, .. } => *receiver != local && *flag != local,
                }
        });
    }
    fn transfer(&mut self, f: &Function, statement: &Statement) {
        let Statement::Assign { dest, value } = statement else {
            return;
        };
        self.kill(*dest);
        let Some((_, declaration)) = f.body.locals.iter().find(|(id, _)| id == dest) else {
            return;
        };
        if declaration.is_cell
            || declaration.name.is_some()
            || f.params.contains(dest)
            || f.env == Some(*dest)
        {
            return;
        }
        let fact = match value {
            Rvalue::Use(_) if declaration.ty == MirType::Dynamic => Some(Fact::Frozen),
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IsModule),
                args,
            } if declaration.ty == MirType::Bool => match args.as_slice() {
                [Operand::Local(receiver)] if self.0.get(receiver) == Some(&Fact::Frozen) => {
                    Some(Fact::Flag(*receiver))
                }
                _ => None,
            },
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                args,
            } if declaration.ty == MirType::Dynamic => match args.as_slice() {
                [
                    Operand::Local(receiver),
                    Operand::Const(Constant::Str(name)),
                ] if self.0.get(receiver) == Some(&Fact::Frozen) => {
                    // 只能接同receiver已建立的条件；多个等价flag时任取会让验证依赖hash顺序，故拒歧义。
                    let mut flags = self
                        .0
                        .iter()
                        .filter_map(|(id, fact)| (fact == &Fact::Flag(*receiver)).then_some(*id));
                    match (flags.next(), flags.next()) {
                        (Some(flag), None) => Some(Fact::Target {
                            receiver: *receiver,
                            flag,
                            name: name.clone(),
                        }),
                        _ => None,
                    }
                }
                _ => None,
            },
            _ => None,
        };
        if let Some(fact) = fact {
            self.0.insert(*dest, fact);
        }
    }
    fn meet(&mut self, other: &Self) {
        self.0.retain(|id, fact| other.0.get(id) == Some(fact));
    }
    fn valid(&self, flag: &Operand, target: &Operand, name: &str) -> bool {
        let (Operand::Local(flag), Operand::Local(target)) = (flag, target) else {
            return false;
        };
        let Some(Fact::Target {
            receiver,
            flag: source_flag,
            name: source_name,
        }) = self.0.get(target)
        else {
            return false;
        };
        flag == source_flag
            && name == source_name
            && self.0.get(receiver) == Some(&Fact::Frozen)
            && self.0.get(flag) == Some(&Fact::Flag(*receiver))
    }
}

pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (_, f) in program.functions.iter() {
        let incoming = inputs(f);
        for (id, block) in f.body.blocks.iter() {
            let mut facts = incoming.get(&id).cloned().unwrap_or_default();
            for statement in &block.stmts {
                let value = match statement {
                    Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                    _ => None,
                };
                if let Some(value) = value {
                    if let Rvalue::DynCallMethod {
                        captured: Some(flag),
                        obj,
                        args,
                        ..
                    } = value
                    {
                        let valid_dest = matches!(statement, Statement::Assign { dest, .. }
                            if f.body.locals.iter().find(|(id, _)| id == dest).is_some_and(|(_, local)| local.ty == MirType::Dynamic));
                        if !valid_dest
                            || crate::validate::operand_type(program, f, flag)
                                != Some(MirType::Bool)
                            || crate::validate::operand_type(program, f, obj)
                                != Some(MirType::Dynamic)
                            || args.len() > 3
                            || args.iter().any(|arg| {
                                crate::validate::operand_type(program, f, arg)
                                    != Some(MirType::Dynamic)
                            })
                        {
                            errors.push(format!("函数 `{}` 的捕获方法固定ABI/结果槽错误", f.name));
                        }
                    }
                    let pair = match value {
                        Rvalue::DynCallMethod {
                            captured: Some(flag),
                            obj,
                            name,
                            ..
                        } => Some((flag, obj, name.as_str())),
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::CallCapturedMethod),
                            args,
                        } => match args.as_slice() {
                            [flag, obj, Operand::Const(Constant::Str(name)), _, _, _] => {
                                Some((flag, obj, name.as_str()))
                            }
                            _ => {
                                errors
                                    .push(format!("函数 `{}` 的模块捕获数组调用形态错误", f.name));
                                None
                            }
                        },
                        _ => None,
                    };
                    if let Some((flag, target, name)) = pair
                        && !facts.valid(flag, target, name)
                    {
                        errors.push(format!(
                            "函数 `{}` 的模块捕获标志/目标缺少同源冻结及支配证明",
                            f.name
                        ));
                    }
                }
                facts.transfer(f, statement);
            }
        }
    }
}
/// 冷EH入口保守按空集合；普通合流取交集。receiver/flag/target是激活局部，不因用户调用杀身份。
fn inputs(f: &Function) -> FxHashMap<BlockId, Facts> {
    let mut states = FxHashMap::default();
    let mut queue = VecDeque::new();
    for root in std::iter::once(f.body.entry).chain(f.body.regions.iter().map(|r| r.handler)) {
        states.insert(root, Facts::default());
        queue.push_back(root);
    }
    while let Some(id) = queue.pop_front() {
        let Some((_, block)) = f.body.blocks.iter().find(|(other, _)| *other == id) else {
            continue;
        };
        let mut out = states[&id].clone();
        for statement in &block.stmts {
            out.transfer(f, statement);
        }
        let edges = match &block.terminator {
            Terminator::Goto(target) => vec![*target],
            Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
            Terminator::Suspend { resume, input, .. } => {
                if let Some(input) = input {
                    out.kill(*input);
                }
                vec![*resume]
            }
            _ => vec![],
        };
        for target in edges {
            if let Some(old) = states.get_mut(&target) {
                let previous = old.clone();
                old.meet(&out);
                if *old != previous {
                    queue.push_back(target);
                }
            } else {
                states.insert(target, out.clone());
                queue.push_back(target);
            }
        }
    }
    states
}

/// 三个新helper只接受固定Call，不能用数组拼接形态绕ABI验证。
pub(crate) fn fixed_abi(runtime: RuntimeFn) -> bool {
    matches!(
        runtime,
        RuntimeFn::IsModule | RuntimeFn::CaptureModuleTarget | RuntimeFn::CallCapturedMethod
    )
}
