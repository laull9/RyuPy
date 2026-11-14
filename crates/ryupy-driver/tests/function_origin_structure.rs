//! 固定Dynamic owner的真实源码结构门：保Static ABI，锁实际实例/defaults/thunk/生成器。
//! 不读取公开Python帧，不替代后续namedtuple消费结果；使用普通默认pipeline。
#[path = "function_origin_structure_support/mod.rs"]
mod support;
use ryupy_mir::{
    Callee, Constant, MirType, ModuleExport, Operand, Program, RuntimeFn, Rvalue, Statement,
};
use support::*;

fn assert_static_numeric(program: &Program) {
    for (name, count, ret) in [
        ("numeric_add", 2, MirType::I64),
        ("numeric_loop", 1, MirType::I64),
        ("numeric_direct", 2, MirType::I64),
        ("numeric_recursive", 1, MirType::I64),
        ("static_output", 1, MirType::Unit),
    ] {
        let (_, function) = function(program, name);
        assert!(
            function.dynamic_origin.is_none(),
            "Static不可具有owner: {name}"
        );
        // 只增加既有私有signature冷载体；不增加owner参数或逐调用对象。
        let push = trace_calls(function, RuntimeFn::TracePushWithOrigin);
        assert_eq!(push.len(), 1);
        assert_eq!(push[0].len(), 3);
        assert_eq!(
            push[0][2],
            Operand::Global(function.signature_global.unwrap())
        );
        assert_eq!(function.params.len(), count, "Static不可新增owner: {name}");
        assert_eq!(function.param_meta.len(), count);
        assert_eq!(function.ret, ret);
        assert!(
            function
                .params
                .iter()
                .all(|id| function.body.locals[*id].ty == MirType::I64)
        );
        assert!(trace_calls(function, RuntimeFn::TracePush).is_empty());
        if name != "static_output" {
            assert!(
                rvalues(function).all(|value| !matches!(
                    value,
                    Rvalue::DynCallValue { .. }
                        | Rvalue::DynCallMethod { .. }
                        | Rvalue::CallWithArgs { .. }
                        | Rvalue::DynBinary { .. }
                        | Rvalue::Box { .. }
                        | Rvalue::Unbox { .. }
                        | Rvalue::MakeFunction { .. }
                        | Rvalue::NewObject { .. }
                        | Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::LoadName | RuntimeFn::CallValue),
                            ..
                        }
                )),
                "数值路径不能添动态dispatch/捕获/装箱: {name}"
            );
        }
    }
    let (add, _) = function(program, "numeric_add");
    for name in ["numeric_loop", "numeric_direct"] {
        assert!(rvalues(function(program, name).1).any(
            |value| matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. } if *id == add)
        ));
    }
    let (recursive, body) = function(program, "numeric_recursive");
    assert!(rvalues(body).any(
        |value| matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. } if *id == recursive)
    ));
}
fn assert_static_cil(cil: &str) {
    for (name, signature) in [
        ("numeric_add", "long m0_numeric_add(long, long)"),
        ("numeric_loop", "long m0_numeric_loop(long)"),
        ("numeric_direct", "long m0_numeric_direct(long, long)"),
        ("numeric_recursive", "long m0_numeric_recursive(long)"),
        ("static_output", "void m0_static_output(long)"),
    ] {
        let body = cil_method(cil, &format!("m0_{name}"));
        assert!(
            body.lines().next().unwrap().contains(signature),
            "Static真实签名改变: {body}"
        );
        if name != "static_output" {
            for instruction in instructions(body) {
                assert!(
                    !instruction.starts_with("Box(")
                        && !instruction.starts_with("Unbox(")
                        && !instruction.starts_with("NewObject(")
                        && !instruction.starts_with("NewArray(")
                );
                if instruction.starts_with("Call(") {
                    assert!(
                        !instruction.contains("PyBinder.")
                            && !instruction.contains("PyOps.CallValue")
                            && !instruction.contains("PyOps.Box")
                            && !instruction.contains("PyOps.Dyn"),
                        "{instruction}"
                    );
                }
            }
        }
    }
    assert!(cil_method(cil, "m0_numeric_add").contains("AddOverflow(Signed)"));
    assert!(
        instructions(cil_method(cil, "m0_numeric_direct"))
            .iter()
            .any(|instruction| instruction
                .contains("Call(static long RyuPy.Generated.Module.m0_numeric_add(long, long))"))
    );
}

