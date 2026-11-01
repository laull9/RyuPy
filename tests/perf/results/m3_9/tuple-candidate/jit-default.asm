; Assembly listing for method RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 272
       lea      rbp, [rsp+0x110]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x108], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      qword ptr [rbp-0x80], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x108]
       dec      eax
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x108]
       mov      esi, 66
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
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0xF0]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0xD8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x38], rax
       mov      rax, gword ptr [rbp-0xD8]
       mov      gword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       G_M000_IG09
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x100], rax
       mov      rdi, gword ptr [rbp-0x100]
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x100]
       mov      rdx, gword ptr [rbp-0x40]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x100]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xE0]
 
G_M000_IG08:
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 645

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 240
       lea      rbp, [rsp+0xF0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0xE8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xE8]
       dec      eax
       mov      dword ptr [rbp-0xE8], eax
       cmp      dword ptr [rbp-0xE8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xE8]
       mov      esi, 78
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
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xD8], rax
       mov      rax, gword ptr [rbp-0xD8]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xD0]
 
G_M000_IG08:
       add      rsp, 240
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 546

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x42
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 3
; 0 inlinees with PGO data; 3 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 48
       mov      qword ptr [rsp+0x148], r15
       mov      qword ptr [rsp+0x140], r14
       mov      qword ptr [rsp+0x138], r13
       mov      qword ptr [rsp+0x130], r12
       mov      qword ptr [rsp+0x128], rbx
       lea      rbp, [rsp+0x30]
       mov      rbx, gword ptr [rbp+0xE8]
       mov      r15, gword ptr [rbp+0xE0]
       mov      r14, qword ptr [rbp+0xD0]
       mov      rdi, qword ptr [rbp+0xB0]
       mov      rsi, qword ptr [rbp+0xA8]
       mov      rax, qword ptr [rbp+0xA0]
 
G_M000_IG02:
       test     rax, rax
       setg     cl
       movzx    rcx, cl
       mov      edx, ecx
       cmp      rdi, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edx
       cmp      rdi, rsi
       setg     r9b
       movzx    r9, r9b
       test     edx, edx
       sete     r10b
       movzx    r10, r10b
       and      r9d, r10d
       or       r8d, r9d
       je       G_M000_IG17
 
G_M000_IG03:
       mov      r13d, dword ptr [(reloc)]
       jmp      SHORT G_M000_IG07
       align    [0 bytes for IG04]
 
G_M000_IG04:
       cmp      r9d, dword ptr [rdx+0x08]
       jae      G_M000_IG18
       mov      r8d, r9d
       mov      dword ptr [rdx+4*r8+0x10], 9
 
G_M000_IG05:
       add      rdi, rax
       jo       G_M000_IG19
       mov      edx, ecx
       cmp      rdi, rsi
       setl     r8b
       movzx    r8, r8b
       and      edx, r8d
       cmp      rdi, rsi
       setg     r8b
       movzx    r8, r8b
       and      r8d, r10d
       or       edx, r8d
       je       SHORT G_M000_IG09
 
G_M000_IG06:
       mov      rdx, rbx
       mov      rbx, r15
       mov      r15, rdx
 
G_M000_IG07:
       test     r13d, r13d
       jle      SHORT G_M000_IG05
 
G_M000_IG08:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      r8, rdx
       lea      r9d, [r13-0x01]
       mov      r11d, r9d
       cmp      r11d, dword ptr [r8+0x08]
       jae      G_M000_IG18
       cmp      dword ptr [r8+4*r11+0x10], 9
       je       SHORT G_M000_IG05
       jmp      SHORT G_M000_IG04
 
G_M000_IG09:
       mov      r13d, dword ptr [(reloc)]
       test     r13d, r13d
       jle      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rdi, rdx
       lea      r9d, [r13-0x01]
       mov      esi, r9d
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG18
       cmp      dword ptr [rdi+4*rsi+0x10], 10
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       mov      rdi, rdx
       mov      esi, r9d
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG18
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [r12+0x18]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       test     r14, r14
       jle      SHORT G_M000_IG15
 
G_M000_IG13:
       movsxd   rax, r13d
       cmp      r14, rax
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       dec      r14d
       mov      dword ptr [(reloc)], r14d
 
G_M000_IG15:
       mov      rax, rbx
 
