//! 仅测试使用的有界观察；不进入安全状态、队列或生产构建。
//! 快照在 visited 插入后、任何语义更新前采集，不追踪入队前驱。
use super::*;
use std::cell::RefCell;
use std::fmt::{self, Write as _};
use std::io::{self, Write as _};

pub(super) const DYNAMIC_LIMIT: usize = 16 * 1024 * 1024;
#[expect(dead_code, reason = "保留已结束的一次性采样审计入口，不重新启用")]
pub(super) const IR_LIMIT: usize = 2 * 1024 * 1024;
const RECORD_LIMIT: usize = 8192;
const EVENT_LIMIT: usize = 16384;

/// 逐片检查编码后的字节，包含暂存；失败不追加部分片段。
pub(super) struct Bounded {
    pub text: String,
    pub limit: usize,
}

impl fmt::Write for Bounded {
    fn write_str(&mut self, value: &str) -> fmt::Result {
        if value.len() > self.limit.saturating_sub(self.text.len()) {
            return Err(fmt::Error);
        }
        self.text
            .try_reserve_exact(value.len())
            .map_err(|_| fmt::Error)?;
        self.text.push_str(value);
        Ok(())
    }
}

fn quoted(out: &mut Bounded, value: &str) -> fmt::Result {
    out.write_char('"')?;
    for character in value.chars() {
        match character {
            '"' => out.write_str("\\\"")?,
            '\\' => out.write_str("\\\\")?,
            '\n' => out.write_str("\\n")?,
            '\r' => out.write_str("\\r")?,
            '\t' => out.write_str("\\t")?,
            c if c < ' ' => write!(out, "\\u{:04x}", c as u32)?,
            c => out.write_char(c)?,
        }
    }
    out.write_char('"')
}

fn snapshot(state: &State, limit: usize) -> Result<String, fmt::Error> {
    let mut out = Bounded {
        text: String::new(),
        limit,
    };
    write!(
        out,
        "{{\"pc\":{},\"block\":{},\"cursor\":{},",
        state.pc,
        state.block.into_raw().into_u32(),
        state.cursor
    )?;
    for (name, values) in [("locals", &state.locals), ("slots", &state.slots)] {
        write!(out, "\"{name}\":[")?;
        for (i, value) in values.iter().enumerate() {
            if i > 0 {
                out.write_char(',')?;
            }
            // Value 的 Debug 仅为固定枚举名或带一个整数/布尔值的变体，无用户字符串。
            write!(out, "\"{value:?}\"")?;
        }
        out.write_str("],")?;
    }
    write!(
        out,
        "\"physical_catch\":{},\"dispatching\":{},\"pending\":\"{:?}\",",
        state.physical_catch, state.dispatching, state.pending
    )?;
    for (name, value) in [
        ("expected_retry_pc", state.expected_retry_pc),
        ("expected_return_pc", state.expected_return_pc),
    ] {
        write!(out, "\"{name}\":")?;
        match value {
            Some(value) => write!(out, "{value}")?,
            None => out.write_str("null")?,
        }
        out.write_char(',')?;
    }
    write!(
        out,
        "\"written\":{:?},\"stored_current\":{:?},\"current_sources\":[",
        state.written, state.stored_current
    )?;
    for (i, sources) in state.current_sources.iter().enumerate() {
        if i > 0 {
            out.write_char(',')?;
        }
        out.write_char('[')?;
        for (j, source) in sources.iter().enumerate() {
            if j > 0 {
                out.write_char(',')?;
            }
            write!(out, "{}", index(*source))?;
        }
        out.write_char(']')?;
    }
    out.write_str("]}")?;
    Ok(out.text)
}

pub(super) struct Observation {
    states: Vec<String>,
    events: Vec<String>,
    bytes: usize,
    attempted: usize,
    status: &'static str,
    ir: String,
    ir_status: &'static str,
    limit: usize,
    analysis: Option<FrameDiagnostics>,
}

impl Observation {
    fn new(limit: usize) -> Self {
        Self {
            states: Vec::new(),
            events: Vec::new(),
            bytes: 0,
            attempted: 0,
            status: "complete",
            ir: String::new(),
            ir_status: "not_collected",
            limit,
            analysis: None,
        }
    }

    fn observe(&mut self, state: &State, inserted: bool) {
        self.attempted += 1;
        if self.status != "complete" {
            return;
        }
        if self.events.len() >= EVENT_LIMIT || (inserted && self.states.len() >= RECORD_LIMIT) {
            self.status = "record_limit";
            return;
        }
        // 包装和事件至多128字节；暂存与已保留字符串合计不超过limit。
        let remaining = self.limit.saturating_sub(self.bytes);
        if remaining < 128 {
            self.status = "byte_limit";
            return;
        }
        let encoded = match snapshot(state, remaining - 128) {
            Ok(value) => value,
            Err(_) => {
                self.status = "encoding_or_byte_limit";
                return;
            }
        };
        let id = if inserted {
            self.states.len()
        } else {
            match self.states.iter().position(|old| old == &encoded) {
                Some(id) => id,
                None => {
                    self.status = "missing_duplicate";
                    return;
                }
            }
        };
        let event = format!(
            "{{\"dequeue\":{},\"id\":{id},\"inserted\":{inserted}}}\n",
            self.attempted
        );
        if inserted {
            self.bytes += encoded.len() + 64;
            self.states.push(encoded);
        }
        self.bytes += event.len();
        self.events.push(event);
    }
}

thread_local! {
    static ACTIVE: RefCell<Option<Observation>> = const { RefCell::new(None) };
    // 仅非干扰自测开启的有界位置序列，不写artifact也不进入State。
    static TEST_SEQUENCE: RefCell<Option<Vec<(i64, u32, usize, bool)>>> = const { RefCell::new(None) };
}