// 只从真实源码生成的实体取ID；普通函数与同名生成器resume通过signature区分。
fn assert_dynamic_shape(program: &Program, exports: &[ModuleExport]) {
    for name in [
        "dynamic_add",
        "target",
        "argument",
        "direct_with_argument",
        "through_alias",
        "factory",
        "nested",
        "stream",
    ] {
        let (_, body) = function(program, name);
        let origin = body.dynamic_origin.expect("Dynamic函数缺owner");
        assert_eq!(body.params.last(), Some(&origin.owner), "{name}");
        assert_eq!(body.body.locals[origin.owner].ty, MirType::Dynamic);
        assert_eq!(
            body.params.len(),
            body.param_meta.len() + usize::from(body.env.is_some()) + 1
        );
        if let Some(env) = body.env {
            assert_ne!(env, origin.owner);
            assert_eq!(body.params[body.params.len() - 2], env);
        }
        assert!(trace_calls(body, RuntimeFn::TracePush).is_empty());
        let push = trace_calls(body, RuntimeFn::TracePushWithOrigin);
        if name == "stream" {
            assert!(push.is_empty());
        } else {
            assert_eq!(push.len(), 1);
            assert_eq!(push[0].len(), 3);
            assert_eq!(local(&push[0][2]), origin.owner);
        }
        if let Some(global) = origin.source_global {
            assert!(
                exports.iter().all(|export| export.value != global),
                "私有来源不可导出"
            );
            assert_ne!(Some(global), body.function_global);
            assert_ne!(Some(global), body.defaults_global);
        }
    }
    let nested = function(program, "nested").1;
    assert!(nested.env.is_some());
    assert!(
        nested.dynamic_origin.unwrap().source_global.is_none(),
        "闭包实例不能借最新全局源"
    );
    let main = &program.functions[program.entry];
    assert!(main.params.is_empty() && main.dynamic_origin.is_none());
    let source = unique(
        main.body
            .blocks
            .iter()
            .flat_map(|(_, block)| &block.stmts)
            .filter_map(|stmt| match stmt {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                            args,
                        },
                } => {
                    let own = unique(program.globals.iter().filter_map(|(id, global)| {
                        matches!(&global.storage, ryupy_mir::GlobalStorage::CodeSource { entry, .. }
                            if *entry == program.entry).then_some(id)
                    }), "ModuleMain自己的代码来源");
                    assert_eq!(
                        args,
                        &vec![Operand::Global(own), Operand::Const(Constant::Null)]
                    );
                    Some(*dest)
                }
                _ => None,
            }),
        "每次模块执行的新来源",
    );
    let push = trace_calls(main, RuntimeFn::TracePushWithOrigin);
    assert_eq!(push.len(), 1);
    assert_eq!(local(&push[0][2]), source);
    assert!(trace_calls(main, RuntimeFn::TracePush).is_empty());
}
fn assert_direct_origin(program: &Program) {
    let (target_id, target) = function(program, "target");
    let (argument_id, _) = function(program, "argument");
    let body = function(program, "direct_with_argument").1;
    let source = target.dynamic_origin.unwrap().source_global.unwrap();
    // 这个直线源码的callee、参数、defaults与call必须在同一块，精确核相对顺序而非全局文本顺序。
    let block = unique(
        body.body
            .blocks
            .iter()
            .map(|(_, block)| block)
            .filter(|block| {
                assigned_calls(&block.stmts, Callee::Fn(target_id))
                    .next()
                    .is_some()
            }),
        "direct调用所在块",
    );
    let (freeze_at, frozen, _) = unique(
        assigned_calls(&block.stmts, Callee::Runtime(RuntimeFn::LoadName)).filter(
            |(_, _, args)| matches!(args.first(), Some(Operand::Global(id)) if *id == source),
        ),
        "实参前实际实例",
    );
    let (argument_at, argument_result, _) = unique(
        assigned_calls(&block.stmts, Callee::Fn(argument_id)),
        "参数回调",
    );
    let (defaults_at, defaults, defaults_args) = unique(
        assigned_calls(&block.stmts, Callee::Runtime(RuntimeFn::FunctionDefaults)),
        "同实例defaults",
    );
    assert_eq!(defaults_args.len(), 1);
    assert_eq!(local(&defaults_args[0]), frozen);
    let (get_at, extra, get_args) = unique(
        assigned_calls(&block.stmts, Callee::Runtime(RuntimeFn::ArrayGet)),
        "缺省extra",
    );
    assert_eq!(get_args.len(), 2);
    assert_eq!(local(&get_args[0]), defaults);
    assert!(matches!(get_args[1], Operand::Const(Constant::I64(1))));
    let (call_at, _, args) = unique(
        assigned_calls(&block.stmts, Callee::Fn(target_id)),
        "普通direct尾owner",
    );
    assert!(
        freeze_at < argument_at
            && argument_at < defaults_at
            && defaults_at < get_at
            && get_at < call_at
    );
    assert_eq!(args.len(), target.params.len());
    assert_eq!(local(&args[0]), argument_result);
    assert_eq!(local(&args[1]), extra);
    assert_eq!(local(args.last().unwrap()), frozen);
    assert_eq!(
        body.body
            .blocks
            .iter()
            .flat_map(|(_, block)| &block.stmts)
            .filter(|stmt| matches!(stmt, Statement::Assign { dest, .. } if *dest == frozen))
            .count(),
        1,
        "冻结local不能被重选"
    );
    assert_eq!(
        trace_calls(body, RuntimeFn::LoadName)
            .iter()
            .filter(|args| matches!(args.first(), Some(Operand::Global(id)) if *id == source))
            .count(),
        1
    );
    let alias = function(program, "through_alias").1;
    let (callee, args) = unique(
        rvalues(alias).filter_map(|value| match value {
            Rvalue::DynCallValue { callee, args } => Some((callee, args)),
            _ => None,
        }),
        "alias真实动态分派",
    );
    let _ = local(callee);
    assert_eq!(args.len(), 1);
    assert!(!rvalues(alias).any(
        |value| matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. } if *id == target_id)
    ));
    // 只锁本切私有槽合法发布，不重复Stage2的定义/注解/装饰器时序测试或大型CFG分析。
    let main = &program.functions[program.entry];
    let (stmts, at, fresh) = unique(
        main.body.blocks.iter().flat_map(|(_, block)| {
            block
                .stmts
                .iter()
                .enumerate()
                .filter_map(move |(at, stmt)| match stmt {
                    Statement::SetGlobal { dest, value } if *dest == source => {
                        Some((&block.stmts, at, local(value)))
                    }
                    _ => None,
                })
        }),
        "私有实例发布",
    );
    assert!(
        matches!(&stmts[at + 1], Statement::SetGlobal { dest, value: Operand::Local(value) }
        if Some(*dest) == target.function_global && *value == fresh)
    );
    assert!(stmts[..at].iter().any(
        |stmt| matches!(stmt, Statement::Assign { dest, value: Rvalue::MakeFunction { func, .. } }
        if *dest == fresh && *func == target_id)
    ));
}
fn assert_generator_origin(program: &Program) {
    let factory = function(program, "stream").1;
    let owner = factory.dynamic_origin.unwrap().owner;
    let (resume, frame, origin) = unique(
        rvalues(factory).filter_map(|value| match value {
            Rvalue::MakeGenerator {
                resume,
                frame,
                origin,
            } => Some((*resume, frame, origin)),
            _ => None,
        }),
        "生成器持有实际owner",
    );
    assert_eq!(local(origin.as_ref().unwrap()), owner);
    let slot = owner.into_raw().into_u32() as usize + 4;
    assert_eq!(local(&frame[slot]), owner);
    let resume = &program.functions[resume];
    assert!(resume.dynamic_origin.is_none() && resume.signature_global.is_none());
    assert_eq!(resume.params.len(), 1);
    let frame = resume.params[0];
    assert_eq!(resume.body.locals[frame].ty, MirType::Dynamic);
    assert!(trace_calls(resume, RuntimeFn::TracePush).is_empty());
    assert!(trace_calls(resume, RuntimeFn::TracePushWithOrigin).is_empty());
    let (_, restored, args) = unique(resume.body.blocks.iter().flat_map(|(_, block)| assigned_calls(&block.stmts, Callee::Runtime(RuntimeFn::ArrayGet))).filter(|(_, _, args)|
        matches!(args.get(1), Some(Operand::Const(Constant::I64(index))) if *index == slot as i64)), "恢复owner槽");
    assert_eq!(restored, owner);
    assert_eq!(local(&args[0]), frame);
    let saves: Vec<_> = trace_calls(resume, RuntimeFn::ArraySet).into_iter().filter(|args|
        matches!(args.get(1), Some(Operand::Const(Constant::I64(index))) if *index == slot as i64)).collect();
    assert_eq!(saves.len(), 2, "两次yield都保存owner");
    for args in saves {
        assert_eq!(local(&args[0]), frame);
        assert_eq!(local(&args[2]), owner);
    }
}
fn assert_dynamic_cil(linked: &ryupy_mir::LinkedProgram, cil: &str) {
    for name in ["target", "nested", "stream"] {
        let (index, (_, function)) = unique(
            linked
                .functions
                .iter()
                .filter(|(_, function)| function.signature_global.is_some())
                .enumerate()
                .filter(|(_, (_, function))| function.name == name),
            name,
        );
        let body = cil_method(cil, &format!("__thunk_{index}"));
        let code = instructions(body);
        let call_name = format!(" RyuPy.Generated.Module.{}(", function.codegen_name);
        let calls: Vec<_> = code
            .iter()
            .enumerate()
            .filter(|(_, instruction)| {
                instruction.starts_with("Call(") && instruction.contains(&call_name)
            })
            .map(|(at, _)| at)
            .collect();
        assert_eq!(calls.len(), 2, "快/慢thunk均调用真实core: {name}");
        for at in calls {
            assert_eq!(code[at - 1], "LoadArgument(0)", "owner必须实际self: {name}");
            if function.env.is_some() {
                assert!(code[at - 2].contains("PyFunction.get_Env()"));
                assert_eq!(code[at - 3], "LoadArgument(0)");
            }
        }
        let defaults = unique(
            code.iter()
                .enumerate()
                .filter(|(_, instruction)| instruction.contains("PyFunction.get_Defaults()")),
            "thunk实例defaults",
        )
        .0;
        assert_eq!(code[defaults - 1], "LoadArgument(0)");
        if let Some(source) = function.dynamic_origin.unwrap().source_global {
            let field = format!(".{}", linked.globals[source].name);
            assert!(
                !code
                    .iter()
                    .any(|instruction| instruction.starts_with("LoadStaticField(")
                        && instruction.contains(&field)),
                "thunk不可查最新私有实例"
            );
        }
    }
    let (_, factory) = unique(
        linked.functions.iter().filter(|(_, function)| {
            function.name == "stream" && function.signature_global.is_some()
        }),
        "链接后生成器工厂",
    );
    let code = instructions(cil_method(cil, &factory.codegen_name));
    let (at, ctor) = unique(
        code.iter().enumerate().filter(|(_, instruction)| {
            instruction.starts_with("NewObject(") && instruction.contains("PyGenerator..ctor(")
        }),
        "五参生成器构造",
    );
    assert!(ctor.ends_with(", object[], string, string, object))"));
    assert_eq!(
        code[at - 1],
        format!("LoadArgument({})", factory.params.len() - 1)
    );
    let resume = unique(
        rvalues(factory).filter_map(|value| match value {
            Rvalue::MakeGenerator { resume, .. } => Some(*resume),
            _ => None,
        }),
        "链接后resume",
    );
    let signature = cil_method(cil, &linked.functions[resume].codegen_name)
        .lines()
        .next()
        .unwrap();
    assert!(signature.contains(&format!(
        " {}([System.Runtime]System.Object)",
        linked.functions[resume].codegen_name
    )));
}

#[test]
fn dynamic_function_origin_preserves_static_abi_and_actual_instance() {
    let root = root();
    let (program, exports) = compile(&root);
    assert_static_numeric(&program);
    assert_dynamic_shape(&program, &exports);
    assert_direct_origin(&program);
    assert_generator_origin(&program);
    let (linked, cil) = cil(&root, program, exports);
    assert_static_cil(&cil);
    assert_dynamic_cil(&linked, &cil);
}