G_M000_IG16:
       add      rsp, 296
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG17:
       mov      rax, rbx
       mov      rbx, r15
       mov      r15, rax
       jmp      G_M000_IG09
 
G_M000_IG18:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG19:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 509

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 0 inlinees with PGO data; 0 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0x118], r15
       mov      qword ptr [rsp+0x110], r14
       mov      qword ptr [rsp+0x108], r13
       mov      qword ptr [rsp+0x100], rbx
       lea      rbp, [rsp+0x20]
       mov      rax, gword ptr [rbp+0xC8]
       mov      rdi, gword ptr [rbp+0xB8]
       mov      r8, qword ptr [rbp+0xA8]
       mov      rcx, qword ptr [rbp+0x88]
       mov      rdx, qword ptr [rbp+0x80]
       mov      rsi, qword ptr [rbp+0x78]
 
G_M000_IG02:
       test     rsi, rsi
       setg     r9b
       movzx    r9, r9b
       mov      r10d, r9d
       cmp      rcx, rdx
       setl     r11b
       movzx    r11, r11b
       and      r11d, r10d
       cmp      rcx, rdx
       setg     bl
       movzx    rbx, bl
       test     r10d, r10d
       sete     r15b
       movzx    r15, r15b
       and      ebx, r15d
       or       r11d, ebx
       je       SHORT G_M000_IG08
 
G_M000_IG03:
       mov      r11d, dword ptr [(reloc)]
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG04]
 
G_M000_IG04:
       mov      r10d, ebx
       mov      dword ptr [rdi+4*r10+0x10], 16
 
G_M000_IG05:
       mov      rdi, rax
       add      rcx, rsi
       jo       G_M000_IG17
       mov      r10d, r9d
       cmp      rcx, rdx
       setl     bl
       movzx    rbx, bl
       and      r10d, ebx
       cmp      rcx, rdx
       setg     bl
       movzx    rbx, bl
       and      ebx, r15d
       or       r10d, ebx
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       test     r11d, r11d
       jle      SHORT G_M000_IG05
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r10, rdi
       lea      ebx, [r11-0x01]
       mov      r14d, ebx
       mov      r13d, dword ptr [r10+0x08]
       cmp      r14d, r13d
       jae      SHORT G_M000_IG16
       mov      r13d, r14d
       cmp      dword ptr [r10+4*r13+0x10], 16
       je       SHORT G_M000_IG05
       jmp      SHORT G_M000_IG04
 
G_M000_IG08:
       mov      r11d, dword ptr [(reloc)]
       test     r11d, r11d
       jle      SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       lea      ebx, [r11-0x01]
       mov      edx, ebx
       mov      r13d, dword ptr [rcx+0x08]
       cmp      edx, r13d
       jae      SHORT G_M000_IG16
       cmp      dword ptr [rcx+4*rdx+0x10], 17
       je       SHORT G_M000_IG11
 
G_M000_IG10:
       mov      ecx, ebx
       mov      dword ptr [rax+4*rcx+0x10], 17
 
G_M000_IG11:
       test     r8, r8
       jle      SHORT G_M000_IG14
 
G_M000_IG12:
       movsxd   rax, r11d
       cmp      r8, rax
       jg       SHORT G_M000_IG14
 
G_M000_IG13:
       dec      r8d
       mov      dword ptr [(reloc)], r8d
 
G_M000_IG14:
       mov      rax, rdi
 
G_M000_IG15:
       add      rsp, 256
       pop      rbx
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
 
; Total bytes of code 370

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_escape(System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 240
       lea      rbp, [rsp+0xF0]
       xor      eax, eax
       mov      qword ptr [rbp-0xE8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqa  xmmword ptr [rbp-0x60], xmm8
       mov      qword ptr [rbp-0x50], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
 
G_M000_IG02:
       mov      dword ptr [rbp-0xF0], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xF0]
       dec      eax
       mov      dword ptr [rbp-0xF0], eax
       cmp      dword ptr [rbp-0xF0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xF0]
       mov      esi, 67
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
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xE8], rax
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xE8]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rcx, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0x48]
       xor      edi, edi
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       G_M000_IG09
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD0], rax
       mov      rsi, gword ptr [rbp-0xD0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Unary(int,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0xD8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG08:
       add      rsp, 240
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 711

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_escape(System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x43
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 7 single block inlinees; 11 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 160
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0xA0]
       mov      r15, gword ptr [rbp+0xC8]
       mov      r14, gword ptr [rbp+0xC0]
       mov      rbx, gword ptr [rbp+0xB8]
       mov      r13, qword ptr [rbp+0x88]
       mov      r12, qword ptr [rbp+0x80]
       mov      rax, qword ptr [rbp+0x78]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x78], rax
       test     rax, rax
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0x3C], edx
       jmp      G_M000_IG44
 
