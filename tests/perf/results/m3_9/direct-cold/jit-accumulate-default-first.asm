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
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0xE0]
       xor      edi, edi
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
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
       mov      esi, 392
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
       mov      esi, 416
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
       mov      esi, 483
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
       mov      esi, 510
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
 
; Total bytes of code 1638

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x129
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 8 inlinees with PGO data; 17 single block inlinees; 24 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 144
       mov      qword ptr [rsp+0x1D8], r15
       mov      qword ptr [rsp+0x1D0], r14
       mov      qword ptr [rsp+0x1C8], r13
       mov      qword ptr [rsp+0x1C0], r12
       mov      qword ptr [rsp+0x1B8], rbx
       lea      rbp, [rsp+0x90]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
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
       mov      dword ptr [rbp-0x7C], r8d
       jmp      G_M000_IG20
 
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
       jae      G_M000_IG70
       cmp      dword ptr [r10+4*r11+0x10], 11
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      edi, dword ptr [r9+0x08]
       jae      G_M000_IG70
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
       jne      G_M000_IG23
 
G_M000_IG09:
       test     r12d, r12d
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      rdx, qword ptr [rbp+0x80]
       lea      r14, [rdx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG26
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG12:
       mov      r14, rax
       mov      rax, gword ptr [rbp+0x70]
 
G_M000_IG13:
       test     r14, r14
       je       SHORT G_M000_IG17
 
G_M000_IG14:
       mov      r12, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG27
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG38
 
G_M000_IG16:
       mov      rsi, 0xD1FFAB1E
       cmp      r12, rsi
       je       G_M000_IG43
 
G_M000_IG17:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG18:
       xor      r12d, r12d
       test     r14, r14
       jne      G_M000_IG52
 
G_M000_IG19:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x7C], ecx
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x7C]
 
G_M000_IG20:
       mov      qword ptr [rbp+0xC8], rdx
       add      r15, rdx
       jo       G_M000_IG71
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
       je       G_M000_IG55
 
G_M000_IG21:
       lea      r9, [r15+0x05]
       cmp      r9, 261
       ja       G_M000_IG03
 
G_M000_IG22:
       test     r9, r9
       jl       G_M000_IG71
       mov      rdi, r9
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG70
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      r9, rdi
       mov      rsi, r9
       jmp      G_M000_IG04
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG09
 
G_M000_IG24:
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
 
G_M000_IG25:
       mov      rdx, rsi
       mov      r12d, 1
       mov      dword ptr [rbp+0x7C], ecx
       mov      qword ptr [rbp+0x80], rdx
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x7C]
       jmp      G_M000_IG20
 
G_M000_IG26:
       test     r14, r14
       jl       G_M000_IG71
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      r14, 262
       jae      G_M000_IG70
       mov      rdi, gword ptr [r9+8*r14+0x10]
       mov      r14, rdi
       mov      rax, r14
       jmp      G_M000_IG12
 
G_M000_IG27:
       mov      rsi, qword ptr [r14+0x08]
       test     rax, rax
       je       G_M000_IG17
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG36
 
G_M000_IG29:
       mov      r12, qword ptr [rax+0x08]
       lea      r14, [rsi+r12]
       mov      rdi, rsi
       xor      rdi, r14
       mov      rax, r12
       xor      rax, r14
       and      rdi, rax
       jge      SHORT G_M000_IG32
 
G_M000_IG30:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x50]
       mov      eax, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x78], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x78]
       mov      rdx, gword ptr [rbp-0x60]
       mov      rcx, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG31:
       mov      r14, rax
       jmp      G_M000_IG18
 
G_M000_IG32:
       lea      r12, [r14+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG35
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG34:
       jmp      SHORT G_M000_IG31
 
G_M000_IG35:
       test     r12, r12
       jl       G_M000_IG71
       mov      rax, r12
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG70
       mov      rax, gword ptr [r9+8*rax+0x10]
       jmp      SHORT G_M000_IG34
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG17
 
G_M000_IG37:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x68], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x68]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG18
 
G_M000_IG38:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rax, rax
       je       G_M000_IG17
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG18
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG17
 
G_M000_IG42:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG18
 
G_M000_IG43:
       mov      r12, r14
       test     rax, rax
       je       G_M000_IG17
 
