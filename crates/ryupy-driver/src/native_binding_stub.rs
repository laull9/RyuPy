//! 从已验证BindingPlan导出可用模块stub和机器合同；保留原名/别名/范围/借用证据。
//! 生成物只写本次独占发布树，posonly形状与真实绑定器一致，名称不适于def时使用typing hook。
use super::BindingPlan;
use anyhow::{Context, Result, ensure};
use ryupy_mir::native_abi::{AbiParameter, AbiSignature, AbiType};
use serde_json::{Value, json};
use std::collections::HashSet;
use std::path::Path;

const ROOT: &str = "_internal/interfaces/v1";

impl BindingPlan {
    fn interface_paths(&self) -> (String, String) {
        let module = self.module.replace('.', "/");
        (
            format!("{ROOT}/{module}.pyi"),
            format!("{ROOT}/{module}.abi.json"),
        )
    }

    pub(crate) fn interface_metadata(&self) -> Value {
        let (stub, machine) = self.interface_paths();
        json!({"schema":1,"module":self.module,"search_root":ROOT,"python_stub":stub,
            "machine_contract":machine,"source":"validated_binding_plan_and_verified_pinvoke_report",
            "artifact_roles":{"python_stub":"native_binding_python_stub","machine_contract":"native_binding_machine_contract"},
            "runtime_validation":"strict_conversions_ranges_and_borrowing_remain_authoritative"})
    }

    pub(crate) fn interface_artifact_role(&self, path: &str) -> Option<&'static str> {
        let (stub, machine) = self.interface_paths();
        if path == stub {
            Some("native_binding_python_stub")
        } else if path == machine {
            Some("native_binding_machine_contract")
        } else {
            None
        }
    }

    pub(crate) fn mark_interfaces(&self, artifacts: &mut [crate::manifest_files::FileDigest]) {
        for artifact in artifacts {
            if let Some(role) = self.interface_artifact_role(&artifact.relative_path) {
                artifact.role = role.into();
            }
        }
    }

    pub(crate) fn write_interfaces(&self, bundle: &Path) -> Result<()> {
        let (stub, machine) = render(self)?;
        let (stub_path, machine_path) = self.interface_paths();
        for (relative, bytes) in [
            (stub_path, stub.into_bytes()),
            (machine_path, serde_json::to_vec_pretty(&machine)?),
        ] {
            crate::manifest_files::safe_relative(&relative)?;
            let path = bundle.join(relative);
            std::fs::create_dir_all(path.parent().context("接口文件缺少父目录")?)
                .with_context(|| format!("绑定接口目录不可创建：{}", path.display()))?;
            // 构建事务拥有此树；拒绝覆盖同次其他生成器的文件，失败交既有事务统一回滚。
            use std::io::Write;
            std::fs::OpenOptions::new()
                .write(true)
                .create_new(true)
                .open(&path)
                .with_context(|| format!("绑定接口目标冲突或不可写：{}", path.display()))?
                .write_all(&bytes)?;
        }
        Ok(())
    }
}

fn signature(plan: &BindingPlan, index: usize) -> AbiSignature {
    plan.signatures[index]
        .clone()
        .unwrap_or_else(|| AbiSignature {
            parameters: ["left", "right"]
                .map(|name| AbiParameter {
                    name: name.into(),
                    ty: AbiType::I32,
                    type_name: "i32".into(),
                })
                .into(),
            returns: AbiType::I64,
        })
}

