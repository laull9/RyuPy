# 官方Ubuntu24.04 linux/amd64 manifest，2026-11-12从registry核验；不是滚动tag。
FROM ubuntu@sha256:77eb101c607d6e3d4f5ad0c8a02c2136c689b98aba1f359e6502ba517e5c57ec
RUN apt-get update && apt-get install -y --no-install-recommends libicu74 libssl3t64 libstdc++6 libgcc-s1 zlib1g ca-certificates && rm -rf /var/lib/apt/lists/*
# 系统库允许存在，Python/.NET宿主不允许存在；同一个构建后的镜像ID供两工具使用。
RUN for p in python python3 dotnet; do if command -v "$p"; then exit 9; fi; done
WORKDIR /tmp
