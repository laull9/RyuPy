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
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x118], 0x3E8
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
       xor      eax, eax
       mov      dword ptr [rbp-0xC8], eax
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
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x118]
       mov      esi, 112
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
       mov      gword ptr [rbp-0xD0], rax
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
       cmp      dword ptr [rbp-0xC8], 0
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
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x118]
       mov      esi, 250
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
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG18
 
G_M000_IG17:
       lea      rdi, [rbp-0x118]
       mov      esi, 281
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG18:
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0x40]
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xC8], eax
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
       xor      eax, eax
       mov      dword ptr [rbp-0xC8], eax
       jmp      G_M000_IG16
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x118]
       mov      esi, 349
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       cmp      dword ptr [rbp-0xC8], 0
       jne      SHORT G_M000_IG19
       jmp      G_M000_IG16
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xC0], rax
       mov      dword ptr [rbp-0xC8], 1
       jmp      G_M000_IG11
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x118]
       mov      esi, 373
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE0], rax
       mov      rax, qword ptr [rbp-0xC0]
       add      rax, qword ptr [rbp-0xE0]
       mov      qword ptr [rbp-0xE8], rax
       mov      rax, qword ptr [rbp-0xC0]
       xor      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF0], rax
       mov      rax, qword ptr [rbp-0xE0]
       xor      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xF0]
       and      rax, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x100], rax
       cmp      qword ptr [rbp-0x100], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x104], eax
       cmp      dword ptr [rbp-0x104], 0
       jne      G_M000_IG23
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG20
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x118]
       mov      esi, 440
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0xD4], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG20
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x118]
       mov      esi, 467
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
       xor      eax, eax
       mov      dword ptr [rbp-0xC8], eax
       jmp      SHORT G_M000_IG30
 
G_M000_IG34:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1494

; Assembly listing for method RyuPy.Generated.Module:m0_sum_range(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xfa
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
       mov      r14, qword ptr [rbp+0xB8]
       mov      rcx, qword ptr [rbp+0xB0]
       mov      r12d, dword ptr [rbp+0x6C]
       mov      r13d, dword ptr [rbp+0x68]
 
G_M000_IG02:
       test     rcx, rcx
       setg     sil
       movzx    rsi, sil
       mov      dword ptr [rbp-0x2C], esi
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
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG38
       cmp      dword ptr [r9+4*r10+0x10], 10
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG38
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 10
 
G_M000_IG07:
       test     r12d, r12d
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       test     rax, rax
       jne      G_M000_IG18
 
G_M000_IG09:
       test     r13d, r13d
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      r8, qword ptr [rbp+0x70]
       lea      r15, [r8+0x05]
       cmp      r15, 261
       jbe      G_M000_IG21
 
G_M000_IG11:
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x70]
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
       xor      r13d, r13d
       test     r15, r15
       jne      G_M000_IG22
 
G_M000_IG14:
       xor      r12d, r12d
       mov      rcx, qword ptr [rbp+0xB0]
       mov      esi, dword ptr [rbp-0x2C]
 
G_M000_IG15:
       mov      qword ptr [rbp+0xB0], rcx
       add      rbx, rcx
       jo       G_M000_IG39
       mov      edi, esi
       cmp      rbx, r14
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       cmp      rbx, r14
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG25
 
G_M000_IG16:
       lea      r8, [rbx+0x05]
       cmp      r8, 261
       ja       G_M000_IG03
 
G_M000_IG17:
       test     r8, r8
       jl       G_M000_IG39
       mov      rdi, r8
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG38
       mov      rdi, gword ptr [r8+8*rdi+0x10]
       mov      r9, rdi
       mov      rax, r9
       jmp      G_M000_IG04
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG09
 
G_M000_IG19:
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      r8, qword ptr [rbp+0x70]
       lea      r9, [r8+rdi]
       mov      r10, r8
       xor      r10, r9
       xor      rdi, r9
       and      rdi, r10
       mov      qword ptr [rbp+0x70], r8
       mov      rax, gword ptr [rbp-0x38]
       jl       G_M000_IG09
 
G_M000_IG20:
       mov      r8, r9
       mov      r13d, 1
       mov      qword ptr [rbp+0x70], r8
       mov      rcx, qword ptr [rbp+0xB0]
       mov      esi, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG21:
       test     r15, r15
       jl       G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      r15, 262
       jae      G_M000_IG38
       mov      rdi, gword ptr [rdi+8*r15+0x10]
       mov      r15, rdi
       mov      gword ptr [rbp-0x38], rax
       mov      rax, r15
       jmp      G_M000_IG12
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      r12d, 1
       mov      r8, qword ptr [r15+0x08]
       mov      rax, r8
       mov      qword ptr [rbp+0x70], rax
       mov      rcx, qword ptr [rbp+0xB0]
       mov      esi, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG24:
       xor      r12d, r12d
       mov      rcx, qword ptr [rbp+0xB0]
       mov      esi, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG25:
       test     r13d, r13d
       je       SHORT G_M000_IG29
 
G_M000_IG26:
       mov      rax, qword ptr [rbp+0x70]
       lea      r15, [rax+0x05]
       cmp      r15, 261
       jbe      G_M000_IG37
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x70]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG28:
       mov      r15, rax
 
G_M000_IG29:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdi, rcx
       lea      esi, [rax-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG38
       cmp      dword ptr [rdi+4*r8+0x10], 11
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       cmp      esi, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG38
       mov      edi, esi
       mov      dword ptr [rcx+4*rdi+0x10], 11
 
G_M000_IG32:
       mov      rdx, qword ptr [rbp+0xE0]
       test     rdx, rdx
       jle      SHORT G_M000_IG35
 
G_M000_IG33:
       cdqe     
       cmp      rdx, rax
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       dec      edx
       mov      dword ptr [(reloc)], edx
 
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
       mov      rdi, gword ptr [rax]
       cmp      r15, 262
       jae      SHORT G_M000_IG38
       mov      rax, gword ptr [rdi+8*r15+0x10]
       jmp      G_M000_IG28
 
G_M000_IG38:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG39:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 872

