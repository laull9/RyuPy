; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 320
       lea      rbp, [rsp+0x140]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x138], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x128], rax
       mov      rdi, gword ptr [rbp-0x128]
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x130], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x140], rax
       mov      rcx, gword ptr [rbp-0x140]
       mov      rsi, gword ptr [rbp-0x128]
       mov      rdx, gword ptr [rbp-0x130]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
       xor      eax, eax
       mov      qword ptr [rbp-0xD0], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xD8], eax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0xD4], 0
       jne      G_M000_IG14
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x138]
       mov      esi, 149
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x88], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x8C]
       and      eax, dword ptr [rbp-0x90]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       cmp      dword ptr [rbp-0x8C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0x94]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x98]
       or       eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       cmp      dword ptr [rbp-0xA4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0xE0], rax
       cmp      dword ptr [rbp-0xD4], 0
       jne      G_M000_IG27
       jmp      G_M000_IG20
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       G_M000_IG34
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       cmp      dword ptr [rbp-0xD8], 0
       jne      G_M000_IG33
       jmp      G_M000_IG30
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG10:
       add      rsp, 320
       pop      rbp
       ret      
 
G_M000_IG11:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x138]
       mov      esi, 297
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
       mov      qword ptr [rbp-0xD0], rax
       jmp      G_M000_IG03
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xD0], rax
       jmp      G_M000_IG11
 
G_M000_IG16:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG18
 
G_M000_IG17:
       lea      rdi, [rbp-0x138]
       mov      esi, 328
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG18:
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0x40]
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rax, gword ptr [rbp-0x120]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xD8], eax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0xD4], 0
       jne      G_M000_IG15
       jmp      G_M000_IG11
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xD0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xD8], eax
       jmp      G_M000_IG16
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x138]
       mov      esi, 396
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       cmp      dword ptr [rbp-0xD8], 0
       jne      SHORT G_M000_IG19
       jmp      G_M000_IG16
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0xD0], rax
       mov      dword ptr [rbp-0xD8], 1
       jmp      G_M000_IG11
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x138]
       mov      esi, 420
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0xE0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xF0], rax
       mov      rax, qword ptr [rbp-0xD0]
       add      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xD0]
       xor      rax, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xF0]
       xor      rax, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x108], rax
       mov      rax, qword ptr [rbp-0x100]
       and      rax, qword ptr [rbp-0x108]
       mov      qword ptr [rbp-0x110], rax
       cmp      qword ptr [rbp-0x110], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x114], eax
       cmp      dword ptr [rbp-0x114], 0
       jne      G_M000_IG23
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG20
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x138]
       mov      esi, 487
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0xE0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0xE4], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG20
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x138]
       mov      esi, 514
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG09
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xD0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xD8], eax
       jmp      SHORT G_M000_IG30
 
G_M000_IG34:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1636

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x129
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 6 single block inlinees; 9 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 48
       mov      qword ptr [rsp+0x178], r15
       mov      qword ptr [rsp+0x170], r14
       mov      qword ptr [rsp+0x168], r13
       mov      qword ptr [rsp+0x160], r12
       mov      qword ptr [rsp+0x158], rbx
       lea      rbp, [rsp+0x30]
       mov      rbx, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      r15, qword ptr [rbp+0xD8]
       mov      r13, qword ptr [rbp+0xD0]
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r12d, dword ptr [rbp+0x78]
 
G_M000_IG02:
       test     rdx, rdx
       setg     r8b
       movzx    r8, r8b
       mov      dword ptr [rbp-0x2C], r8d
       jmp      G_M000_IG15
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG04:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       mov      r10, r9
       dec      edi
       mov      r11d, edi
       cmp      r11d, dword ptr [r10+0x08]
       jae      G_M000_IG38
       cmp      dword ptr [r10+4*r11+0x10], 11
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      edi, dword ptr [r9+0x08]
       jae      G_M000_IG38
       mov      edi, edi
       mov      dword ptr [r9+4*rdi+0x10], 11
 
G_M000_IG07:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x70], rax
       mov      ecx, dword ptr [rbp+0x7C]
       test     ecx, ecx
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       test     rax, rax
       jne      G_M000_IG18
 