G_M000_IG03:
       mov      rax, qword ptr [rbp-0x30]
 
G_M000_IG04:
       mov      edx, dword ptr [rbp-0x34]
       inc      edx
       cmp      edx, 4
       jge      G_M000_IG24
 
G_M000_IG05:
       mov      dword ptr [rbp-0x34], edx
       mov      edi, edx
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       imul     rdi, rdi, 56
       lea      rsi, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x58], rsi
       cmp      byte  ptr [rsi+0x34], 0
       je       SHORT G_M000_IG04
 
G_M000_IG06:
       mov      rdi, gword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       cmp      rdi, r8
       je       SHORT G_M000_IG10
 
G_M000_IG07:
       test     rdi, rdi
       je       SHORT G_M000_IG04
 
G_M000_IG08:
       cmp      dword ptr [rdi+0x08], 6
       jne      SHORT G_M000_IG04
 
G_M000_IG09:
       mov      r8, 0xD1FFAB1E
       xor      r8, qword ptr [rdi+0x0C]
       mov      edi, dword ptr [rdi+0x14]
       xor      edi, 0xD1FFAB1E
       or       rdi, r8
       jne      SHORT G_M000_IG04
 
G_M000_IG10:
       mov      rdi, gword ptr [rsi+0x08]
       cmp      rdi, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG04
 
G_M000_IG11:
       cmp      qword ptr [rsi+0x28], rax
       jne      SHORT G_M000_IG03
 
G_M000_IG12:
       cmp      dword ptr [rsi+0x30], 0
       jl       G_M000_IG27
 
G_M000_IG13:
       mov      edi, dword ptr [rsi+0x30]
       mov      r8, gword ptr [rbx+0x10]
       cmp      dword ptr [r8+0x08], edi
       jbe      G_M000_IG35
 
G_M000_IG14:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      r8, gword ptr [r8+8*rdi+0x10]
       test     r8, r8
       je       G_M000_IG26
 
G_M000_IG15:
       mov      gword ptr [rbp-0xA0], r8
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG51
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rbp-0xA0]
       cmp      r8, gword ptr [rdi]
       setne    dil
       movzx    rdi, dil
       test     edi, edi
       mov      rax, qword ptr [rbp-0x30]
       mov      rsi, bword ptr [rbp-0x58]
       jne      G_M000_IG04
       jmp      G_M000_IG27
 
G_M000_IG17:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG20
 
G_M000_IG18:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [rax+4*rdx+0x10], 23
       je       SHORT G_M000_IG20
 
G_M000_IG19:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 23
 
G_M000_IG20:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Unary(int,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0xA8]
       test     rbx, rbx
       jle      SHORT G_M000_IG23
 
G_M000_IG21:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG23:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp-0x90], rdx
       mov      r8, gword ptr [rdx+0x10]
       test     r8, r8
       je       G_M000_IG37
 
G_M000_IG25:
       cmp      dword ptr [r8+0x08], 1
       mov      gword ptr [rbp-0x68], r8
       je       G_M000_IG38
       jmp      G_M000_IG37
 
G_M000_IG26:
       mov      rsi, bword ptr [rbp-0x58]
 
G_M000_IG27:
       mov      rax, gword ptr [rsi+0x10]
       mov      gword ptr [rbp-0x78], rax
       mov      rdx, gword ptr [rsi+0x18]
       mov      gword ptr [rbp-0x80], rdx
       mov      r8, gword ptr [rsi+0x20]
       mov      gword ptr [rbp-0x88], r8
       movzx    r9, byte  ptr [rsi+0x35]
       mov      dword ptr [rbp-0x38], r9d
       mov      rdi, 0xD1FFAB1E
       mov      r10, gword ptr [rdi]
       mov      gword ptr [rbp-0x98], r10
       mov      r11, gword ptr [r10+0x10]
       test     r11, r11
       je       SHORT G_M000_IG29
 
