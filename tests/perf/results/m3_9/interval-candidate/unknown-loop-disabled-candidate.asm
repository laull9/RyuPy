; Assembly listing for method RyuPy.Generated.Module:m0_unknown_loop(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 4 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x50]
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 36
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 37
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, rax
       mov      rsi, r13
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       mov      r13, gword ptr [rbp-0x40]
       jmp      G_M000_IG13
 
G_M000_IG03:
       mov      edi, 38
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rbx
       call     [RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x38], rax
       movzx    rdi, byte  ptr [r13+0x18]
       test     edi, edi
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      rsi, gword ptr [r13+0x08]
       jmp      G_M000_IG11
 
G_M000_IG05:
       test     rax, rax
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r13+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp-0x38]
       jl       SHORT G_M000_IG08
       mov      qword ptr [r13+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r13+0x08], rdi
       jmp      G_M000_IG12
 
G_M000_IG08:
       mov      rsi, gword ptr [r13+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG11
       mov      rcx, qword ptr [r13+0x10]
       mov      qword ptr [rbp-0x30], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG09
       test     rdi, rdi
       jl       G_M000_IG17
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG16
       mov      rcx, gword ptr [rcx+8*rdi+0x10]
       mov      rsi, rcx
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x30]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG10:
       lea      rdi, bword ptr [r13+0x08]
       mov      gword ptr [rbp-0x48], rsi
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x48]
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG11:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
 
G_M000_IG12:
       add      r12, 1
       jo       SHORT G_M000_IG17
 
G_M000_IG13:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG14:
       mov      rdi, r13
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      r13, rax
       mov      edi, 39
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG15:
       add      rsp, 40
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG16:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG17:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 515

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_unknown_loop(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_unknown_loop(System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 35

