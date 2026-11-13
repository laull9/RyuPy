//! v3 签名解析；严格检查编码、借用、长度合同后才允许读取原生输入。
use anyhow::{Context, Result, ensure};
use ryupy_mir::native_abi::{AbiParameter, AbiSignature, AbiType};
use serde::{Deserialize, Deserializer};

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Signature {
    calling_convention: String,
    parameters: Vec<Parameter>,
    returns: String,
}

#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Parameter {
    name: String,
    #[serde(rename = "type")]
    type_name: String,
    #[serde(default, deserialize_with = "present_string")]
    ownership: Option<String>,
    #[serde(default, deserialize_with = "present_string")]
    length: Option<String>,
}

// 缺省字段与显式 null 不可混淆；封送合同只接受非空类型正确的声明值。
fn present_string<'de, D: Deserializer<'de>>(deserializer: D) -> Result<Option<String>, D::Error> {
    String::deserialize(deserializer).map(Some)
}

pub(crate) fn parse(value: &serde_json::Value) -> Result<AbiSignature> {
    let signature: Signature = serde_json::from_value(value.clone()).context("无效v3 ABI签名")?;
    ensure!(signature.calling_convention == "cdecl", "C ABI仅支持cdecl");
    let mut parameters = Vec::new();
    for parameter in signature.parameters {
        let ty = AbiType::parse(&parameter.type_name).context("不支持的C ABI参数类型")?;
        if ty.is_buffer() {
            ensure!(
                parameter.ownership.as_deref() == Some("borrowed_call")
                    && parameter.length.as_deref() == Some("usize"),
                "utf8/bytes必须显式声明borrowed_call只读借用和usize字节长度"
            );
        } else {
            ensure!(
                parameter.ownership.is_none() && parameter.length.is_none(),
                "标量参数不能声明缓冲区封送字段"
            );
        }
        parameters.push(AbiParameter {
            name: parameter.name,
            ty,
            type_name: parameter.type_name,
        });
    }
    let returns = AbiType::parse(&signature.returns).context("不支持的C ABI返回类型")?;
    let result = AbiSignature {
        parameters,
        returns,
    };
    result.validate().map_err(anyhow::Error::msg)?;
    Ok(result)
}

#[cfg(test)]
mod tests {
    use super::*;
    use serde_json::json;

    #[test]
    fn buffers_require_explicit_borrowing_and_native_length() {
        let value = json!({"calling_convention":"cdecl","parameters":[
            {"name":"text","type":"utf8","ownership":"borrowed_call","length":"usize"},
            {"name":"data","type":"bytes","ownership":"borrowed_call","length":"usize"}],"returns":"u64"});
        let signature = parse(&value).unwrap();
        assert_eq!(signature.parameters[0].ty, AbiType::Utf8);
        assert_eq!(signature.parameters[1].ty, AbiType::Bytes);
        for field in ["ownership", "length"] {
            let mut bad = value.clone();
            bad["parameters"][0].as_object_mut().unwrap().remove(field);
            assert!(parse(&bad).is_err());
            bad["parameters"][0][field] = serde_json::Value::Null;
            assert!(parse(&bad).is_err());
        }
    }

    #[test]
    fn scalars_reject_implicit_or_irrelevant_marshalling() {
        for field in ["ownership", "length", "nullable", "encoding"] {
            let mut value = json!({"calling_convention":"cdecl","parameters":[
                {"name":"value","type":"i32"}],"returns":"void"});
            value["parameters"][0][field] = serde_json::Value::Null;
            assert!(parse(&value).is_err(), "{field}");
            value["parameters"][0][field] = json!("borrowed_call");
            assert!(parse(&value).is_err(), "{field}");
        }
    }

    #[test]
    fn all_scalar_spellings_and_zero_argument_void_are_explicit() {
        for kind in [
            "i8", "u8", "i16", "u16", "i32", "u32", "i64", "u64", "isize", "usize", "f32", "f64",
            "bool8",
        ] {
            let value = json!({"calling_convention":"cdecl","parameters":[
                {"name":"value","type":kind}],"returns":kind});
            assert_eq!(
                parse(&value).unwrap().returns,
                AbiType::parse(kind).unwrap()
            );
        }
        assert!(
            parse(&json!({"calling_convention":"cdecl","parameters":[],"returns":"void"})).is_ok()
        );
    }
}

#[cfg(test)]
mod unsupported_tests {
    use super::parse;
    use serde_json::json;

    #[test]
    fn complex_abi_requires_an_explicit_adapter() {
        // 这些布局与生命周期需要独立合同；不能误当整数或隐式缓冲封送。
        for kind in ["pointer", "struct", "callback", "owned_bytes", "handle"] {
            let value = json!({"calling_convention":"cdecl", "parameters":[
                {"name":"value", "type":kind}], "returns":"i32"});
            let error = parse(&value).unwrap_err().to_string();
            assert!(error.contains("不支持的C ABI参数类型"), "{kind}: {error}");
        }
    }
}
