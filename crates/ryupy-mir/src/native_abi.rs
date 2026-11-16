//! 声明式 C ABI 静态合同；类型集合封闭，缓冲输入展开为地址与 size_t。

/// 精确宽度的 PInvoke 类型；Bool8 明确采用 uint8_t 的 0/1 协议。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum AbiType {
    I8,
    U8,
    I16,
    U16,
    I32,
    U32,
    I64,
    U64,
    Isize,
    Usize,
    F32,
    F64,
    Bool8,
    Utf8,
    Bytes,
    Void,
}

impl AbiType {
    /// 稳定拼写供受信 runtime 预转换使用，指针与隐含封送类型均无入口。
    pub fn parse(name: &str) -> Option<Self> {
        Some(match name {
            "i8" => Self::I8,
            "u8" => Self::U8,
            "i16" => Self::I16,
            "u16" => Self::U16,
            "i32" => Self::I32,
            "u32" => Self::U32,
            "i64" => Self::I64,
            "u64" => Self::U64,
            "isize" => Self::Isize,
            "usize" => Self::Usize,
            "f32" => Self::F32,
            "f64" => Self::F64,
            "bool8" => Self::Bool8,
            "utf8" => Self::Utf8,
            "bytes" => Self::Bytes,
            "void" => Self::Void,
            _ => return None,
        })
    }

    /// 缓冲区只用于输入，永不作为可见 Python 地址或返回值。
    pub fn is_buffer(self) -> bool {
        matches!(self, Self::Utf8 | Self::Bytes)
    }
}

/// 名字参与 Python 位置专用签名；type_name 为稳定的 ABI 类型拼写。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct AbiParameter {
    pub name: String,
    pub ty: AbiType,
    pub type_name: String,
}

/// MIR 层的 cdecl 合同，codegen 只消费这些显式数据。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct AbiSignature {
    pub parameters: Vec<AbiParameter>,
    pub returns: AbiType,
}

impl AbiSignature {
    /// 同符号别名必须具有相同 ABI；Python 参数名字允许不同。
    pub fn same_abi(&self, other: &Self) -> bool {
        self.returns == other.returns
            && self
                .parameters
                .iter()
                .map(|parameter| parameter.ty)
                .eq(other.parameters.iter().map(|parameter| parameter.ty))
    }

    /// 内部入口同样校验，防止未验证侧记录产生无效元数据。
    pub fn validate(&self) -> Result<(), String> {
        if self.parameters.len() > 16 || self.returns.is_buffer() {
            return Err("C ABI 最多16个逻辑参数，返回值必须为标量或void".into());
        }
        let mut names = std::collections::HashSet::new();
        for parameter in &self.parameters {
            let name = &parameter.name;
            if parameter.ty == AbiType::Void
                || AbiType::parse(&parameter.type_name) != Some(parameter.ty)
                || name.is_empty()
                || name.len() > 128
                || !name.bytes().enumerate().all(|(index, byte)| {
                    byte == b'_'
                        || byte.is_ascii_alphabetic()
                        || (index > 0 && byte.is_ascii_digit())
                })
                || !names.insert(name)
            {
                return Err("C ABI 参数必须具有唯一ASCII名称与有效输入类型".into());
            }
        }
        Ok(())
    }
}
