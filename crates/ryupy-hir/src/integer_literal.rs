//! Ruff已验证整数字面量的有界十进制正规化，不重新实现Python词法分析。
//!
//! Ruff的超u64整数保存原token，非十进制token仍包含前缀与下划线。
//! 十进制直接去分隔符/前导零；2/8/16进制用base10^9小端limb逐位乘加。
//! 每步检查4300有效十进制位上限，避免先构造无界大整数再拒绝。

/// 正规化结果的有效十进制位上限；一元符号由独立HIR表达式负责。
pub(crate) const MAX_INTEGER_DIGITS: usize = 4300;
const LIMB_BASE: u64 = 1_000_000_000;

/// 调用者把错误附到原token源码范围；不能因异常token或容量超限panic。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) enum IntegerLiteralError {
    Capacity,
    InvalidToken,
}

/// 将已经由Ruff验证的正整数字面量转换为不带前导零的十进制字符串。
///
/// 结果始终非空：全零输入返回"0"。此函数不处理一元正负号。
pub(crate) fn decimal_integer(token: &str) -> Result<Box<str>, IntegerLiteralError> {
    let (radix, digits) = if let Some(digits) = token
        .strip_prefix("0x")
        .or_else(|| token.strip_prefix("0X"))
    {
        (16, digits)
    } else if let Some(digits) = token
        .strip_prefix("0o")
        .or_else(|| token.strip_prefix("0O"))
    {
        (8, digits)
    } else if let Some(digits) = token
        .strip_prefix("0b")
        .or_else(|| token.strip_prefix("0B"))
    {
        (2, digits)
    } else {
        (10, token)
    };
    if radix == 10 {
        return decimal_digits(digits);
    }
    let mut limbs = vec![0u32];
    let mut saw_digit = false;
    let mut significant = false;
    for byte in digits.bytes().filter(|byte| *byte != b'_') {
        let digit = digit_value(byte)
            .filter(|value| *value < radix)
            .ok_or(IntegerLiteralError::InvalidToken)?;
        saw_digit = true;
        if !significant && digit == 0 {
            continue;
        }
        significant = true;
        // limb<10^9、radix<=16，乘积和进位都严格落在u64中。
        let mut carry = u64::from(digit);
        for limb in &mut limbs {
            let value = u64::from(*limb) * u64::from(radix) + carry;
            *limb = (value % LIMB_BASE) as u32;
            carry = value / LIMB_BASE;
        }
        if carry != 0 {
            limbs.push(carry as u32);
        }
        let highest = limbs.last().copied().unwrap_or(0);
        let top_digits = if highest == 0 {
            1
        } else {
            highest.ilog10() as usize + 1
        };
        if (limbs.len() - 1) * 9 + top_digits > MAX_INTEGER_DIGITS {
            return Err(IntegerLiteralError::Capacity);
        }
    }
    if !saw_digit {
        return Err(IntegerLiteralError::InvalidToken);
    }
    let mut reversed = limbs.iter().rev();
    let mut result = reversed.next().copied().unwrap_or(0).to_string();
    for limb in reversed {
        result.push_str(&format!("{limb:09}"));
    }
    Ok(result.into_boxed_str())
}

fn decimal_digits(digits: &str) -> Result<Box<str>, IntegerLiteralError> {
    let mut result = String::new();
    let mut saw_digit = false;
    for byte in digits.bytes().filter(|byte| *byte != b'_') {
        if !byte.is_ascii_digit() {
            return Err(IntegerLiteralError::InvalidToken);
        }
        saw_digit = true;
        if result.is_empty() && byte == b'0' {
            continue;
        }
        if result.len() == MAX_INTEGER_DIGITS {
            return Err(IntegerLiteralError::Capacity);
        }
        result.push(char::from(byte));
    }
    if !saw_digit {
        return Err(IntegerLiteralError::InvalidToken);
    }
    if result.is_empty() {
        result.push('0');
    }
    Ok(result.into_boxed_str())
}

fn digit_value(byte: u8) -> Option<u32> {
    match byte {
        b'0'..=b'9' => Some(u32::from(byte - b'0')),
        b'a'..=b'f' => Some(u32::from(byte - b'a') + 10),
        b'A'..=b'F' => Some(u32::from(byte - b'A') + 10),
        _ => None,
    }
}

#[cfg(test)]
mod tests {
    use super::{IntegerLiteralError, decimal_integer};

    #[test]
    fn radix_prefixes_separators_and_leading_zeros_are_normalized() {
        for source in [
            "18446744073709551616",
            "18_446_744_073_709_551_616",
            "0X_10000000000000000",
            "0o2000000000000000000000",
            "0B1_0000000000000000000000000000000000000000000000000000000000000000",
        ] {
            assert_eq!(&*decimal_integer(source).unwrap(), "18446744073709551616");
        }
        assert_eq!(&*decimal_integer("000_000_123").unwrap(), "123");
        for source in ["0", "000_000", "0x000_00", "0o0", "0b0"] {
            assert_eq!(&*decimal_integer(source).unwrap(), "0");
        }
    }

    #[test]
    fn decimal_capacity_counts_significant_digits_not_separators_or_zeros() {
        let accepted = "9".repeat(4300);
        assert_eq!(&*decimal_integer(&accepted).unwrap(), accepted);
        assert_eq!(
            decimal_integer(&"9".repeat(4301)),
            Err(IntegerLiteralError::Capacity)
        );
        let padded = format!("{}{}", "0_".repeat(5000), accepted);
        assert_eq!(decimal_integer(&padded).unwrap().len(), 4300);
    }

    #[test]
    fn radix_capacity_is_exact_and_checked_during_conversion() {
        // 2^14284有4300位，2^14285有4301位；三种进制必须得到同一正规值。
        let binary = format!("0b1{}", "0".repeat(14284));
        let hex = format!("0x1{}", "0".repeat(3571));
        let octal = format!("0o2{}", "0".repeat(4761));
        let expected = decimal_integer(&binary).unwrap();
        assert_eq!(expected.len(), 4300);
        assert!(expected.starts_with("817444101320944225038496512205"));
        assert!(expected.ends_with("508245884247391660265823010816"));
        assert_eq!(decimal_integer(&hex).unwrap(), expected);
        assert_eq!(decimal_integer(&octal).unwrap(), expected);
        for source in [
            format!("0b1{}", "0".repeat(14285)),
            format!("0x2{}", "0".repeat(3571)),
            format!("0o4{}", "0".repeat(4761)),
        ] {
            assert_eq!(decimal_integer(&source), Err(IntegerLiteralError::Capacity));
        }
    }

    #[test]
    fn invalid_input_is_a_result_not_a_panic() {
        for source in ["", "_", "0x", "0o8", "0b2", "-1", "+1", "12x", "１２"] {
            assert_eq!(
                decimal_integer(source),
                Err(IntegerLiteralError::InvalidToken)
            );
        }
    }
}
