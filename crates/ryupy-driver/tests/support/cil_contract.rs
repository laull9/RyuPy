//! 有界CIL结构读取：精确方法身份与指令序号先校验，再供各语言合同断言调用/操作码关系。
#![allow(dead_code)]
use std::collections::BTreeSet;

pub struct Cil<'a> {
    pub methods: Vec<Method<'a>>,
    pub fields: Vec<&'a str>,
}
pub struct Method<'a> {
    pub owner: &'a str,
    pub header: &'a str,
    pub name: &'a str,
    pub instructions: Vec<&'a str>,
    pub body: Vec<&'a str>,
    pub eh: Vec<&'a str>,
}
impl<'a> Cil<'a> {
    pub fn parse(text: &'a str) -> Self {
        let mut methods = Vec::new();
        let mut fields = Vec::new();
        let mut owner = None;
        let mut current: Option<Method<'a>> = None;
        let mut identities = BTreeSet::new();
        for line in text.lines() {
            if let Some(method) = current.as_mut() {
                method.body.push(line);
                if line.trim_start().starts_with("// EH:") {
                    method.eh.push(line.trim());
                }
            }
            if let Some(class) = line.strip_prefix(".class ") {
                assert!(current.is_none(), "类出现在未结束的方法中");
                owner = class.split_whitespace().last();
            } else if let Some(header) = line.strip_prefix("    .method ") {
                assert!(current.is_none(), "方法未结束");
                let (before, signature) = header.split_once('(').expect("方法签名");
                let name = before.split_whitespace().last().unwrap();
                let owner = owner.expect("方法必须有owner");
                let return_type = before.split_whitespace().rev().nth(1).expect("返回类型");
                let is_static = before.split_whitespace().any(|part| part == "static");
                assert!(
                    identities.insert((owner, name, signature, return_type, is_static)),
                    "重复方法描述符：{header}"
                );
                current = Some(Method {
                    owner,
                    header,
                    name,
                    instructions: vec![],
                    body: vec![],
                    eh: vec![],
                });
            } else if line == "    }" {
                if let Some(method) = current.take() {
                    methods.push(method);
                }
            } else if let Some(field) = line.strip_prefix("    .field ") {
                fields.push(field);
            } else if let Some(instruction) = line.trim_start().strip_prefix("IL_") {
                let (index, value) = instruction.split_once(": ").expect("指令格式");
                let method = current.as_mut().expect("指令必须属于方法");
                assert_eq!(
                    usize::from_str_radix(index, 16).unwrap(),
                    method.instructions.len(),
                    "IL位置不连续"
                );
                method.instructions.push(value);
            }
        }
        assert!(current.is_none(), "末尾方法未闭合");
        assert!(!methods.is_empty(), "CIL不能没有MethodDef");
        Self { methods, fields }
    }
    pub fn exact(&self, name: &str) -> &Method<'a> {
        let matches: Vec<_> = self.methods.iter().filter(|m| m.name == name).collect();
        assert_eq!(matches.len(), 1, "精确方法不唯一：{name}");
        matches[0]
    }
    pub fn methods_named(&self, name: &str) -> Vec<&Method<'a>> {
        let prefix = format!("m0_{name}$f");
        self.methods
            .iter()
            .filter(|m| {
                m.name.strip_prefix(&prefix).is_some_and(|tail| {
                    !tail.is_empty() && tail.bytes().all(|b| b.is_ascii_digit())
                })
            })
            .collect()
    }
    pub fn method(&self, name: &str) -> &Method<'a> {
        let matches = self.methods_named(name);
        assert_eq!(matches.len(), 1, "Python核心方法不唯一：{name}");
        matches[0]
    }
    pub fn runtime_count(&self, member: &str) -> usize {
        self.methods.iter().map(|m| m.runtime_count(member)).sum()
    }
}
impl Method<'_> {
    pub fn generated_call_count(&self, target: &Method<'_>) -> usize {
        let parameters = target
            .header
            .split_once('(')
            .unwrap()
            .1
            .strip_suffix(" cil managed")
            .unwrap();
        let reference = format!("{}.{}({parameters})", target.owner, target.name);
        self.instructions
            .iter()
            .filter(|s| s.starts_with("Call(") && s.ends_with(&reference))
            .count()
    }
    pub fn opcode_count(&self, opcode: &str) -> usize {
        self.instructions
            .iter()
            .filter(|s| s.split('(').next() == Some(opcode))
            .count()
    }
    pub fn runtime_count(&self, member: &str) -> usize {
        self.instructions
            .iter()
            .filter(|s| runtime_call(s, member))
            .count()
    }
    pub fn ordered_calls(&self, members: &[&str]) {
        let mut position = 0;
        for member in members {
            let next = self.instructions[position..]
                .iter()
                .position(|s| runtime_call(s, member))
                .unwrap_or_else(|| panic!("{}缺少有序调用{member}", self.name));
            position += next + 1;
        }
    }
}
fn runtime_call(instruction: &str, member: &str) -> bool {
    (instruction.starts_with("Call(") || instruction.starts_with("CallVirtual("))
        && instruction.contains(&format!("RyuPy.Runtime.{member}("))
}

#[test]
fn parser_rejects_duplicate_methods_and_missing_instruction_positions() {
    let method = "    .method public static void sample() cil managed\n    {\n        IL_0000: Return\n    }\n";
    let one = format!(".class public Owner\n{{\n{method}}}\n");
    assert_eq!(Cil::parse(&one).exact("sample").opcode_count("Return"), 1);
    assert!(
        std::panic::catch_unwind(|| {
            let _ = Cil::parse(&format!("{one}{one}"));
        })
        .is_err()
    );
    assert!(
        std::panic::catch_unwind(|| {
            let _ = Cil::parse(&one.replace("IL_0000", "IL_0001"));
        })
        .is_err()
    );
    let private = one.replace("public static", "private static");
    assert!(
        std::panic::catch_unwind(|| {
            let _ = Cil::parse(&format!("{one}{private}"));
        })
        .is_err()
    );
}
