; Assembly listing for method RyuPy.Generated.Module:m0_sum_range(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 4 inlinees without PGO data

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
       mov      r15, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       xor      r12d, r12d
       xor      eax, eax
       mov      dword ptr [rbp-0x34], eax
       test     rbx, rbx
       je       G_M000_IG11
 
G_M000_IG03:
       mov      rdi, qword ptr [rbx]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG10
 
G_M000_IG04:
       mov      r12d, 1
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x30], rcx
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      r12d, 1
       mov      rcx, qword ptr [rbx+0x08]
       mov      rax, rcx
       mov      edx, edi
 
G_M000_IG06:
       add      r13, 1
       jo       G_M000_IG32
       mov      qword ptr [rbp-0x30], rcx
       mov      dword ptr [rbp-0x34], edx
 
G_M000_IG07:
       cmp      r13, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG26
 
G_M000_IG08:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG12
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r13
       mov      gword ptr [rbp-0x40], rsi
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       xor      edi, edi
       xor      ecx, ecx
       mov      qword ptr [rbp-0x30], r12
       mov      r12d, ecx
       jmp      SHORT G_M000_IG07
 
G_M000_IG11:
       xor      ecx, ecx
       mov      qword ptr [rbp-0x30], r12
       mov      r12d, ecx
       jmp      SHORT G_M000_IG07
 
G_M000_IG12:
       test     rdi, rdi
       jl       G_M000_IG32
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG31
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x40], rsi
 
G_M000_IG13:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r12d, r12d
       je       SHORT G_M000_IG19
 
G_M000_IG14:
       mov      rax, gword ptr [rbp-0x40]
       test     rax, rax
       je       SHORT G_M000_IG18
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG17
 
G_M000_IG16:
       mov      gword ptr [rbp-0x40], rax
       jmp      SHORT G_M000_IG19
 
G_M000_IG17:
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [rbp-0x30]
       lea      rdx, [rcx+rdi]
       mov      rsi, rcx
       xor      rsi, rdx
       xor      rdi, rdx
       and      rdi, rsi
       mov      qword ptr [rbp-0x30], rcx
       jl       SHORT G_M000_IG19
       mov      rcx, rdx
       mov      edx, 1
       jmp      G_M000_IG06
 
G_M000_IG18:
       mov      gword ptr [rbp-0x40], rax
 
G_M000_IG19:
       cmp      dword ptr [rbp-0x34], 0
       je       G_M000_IG23
       mov      rcx, qword ptr [rbp-0x30]
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG20
       test     rdi, rdi
       jl       G_M000_IG32
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG31
       mov      rbx, gword ptr [rdx+8*rsi+0x10]
       mov      r12, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      r12, qword ptr [rbp-0x30]
       mov      qword ptr [rbx+0x08], r12
 
G_M000_IG21:
       mov      rdx, gword ptr [rbp-0x40]
       mov      rsi, rbx
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rbx, rax
       xor      edx, edx
       xor      edi, edi
       test     rbx, rbx
       jne      SHORT G_M000_IG24
 
G_M000_IG22:
       xor      eax, eax
       mov      rcx, r12
       mov      edx, edi
       mov      r12d, eax
       jmp      G_M000_IG06
 
G_M000_IG23:
       mov      r12, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG21
 
G_M000_IG24:
       mov      rax, qword ptr [rbx]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       je       G_M000_IG05
 
G_M000_IG25:
       xor      eax, eax
       mov      rcx, r12
       mov      edx, edi
       mov      r12d, eax
       jmp      G_M000_IG06
 
G_M000_IG26:
       cmp      dword ptr [rbp-0x34], 0
       je       SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rcx, qword ptr [rbp-0x30]
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG28
       test     rdi, rdi
       jl       SHORT G_M000_IG32
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      SHORT G_M000_IG31
       mov      rbx, gword ptr [rdx+8*rsi+0x10]
       jmp      SHORT G_M000_IG29
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      r12, qword ptr [rbp-0x30]
       mov      qword ptr [rbx+0x08], r12
 
G_M000_IG29:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG30:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG31:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG32:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 744