fn render(plan: &BindingPlan) -> Result<(String, Value)> {
    ensure!(
        plan.exports.len() == plan.signatures.len(),
        "绑定接口签名数量不符"
    );
    let signatures: Vec<_> = (0..plan.exports.len())
        .map(|index| signature(plan, index))
        .collect();
    let mut used: HashSet<String> = plan
        .exports
        .iter()
        .map(|export| export.name.clone())
        .chain(
            signatures
                .iter()
                .flat_map(|s| s.parameters.iter().map(|p| p.name.clone())),
        )
        .collect();
    let builtins = unique("_ryupy_builtins", &mut used);
    let typing = unique("_ryupy_typing", &mut used);
    let mut text = format!(
        "# RyuPy已验证C ABI绑定自动导出；机器范围、类型严格性和借用规则见同名.abi.json。\nimport builtins as {builtins}\nimport typing as {typing}\n\n"
    );
    let special: Vec<_> = plan
        .exports
        .iter()
        .enumerate()
        .filter(|(_, e)| !def_name(&e.name))
        .collect();
    let hook_count =
        special.len() + usize::from(plan.exports.iter().any(|e| e.name == "__getattr__"));
    let mut exports = Vec::new();
    for (index, (export, signature)) in plan.exports.iter().zip(&signatures).enumerate() {
        let mut names: HashSet<_> = signature
            .parameters
            .iter()
            .filter(|p| def_name(&p.name))
            .map(|p| p.name.clone())
            .collect();
        let parameters: Vec<_> = signature
            .parameters
            .iter()
            .enumerate()
            .map(|(slot, parameter)| {
                let name = if def_name(&parameter.name) {
                    parameter.name.clone()
                } else {
                    unique(&format!("_p{slot}"), &mut names)
                };
                names.insert(name.clone());
                (name, parameter)
            })
            .collect();
        let return_type = python_type(signature.returns, &builtins);
        if def_name(&export.name) {
            if export.name == "__getattr__" && hook_count > 1 {
                text.push_str(&format!("@{typing}.overload\n"));
            }
            let arguments = parameters
                .iter()
                .map(|(name, parameter)| {
                    format!("{name}: {}", python_type(parameter.ty, &builtins))
                })
                .collect::<Vec<_>>()
                .join(", ");
            text.push_str(&format!(
                "def {}({arguments}{}) -> {return_type}: ...\n\n",
                export.name,
                if parameters.is_empty() { "" } else { ", /" }
            ));
        }
        let aliases: Vec<_> = plan
            .exports
            .iter()
            .enumerate()
            .filter(|(other, e)| *other != index && e.symbol == export.symbol)
            .map(|(_, e)| &e.name)
            .collect();
        exports.push(json!({"binding_id":format!("b{index}"),"module":plan.module,"export":export.name,
            "library":plan.library,"symbol":export.symbol,"same_symbol_exports":aliases,
            "python_function_identity":"distinct_per_export","declared_signature":export.signature,
            "python":{"representation":if def_name(&export.name){"def"}else{"module_getattr_literal_callable"},
                "parameters":parameters.iter().map(|(name,p)|json!({"declared_name":p.name,"stub_name":name,
                    "kind":"positional_only","required":true,"python_type":python_type(p.ty,"builtins"),
                    "abi":type_contract(p.ty)})).collect::<Vec<_>>(),"returns":python_type(signature.returns,"builtins"),"keywords":false},
            "machine":{"calling_convention":"cdecl","pinvoke_signature":ryupy_codegen_cil::native_abi_types::signature_display(signature),
                "verified_report_reference":format!("ryupy-artifact.json#/native_bindings/{index}/native/signature"),
                "parameters":signature.parameters.iter().enumerate().flat_map(|(logical,p)|{
                    machine_parameters(p.ty).into_iter().enumerate().map(move |(part,mut parameter)| {
                        parameter["logical_index"]=json!(logical);
                        parameter["declared_name"]=json!(p.name);
                        parameter["expansion_index"]=json!(part);
                        parameter
                    })
                }).collect::<Vec<_>>(),"returns":type_contract(signature.returns)}}));
    }
    for (index, export) in special {
        let signature = &signatures[index];
        if hook_count > 1 {
            text.push_str(&format!("@{typing}.overload\n"));
        }
        let types = signature
            .parameters
            .iter()
            .map(|p| python_type(p.ty, &builtins))
            .collect::<Vec<_>>()
            .join(", ");
        text.push_str(&format!("def __getattr__(name: {typing}.Literal[{}], /) -> {typing}.Callable[[{types}], {}]: ...\n\n",
            serde_json::to_string(&export.name)?, python_type(signature.returns,&builtins)));
    }
    let machine = json!({"schema":1,"module":plan.module,"declaration_version":plan.declaration_version,
        "source":"validated_binding_plan","target":{"rid":plan.platform.rid(),"pointer_bits":64},
        "linkage":if plan.dynamic{"shared"}else{"static_archive"},"exports":exports,
        "conversion_order":"all_arguments_validated_before_native_call_or_pin",
        "buffers":{"ownership":"borrowed_call","access":"read_only","lifetime":"synchronous_call_only",
            "native_expansion":"const_uint8_pointer_followed_by_usize_byte_length","utf8":"strict_without_bom_or_terminator",
            "bytes":"independent_snapshot_per_argument","retention_or_free_by_native":false},
        "type_hint_limits":["int注解不能表达拒绝bool和固定宽度范围","float注解不能表达只接受实际float及f32舍入", "str/bytes注解不能表达调用期借用与长度展开"]});
    Ok((text, machine))
}

