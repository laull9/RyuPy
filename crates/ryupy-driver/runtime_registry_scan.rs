//! 构建期 C# 字面注册提取器：跳过注释与字符串内容，只采集明确的模块注册调用。
#[derive(Debug, PartialEq)]
enum Token {
    Word(String),
    Literal(String),
    Punct(u8),
}

/// 仅支持普通字符串字面模块名；动态表达式不签发静态注册事实。
pub fn registrations(source: &str) -> Vec<(String, usize)> {
    let bytes = source.as_bytes();
    let mut tokens = Vec::new();
    let (mut i, mut line) = (0, 1);
    while i < bytes.len() {
        match bytes[i] {
            b'\n' => {
                line += 1;
                i += 1;
            }
            c if c.is_ascii_whitespace() => i += 1,
            b'/' if bytes.get(i + 1) == Some(&b'/') => {
                while i < bytes.len() && bytes[i] != b'\n' {
                    i += 1;
                }
            }
            b'/' if bytes.get(i + 1) == Some(&b'*') => {
                i += 2;
                while i < bytes.len() {
                    if bytes[i] == b'\n' {
                        line += 1;
                    }
                    if bytes.get(i..i + 2) == Some(b"*/") {
                        i += 2;
                        break;
                    }
                    i += 1;
                }
            }
            b'"' | b'\'' => {
                let start_line = line;
                let quote = bytes[i];
                let verbatim = i > 0 && bytes[i - 1] == b'@';
                // raw 字符串整体跳过；其中的伪调用不能成为注册事实。
                let quotes = bytes[i..].iter().take_while(|&&c| c == b'"').count();
                if quote == b'"' && quotes >= 3 {
                    i += quotes;
                    while i < bytes.len() {
                        if bytes[i] == b'\n' {
                            line += 1;
                        }
                        if bytes[i] == b'"'
                            && bytes[i..].iter().take_while(|&&c| c == b'"').count() >= quotes
                        {
                            i += quotes;
                            break;
                        }
                        i += 1;
                    }
                    tokens.push((Token::Punct(b'$'), start_line));
                    continue;
                }
                i += 1;
                let mut value = String::new();
                let mut simple = true;
                while i < bytes.len() {
                    let c = bytes[i];
                    if c == b'\n' {
                        line += 1;
                    }
                    if c == quote {
                        if verbatim && bytes.get(i + 1) == Some(&quote) {
                            simple = false;
                            i += 2;
                            continue;
                        }
                        i += 1;
                        break;
                    }
                    if !verbatim && c == b'\\' {
                        simple = false;
                        i = (i + 2).min(bytes.len());
                        continue;
                    }
                    if c.is_ascii() {
                        value.push(char::from(c));
                    } else {
                        simple = false;
                    }
                    i += 1;
                }
                tokens.push((
                    if simple && quote == b'"' {
                        Token::Literal(value)
                    } else {
                        Token::Punct(b'$')
                    },
                    start_line,
                ));
            }
            c if c.is_ascii_alphabetic() || c == b'_' => {
                let start = i;
                i += 1;
                while i < bytes.len() && (bytes[i].is_ascii_alphanumeric() || bytes[i] == b'_') {
                    i += 1;
                }
                tokens.push((Token::Word(source[start..i].into()), line));
            }
            c => {
                tokens.push((Token::Punct(c), line));
                i += 1;
            }
        }
    }
    tokens
        .windows(6)
        .filter_map(|window| {
            match (
                &window[0].0,
                &window[1].0,
                &window[2].0,
                &window[3].0,
                &window[4].0,
                &window[5].0,
            ) {
                (
                    Token::Word(owner),
                    Token::Punct(b'.'),
                    Token::Word(method),
                    Token::Punct(b'('),
                    Token::Literal(name),
                    Token::Punct(b','),
                ) if owner == "PyModules"
                    && matches!(method.as_str(), "RegisterNative" | "RegisterNativeLazy")
                    && !name.is_empty()
                    && name
                        .bytes()
                        .all(|c| c.is_ascii_alphanumeric() || c == b'_' || c == b'.') =>
                {
                    Some((name.clone(), window[0].1))
                }
                _ => None,
            }
        })
        .collect()
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn extracts_multiline_calls_but_ignores_comments_strings_and_dynamic_names() {
        let source = r####"
// PyModules.RegisterNative("fake_comment", x);
var text = "PyModules.RegisterNative(\"fake_string\", x)";
var raw = """PyModules.RegisterNative("fake_raw", x)""";
/* PyModules.RegisterNative("fake_block", x); */
PyModules . RegisterNativeLazy (
    "sqlite3", factory);
PyModules.RegisterNative("urllib.parse", attrs);
PyModules.RegisterNative(name, attrs);
PyModules.RegisterNative("partial" + suffix, attrs);
"####;
        assert_eq!(
            registrations(source),
            vec![("sqlite3".into(), 6), ("urllib.parse".into(), 8)]
        );
    }
}
