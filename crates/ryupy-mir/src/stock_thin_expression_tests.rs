//! 通用薄体表达式的实际HIR→MIR门：动态协议、位置值调用与lease隔离共同验证。
use super::compile;
use crate::*;

#[test]
fn stock_thin_expressions_compile_independent_application_shapes() {
    for source in [
        include_str!("../../../tests/cases/stock_thin_compiler/numeric_pipeline.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/call_pipeline.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/operator_protocol.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/frame_boundaries.py"),
    ] {
        let (program, _) = compile(source);
        validate::validate(&program).unwrap();
        for (id, function) in program.functions.iter() {
            if id != program.entry {
                assert_eq!(function.core, FunctionCore::StockThinStatic);
                assert!(function.env.is_none() && function.signature_global.is_none());
            }
        }
    }
}

#[test]
fn stock_thin_expression_protocols_remain_dynamic_and_source_ordered() {
    let (program, _) = compile(
        "def f(x):\n    return target(first(x), second(x)) + 1267650600228229401496703205376\n",
    );
    let (_, function) = program
        .functions
        .iter()
        .find(|(_, f)| f.name == "f")
        .unwrap();
    let values: Vec<_> = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
        .collect();
    let names: Vec<_> = values
        .iter()
        .filter_map(|value| match value {
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::StockThinLoadGlobal),
                args,
            } => Some(args[1].clone()),
            _ => None,
        })
        .collect();
    assert_eq!(
        names,
        ["target", "first", "second"].map(|name| Operand::Const(Constant::Str(name.into())))
    );
    assert_eq!(
        values
            .iter()
            .filter(|v| matches!(v, Rvalue::DynCallValue { .. }))
            .count(),
        3
    );
    assert!(values.iter().any(|v| matches!(
        v,
        Rvalue::DynBinary {
            op: DynOp::Add,
            inplace: false,
            ..
        }
    )));
    assert!(values.iter().any(|v| matches!(
        v,
        Rvalue::Box {
            value: Operand::Const(Constant::BigInt(_)),
            ..
        } | Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::DynFromDecimal),
            ..
        }
    )));
    assert!(values.iter().all(|v| !matches!(
        v,
        Rvalue::Call {
            callee: Callee::Fn(_),
            ..
        } | Rvalue::MakeFunction { .. }
    )));
}

#[test]
fn stock_thin_value_call_never_receives_hidden_lease() {
    for mode in 0..3 {
        let (mut program, _) = compile("def f(x):\n    return x\n");
        let (_, function) = program
            .functions
            .iter_mut()
            .find(|(_, f)| f.name == "f")
            .unwrap();
        let x = function.params[0];
        let lease = function.module_state.unwrap();
        let value = match mode {
            0 => Rvalue::DynCallValue {
                callee: Operand::Local(x),
                args: vec![Operand::Local(lease)],
            },
            1 => Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CallValue),
                args: vec![
                    Operand::Local(x),
                    Operand::Array(vec![Operand::Array(vec![Operand::Local(lease)])]),
                    Operand::Array(vec![]),
                    Operand::Array(vec![]),
                ],
            },
            _ => Rvalue::InstanceZeroCall {
                callee: Operand::Local(x),
                frame: ZeroCallFrame {
                    class: SuperClassSource::Missing,
                    first_parameter: Some(lease),
                },
            },
        };
        function.body.blocks[function.body.entry]
            .stmts
            .insert(0, Statement::Eval(value));
        assert!(validate::validate(&program).is_err());
    }
}

#[test]
fn stock_thin_globals_value_call_never_borrows_context_or_lease() {
    let (program, _) = compile("def f(x):\n    return globals()\n");
    let (_, function) = program
        .functions
        .iter()
        .find(|(_, f)| f.name == "f")
        .unwrap();
    let values: Vec<_> = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
        .collect();
    assert!(values.iter().any(|v| matches!(v, Rvalue::InstanceZeroCall { frame, .. } if frame.class == SuperClassSource::Missing && frame.first_parameter == Some(function.params[0]))));
    assert!(!values.iter().any(|v| matches!(
        v,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceDirectGlobals),
            ..
        }
    )));
}
