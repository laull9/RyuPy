; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 3 inlinees with PGO data; 7 single block inlinees; 15 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 136
       lea      rbp, [rsp+0xB0]
       xor      eax, eax
       mov      qword ptr [rbp-0x78], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x70], xmm8
       mov      qword ptr [rbp-0x60], rax
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
       jmp      G_M000_IG33
 
G_M000_IG03:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0xA0], rax
 
G_M000_IG04:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rax, gword ptr [rsi]
       mov      gword ptr [rbp-0xA8], rax
       mov      rcx, 0xD1FFAB1E
       mov      qword ptr [rbp-0x98], rcx
       mov      rdx, gword ptr [rbp-0xA0]
       cmp      qword ptr [rdx], rcx
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp-0xA0], rdx
       mov      rsi, rdx
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, qword ptr [rbp-0x98]
       mov      rdx, gword ptr [rbp-0xA0]
 
G_M000_IG06:
       mov      rdx, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x30], rdx
       mov      rax, gword ptr [rbp-0xA8]
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rsi, rax
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp-0xA8]
       mov      rcx, qword ptr [rbp-0x98]
 
G_M000_IG08:
       mov      rdx, qword ptr [rbp-0x30]
       add      rdx, qword ptr [rax+0x08]
       jo       G_M000_IG40
       mov      rax, rdx
       mov      qword ptr [rbp-0x38], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG10
 
G_M000_IG09:
       test     rdi, rdi
       jl       G_M000_IG40
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG39
       mov      rdx, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x38]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG11:
       test     r13, r13
       je       G_M000_IG36
 
G_M000_IG12:
       mov      rax, qword ptr [r13]
       mov      rcx, qword ptr [rbp-0x98]
       cmp      rax, rcx
       jne      G_M000_IG22
 
G_M000_IG13:
       mov      rsi, qword ptr [r13+0x08]
       test     rdx, rdx
       je       G_M000_IG36
 
G_M000_IG14:
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG20
 
G_M000_IG15:
       mov      r13, qword ptr [rdx+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0x58], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rdx, r13
       xor      rdx, rax
       and      rdi, rdx
       jl       SHORT G_M000_IG18
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG16
       test     rdi, rdi
       jl       G_M000_IG40
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG39
       mov      r13, gword ptr [rax+8*rcx+0x10]
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rdi, qword ptr [rbp-0x58]
       mov      qword ptr [r13+0x08], rdi
 
G_M000_IG17:
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0xB0], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x8C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xB0]
       mov      esi, dword ptr [rbp-0x8C]
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, qword ptr [rbp-0x70]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG19:
       jmp      G_M000_IG32
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG36
 
G_M000_IG21:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x80], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x80]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG32
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       test     rdx, rdx
       je       G_M000_IG36
 
G_M000_IG24:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG26:
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG36
 
G_M000_IG27:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG36
 
G_M000_IG29:
       mov      rdi, r13
       test     rdx, rdx
       je       SHORT G_M000_IG36
 
G_M000_IG30:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG36
 
G_M000_IG31:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
 
G_M000_IG32:
       add      r12, 1
       jo       G_M000_IG40
 
G_M000_IG33:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       SHORT G_M000_IG37
 
G_M000_IG34:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG03
 
G_M000_IG35:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0xA0], rax
       jmp      G_M000_IG04
 
G_M000_IG36:
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG37:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG38:
       add      rsp, 136
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG39:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG40:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1099

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