fn python_type(ty: AbiType, builtins: &str) -> String {
    let name = match ty {
        AbiType::F32 | AbiType::F64 => "float",
        AbiType::Bool8 => "bool",
        AbiType::Utf8 => "str",
        AbiType::Bytes => "bytes",
        AbiType::Void => return "None".into(),
        _ => "int",
    };
    format!("{builtins}.{name}")
}

fn type_contract(ty: AbiType) -> Value {
    use AbiType::*;
    match ty {
        I8 => integer(8, true),
        U8 => integer(8, false),
        I16 => integer(16, true),
        U16 => integer(16, false),
        I32 => integer(32, true),
        U32 => integer(32, false),
        I64 | Isize => integer(64, true),
        U64 | Usize => integer(64, false),
        F32 | F64 => {
            json!({"kind":"float","bits":if ty==F32{32}else{64},"python_input":"actual_float_only",
            "finite_overflow":"OverflowError","rounding":if ty==F32{"binary32_then_promote_binary64"}else{"binary64_identity"},"nan_payload":"unspecified"})
        }
        Bool8 => {
            json!({"kind":"bool8","bits":8,"python_input":"actual_bool_only","native_values":[0,1],"invalid_return":"ValueError"})
        }
        Utf8 | Bytes => {
            json!({"kind":if ty==Utf8{"utf8"}else{"bytes"},"ownership":"borrowed_call","length":"usize","read_only":true})
        }
        Void => json!({"kind":"void","python_return":"None"}),
    }
}

fn integer(bits: usize, signed: bool) -> Value {
    let limit = 1i128 << (bits - usize::from(signed));
    json!({"kind":"integer","bits":bits,"signed":signed,
        "minimum":if signed{(-limit).to_string()}else{"0".into()},"maximum":(limit-1).to_string(),
        "python_input":"actual_int_excluding_bool","conversion_protocols":false})
}

fn machine_parameters(ty: AbiType) -> Vec<Value> {
    if ty.is_buffer() {
        vec![
            json!({"kind":"const_uint8_pointer","borrowed_call":true}),
            json!({"kind":"usize","bits":64,"unit":"bytes"}),
        ]
    } else {
        vec![type_contract(ty)]
    }
}

fn unique(base: &str, names: &mut HashSet<String>) -> String {
    let mut name = base.to_string();
    let mut suffix = 0;
    while !names.insert(name.clone()) {
        suffix += 1;
        name = format!("{base}_{suffix}");
    }
    name
}

fn def_name(name: &str) -> bool {
    !matches!(
        name,
        "False"
            | "None"
            | "True"
            | "and"
            | "as"
            | "assert"
            | "async"
            | "await"
            | "break"
            | "class"
            | "continue"
            | "def"
            | "del"
            | "elif"
            | "else"
            | "except"
            | "finally"
            | "for"
            | "from"
            | "global"
            | "if"
            | "import"
            | "in"
            | "is"
            | "lambda"
            | "nonlocal"
            | "not"
            | "or"
            | "pass"
            | "raise"
            | "return"
            | "try"
            | "while"
            | "with"
            | "yield"
            | "__debug__"
    )
}

#[cfg(test)]
#[path = "native_binding_stub_tests.rs"]
mod tests;
