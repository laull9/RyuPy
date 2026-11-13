# RyuPy 常用命令。`make help` 查看全部。
.DEFAULT_GOAL := help
FILE ?= tests/e2e/phase1/m1_0_print_int.py
EMIT ?= hir
# 差分测试的标准答案必须来自固定的小版本（见 tests/pinned-python.txt）。
# 优先用 uv 找到精确版本；找不到时回退 python3，由脚本打印警告。
PYTHON_VERSION := $(shell cat tests/pinned-python.txt 2>/dev/null)
PYTHON ?= $(shell uv python find $(PYTHON_VERSION) 2>/dev/null || echo python3)
export PYTHON

# 编译期隔离候选贯穿下列直接Cargo recipe；间接bench脚本不消费此变量。
# 指定check/projects门禁保持原CLI与项目mode；性能另用确切冻结工具。
CARGO_FEATURES ?=
CARGO_FEATURE_ARGS = $(if $(strip $(CARGO_FEATURES)),--features $(CARGO_FEATURES))
# 完整CI仅增加本轮TRX输出参数，不新启动测试进程。
DOTNET_TEST_ARGS ?=

# 发行包命名：只产出宿主平台产物，不假装支持交叉编译。
VERSION := $(shell sed -n 's/^version = "\(.*\)"/\1/p' Cargo.toml | head -1)
HOST_OS := $(shell uname -s | tr '[:upper:]' '[:lower:]')
HOST_ARCH := $(shell uname -m)
RELEASE_NAME := ryupy-$(VERSION)-$(HOST_OS)-$(HOST_ARCH)
RELEASE_DIR := releases/$(RELEASE_NAME)

.PHONY: help
help: ## 显示帮助
	@grep -hE '^[a-zA-Z0-9_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

.PHONY: runtime
runtime: ## 构建 C# 运行时 RyuPy.Runtime（Release）
	dotnet build runtime/RyuPy.Runtime -c Release -nologo -v q