G_M000_IG44:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG17
 
G_M000_IG45:
       mov      r14, rax
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG50
 
G_M000_IG46:
       cmp      dword ptr [r14+0x08], 0
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       mov      eax, dword ptr [r12+0x08]
       mov      dword ptr [rbp-0x74], eax
       mov      esi, eax
       add      esi, dword ptr [r14+0x08]
       js       G_M000_IG69
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x88], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x74]
       mov      r12, gword ptr [rbp-0x88]
       lea      rdi, bword ptr [r12+2*rdi+0x0C]
       mov      edx, dword ptr [r14+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r14+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r14, r12
 
G_M000_IG48:
       jmp      G_M000_IG18
 
G_M000_IG49:
       mov      r14, r12
       jmp      SHORT G_M000_IG48
 
G_M000_IG50:
       cmp      dword ptr [r14+0x08], 0
       je       G_M000_IG68
 
G_M000_IG51:
       jmp      SHORT G_M000_IG48
 
G_M000_IG52:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r14], rax
       jne      SHORT G_M000_IG54
 
G_M000_IG53:
       mov      ecx, 1
       mov      rdx, qword ptr [r14+0x08]
       mov      rax, rdx
       mov      dword ptr [rbp+0x7C], ecx
       mov      qword ptr [rbp+0x80], rax
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x7C]
       jmp      G_M000_IG20
 
G_M000_IG54:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x7C], ecx
       mov      rdx, qword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp-0x7C]
       jmp      G_M000_IG20
 
G_M000_IG55:
       test     r12d, r12d
       je       SHORT G_M000_IG59
 
G_M000_IG56:
       mov      rcx, qword ptr [rbp+0x80]
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG67
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG58:
       mov      r14, rax
 
G_M000_IG59:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG62
 
G_M000_IG60:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      edi, [rax-0x01]
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG70
       cmp      dword ptr [rdx+4*r8+0x10], 12
       je       SHORT G_M000_IG62
 
G_M000_IG61:
       cmp      edi, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG70
       mov      edx, edi
       mov      dword ptr [rcx+4*rdx+0x10], 12
 
G_M000_IG62:
       mov      rsi, qword ptr [rbp+0x100]
       test     rsi, rsi
       jle      SHORT G_M000_IG65
 
G_M000_IG63:
       cdqe     
       cmp      rsi, rax
       jg       SHORT G_M000_IG65
 
G_M000_IG64:
       dec      esi
       mov      dword ptr [(reloc)], esi
 
G_M000_IG65:
       mov      rax, r14
 
G_M000_IG66:
       add      rsp, 440
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG67:
       test     r14, r14
       jl       SHORT G_M000_IG71
       mov      rax, 0xD1FFAB1E
       mov      r9, gword ptr [rax]
       cmp      r14, 262
       jae      SHORT G_M000_IG70
       mov      rax, gword ptr [r9+8*r14+0x10]
       jmp      G_M000_IG58
 
G_M000_IG68:
       mov      r14, 0xD1FFAB1E
       jmp      G_M000_IG48
 
G_M000_IG69:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG70:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG71:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1665

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x148
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 26 inlinees with PGO data; 29 single block inlinees; 27 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 160
       mov      qword ptr [rsp+0x1E8], r15
       mov      qword ptr [rsp+0x1E0], r14
       mov      qword ptr [rsp+0x1D8], r13
       mov      qword ptr [rsp+0x1D0], r12
       mov      qword ptr [rsp+0x1C8], rbx
       lea      rbp, [rsp+0xA0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       mov      rbx, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      r15, qword ptr [rbp+0xD8]
       mov      r12, qword ptr [rbp+0xD0]
       mov      rax, qword ptr [rbp+0xC8]
       mov      r13, gword ptr [rbp+0x70]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xC8], rax
       test     rax, rax
       setg     r8b
       movzx    r8, r8b
       mov      dword ptr [rbp-0x78], r8d
       jmp      G_M000_IG15
 
G_M000_IG03:
       test     r14, r14
       jl       G_M000_IG93
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      r14, 262
       jae      G_M000_IG92
       mov      rdi, gword ptr [rdi+8*r14+0x10]
       mov      r14, rdi
       mov      rax, r14
       mov      r14, qword ptr [rbp+0x80]
       jmp      G_M000_IG30
 
