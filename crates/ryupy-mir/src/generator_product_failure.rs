//! 自动选择的失败边界：只有未覆盖能力允许丢弃整个未lower候选；坏合同和原诊断不能回退。
//! 收集器只保存拒绝原因，不生成替代值，也不授予任何IR/协议能力。
#[derive(Debug)]
pub enum Failure {
    NotInDomain(String),
    InvalidCompilerState(String),
    Diagnostics(Vec<ryupy_base::Diagnostic>),
}
impl Failure {
    pub(super) fn unsupported(reason: impl Into<String>) -> Self {
        Self::NotInDomain(reason.into())
    }
}
impl From<String> for Failure {
    fn from(reason: String) -> Self {
        Self::InvalidCompilerState(reason)
    }
}
impl From<&str> for Failure {
    fn from(reason: &str) -> Self {
        Self::InvalidCompilerState(reason.into())
    }
}
impl std::fmt::Display for Failure {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::NotInDomain(reason) | Self::InvalidCompilerState(reason) => f.write_str(reason),
            Self::Diagnostics(diagnostics) => write!(f, "高层构建诊断：{diagnostics:?}"),
        }
    }
}
impl std::error::Error for Failure {}

#[derive(Default)]
pub(super) struct Limitations(Option<String>);
impl Limitations {
    pub fn note(&mut self, reason: impl Into<String>) {
        if self.0.is_none() {
            self.0 = Some(reason.into());
        }
    }
    pub fn check<T>(&mut self, result: Result<T, Failure>) -> Result<Option<T>, Failure> {
        match result {
            Ok(value) => Ok(Some(value)),
            Err(Failure::NotInDomain(reason)) => {
                self.note(reason);
                Ok(None)
            }
            Err(error) => Err(error),
        }
    }
    pub fn finish(self) -> Result<(), Failure> {
        match self.0 {
            Some(reason) => Err(Failure::NotInDomain(reason)),
            None => Ok(()),
        }
    }
}