G_M000_IG09:
       test     r12d, r12d
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      rdx, qword ptr [rbp+0x80]
       lea      r14, [rdx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG21
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG12:
       mov      r14, rax
       mov      rax, gword ptr [rbp+0x70]
 
G_M000_IG13:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
       xor      r12d, r12d
       test     r14, r14
       jne      G_M000_IG22
 
G_M000_IG14:
       xor      edi, edi
       mov      dword ptr [rbp+0x7C], edi
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x2C]
 
G_M000_IG15:
       mov      qword ptr [rbp+0xC8], rdx
       add      r15, rdx
       jo       G_M000_IG39
       mov      edi, r8d
       cmp      r15, r13
       setl     r9b
       movzx    r9, r9b
       and      r9d, edi
       cmp      r15, r13
       setg     r10b
       movzx    r10, r10b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r10d
       or       edi, r9d
       je       G_M000_IG25
 
G_M000_IG16:
       lea      r9, [r15+0x05]
       cmp      r9, 261
       ja       G_M000_IG03
 
G_M000_IG17:
       test     r9, r9
       jl       G_M000_IG39
       mov      rdi, r9
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG38
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      r9, rdi
       mov      rsi, r9
       jmp      G_M000_IG04
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG09
 
G_M000_IG19:
       mov      rdi, qword ptr [rax+0x08]
       mov      rdx, qword ptr [rbp+0x80]
       lea      rsi, [rdx+rdi]
       mov      r8, rdx
       xor      r8, rsi
       xor      rdi, rsi
       and      rdi, r8
       mov      qword ptr [rbp+0x80], rdx
       mov      rax, gword ptr [rbp+0x70]
       jl       G_M000_IG09
 
G_M000_IG20:
       mov      rdx, rsi
       mov      r12d, 1
       mov      dword ptr [rbp+0x7C], ecx
       mov      qword ptr [rbp+0x80], rdx
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG21:
       test     r14, r14
       jl       G_M000_IG39
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rsi]
       cmp      r14, 262
       jae      G_M000_IG38
       mov      rsi, gword ptr [r9+8*r14+0x10]
       mov      r14, rsi
       mov      rax, r14
       jmp      G_M000_IG12
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      edi, 1
       mov      rdx, qword ptr [r14+0x08]
       mov      rax, rdx
       mov      dword ptr [rbp+0x7C], edi
       mov      qword ptr [rbp+0x80], rax
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG24:
       xor      edi, edi
       mov      dword ptr [rbp+0x7C], edi
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x2C]
       jmp      G_M000_IG15
 
G_M000_IG25:
       test     r12d, r12d
       je       SHORT G_M000_IG29
 
G_M000_IG26:
       mov      rcx, qword ptr [rbp+0x80]
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG37
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG28:
       mov      r14, rax
 
G_M000_IG29:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      edi, [rax-0x01]
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG38
       cmp      dword ptr [rdx+4*r8+0x10], 12
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       cmp      edi, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG38
       mov      edx, edi
       mov      dword ptr [rcx+4*rdx+0x10], 12
 
G_M000_IG32:
       mov      rsi, qword ptr [rbp+0x100]
       test     rsi, rsi
       jle      SHORT G_M000_IG35
 
G_M000_IG33:
       cdqe     
       cmp      rsi, rax
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       dec      esi
       mov      dword ptr [(reloc)], esi
 
G_M000_IG35:
       mov      rax, r14
 
G_M000_IG36:
       add      rsp, 344
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG37:
       test     r14, r14
       jl       SHORT G_M000_IG39
       mov      rax, 0xD1FFAB1E
       mov      r9, gword ptr [rax]
       cmp      r14, 262
       jae      SHORT G_M000_IG38
       mov      rax, gword ptr [r9+8*r14+0x10]
       jmp      G_M000_IG28
 
G_M000_IG38:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG39:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 920

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x148
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 6 single block inlinees; 9 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 48
       mov      qword ptr [rsp+0x178], r15
       mov      qword ptr [rsp+0x170], r14
       mov      qword ptr [rsp+0x168], r13
       mov      qword ptr [rsp+0x160], r12
       mov      qword ptr [rsp+0x158], rbx
       lea      rbp, [rsp+0x30]
       mov      rbx, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      r15, qword ptr [rbp+0xD8]
       mov      r12, qword ptr [rbp+0xD0]
       mov      rax, qword ptr [rbp+0xC8]
       mov      r13, gword ptr [rbp+0x70]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xC8], rax
       test     rax, rax
       setg     r9b
       movzx    r9, r9b
       mov      dword ptr [rbp-0x2C], r9d
       jmp      G_M000_IG14
 