G_M000_IG04:
       xor      eax, eax
       mov      dword ptr [rbp+0x7C], eax
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       xor      eax, eax
       mov      dword ptr [rbp+0x7C], eax
 
G_M000_IG06:
       add      r15, qword ptr [rbp+0xC8]
       jo       G_M000_IG93
       mov      esi, dword ptr [rbp-0x78]
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
       je       G_M000_IG61
 
G_M000_IG07:
       lea      r8, [r15+0x05]
       cmp      r8, 261
       jbe      G_M000_IG57
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG09:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG92
       cmp      dword ptr [r9+4*r10+0x10], 11
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG92
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 11
 
G_M000_IG12:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      edi, dword ptr [rbp+0x7C]
       test     edi, edi
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       test     rax, rax
       jne      G_M000_IG58
 
G_M000_IG14:
       test     r13d, r13d
       mov      r13, rax
       jne      G_M000_IG28
 
G_M000_IG15:
       test     r14, r14
       je       G_M000_IG53
 
G_M000_IG16:
       mov      r9, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      r9, rdi
       jne      G_M000_IG37
 
G_M000_IG17:
       mov      rsi, qword ptr [r14+0x08]
       test     r13, r13
       je       G_M000_IG53
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG35
 
G_M000_IG19:
       mov      r13, qword ptr [r13+0x08]
       lea      r14, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r14
       mov      r9, r13
       xor      r9, r14
       and      rdi, r9
       jge      G_M000_IG31
 
G_M000_IG20:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x50]
       mov      eax, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x74], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x74]
       mov      rdx, gword ptr [rbp-0x60]
       mov      rcx, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r14, rax
       mov      r13d, edx
       test     r14, r14
       jne      G_M000_IG73
 
G_M000_IG21:
       test     r14, r14
       jne      G_M000_IG82
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG26
 
G_M000_IG22:
       mov      edi, -1
 
G_M000_IG23:
       test     edi, edi
       jg       SHORT G_M000_IG27
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x90], rax
       mov      rdi, r14
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x90]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG25:
       mov      r14, r13
       jmp      G_M000_IG54
 
G_M000_IG26:
       xor      edi, edi
       jmp      SHORT G_M000_IG23
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x88], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x88]
       mov      dword ptr [r14+0x10], r13d
       mov      r13, r14
       jmp      SHORT G_M000_IG25
 
G_M000_IG28:
       mov      rcx, qword ptr [rbp+0x80]
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG03
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG30:
       mov      qword ptr [rbp+0x80], r14
       mov      r14, rax
       jmp      G_M000_IG15
 
G_M000_IG31:
       lea      r13, [r14+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG34
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG33:
       mov      r13, rax
       jmp      G_M000_IG25
 
G_M000_IG34:
       test     r13, r13
       jl       G_M000_IG93
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       mov      r14, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG92
       mov      rdi, gword ptr [r14+8*rdi+0x10]
       mov      r13, rdi
       mov      rax, r13
       jmp      SHORT G_M000_IG33
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG53
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x68], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x68]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG54
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      r9, rdi
       jne      G_M000_IG43
 
G_M000_IG38:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r13, r13
       je       G_M000_IG53
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG54
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG53
 
G_M000_IG42:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG54
 
G_M000_IG43:
       mov      rsi, 0xD1FFAB1E
       cmp      r9, rsi
       jne      G_M000_IG53
 
G_M000_IG44:
       mov      rsi, r14
       mov      gword ptr [rbp-0x80], rsi
       test     r13, r13
       je       G_M000_IG53
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG53
 
G_M000_IG46:
       cmp      dword ptr [rsi+0x08], 0
       je       SHORT G_M000_IG51
 
G_M000_IG47:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG50
 