pub(super) fn record(state: &State, inserted: bool) {
    TEST_SEQUENCE.with(|sequence| {
        if let Ok(mut sequence) = sequence.try_borrow_mut()
            && let Some(sequence) = sequence.as_mut()
            && sequence.len() < EVENT_LIMIT
        {
            sequence.push((
                state.pc,
                state.block.into_raw().into_u32(),
                state.cursor,
                inserted,
            ));
        }
    });
    // 不持有借用跨inspect；借用冲突只丢观察，不影响安全搜索。
    ACTIVE.with(|active| {
        if let Ok(mut active) = active.try_borrow_mut()
            && let Some(observation) = active.as_mut()
        {
            observation.observe(state, inserted);
        }
    });
}

/// 独立安全计数在inspect返回后只读复制；缺失副本必须使采集不完整。
pub(super) fn analysis(diagnostics: &FrameDiagnostics) {
    ACTIVE.with(|active| {
        if let Ok(mut active) = active.try_borrow_mut()
            && let Some(observation) = active.as_mut()
        {
            observation.analysis = Some(diagnostics.clone());
        }
    });
}

#[expect(dead_code, reason = "保留已结束的一次性采样审计入口，不重新启用")]
pub(crate) fn start(program: &Program, origin: &GeneratorOrigin, layout: &GeneratorLayout) {
    // 本地普通cargo test不写artifact；hosted采集器提供已验证的绝对路径。
    if std::env::var_os("RYUPY_FRAME_OBSERVATION").is_none() {
        return;
    }
    let mut observation = Observation::new(DYNAMIC_LIMIT);
    let mut ir = Bounded {
        text: String::new(),
        limit: IR_LIMIT,
    };
    let result = write!(
        ir,
        "stage=audit-build-output-not-linked\norigin={origin:#?}\nlayout={layout:#?}\nprogram={program:#?}\n"
    );
    observation.ir_status = if result.is_ok() {
        "complete"
    } else {
        "encoding_or_byte_limit"
    };
    observation.ir = ir.text;
    ACTIVE.with(|active| {
        if let Ok(mut active) = active.try_borrow_mut() {
            *active = Some(observation);
        }
    });
}

fn export(observation: &Observation, directory: &std::path::Path, result: &str) -> io::Result<()> {
    std::fs::create_dir_all(directory)?;
    let mut states = std::fs::File::create(directory.join("states.jsonl"))?;
    for (id, state) in observation.states.iter().enumerate() {
        writeln!(states, "{{\"id\":{id},\"state\":{state}}}")?;
    }
    let mut events = std::fs::File::create(directory.join("events.jsonl"))?;
    for event in &observation.events {
        events.write_all(event.as_bytes())?;
    }
    std::fs::write(directory.join("ir.txt"), &observation.ir)?;
    let mut meta = Bounded {
        text: String::new(),
        limit: 8192,
    };
    // result只来自原分析，编码/IO错误由调用者报告，绝不回传给inspect。
    write!(meta, "{{\"schema\":1,\"observer_status\":\"{}\",\"ir_status\":\"{}\",\"attempted\":{},\"retained_states\":{},\"retained_events\":{},\"limits\":{{\"states\":8192,\"events\":16384,\"dynamic_bytes\":16777216,\"ir_bytes\":2097152}},\"analysis_result\":",
        observation.status, observation.ir_status, observation.attempted, observation.states.len(), observation.events.len())
        .map_err(|_| io::Error::other("metadata encoding"))?;
    quoted(&mut meta, result).map_err(|_| io::Error::other("result encoding"))?;
    meta.write_str(",\"analysis_diagnostics\":")
        .map_err(|_| io::Error::other("metadata encoding"))?;
    encode_diagnostics(&mut meta, observation.analysis.as_ref())
        .map_err(|_| io::Error::other("diagnostics encoding"))?;
    meta.write_str("}\n")
        .map_err(|_| io::Error::other("metadata encoding"))?;
    std::fs::write(directory.join("metadata.json"), meta.text)
}

fn encode_diagnostics(out: &mut Bounded, diagnostics: Option<&FrameDiagnostics>) -> fmt::Result {
    let Some(value) = diagnostics else {
        return out.write_str("null");
    };
    write!(
        out,
        "{{\"processed\":{},\"unique_states\":{},\"duplicate_states\":{},\"self_edges\":{},\"value_cells\":{},\"states_by_pc\":{{",
        value.processed,
        value.unique_states,
        value.duplicate_states,
        value.self_edges,
        value.value_cells
    )?;
    for (i, (pc, count)) in value.states_by_pc.iter().enumerate() {
        if i > 0 {
            out.write_char(',')?;
        }
        write!(out, "\"{pc}\":{count}")?;
    }
    out.write_str("}}")
}

#[expect(dead_code, reason = "保留已结束的一次性采样审计入口，不重新启用")]
pub(crate) fn finish(result: &Result<FrameObligations<'_>, String>) {
    let observation = ACTIVE.with(|active| {
        active
            .try_borrow_mut()
            .ok()
            .and_then(|mut value| value.take())
    });
    let Some(observation) = observation else {
        return;
    };
    let Some(directory) = std::env::var_os("RYUPY_FRAME_OBSERVATION") else {
        return;
    };
    let outcome = match result {
        Ok(_) => "ok",
        Err(error) => error.as_str(),
    };
    if let Err(error) = export(&observation, std::path::Path::new(&directory), outcome) {
        let _ = writeln!(
            io::stderr(),
            "frame observer export failed: {error}; original analysis retained"
        );
    }
}

#[cfg(test)]
#[path = "trace_frame_observer_tests.rs"]
mod tests;
