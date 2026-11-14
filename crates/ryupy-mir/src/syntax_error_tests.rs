//! SyntaxError完整参数和真实类型值的结构门；独立命名空间不改变既有KeyError过滤集合。
use crate::module_delete_tests::compile;
use crate::*;

fn lookups(program: &Program) -> usize {
    program
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter(|stmt| {
            matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::LoadDefaultBuiltin),
                        ..
                    },
                    ..
                }
            )
        })
        .count()
}

#[test]
fn syntax_error_keeps_full_arguments_and_real_value_binding() {
    let program = compile(
        "a = SyntaxError('m', print('second'))\nE = SyntaxError\nb = E('m', print('alias'))\nSyntaxError(msg=print('keyword'))\n",
        false,
    );
    assert_eq!(lookups(&program), 3);
    let dump = pretty::dump(&program);
    assert!(
        !dump.contains("MakeException"),
        "定位/关键词实参不能被旧异常入口吞掉"
    );
    for marker in ["second", "alias", "keyword"] {
        assert!(dump.contains(marker), "{dump}");
    }
    let calls = program.functions.iter().flat_map(|(_, f)| f.body.blocks.iter()).flat_map(|(_, b)| &b.stmts)
        .filter(|s| matches!(s, Statement::Assign { value: Rvalue::DynCallValue { args, .. }, .. } if args.len() == 2)).count();
    assert_eq!(calls, 2, "direct和alias均保第二个表达式");
    assert!(program.functions.iter().flat_map(|(_, f)| f.body.blocks.iter()).flat_map(|(_, b)| &b.stmts)
        .any(|s| matches!(s, Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CallValue), args }, .. }
            if matches!(&args[2], Operand::Array(names) if !names.is_empty()))), "关键词调用必须进入完整CallValue");
    for source in [
        "def f(SyntaxError):\n    return SyntaxError('x')\n",
        "def replacement(value):\n    return value\nSyntaxError = replacement\nSyntaxError('x')\n",
    ] {
        assert_eq!(lookups(&compile(source, false)), 0);
    }
    let instance = compile("a = SyntaxError('x')\nb = SyntaxError\n", true);
    assert_eq!(lookups(&instance), 0);
    assert!(pretty::dump(&instance).contains("InstanceLoadName"));
    assert_eq!(lookups(&compile("raise SyntaxError\n", false)), 1);
    // 新族只能尾部追加，旧异常token的相对顺序不动。
    assert_eq!(
        &EXCEPTION_TYPE_NAMES[58..60],
        ["SyntaxError", "xml.etree.ElementTree.ParseError"]
    );
    assert_eq!(EXCEPTION_TYPE_NAMES[60], "binascii.Error");
}