G_M000_IG03:
       test     r14, r14
       jl       G_M000_IG38
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      r14, 262
       jae      G_M000_IG37
       mov      rdi, gword ptr [rdi+8*r14+0x10]
       mov      r14, rdi
       mov      rax, r14
       mov      r14, qword ptr [rbp+0x80]
       jmp      G_M000_IG19
 
G_M000_IG04:
       xor      eax, eax
       mov      dword ptr [rbp+0x7C], eax
 
G_M000_IG05:
       add      r15, qword ptr [rbp+0xC8]
       jo       G_M000_IG38
       mov      esi, dword ptr [rbp-0x2C]
       mov      edi, esi
       cmp      r15, r12
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       cmp      r15, r12
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG25
 
G_M000_IG06:
       lea      r8, [r15+0x05]
       cmp      r8, 261
       jbe      G_M000_IG21
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG08:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG11
 
G_M000_IG09:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG37
       cmp      dword ptr [r9+4*r10+0x10], 11
       je       SHORT G_M000_IG11
 
G_M000_IG10:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG37
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 11
 
G_M000_IG11:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      edi, dword ptr [rbp+0x7C]
       test     edi, edi
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       test     rax, rax
       jne      G_M000_IG22
 
G_M000_IG13:
       test     r13d, r13d
       mov      r13, rax
       jne      SHORT G_M000_IG17
 
G_M000_IG14:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
       xor      r13d, r13d
       test     r14, r14
       je       G_M000_IG04
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG20
 
G_M000_IG16:
       mov      eax, 1
       mov      rcx, qword ptr [r14+0x08]
       mov      qword ptr [rbp+0x80], rcx
       mov      dword ptr [rbp+0x7C], eax
       jmp      G_M000_IG05
 
G_M000_IG17:
       mov      rcx, qword ptr [rbp+0x80]
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG03
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG19:
       mov      qword ptr [rbp+0x80], r14
       mov      r14, rax
       jmp      G_M000_IG14
 
G_M000_IG20:
       xor      eax, eax
       mov      dword ptr [rbp+0x7C], eax
       jmp      G_M000_IG05
 
G_M000_IG21:
       test     r8, r8
       jl       G_M000_IG38
       mov      rdi, r8
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG37
       mov      rdi, gword ptr [r8+8*rdi+0x10]
       mov      r8, rdi
       mov      rsi, r8
       jmp      G_M000_IG08
 
G_M000_IG22:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG13
 
G_M000_IG23:
       mov      rcx, qword ptr [rax+0x08]
       mov      rdx, qword ptr [rbp+0x80]
       lea      rsi, [rdx+rcx]
       mov      r8, rdx
       xor      r8, rsi
       xor      rcx, rsi
       and      rcx, r8
       mov      qword ptr [rbp+0x80], rdx
       jl       G_M000_IG13
 
G_M000_IG24:
       mov      rdx, rsi
       mov      r13d, 1
       mov      dword ptr [rbp+0x7C], edi
       mov      qword ptr [rbp+0x80], rdx
       jmp      G_M000_IG05
 
G_M000_IG25:
       test     r13d, r13d
       je       SHORT G_M000_IG28
 
G_M000_IG26:
       mov      rcx, qword ptr [rbp+0x80]
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG36
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG28:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      edi, [rax-0x01]
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG37
       cmp      dword ptr [rdx+4*rsi+0x10], 12
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       cmp      edi, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG37
       mov      edx, edi
       mov      dword ptr [rcx+4*rdx+0x10], 12
 
G_M000_IG31:
       mov      r8, qword ptr [rbp+0x100]
       test     r8, r8
       jle      SHORT G_M000_IG34
 
G_M000_IG32:
       cdqe     
       cmp      r8, rax
       jg       SHORT G_M000_IG34
 
G_M000_IG33:
       dec      r8d
       mov      dword ptr [(reloc)], r8d
 
G_M000_IG34:
       mov      rax, r14
 
G_M000_IG35:
       add      rsp, 344
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG36:
       test     r14, r14
       jl       SHORT G_M000_IG38
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       cmp      r14, 262
       jae      SHORT G_M000_IG37
       mov      r14, gword ptr [rdi+8*r14+0x10]
       jmp      G_M000_IG28
 
G_M000_IG37:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG38:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 888