.PHONY: build
build: runtime ## 打发行包：Release CLI + 运行时 → releases/ryupy-<版本>-<平台>.tar.gz
	cargo build $(CARGO_FEATURE_ARGS) --release -p ryupy-cli
	@set -eu; \
	target_dir=$$(cargo metadata --format-version 1 --no-deps | "$(PYTHON)" -c 'import json,sys; print(json.load(sys.stdin)["target_directory"])'); \
	rm -rf "$(RELEASE_DIR)"; \
	mkdir -p "$(RELEASE_DIR)/bin" "$(RELEASE_DIR)/runtime"; \
	cp "$$target_dir/release/ryupy" "$(RELEASE_DIR)/bin/ryupy"; \
	cp runtime/RyuPy.Runtime/bin/Release/net10.0/*.dll runtime/RyuPy.Runtime/bin/Release/net10.0/*.props "$(RELEASE_DIR)/runtime/"; \
	cp runtime/RyuPy.Runtime/bin/Release/net10.0/ThirdPartyNotices.sqlite3.txt "$(RELEASE_DIR)/runtime/"; \
	runtime_rid=$$(dotnet msbuild runtime/RyuPy.Runtime -nologo -getProperty:NETCoreSdkRuntimeIdentifier); \
	mkdir -p "$(RELEASE_DIR)/runtime/runtimes/$$runtime_rid"; \
	cp -R "runtime/RyuPy.Runtime/bin/Release/net10.0/runtimes/$$runtime_rid/native" "$(RELEASE_DIR)/runtime/runtimes/$$runtime_rid/"; \
	tar -czf "releases/$(RELEASE_NAME).tar.gz" -C releases "$(RELEASE_NAME)"; \
	echo "发行包：releases/$(RELEASE_NAME).tar.gz"; \
	tmp=$$(mktemp -d); \
	trap 'rm -rf "$$tmp"' EXIT; \
	printf 'print("release ok")\n' > "$$tmp/hello.py"; \
	env -u RYUPY_RUNTIME_DIR "$(RELEASE_DIR)/bin/ryupy" build "$$tmp/hello.py" -o "$$tmp/out"; \
	out=$$(env -u RYUPY_RUNTIME_DIR dotnet "$${tmp}/out/hello.dll"); \
	test "$${out}" = "release ok"; \
	echo "自检：发行包可独立定位运行时并运行示例（$${out}）"

.PHONY: fmt
fmt: ## 格式化 Rust 代码
	cargo fmt --all

.PHONY: lint
lint: ## fmt 检查 + clippy（警告视为错误）
	cargo fmt --all --check
	cargo clippy $(CARGO_FEATURE_ARGS) --workspace --all-targets -- -D warnings

.PHONY: test
test: runtime python-baseline ## Python 基线 + 单元测试 + 快照测试（不含编译 e2e）
	@echo "主门明确隔离旧反向frame证明实验4项（frame 3 + diamond 1），状态仍为已知失败/本门未执行；其余安全负例照跑。独立运行：make trace-frame-proof-experiment"
	dotnet build runtime/RyuPy.Runtime.Tests -nologo -v q
	@if [ -n "$$FULL_CHECK_EVIDENCE" ]; then "$(PYTHON)" -B tests/aot/wcwidth_ci/build_identity.py runtimes-before; fi
	dotnet test runtime/RyuPy.Runtime.Tests --no-build -nologo -v q $(DOTNET_TEST_ARGS)
	cargo test $(CARGO_FEATURE_ARGS) --workspace --exclude ryupy-e2e

.PHONY: trace-frame-proof-experiment
trace-frame-proof-experiment: ## 独立运行四项旧已知失败实验；保留原断言，不作为产品准入
	@status=0; \
	cargo test $(CARGO_FEATURE_ARGS) -p ryupy-mir --lib trace_frame_tests:: -- --ignored --test-threads=1 || status=1; \
	cargo test $(CARGO_FEATURE_ARGS) -p ryupy-mir --lib trace_frame_dataflow::tests::actual_mir_diamond_reaches_fixed_point -- --ignored --exact --test-threads=1 || status=1; \
	exit $$status

.PHONY: python-baseline
python-baseline: ## 自检 Phase 1–3 的 CPython 用例与预期错误退出
	"$(PYTHON)" -B tests/test_case_manifest.py
	"$(PYTHON)" -B tests/test_project_gate.py
	"$(PYTHON)" tests/run_python_baseline.py

.PHONY: python-extensions
python-extensions: ## 自检 Phase 4 扩展基线（需已安装 NumPy、sqlite3、zlib）
	"$(PYTHON)" tests/run_python_baseline.py --phase phase4

.PHONY: e2e
e2e: runtime ## 端到端差分测试（跳过 pending 用例）
	# --test-threads=1：让用例按名字顺序执行，输出带 [已跑/总数] 进度、不会乱序。
	cargo test $(CARGO_FEATURE_ARGS) -p ryupy-e2e -- --test-threads=1

.PHONY: e2e-all
e2e-all: runtime ## 端到端测试，包括 pending 用例（查看进度用）
	-cargo test $(CARGO_FEATURE_ARGS) -p ryupy-e2e -- --include-ignored --test-threads=1

.PHONY: cases-selftest
cases-selftest: ## 只自检用例与框架：多文件/参数/环境/输入/错误路径（不编译 RyuPy）
	E2E_PYTHON_ONLY=1 cargo test $(CARGO_FEATURE_ARGS) -p ryupy-e2e -- --include-ignored --test-threads=1

.PHONY: projects-fetch
projects-fetch: ## 抓取固定项目集的 sdist 源码（需要联网，源码不进版本库）
	"$(PYTHON)" tests/fetch_projects.py

.PHONY: projects-baseline
projects-baseline: ## 用固定 CPython 记录项目入口的基线答案
	"$(PYTHON)" tests/run_projects_baseline.py

.PHONY: projects
projects: runtime ## 项目级运行：编译并运行项目入口，对照 CPython 基线
	cargo run $(CARGO_FEATURE_ARGS) -q -p ryupy-e2e --bin projects

# 项目门禁需要预先准备的锁定原源，不下载、不改基线。每次独立运行完整20项。
PROJECTS_STORE ?= $(CURDIR)/third_party/projects
PROJECTS_OUTPUT ?=

.PHONY: projects-runner projects-preserve projects-promote
projects-runner: runtime
	cargo build $(CARGO_FEATURE_ARGS) -p ryupy-e2e --bin projects

projects-preserve: projects-runner ## 完整20项默认JIT，逐项保护当前九项（不是默认晋升）
	@set -eu; target_dir=$$(cargo metadata --format-version 1 --no-deps | "$(PYTHON)" -c 'import json,sys; print(json.load(sys.stdin)["target_directory"])'); \
	"$(PYTHON)" -B tests/run_projects_gate.py --gate preserve --runner "$$target_dir/debug/projects" --source-store "$(abspath $(PROJECTS_STORE))" $(if $(PROJECTS_OUTPUT),--output "$(abspath $(PROJECTS_OUTPUT))")

projects-promote: projects-runner ## 完整20项默认JIT，保九项并要求历史progress回归恢复
	@set -eu; target_dir=$$(cargo metadata --format-version 1 --no-deps | "$(PYTHON)" -c 'import json,sys; print(json.load(sys.stdin)["target_directory"])'); \
	"$(PYTHON)" -B tests/run_projects_gate.py --gate promote --runner "$$target_dir/debug/projects" --source-store "$(abspath $(PROJECTS_STORE))" $(if $(PROJECTS_OUTPUT),--output "$(abspath $(PROJECTS_OUTPUT))")

# 基准参数：make bench BENCH="dynamic strings" ARGS="--save before"
BENCH ?=
ARGS ?=

.PHONY: bench
bench: runtime ## 性能基准：CPython vs RyuPy（BENCH=名字/类别过滤，ARGS=额外参数）
	"$(PYTHON)" tests/run_benchmarks.py --detail $(ARGS) $(BENCH)

.PHONY: bench-aot
bench-aot: runtime ## NativeAOT基准：每例含ILC构建，直接运行原生可执行文件
	"$(PYTHON)" tests/run_benchmarks.py --detail --aot $(ARGS) $(BENCH)

.PHONY: bench-quick
bench-quick: runtime ## 快速基准：1 次热身 + 3 次计时，适合改动后的粗测
	"$(PYTHON)" tests/run_benchmarks.py --quick $(ARGS) $(BENCH)

.PHONY: bench-list
bench-list: ## 列出全部基准、类别、说明与 pending 状态
	"$(PYTHON)" tests/run_benchmarks.py --list

.PHONY: bench-profile
bench-profile: runtime ## 采样剖析热点函数（需 dotnet-trace）：make bench-profile BENCH=sorting
	"$(PYTHON)" tests/run_benchmarks.py --profile $(BENCH)

.PHONY: bench-save
bench-save: runtime ## 测量并另存为基线：make bench-save NAME=before
	"$(PYTHON)" tests/run_benchmarks.py --detail --save $(or $(NAME),baseline) $(ARGS) $(BENCH)

.PHONY: bench-compare
bench-compare: runtime ## 测量并与基线对比：make bench-compare NAME=before
	"$(PYTHON)" tests/run_benchmarks.py --compare $(or $(NAME),baseline) $(ARGS) $(BENCH)

.PHONY: check
check: lint test cases-selftest e2e ## 提交前必跑：lint + test + 用例自检 + e2e

.PHONY: snap
snap: ## 交互式审阅 insta 快照（需 cargo install cargo-insta）
	cargo insta review

.PHONY: dump
dump: ## 打印中间表示：make dump FILE=x.py EMIT=ast|hir|types|mir|cil
	cargo run $(CARGO_FEATURE_ARGS) -q -p ryupy-cli -- dump $(FILE) --emit $(EMIT)

.PHONY: run
run: runtime ## 编译并运行：make run FILE=x.py
	cargo run $(CARGO_FEATURE_ARGS) -q -p ryupy-cli -- run $(FILE)

.PHONY: clean
clean: ## 清理全部产物
	cargo clean
	rm -rf runtime/RyuPy.Runtime/bin runtime/RyuPy.Runtime/obj

.PHONY: upstream
upstream: runtime ## 固定上游移植用例差分（需已准备登记版本的项目源码）
	"$(PYTHON)" tests/run_upstream.py