G_M000_IG48:
       mov      r14d, dword ptr [rsi+0x08]
       mov      edi, r14d
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG91
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x98], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x80]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r14d
       mov      r14, gword ptr [rbp-0x98]
       lea      rdi, bword ptr [r14+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG49:
       jmp      SHORT G_M000_IG54
 
G_M000_IG50:
       mov      r14, rsi
       jmp      SHORT G_M000_IG49
 
G_M000_IG51:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG90
 
G_M000_IG52:
       mov      r14, r13
       jmp      SHORT G_M000_IG49
 
G_M000_IG53:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG54:
       xor      r13d, r13d
       test     r14, r14
       je       G_M000_IG05
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG04
 
G_M000_IG56:
       mov      eax, 1
       mov      rcx, qword ptr [r14+0x08]
       mov      qword ptr [rbp+0x80], rcx
       mov      dword ptr [rbp+0x7C], eax
       jmp      G_M000_IG06
 
G_M000_IG57:
       test     r8, r8
       jl       G_M000_IG93
       mov      rdi, r8
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG92
       mov      rdi, gword ptr [r8+8*rdi+0x10]
       mov      r8, rdi
       mov      rsi, r8
       jmp      G_M000_IG09
 
G_M000_IG58:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG14
 
G_M000_IG59:
       mov      rcx, qword ptr [rax+0x08]
       mov      rdx, qword ptr [rbp+0x80]
       lea      rsi, [rdx+rcx]
       mov      r8, rdx
       xor      r8, rsi
       xor      rcx, rsi
       and      rcx, r8
       mov      qword ptr [rbp+0x80], rdx
       jl       G_M000_IG14
 
G_M000_IG60:
       mov      rdx, rsi
       mov      r13d, 1
       mov      dword ptr [rbp+0x7C], edi
       mov      qword ptr [rbp+0x80], rdx
       jmp      G_M000_IG06
 
G_M000_IG61:
       test     r13d, r13d
       je       SHORT G_M000_IG64
 
G_M000_IG62:
       mov      rcx, qword ptr [rbp+0x80]
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG72
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp+0x80]
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG64:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG67
 
G_M000_IG65:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      edi, [rax-0x01]
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG92
       cmp      dword ptr [rdx+4*rsi+0x10], 12
       je       SHORT G_M000_IG67
 
G_M000_IG66:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG92
       mov      edx, edi
       mov      dword ptr [rcx+4*rdx+0x10], 12
 
G_M000_IG67:
       mov      rbx, qword ptr [rbp+0x100]
       test     rbx, rbx
       jle      SHORT G_M000_IG70
 
G_M000_IG68:
       cdqe     
       cmp      rbx, rax
       jg       SHORT G_M000_IG70
 
G_M000_IG69:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG70:
       mov      rax, r14
 
G_M000_IG71:
       add      rsp, 456
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG72:
       test     r14, r14
       jl       G_M000_IG93
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       cmp      r14, 262
       jae      G_M000_IG92
       mov      r14, gword ptr [rdi+8*r14+0x10]
       jmp      G_M000_IG64
 
G_M000_IG73:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG74
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG75
 
G_M000_IG74:
       mov      eax, r13d
       jmp      SHORT G_M000_IG81
 
G_M000_IG75:
       cmp      edi, 2
       je       SHORT G_M000_IG76
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG92
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG77:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG78
       mov      eax, -1
       jmp      SHORT G_M000_IG80
 
G_M000_IG78:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG79
       mov      eax, 1
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:
       xor      eax, eax
 
G_M000_IG80:
       imul     eax, r13d
 
G_M000_IG81:
       test     eax, eax
       jl       G_M000_IG27
       jmp      G_M000_IG21
 
G_M000_IG82:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG83
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG84
 
G_M000_IG83:
       mov      edi, r13d
       jmp      G_M000_IG23
 
G_M000_IG84:
       cmp      edi, 2
       je       SHORT G_M000_IG85
       cmp      dword ptr [r14+0x08], 0
       jbe      SHORT G_M000_IG92
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG86
 
G_M000_IG85:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG86:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG87
       mov      eax, -1
       jmp      SHORT G_M000_IG89
 
G_M000_IG87:
       test     rdi, rdi
       jge      SHORT G_M000_IG88
       mov      eax, 1
       jmp      SHORT G_M000_IG89
 
G_M000_IG88:
       xor      eax, eax
 
G_M000_IG89:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG23
 
G_M000_IG90:
       mov      r14, 0xD1FFAB1E
       jmp      G_M000_IG49
 
G_M000_IG91:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG92:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG93:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2057

