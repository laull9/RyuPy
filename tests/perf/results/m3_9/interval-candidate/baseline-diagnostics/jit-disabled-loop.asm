; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 12 single block inlinees; 28 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 200
       lea      rbp, [rsp+0xF0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       jmp      G_M000_IG48
 
G_M000_IG03:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0xE0], rax
 
G_M000_IG04:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0xE0]
       test     rax, rax
       je       G_M000_IG25
 
G_M000_IG05:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG15
 
G_M000_IG06:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG25
 
G_M000_IG07:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG13
 
G_M000_IG08:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x48], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x50], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG11
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG09
       test     rax, rax
       jl       G_M000_IG55
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG54
       mov      rdx, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x50]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG10:
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xE8], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xD4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xE8]
       mov      esi, dword ptr [rbp-0xD4]
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG12:
       jmp      G_M000_IG26
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG25
 
G_M000_IG14:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x78], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG26
 
G_M000_IG15:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG21
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rdx, rdx
       je       G_M000_IG25
 
G_M000_IG17:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG19
 
G_M000_IG18:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG19:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG25
 
G_M000_IG20:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x80]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG25
 
G_M000_IG22:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG25
 
G_M000_IG24:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
       mov      rdx, rsi
 
G_M000_IG26:
       test     r13, r13
       je       G_M000_IG51
 
G_M000_IG27:
       mov      rax, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG37
 
G_M000_IG28:
       mov      rsi, qword ptr [r13+0x08]
       test     rdx, rdx
       je       G_M000_IG51
 
G_M000_IG29:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG35
 
G_M000_IG30:
       mov      r13, qword ptr [rdx+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xA0], rax
       mov      rcx, rsi
       xor      rcx, rax
       mov      rdx, r13
       xor      rdx, rax
       and      rcx, rdx
       jl       SHORT G_M000_IG33
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG31
       test     rcx, rcx
       jl       G_M000_IG55
       mov      rdi, rcx
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG54
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rdi, qword ptr [rbp-0xA0]
       mov      qword ptr [r13+0x08], rdi
 
G_M000_IG32:
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0xF0], rax
       mov      ecx, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0xD8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xF0]
       mov      esi, dword ptr [rbp-0xD8]
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rcx, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG34:
       jmp      G_M000_IG47
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG51
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xC8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG47
 
G_M000_IG37:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG43
 
G_M000_IG38:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     rdx, rdx
       je       G_M000_IG51
 
G_M000_IG39:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG41:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG51
 
G_M000_IG42:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG51
 
G_M000_IG44:
       mov      rdi, r13
       test     rdx, rdx
       je       SHORT G_M000_IG51
 
G_M000_IG45:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG51
 
G_M000_IG46:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
 
G_M000_IG47:
       add      r12, 1
       jo       G_M000_IG55
 
G_M000_IG48:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG52
 
G_M000_IG49:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG03
 
G_M000_IG50:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0xE0], rax
       jmp      G_M000_IG04
 
G_M000_IG51:
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG52:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG53:
       add      rsp, 200
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG54:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG55:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1493

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_loop_diamond(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rsi+0x08]
       call     [RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 35

