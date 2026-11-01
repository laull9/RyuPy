; Assembly listing for method RyuPy.Generated.Module:m0_call_alias(System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 7 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x60]
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r12
       mov      rsi, r14
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       jmp      G_M000_IG25
 
G_M000_IG03:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG05
 
G_M000_IG04:
       test     rdi, rdi
       jl       G_M000_IG29
       mov      rax, 0xD1FFAB1E
       mov      r14, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG28
       mov      r14, gword ptr [r14+8*rdi+0x10]
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      r14, 0xD1FFAB1E
       mov      rdi, r14
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r13
 
G_M000_IG06:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rcx, 0xD1FFAB1E
       cmp      rax, gword ptr [rcx]
       jne      G_M000_IG12
 
G_M000_IG07:
       test     r14, r14
       je       SHORT G_M000_IG12
 
G_M000_IG08:
       mov      rcx, 0xD1FFAB1E
       mov      qword ptr [rbp-0x40], rcx
       cmp      qword ptr [r14], rcx
       jne      SHORT G_M000_IG12
 
G_M000_IG09:
       mov      rdx, qword ptr [r14+0x08]
       mov      rdi, rdx
       add      rdx, rdi
       mov      qword ptr [rbp-0x30], rdx
       xor      rdi, rdx
       jl       SHORT G_M000_IG12
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      r14, qword ptr [rbp-0x30]
       lea      rdi, [r14+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG10
       test     rdi, rdi
       jl       G_M000_IG29
       mov      rax, 0xD1FFAB1E
       mov      r14, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG28
       mov      r14, gword ptr [r14+8*rdi+0x10]
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      rdi, qword ptr [rbp-0x40]
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG11:
       jmp      SHORT G_M000_IG15
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x60], rcx
       mov      rdx, gword ptr [rcx+0x18]
       test     rdx, rdx
       je       SHORT G_M000_IG13
       cmp      dword ptr [rdx+0x08], 1
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x60]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x58], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x58]
 
G_M000_IG14:
       mov      gword ptr [rbp-0x58], rdx
       mov      rdi, rdx
       mov      rdx, r14
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      rdx, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x48]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object]
       mov      r14, rax
 
G_M000_IG15:
       movzx    rdi, byte  ptr [r12+0x18]
       test     edi, edi
       jne      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rsi, gword ptr [r12+0x08]
       jmp      G_M000_IG23
 
G_M000_IG17:
       test     r14, r14
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       mov      rax, rdi
       cmp      qword ptr [r14], rax
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       mov      rax, qword ptr [r14+0x08]
       mov      rdi, qword ptr [r12+0x10]
       lea      rcx, [rdi+rax]
       xor      rdi, rcx
       xor      rax, rcx
       and      rdi, rax
       jl       SHORT G_M000_IG20
       mov      qword ptr [r12+0x10], rcx
       xor      rdi, rdi
       mov      gword ptr [r12+0x08], rdi
       jmp      G_M000_IG24
 
G_M000_IG20:
       mov      rsi, gword ptr [r12+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG23
       mov      rax, qword ptr [r12+0x10]
       mov      qword ptr [rbp-0x38], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG21
       test     rdi, rdi
       jl       G_M000_IG29
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG28
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      rsi, rax
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x38]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG22:
       lea      rdi, bword ptr [r12+0x08]
       mov      gword ptr [rbp-0x50], rsi
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x50]
 
G_M000_IG23:
       mov      rdx, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
 
G_M000_IG24:
       add      r13, 1
       jo       SHORT G_M000_IG29
 
G_M000_IG25:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG26:
       mov      rdi, r12
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      r14, rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG27:
       add      rsp, 56
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG28:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG29:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 857

; Assembly listing for method RyuPy.Generated.Module:m0_call_direct(System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 5 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r12
       mov      rsi, r14
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       jmp      G_M000_IG16
 
G_M000_IG03:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG05
 
G_M000_IG04:
       test     rdi, rdi
       jl       G_M000_IG20
       mov      rax, 0xD1FFAB1E
       mov      r14, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG19
       mov      r14, gword ptr [r14+8*rdi+0x10]
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r13
 
G_M000_IG06:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
       mov      r14, rax
       movzx    rdi, byte  ptr [r12+0x18]
       test     edi, edi
       jne      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rsi, gword ptr [r12+0x08]
       jmp      G_M000_IG14
 
G_M000_IG08:
       test     r14, r14
       je       SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      rdi, qword ptr [r14+0x08]
       mov      rax, qword ptr [r12+0x10]
       lea      rcx, [rax+rdi]
       xor      rax, rcx
       xor      rdi, rcx
       and      rdi, rax
       jl       SHORT G_M000_IG11
       mov      qword ptr [r12+0x10], rcx
       xor      rdi, rdi
       mov      gword ptr [r12+0x08], rdi
       jmp      G_M000_IG15
 
G_M000_IG11:
       mov      rsi, gword ptr [r12+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG14
       mov      rax, qword ptr [r12+0x10]
       mov      qword ptr [rbp-0x30], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG12
       test     rdi, rdi
       jl       G_M000_IG20
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG19
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x30]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG13:
       lea      rdi, bword ptr [r12+0x08]
       mov      gword ptr [rbp-0x38], rsi
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x38]
 
G_M000_IG14:
       mov      rdx, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
 
G_M000_IG15:
       add      r13, 1
       jo       SHORT G_M000_IG20
 
G_M000_IG16:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG17:
       mov      rdi, r12
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      r14, rax
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG18:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG19:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG20:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 570

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_call_alias(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_call_alias(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_call_direct(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_call_direct(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