G_M000_IG28:
       cmp      dword ptr [r11+0x08], 2
       mov      gword ptr [rbp-0x60], r11
       je       SHORT G_M000_IG30
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x98]
       lea      rdi, bword ptr [rdi+0x10]
       mov      gword ptr [rbp-0x60], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG30:
       mov      edi, dword ptr [rbp-0x38]
       cmp      edi, 2
       je       G_M000_IG36
 
G_M000_IG31:
       mov      rdx, rbx
 
G_M000_IG32:
       mov      rdi, gword ptr [rbp-0x60]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x60]
       mov      rdx, gword ptr [rbp+0x48]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp-0x80]
       mov      rdi, rax
       test     rdi, rdi
       je       SHORT G_M000_IG34
 
G_M000_IG33:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [rdi], r10
       jne      G_M000_IG52
 
G_M000_IG34:
       mov      rax, gword ptr [rbp-0x60]
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG53
       mov      rdx, gword ptr [rax+0x10]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       mov      rsi, gword ptr [rbp-0x78]
       mov      rcx, rax
       call     [RyuPy.Runtime.PyObject:InvokePrepared(RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,System.Object,System.Object[],System.String[],System.Object[]):System.Object]
       jmp      G_M000_IG43
 
G_M000_IG35:
       mov      rsi, bword ptr [rbp-0x58]
       jmp      G_M000_IG27
 
G_M000_IG36:
       mov      rdx, gword ptr [rbp-0x88]
       jmp      G_M000_IG32
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rcx, gword ptr [rbp-0x90]
       lea      rdi, bword ptr [rcx+0x10]
       mov      gword ptr [rbp-0x68], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG38:
       mov      rdi, gword ptr [rbp-0x68]
       mov      rdx, gword ptr [rbp+0x48]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, qword ptr [rbp-0x30]
       mov      rsi, rbx
       mov      r8, gword ptr [rbp-0x68]
       xor      edi, edi
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodSlow(int,RyuPy.Runtime.PyInstance,System.String,long,System.Object[]):System.Object]
       jmp      SHORT G_M000_IG43
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x90], rcx
       mov      rdx, gword ptr [rcx+0x10]
       test     rdx, rdx
       je       SHORT G_M000_IG41
 
G_M000_IG40:
       cmp      dword ptr [rdx+0x08], 1
       mov      gword ptr [rbp-0x70], rdx
       je       SHORT G_M000_IG42
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x90]
       lea      rdi, bword ptr [rdi+0x10]
       mov      gword ptr [rbp-0x70], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG42:
       mov      rdi, gword ptr [rbp-0x70]
       mov      rdx, gword ptr [rbp-0x50]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, rbx
       xor      edi, edi
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG43:
       mov      rsi, qword ptr [rbp+0x78]
       add      r13, rsi
       jo       G_M000_IG54
       mov      qword ptr [rbp+0x78], rsi
       mov      edx, dword ptr [rbp-0x3C]
 
G_M000_IG44:
       mov      edi, edx
       cmp      r13, r12
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r13, r12
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG17
 
G_M000_IG45:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG48
 
G_M000_IG46:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [r8+4*r9+0x10], 22
       je       SHORT G_M000_IG48
 
G_M000_IG47:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 22
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x48], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x48]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x50], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x48]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp+0x48], rax
       test     rbx, rbx
       je       G_M000_IG39
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG39
 
G_M000_IG50:
       mov      rax, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x30], rax
       xor      edx, edx
       jmp      G_M000_IG05
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_GCSTATIC_BASE]
       jmp      G_M000_IG16
 
G_M000_IG52:
       mov      rsi, rax
       mov      rdi, r10
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG53:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG54:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1389

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_tuple_swap(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:
       push     rax
       mov      rax, rsi
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rax]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rax+0x08]
       cmp      edx, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rax+0x10]
       call     [RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_tuple_read(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:
       push     rax
       mov      rax, rsi
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rax]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rax+0x08]
       cmp      edx, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rax+0x10]
       call     [RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_tuple_escape(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:
       push     rax
       mov      rcx, rsi
       mov      eax, edx
 
G_M000_IG02:
       test     eax, eax
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rcx]
       cmp      eax, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rcx+0x08]
       cmp      eax, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rcx+0x10]
       cmp      eax, 3
       jbe      SHORT G_M000_IG04
       mov      rcx, gword ptr [rcx+0x18]
       call     [RyuPy.Generated.Module:m0_tuple_escape(System.Object,System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 58

