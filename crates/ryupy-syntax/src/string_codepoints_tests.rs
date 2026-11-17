//! 源码恢复的结构门：raw、转义奇偶、隐式拼接、私用码点碰撞与f-string/spec均保留原值。
use super::*;
fn expression(source: &str) -> ast::Expr {
    ruff_python_parser::parse_expression(source)
        .unwrap()
        .into_syntax()
        .body
        .as_ref()
        .clone()
}
#[test]
fn exact_lone_pairs_and_ordinary_scalars_remain_distinct() {
    for (source, expected) in [
        (r"'\ud800x'", vec![0xd800, 120]),
        (r"'\U0000dfff'", vec![0xdfff]),
        (r"'\ud83d' '\ude00'", vec![0xd83d, 0xde00]),
        (r"'😀\ud83d\ude00'", vec![0x1f600, 0xd83d, 0xde00]),
        (r"'\U000f0000\ud800\ufffd'", vec![0xf0000, 0xd800, 0xfffd]),
        (
            r"r'\ud800' '\ud800'",
            vec![92, 117, 100, 56, 48, 48, 0xd800],
        ),
    ] {
        let ast::Expr::StringLiteral(value) = expression(source) else {
            panic!("手写测试必须是字符串")
        };
        assert_eq!(
            string_codepoints(&value.value, source),
            Some(expected),
            "{source}"
        );
    }
    for source in [r"r'\ud800'", r"'\\ud800'", r"'\U0001f600'", r"'\ufffd'"] {
        let ast::Expr::StringLiteral(value) = expression(source) else {
            panic!("手写测试必须是字符串")
        };
        assert!(
            string_codepoints(&value.value, source).is_none(),
            "{source}"
        );
    }
}
#[test]
fn fstring_segments_and_specs_reuse_parser_brace_rules() {
    for source in [
        r"f'\ud800'",
        r"f'x\ud800{value}'",
        r"f'{{\ud800}}{value:\udc00}'",
        r"f'\ud83d' '\ude00'",
    ] {
        let ast::Expr::FString(value) = expression(source) else {
            panic!("手写测试必须是f-string")
        };
        let points = fstring_codepoints(&value, source);
        assert!(!points.is_empty(), "{source}");
        assert!(
            points
                .values()
                .flatten()
                .any(|point| *point == 0xd800 || *point == 0xd83d)
        );
    }
}
#[test]
fn repeated_surrogates_reuse_finite_placeholder_space() {
    let source = format!("'{}'", r"\ud800".repeat(140_000));
    let ast::Expr::StringLiteral(value) = expression(&source) else {
        panic!("手写测试必须是字符串")
    };
    assert_eq!(
        string_codepoints(&value.value, &source).unwrap().len(),
        140_000
    );
}
