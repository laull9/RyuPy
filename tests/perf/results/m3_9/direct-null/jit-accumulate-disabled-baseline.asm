; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x70]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r12, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, rax
       mov      rsi, r15
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       mov      r15, gword ptr [rbp-0x58]
       jmp      G_M000_IG16
 
G_M000_IG03:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG05
 
G_M000_IG04:
       test     rdi, rdi
       jl       G_M000_IG20
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG19
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      gword ptr [rbp-0x60], rax
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      gword ptr [rbp-0x60], rax
 
G_M000_IG06:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x50], rax
       movzx    rdi, byte  ptr [r15+0x18]
       test     edi, edi
       jne      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rsi, gword ptr [r15+0x08]
       jmp      G_M000_IG14
 
G_M000_IG08:
       test     rax, rax
       je       SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r15+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp-0x50]
       jl       SHORT G_M000_IG11
       mov      qword ptr [r15+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r15+0x08], rdi
       jmp      G_M000_IG15
 
G_M000_IG11:
       mov      rsi, gword ptr [r15+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG14
       mov      rcx, qword ptr [r15+0x10]
       mov      qword ptr [rbp-0x48], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG12
       test     rdi, rdi
       jl       G_M000_IG20
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG19
       mov      rcx, gword ptr [rcx+8*rdi+0x10]
       mov      rsi, rcx
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x48]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG13:
       lea      rdi, bword ptr [r15+0x08]
       mov      gword ptr [rbp-0x68], rsi
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x68]
       mov      rax, gword ptr [rbp-0x50]
 
G_M000_IG14:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
 
G_M000_IG15:
       add      r13, 1
       jo       SHORT G_M000_IG20
 
G_M000_IG16:
       cmp      r13, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG17:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      rbx, rax
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG18:
       add      rsp, 72
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
 
; Total bytes of code 690

