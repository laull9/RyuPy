//! 默认关闭的编译阶段观测：仅记录有界阶段/模块元数据，不输出 IR 或改变编译结果。
//! 关闭时不读时钟、不构造消息、不取得 stderr；启用时每条写出后 flush。
//! 写入失败即停用本次观测，保留原编译行为；缺失标记只能视为证据不完整。
use std::cell::Cell;
use std::io::Write;
use std::time::Instant;

use ryupy_mir::passes::{PipelineEvent, PipelineObserver, PipelinePhase, PipelineStep};

/// driver接收MIR元数据，只记录诊断，不取得IR也不改变流水线控制。
pub(crate) struct PassTrace<'a> {
    trace: &'a StageTrace,
    module: &'a str,
}

impl<'a> PassTrace<'a> {
    pub(crate) fn new(trace: &'a StageTrace, module: &'a str) -> Self {
        Self { trace, module }
    }
}

impl PipelineObserver for PassTrace<'_> {
    fn observe(&mut self, event: PipelineEvent) {
        self.trace
            .mark_pass_with(event, self.module, std::io::stderr);
    }
}

pub(crate) struct StageTrace {
    start: Option<Instant>,
    failed: Cell<bool>,
}

impl StageTrace {
    pub(crate) fn new(enabled: bool) -> Self {
        Self {
            start: enabled.then(Instant::now),
            failed: Cell::new(false),
        }
    }

    pub(crate) fn mark(&self, event: &str, stage: &str, module: &str) {
        self.mark_with(event, stage, module, std::io::stderr);
    }

    fn mark_with<W: Write>(
        &self,
        event: &str,
        stage: &str,
        module: &str,
        writer: impl FnOnce() -> W,
    ) {
        self.write_with(event, stage, module, None, writer);
    }

    fn mark_pass_with<W: Write>(
        &self,
        event: PipelineEvent,
        module: &str,
        writer: impl FnOnce() -> W,
    ) {
        let stage = match event.phase {
            PipelinePhase::Run => "pass-run",
            PipelinePhase::Validate => "pass-validate",
        };
        let step = match event.step {
            PipelineStep::Begin => "begin",
            PipelineStep::End => "end",
        };
        self.write_with(step, stage, module, Some(event), writer);
    }

