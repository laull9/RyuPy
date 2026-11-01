; Assembly listing for method RyuPy.Generated.Module:m0_sum_range(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 288
       lea      rbp, [rsp+0x120]
       xor      eax, eax
       mov      qword ptr [rbp-0x118], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x120], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      qword ptr [rbp-0x80], 1
       xor      eax, eax
       mov      qword ptr [rbp-0xC0], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0xC4], 0
       jne      G_M000_IG14
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x120]
       mov      esi, 109
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x80], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x84], eax
       mov      rax, qword ptr [rbp-0x70]
       cmp      rax, qword ptr [rbp-0x78]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x88], eax
       mov      rax, qword ptr [rbp-0x70]
       cmp      rax, qword ptr [rbp-0x78]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      eax, dword ptr [rbp-0x84]
       and      eax, dword ptr [rbp-0x88]
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       cmp      dword ptr [rbp-0x84], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x8C]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x90]
       or       eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       cmp      dword ptr [rbp-0x9C], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xA8], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0xD8], rax
       cmp      dword ptr [rbp-0xC4], 0
       jne      G_M000_IG27
       jmp      G_M000_IG20
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       G_M000_IG34
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       cmp      gword ptr [rbp-0x40], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xC8], eax
       mov      eax, dword ptr [rbp-0xC4]
       and      eax, dword ptr [rbp-0xC8]
       movzx    rax, al
       mov      dword ptr [rbp-0xCC], eax
       cmp      dword ptr [rbp-0xCC], 0
       jne      G_M000_IG33
       jmp      G_M000_IG30
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xB8]
 
G_M000_IG10:
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG11:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x120]
       mov      esi, 260
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG13:
       jmp      G_M000_IG07
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xC0], rax
       jmp      G_M000_IG03
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xC0], rax
       jmp      G_M000_IG11
 
G_M000_IG16:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG18
 
G_M000_IG17:
       lea      rdi, [rbp-0x120]
       mov      esi, 291
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG18:
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rsi, gword ptr [rbp-0x40]
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0xC4], 0
       jne      G_M000_IG15
       jmp      G_M000_IG11
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG16
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x120]
       mov      esi, 353
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       cmp      gword ptr [rbp-0x40], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xC8], eax
       mov      eax, dword ptr [rbp-0xC4]
       and      eax, dword ptr [rbp-0xC8]
       movzx    rax, al
       mov      dword ptr [rbp-0xCC], eax
       cmp      dword ptr [rbp-0xCC], 0
       jne      SHORT G_M000_IG19
       jmp      G_M000_IG16
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0xC0], rax
       xor      rax, rax
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG11
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x120]
       mov      esi, 389
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE8], rax
       mov      rax, qword ptr [rbp-0xC0]
       add      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF0], rax
       mov      rax, qword ptr [rbp-0xC0]
       xor      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xE8]
       xor      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xF8]
       and      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x108], rax
       cmp      qword ptr [rbp-0x108], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x10C], eax
       cmp      dword ptr [rbp-0x10C], 0
       jne      G_M000_IG23
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG20
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x120]
       mov      esi, 456
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xDC], eax
       cmp      dword ptr [rbp-0xDC], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG20
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x120]
       mov      esi, 483
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xB8], rax
       jmp      G_M000_IG09
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x40], rax
       jmp      SHORT G_M000_IG30
 
G_M000_IG34:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1541

; Assembly listing for method RyuPy.Generated.Module:m0_sum_range(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x104
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 3 single block inlinees; 9 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 64
       mov      qword ptr [rsp+0x168], r15
       mov      qword ptr [rsp+0x160], r14
       mov      qword ptr [rsp+0x158], r13
       mov      qword ptr [rsp+0x150], r12
       mov      qword ptr [rsp+0x148], rbx
       lea      rbp, [rsp+0x40]
       mov      r15, gword ptr [rbp+0xF0]
       mov      rbx, qword ptr [rbp+0xC0]
       mov      r13, qword ptr [rbp+0xB8]
       mov      rax, qword ptr [rbp+0xB0]
       mov      r12, qword ptr [rbp+0x70]
       mov      r14d, dword ptr [rbp+0x6C]
 
G_M000_IG02:
       test     rax, rax
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0x2C], edx
       jmp      G_M000_IG15
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], rbx
 
G_M000_IG04:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG38
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG38
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 10
 
G_M000_IG07:
       test     r14d, r14d
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       test     rax, rax
       jne      G_M000_IG18
 
G_M000_IG09:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r14d, edi
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      G_M000_IG21
 
G_M000_IG11:
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG12:
       mov      r15, rax
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG13:
       mov      rdx, rax
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
       test     r15, r15
       jne      G_M000_IG22
 
G_M000_IG14:
       xor      r14d, r14d
       mov      rax, qword ptr [rbp+0xB0]
       mov      edx, dword ptr [rbp-0x2C]
 
G_M000_IG15:
       mov      qword ptr [rbp+0xB0], rax
       add      rbx, rax
       jo       G_M000_IG39
       mov      edi, edx
       cmp      rbx, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      rbx, r13
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG25
 
G_M000_IG16:
       lea      rsi, [rbx+0x05]
       cmp      rsi, 261
       ja       G_M000_IG03
 
G_M000_IG17:
       test     rsi, rsi
       jl       G_M000_IG39
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG38
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       mov      r8, rdi
       mov      rax, r8
       jmp      G_M000_IG04
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG09
 
G_M000_IG19:
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rax+0x08]
       lea      rsi, [r12+rdi]
       mov      r8, r12
       xor      r8, rsi
       xor      rdi, rsi
       and      rdi, r8
       mov      rax, gword ptr [rbp-0x38]
       jl       G_M000_IG09
 
G_M000_IG20:
       mov      r12, rsi
       xor      r15, r15
       mov      rax, qword ptr [rbp+0xB0]
       mov      edx, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG21:
       test     r15, r15
       jl       G_M000_IG39
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      r15, 262
       jae      G_M000_IG38
       mov      rsi, gword ptr [rsi+8*r15+0x10]
       mov      r15, rsi
       mov      gword ptr [rbp-0x38], rax
       mov      rax, r15
       jmp      G_M000_IG12
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      r14d, 1
       mov      r12, qword ptr [r15+0x08]
       mov      rax, qword ptr [rbp+0xB0]
       mov      edx, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG24:
       xor      r14d, r14d
       mov      rax, qword ptr [rbp+0xB0]
       mov      edx, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG25:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r14d, edi
       je       SHORT G_M000_IG29
 
G_M000_IG26:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      G_M000_IG37
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG28:
       mov      r15, rax
 
G_M000_IG29:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rax-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG38
       cmp      dword ptr [rdi+4*r8+0x10], 11
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       cmp      esi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG38
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 11
 
G_M000_IG32:
       mov      rcx, qword ptr [rbp+0xE0]
       test     rcx, rcx
       jle      SHORT G_M000_IG35
 
G_M000_IG33:
       cdqe     
       cmp      rcx, rax
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       dec      ecx
       mov      dword ptr [(reloc)], ecx
 
G_M000_IG35:
       mov      rax, r15
 
G_M000_IG36:
       add      rsp, 328
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG37:
       test     r15, r15
       jl       SHORT G_M000_IG39
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      r15, 262
       jae      SHORT G_M000_IG38
       mov      rax, gword ptr [rsi+8*r15+0x10]
       jmp      G_M000_IG28
 
G_M000_IG38:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG39:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 849