    fn write_with<W: Write>(
        &self,
        event: &str,
        stage: &str,
        module: &str,
        pass: Option<PipelineEvent>,
        writer: impl FnOnce() -> W,
    ) {
        let Some(start) = self.start else { return };
        if self.failed.get() {
            return;
        }
        // 模块名来自源码；最多96个字符并使用debug转义，禁止换行/终端控制注入。
        let module: String = module.chars().take(96).collect();
        let mut writer = writer();
        let result = if let Some(pass) = pass {
            // 静态名称仍可由外部pass实现提供，和模块名一样有界转义。
            let name: String = pass.pass_name.chars().take(96).collect();
            writeln!(
                writer,
                "[ryupy-stage] event={event} stage={stage} module={module:?} pass_index={} pass={name:?} elapsed_us={}",
                pass.ordinal,
                start.elapsed().as_micros()
            )
        } else {
            writeln!(
                writer,
                "[ryupy-stage] event={event} stage={stage} module={module:?} elapsed_us={}",
                start.elapsed().as_micros()
            )
        }
        .and_then(|()| writer.flush());
        if result.is_err() {
            self.failed.set(true);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io;

    #[derive(Default)]
    struct Sink {
        bytes: Vec<u8>,
        flushes: usize,
        fail_flush: bool,
    }
    impl Write for Sink {
        fn write(&mut self, bytes: &[u8]) -> io::Result<usize> {
            self.bytes.extend_from_slice(bytes);
            Ok(bytes.len())
        }
        fn flush(&mut self) -> io::Result<()> {
            self.flushes += 1;
            if self.fail_flush {
                Err(io::Error::other("测试flush失败"))
            } else {
                Ok(())
            }
        }
    }

    #[test]
    fn disabled_has_no_clock_or_sink() {
        let trace = StageTrace::new(false);
        assert!(trace.start.is_none());
        trace.mark_with("begin", "hir", "x", || -> Sink {
            panic!("不可取得sink")
        });
    }

    #[test]
    fn enabled_flushes_bounded_escaped_metadata() {
        let trace = StageTrace::new(true);
        let mut sink = Sink::default();
        trace.mark_with(
            "begin",
            "hir",
            &format!("a\n\x1b{}", "长".repeat(200)),
            || &mut sink,
        );
        assert_eq!(sink.flushes, 1);
        let text = String::from_utf8(sink.bytes).unwrap();
        assert_eq!(text.lines().count(), 1);
        assert!(!text.contains('\x1b'));
        assert!(text.contains("a\\n\\u{1b}"));
        assert!(text.len() < 1024);
    }

    #[test]
    fn failed_sink_does_not_fail_compilation_or_retry() {
        let trace = StageTrace::new(true);
        let mut sink = Sink {
            fail_flush: true,
            ..Sink::default()
        };
        trace.mark_with("begin", "hir", "x", || &mut sink);
        assert!(trace.failed.get());
        trace.mark_with("end", "hir", "x", || -> Sink {
            panic!("失败后不可再取得sink")
        });
    }

    fn pass_event(name: &'static str) -> PipelineEvent {
        PipelineEvent {
            ordinal: 5,
            pass_name: name,
            phase: PipelinePhase::Validate,
            step: PipelineStep::Begin,
        }
    }

    #[test]
    fn pass_metadata_is_bounded_escaped_and_flushes() {
        let trace = StageTrace::new(true);
        let mut sink = Sink::default();
        // 超过96字符的静态恶意名称也必须截断和转义。
        let name = concat!(
            "bad\n\x1b",
            "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz",
            "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz",
            "TAIL"
        );
        trace.mark_pass_with(pass_event(name), "module\n\x1b", || &mut sink);
        assert_eq!(sink.flushes, 1);
        let text = String::from_utf8(sink.bytes).unwrap();
        assert_eq!(text.lines().count(), 1);
        assert!(!text.contains('\x1b'));
        assert!(!text.contains("TAIL"));
        assert!(text.contains("pass_index=5 pass=\"bad\\n\\u{1b}"));
        assert!(text.contains("stage=pass-validate"));
        assert!(text.len() < 1024);
    }

    #[test]
    fn disabled_and_failed_pass_sink_are_never_retrieved() {
        StageTrace::new(false).mark_pass_with(pass_event("x"), "m", || -> Sink {
            panic!("关闭时不可取得sink")
        });
        let trace = StageTrace::new(true);
        let mut sink = Sink {
            fail_flush: true,
            ..Sink::default()
        };
        trace.mark_pass_with(pass_event("x"), "m", || &mut sink);
        trace.mark_with("end", "pipeline", "m", || -> Sink {
            panic!("pass写失败后普通阶段也不得再写")
        });
        trace.mark_pass_with(pass_event("x"), "m", || -> Sink {
            panic!("pass写失败后不得重试")
        });
    }

    struct WriteFailure;
    impl Write for WriteFailure {
        fn write(&mut self, _: &[u8]) -> io::Result<usize> {
            Err(io::Error::other("测试write失败"))
        }
        fn flush(&mut self) -> io::Result<()> {
            panic!("write失败不应flush")
        }
    }

    #[test]
    fn ordinary_write_failure_disables_pass_observation() {
        let trace = StageTrace::new(true);
        trace.mark_with("begin", "pipeline", "m", || WriteFailure);
        trace.mark_pass_with(pass_event("x"), "m", || -> Sink {
            panic!("普通阶段失败后pass不得再写")
        });
    }
}
