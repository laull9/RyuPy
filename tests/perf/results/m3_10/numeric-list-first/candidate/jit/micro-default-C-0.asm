; Assembly listing for method RyuPy.Generated.Module:m0_construct_refs(System.Object,System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 64
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x30], ymm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x18], rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, gword ptr [rbp-0x38]
       mov      rdx, gword ptr [rbp-0x10]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x28], rax
       mov      rdx, gword ptr [rbp-0x28]
       mov      rcx, gword ptr [rbp-0x08]
       mov      edi, 4
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x20], rax
       mov      rdi, qword ptr [rbp-0x18]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x20]
 
G_M000_IG03:
       add      rsp, 64
       pop      rbp
       ret      
 
; Total bytes of code 185

; Assembly listing for method RyuPy.Generated.Module:m0_append_refs(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 208
       lea      rbp, [rsp+0xD0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xC8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 26
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
       mov      eax, dword ptr [rbp-0xC8]
       dec      eax
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0xC8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xC8]
       mov      esi, 91
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
       mov      edi, 27
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x40]
       mov      edi, 5
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 28
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC0]
 
G_M000_IG08:
       add      rsp, 208
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 556

; Assembly listing for method RyuPy.Generated.Module:m0_append_refs(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5b
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 8 single block inlinees; 12 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 64
       mov      qword ptr [rsp+0x118], r15
       mov      qword ptr [rsp+0x110], r14
       mov      qword ptr [rsp+0x108], r13
       mov      qword ptr [rsp+0x100], r12
       mov      qword ptr [rsp+0xF8], rbx
       lea      rbp, [rsp+0x40]
       mov      rbx, gword ptr [rbp+0xA8]
       mov      r13, gword ptr [rbp+0xA0]
       mov      r15, qword ptr [rbp+0x68]
       mov      r14, qword ptr [rbp+0x60]
       mov      r12, qword ptr [rbp+0x58]
 
G_M000_IG02:
       test     r12, r12
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0x2C], ecx
       jmp      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x40]
       lea      rdi, bword ptr [rdi+0x38]
       mov      gword ptr [rbp-0x38], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG04:
       mov      rdi, gword ptr [rbp-0x38]
       mov      rdx, rbx
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x38]
       mov      rsi, r13
       mov      edi, 5
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       add      r15, r12
       jo       G_M000_IG20
       mov      ecx, dword ptr [rbp-0x2C]
 
G_M000_IG05:
       mov      edi, ecx
       cmp      r15, r14
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r15, r14
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       SHORT G_M000_IG11
 
G_M000_IG06:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       dec      edi
       mov      r8d, edi
       mov      r9d, dword ptr [rdx+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG19
       cmp      dword ptr [rdx+4*r8+0x10], 27
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 27
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp-0x40], rdx
       mov      r8, gword ptr [rdx+0x38]
       test     r8, r8
       je       G_M000_IG03
 
G_M000_IG10:
       cmp      dword ptr [r8+0x08], 1
       mov      gword ptr [rbp-0x38], r8
       je       G_M000_IG04
       jmp      G_M000_IG03
 
G_M000_IG11:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       mov      rcx, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       mov      r9d, dword ptr [rcx+0x08]
       cmp      r8d, r9d
       jae      SHORT G_M000_IG19
       cmp      dword ptr [rcx+4*r8+0x10], 28
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       mov      rcx, rsi
       mov      edx, edx
       mov      dword ptr [rcx+4*rdx+0x10], 28
 
G_M000_IG14:
       mov      rax, qword ptr [rbp+0x90]
       test     rax, rax
       jle      SHORT G_M000_IG17
 
G_M000_IG15:
       movsxd   rcx, edi
       cmp      rax, rcx
       jg       SHORT G_M000_IG17
 
G_M000_IG16:
       dec      eax
       mov      dword ptr [(reloc)], eax
 
G_M000_IG17:
       mov      rax, r13
 
G_M000_IG18:
       add      rsp, 248
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
 
; Total bytes of code 464

; Assembly listing for method RyuPy.Generated.Module:m0_read_refs(System.Object,System.Object):System.Object (Instrumented Tier0)
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
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x108], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, gword ptr [rbp-0xF8]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x100], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x110], rax
       mov      rcx, gword ptr [rbp-0x110]
       mov      rsi, gword ptr [rbp-0xF8]
       mov      rdx, gword ptr [rbp-0x100]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x90], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x108]
       dec      eax
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x108]
       mov      esi, 123
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x90], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       cmp      dword ptr [rbp-0x94], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0x9C]
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA0]
       or       eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       cmp      dword ptr [rbp-0xAC], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG10
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x80]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 34
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyOps:Is(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0xC4], eax
       mov      edi, dword ptr [rbp-0xC4]
       call     [RyuPy.Runtime.PyOps:BoxBool(bool):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdi, gword ptr [rbp-0xD0]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0xD4], 0
       jne      SHORT G_M000_IG11
 
G_M000_IG07:
       mov      eax, dword ptr [rbp-0x108]
       dec      eax
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jg       SHORT G_M000_IG09
 
G_M000_IG08:
       lea      rdi, [rbp-0x108]
       mov      esi, 262
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       G_M000_IG14
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG10:
       mov      edi, 36
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 35
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0xE0]
       mov      edi, 6
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG07
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG13:
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG14:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 925

; Assembly listing for method RyuPy.Generated.Module:m0_read_refs(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x106
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 26 inlinees with PGO data; 44 single block inlinees; 30 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 176
       mov      qword ptr [rsp+0x1C8], r15
       mov      qword ptr [rsp+0x1C0], r14
       mov      qword ptr [rsp+0x1B8], r13
       mov      qword ptr [rsp+0x1B0], r12
       mov      qword ptr [rsp+0x1A8], rbx
       lea      rbp, [rsp+0xB0]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x60], xmm8
       mov      qword ptr [rbp-0x50], rax
       mov      r15, gword ptr [rbp+0xF0]
       mov      rbx, gword ptr [rbp+0xE8]
       mov      r13, gword ptr [rbp+0xE0]
       mov      r14, qword ptr [rbp+0xA0]
       mov      r12, qword ptr [rbp+0x98]
       mov      rax, qword ptr [rbp+0x90]
 
G_M000_IG02:
       test     rax, rax
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0x88], edx
       jmp      G_M000_IG35
 
G_M000_IG03:
       test     r13, r13
       jl       G_M000_IG105
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       mov      r8, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG104
       mov      r13, gword ptr [r8+8*rdi+0x10]
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG52
 
G_M000_IG04:
       mov      r9, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      r9, rdi
       jne      G_M000_IG57
 
G_M000_IG05:
       mov      rsi, qword ptr [r13+0x08]
       test     r8, r8
       je       G_M000_IG56
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG54
 
G_M000_IG07:
       mov      r13, qword ptr [r8+0x08]
       lea      r8, [rsi+r13]
       mov      qword ptr [rbp-0x48], r8
       mov      rdi, rsi
       xor      rdi, r8
       mov      r9, r13
       xor      r9, r8
       and      rdi, r9
       jge      G_M000_IG50
 
G_M000_IG08:
       mov      qword ptr [rbp+0x90], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0xB0], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x80], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xB0]
       mov      esi, dword ptr [rbp-0x80]
       mov      rdx, gword ptr [rbp-0x68]
       mov      rcx, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r13, rax
       mov      eax, edx
       mov      dword ptr [rbp-0x84], edx
       test     r13, r13
       jne      G_M000_IG85
 
G_M000_IG09:
       test     r13, r13
       jne      G_M000_IG94
 
G_M000_IG10:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG48
 
G_M000_IG11:
       mov      edi, -1
 
G_M000_IG12:
       test     edi, edi
       jg       G_M000_IG49
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xA0], rax
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0x84]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0xA0]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG14:
       jmp      G_M000_IG34
 
G_M000_IG15:
       movzx    rdi, byte  ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG45
 
G_M000_IG16:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG45
 
G_M000_IG17:
       movzx    rsi, byte  ptr [rbx+0x08]
       cmp      edi, esi
       sete     dil
       movzx    rdi, dil
       jmp      G_M000_IG46
 
G_M000_IG18:
       mov      rdi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG45
 
G_M000_IG19:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG45
 
G_M000_IG20:
       cmp      rdi, qword ptr [rbx+0x08]
       jne      SHORT G_M000_IG25
 
G_M000_IG21:
       cmp      rdi, -5
       jl       SHORT G_M000_IG24
 
G_M000_IG22:
       cmp      rdi, 256
       setle    dil
       movzx    rdi, dil
 
G_M000_IG23:
       movzx    rdi, dil
       jmp      G_M000_IG46
 
G_M000_IG24:
       xor      edi, edi
       jmp      SHORT G_M000_IG23
 
G_M000_IG25:
       xor      edi, edi
       jmp      SHORT G_M000_IG23
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
 
G_M000_IG27:
       movzx    rdi, byte  ptr [rdi+0x08]
       test     edi, edi
       mov      rax, qword ptr [rbp+0x90]
       mov      edx, dword ptr [rbp-0x88]
       je       G_M000_IG35
 
G_M000_IG28:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG104
       cmp      dword ptr [r8+4*r9+0x10], 35
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG104
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 35
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       test     r13, r13
       jne      G_M000_IG04
 
G_M000_IG32:
       mov      qword ptr [rbp+0x90], rax
 
G_M000_IG33:
       mov      rdx, r8
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG34:
       mov      rax, qword ptr [rbp+0x90]
       mov      edx, dword ptr [rbp-0x88]
 
G_M000_IG35:
       mov      qword ptr [rbp+0x90], rax
       add      r14, rax
       jo       G_M000_IG105
       mov      edi, edx
       cmp      r14, r12
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r14, r12
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG77
 
G_M000_IG36:
       lea      rsi, [r14+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG53
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG38:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG41
 
G_M000_IG39:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG104
       cmp      dword ptr [r9+4*r10+0x10], 34
       je       SHORT G_M000_IG41
 
G_M000_IG40:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG104
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 34
 
G_M000_IG41:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       cmp      rax, rbx
       je       G_M000_IG26
 
G_M000_IG42:
       test     rax, rax
       je       SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       je       G_M000_IG15
 
G_M000_IG44:
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       je       G_M000_IG18
 
G_M000_IG45:
       xor      edi, edi
 
G_M000_IG46:
       test     edi, edi
       jne      G_M000_IG26
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       jmp      G_M000_IG27
 
G_M000_IG48:
       xor      edi, edi
       jmp      G_M000_IG12
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x98], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      r13, gword ptr [rbp-0x98]
       mov      edi, dword ptr [rbp-0x84]
       mov      dword ptr [r13+0x10], edi
       jmp      G_M000_IG14
 
G_M000_IG50:
       lea      r13, [r8+0x05]
       cmp      r13, 261
       jbe      G_M000_IG03
 
G_M000_IG51:
       mov      qword ptr [rbp+0x90], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0x48]
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG52:
       jmp      G_M000_IG14
 
G_M000_IG53:
       test     rsi, rsi
       jl       G_M000_IG105
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      r8, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG104
       mov      rsi, gword ptr [r8+8*rdi+0x10]
       jmp      G_M000_IG38
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       mov      qword ptr [rbp+0x90], rax
       jne      G_M000_IG33
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x70], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x70]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG34
 
G_M000_IG56:
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG33
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      r9, rdi
       jne      G_M000_IG64
 
G_M000_IG58:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r8, r8
       je       G_M000_IG63
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG61
 
G_M000_IG60:
       mov      qword ptr [rbp+0x90], rax
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG34
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       mov      qword ptr [rbp+0x90], rax
       jne      G_M000_IG33
 
G_M000_IG62:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG34
 
G_M000_IG63:
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG33
 
G_M000_IG64:
       mov      rsi, 0xD1FFAB1E
       cmp      r9, rsi
       jne      G_M000_IG76
 
G_M000_IG65:
       mov      rsi, r13
       mov      gword ptr [rbp-0x90], rsi
       test     r8, r8
       je       G_M000_IG75
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       je       SHORT G_M000_IG68
 
G_M000_IG67:
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG33
 
G_M000_IG68:
       mov      r13, r8
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG73
 
G_M000_IG69:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG72
 
G_M000_IG70:
       mov      r8d, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x7C], r8d
       mov      edi, r8d
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG103
       mov      qword ptr [rbp+0x90], rax
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0xA8], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x90]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x7C]
       mov      rax, gword ptr [rbp-0xA8]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0xA8]
 
G_M000_IG71:
       jmp      G_M000_IG34
 
G_M000_IG72:
       mov      r13, rsi
       mov      qword ptr [rbp+0x90], rax
       jmp      SHORT G_M000_IG71
 
G_M000_IG73:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG102
 
G_M000_IG74:
       mov      qword ptr [rbp+0x90], rax
       jmp      SHORT G_M000_IG71
 
G_M000_IG75:
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG33
 
G_M000_IG76:
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG33
 
G_M000_IG77:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG80
 
G_M000_IG78:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rax-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG104
       cmp      dword ptr [rdi+4*r8+0x10], 36
       je       SHORT G_M000_IG80
 
G_M000_IG79:
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG104
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 36
 
G_M000_IG80:
       mov      rcx, qword ptr [rbp+0xD0]
       test     rcx, rcx
       jle      SHORT G_M000_IG83
 
G_M000_IG81:
       cdqe     
       cmp      rcx, rax
       jg       SHORT G_M000_IG83
 
G_M000_IG82:
       dec      ecx
       mov      dword ptr [(reloc)], ecx
 
G_M000_IG83:
       mov      rax, r13
 
G_M000_IG84:
       add      rsp, 424
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG85:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG86
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG87
 
G_M000_IG86:
       mov      ecx, eax
       jmp      SHORT G_M000_IG93
 
G_M000_IG87:
       cmp      edi, 2
       je       SHORT G_M000_IG88
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG104
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG89
 
G_M000_IG88:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG89:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG90
       mov      ecx, -1
       jmp      SHORT G_M000_IG92
 
G_M000_IG90:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG91
       mov      ecx, 1
       jmp      SHORT G_M000_IG92
 
G_M000_IG91:
       xor      ecx, ecx
 
G_M000_IG92:
       imul     ecx, eax
 
G_M000_IG93:
       test     ecx, ecx
       jge      G_M000_IG09
       jmp      G_M000_IG49
 
G_M000_IG94:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG95
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG96
 
G_M000_IG95:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x84]
       jmp      G_M000_IG12
 
G_M000_IG96:
       cmp      edi, 2
       je       SHORT G_M000_IG97
       cmp      dword ptr [r13+0x08], 0
       jbe      SHORT G_M000_IG104
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG98
 
G_M000_IG97:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG98:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG99
       mov      ecx, -1
       jmp      SHORT G_M000_IG101
 
G_M000_IG99:
       test     rdi, rdi
       jge      SHORT G_M000_IG100
       mov      ecx, 1
       jmp      SHORT G_M000_IG101
 
G_M000_IG100:
       xor      ecx, ecx
 
G_M000_IG101:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG12
 
G_M000_IG102:
       mov      r13, 0xD1FFAB1E
       mov      qword ptr [rbp+0x90], rax
       jmp      G_M000_IG71
 
G_M000_IG103:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG104:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG105:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2199

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 880
       lea      rbp, [rsp+0x370]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -816
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x360], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x78], rax
       mov      edi, 40
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyNumericListOps:Create():System.Object]
       mov      gword ptr [rbp-0x80], rax
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 41
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x88], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x90], rax
       mov      rdi, gword ptr [rbp-0x88]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x98], rax
       mov      rdi, gword ptr [rbp-0x90]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xA0], rax
       mov      rax, qword ptr [rbp-0x98]
       mov      qword ptr [rbp-0xA8], rax
       mov      rax, qword ptr [rbp-0xA0]
       mov      qword ptr [rbp-0xB0], rax
       mov      qword ptr [rbp-0xB8], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x360]
       mov      esi, 97
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0xB8], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       mov      rax, qword ptr [rbp-0xA8]
       cmp      rax, qword ptr [rbp-0xB0]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xC0], eax
       mov      rax, qword ptr [rbp-0xA8]
       cmp      rax, qword ptr [rbp-0xB0]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xC4], eax
       mov      eax, dword ptr [rbp-0xBC]
       and      eax, dword ptr [rbp-0xC0]
       movzx    rax, al
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0xBC], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xCC], eax
       mov      eax, dword ptr [rbp-0xCC]
       and      eax, dword ptr [rbp-0xC4]
       movzx    rax, al
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0xC8]
       or       eax, dword ptr [rbp-0xD0]
       movzx    rax, al
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0xD4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      edi, 42
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 43
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x338], rax
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x340], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x368], rax
       mov      rcx, gword ptr [rbp-0x368]
       mov      rsi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x340]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, gword ptr [rbp-0xF0]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x100], rax
       mov      rdi, gword ptr [rbp-0xF8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x108], rax
       mov      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x110], rax
       mov      rax, qword ptr [rbp-0x108]
       mov      qword ptr [rbp-0x118], rax
       mov      qword ptr [rbp-0x120], 1
       xor      eax, eax
       mov      qword ptr [rbp-0x288], rax
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x28C], eax
       cmp      dword ptr [rbp-0x28C], 0
       jne      G_M000_IG60
       jmp      G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xA8]
       add      rax, qword ptr [rbp-0xB8]
       jo       G_M000_IG103
       mov      qword ptr [rbp-0xA8], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 46
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyNumericListOps:Create():System.Object]
       mov      gword ptr [rbp-0x178], rax
       mov      rax, gword ptr [rbp-0x178]
       mov      gword ptr [rbp-0x68], rax
       mov      edi, 48
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x180], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x188], rax
       mov      rdi, gword ptr [rbp-0x180]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x190], rax
       mov      rdi, gword ptr [rbp-0x188]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x198], rax
       mov      rax, qword ptr [rbp-0x190]
       mov      qword ptr [rbp-0x1A0], rax
       mov      rax, qword ptr [rbp-0x198]
       mov      qword ptr [rbp-0x1A8], rax
       mov      qword ptr [rbp-0x1B0], 1
       jmp      G_M000_IG15
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x360]
       mov      esi, 402
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG11:
       cmp      qword ptr [rbp-0x120], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x124], eax
       mov      rax, qword ptr [rbp-0x110]
       cmp      rax, qword ptr [rbp-0x118]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x128], eax
       mov      rax, qword ptr [rbp-0x110]
       cmp      rax, qword ptr [rbp-0x118]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x12C], eax
       mov      eax, dword ptr [rbp-0x124]
       and      eax, dword ptr [rbp-0x128]
       movzx    rax, al
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x124], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x134], eax
       mov      eax, dword ptr [rbp-0x134]
       and      eax, dword ptr [rbp-0x12C]
       movzx    rax, al
       mov      dword ptr [rbp-0x138], eax
       mov      eax, dword ptr [rbp-0x130]
       or       eax, dword ptr [rbp-0x138]
       movzx    rax, al
       mov      dword ptr [rbp-0x13C], eax
       cmp      dword ptr [rbp-0x13C], 0
       jne      SHORT G_M000_IG12
       jmp      SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rdi, qword ptr [rbp-0x110]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 44
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x28C], 0
       jne      G_M000_IG57
       jmp      G_M000_IG50
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x110]
       add      rax, qword ptr [rbp-0x120]
       jo       G_M000_IG103
       mov      qword ptr [rbp-0x110], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       cmp      gword ptr [rbp-0x58], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x290], eax
       mov      eax, dword ptr [rbp-0x28C]
       and      eax, dword ptr [rbp-0x290]
       movzx    rax, al
       mov      dword ptr [rbp-0x294], eax
       cmp      dword ptr [rbp-0x294], 0
       jne      G_M000_IG65
       jmp      G_M000_IG64
 
G_M000_IG15:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG17
 
G_M000_IG16:
       lea      rdi, [rbp-0x360]
       mov      esi, 541
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG17:
       cmp      qword ptr [rbp-0x1B0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1B4], eax
       mov      rax, qword ptr [rbp-0x1A0]
       cmp      rax, qword ptr [rbp-0x1A8]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1B8], eax
       mov      rax, qword ptr [rbp-0x1A0]
       cmp      rax, qword ptr [rbp-0x1A8]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1BC], eax
       mov      eax, dword ptr [rbp-0x1B4]
       and      eax, dword ptr [rbp-0x1B8]
       movzx    rax, al
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1B4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1C4], eax
       mov      eax, dword ptr [rbp-0x1C4]
       and      eax, dword ptr [rbp-0x1BC]
       movzx    rax, al
       mov      dword ptr [rbp-0x1C8], eax
       mov      eax, dword ptr [rbp-0x1C0]
       or       eax, dword ptr [rbp-0x1C8]
       movzx    rax, al
       mov      dword ptr [rbp-0x1CC], eax
       cmp      dword ptr [rbp-0x1CC], 0
       jne      SHORT G_M000_IG18
       jmp      G_M000_IG20
 
G_M000_IG18:
       mov      edi, 49
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x1E0], rax
       mov      rax, gword ptr [rbp-0x1E0]
       mov      gword ptr [rbp-0x70], rax
       mov      edi, 50
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x1E8], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x350], rax
       mov      rdi, gword ptr [rbp-0x350]
       mov      rdx, gword ptr [rbp-0x48]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x358], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x370], rax
       mov      rcx, gword ptr [rbp-0x370]
       mov      rsi, gword ptr [rbp-0x350]
       mov      rdx, gword ptr [rbp-0x358]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x1F0], rax
       mov      rdi, gword ptr [rbp-0x1E8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x1F8], rax
       mov      rdi, gword ptr [rbp-0x1F0]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x200], rax
       mov      rax, qword ptr [rbp-0x1F8]
       mov      qword ptr [rbp-0x208], rax
       mov      rax, qword ptr [rbp-0x200]
       mov      qword ptr [rbp-0x210], rax
       mov      qword ptr [rbp-0x218], 1
       xor      eax, eax
       mov      qword ptr [rbp-0x2E0], rax
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E4], eax
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG97
       jmp      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x1A0]
       add      rax, qword ptr [rbp-0x1B0]
       jo       G_M000_IG103
       mov      qword ptr [rbp-0x1A0], rax
       jmp      G_M000_IG15
 
G_M000_IG20:
       mov      edi, 53
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x278], rax
       mov      rdi, gword ptr [rbp-0x68]
       mov      rsi, gword ptr [rbp-0x278]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x280], rax
       mov      rax, gword ptr [rbp-0x280]
       mov      gword ptr [rbp-0x270], rax
       jmp      G_M000_IG27
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x360]
       mov      esi, 801
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       cmp      qword ptr [rbp-0x218], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x21C], eax
       mov      rax, qword ptr [rbp-0x208]
       cmp      rax, qword ptr [rbp-0x210]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x220], eax
       mov      rax, qword ptr [rbp-0x208]
       cmp      rax, qword ptr [rbp-0x210]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x224], eax
       mov      eax, dword ptr [rbp-0x21C]
       and      eax, dword ptr [rbp-0x220]
       movzx    rax, al
       mov      dword ptr [rbp-0x228], eax
       cmp      dword ptr [rbp-0x21C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x22C], eax
       mov      eax, dword ptr [rbp-0x22C]
       and      eax, dword ptr [rbp-0x224]
       movzx    rax, al
       mov      dword ptr [rbp-0x230], eax
       mov      eax, dword ptr [rbp-0x228]
       or       eax, dword ptr [rbp-0x230]
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      SHORT G_M000_IG24
       jmp      SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rdi, qword ptr [rbp-0x208]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x240], rax
       mov      rax, gword ptr [rbp-0x240]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 51
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG94
       jmp      G_M000_IG87
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x208]
       add      rax, qword ptr [rbp-0x218]
       jo       G_M000_IG103
       mov      qword ptr [rbp-0x208], rax
       jmp      G_M000_IG21
 
G_M000_IG26:
       cmp      gword ptr [rbp-0x70], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E8], eax
       mov      eax, dword ptr [rbp-0x2E4]
       and      eax, dword ptr [rbp-0x2E8]
       movzx    rax, al
       mov      dword ptr [rbp-0x2EC], eax
       cmp      dword ptr [rbp-0x2EC], 0
       jne      G_M000_IG102
       jmp      G_M000_IG101
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x270]
 
G_M000_IG28:
       add      rsp, 880
       pop      rbp
       ret      
 
G_M000_IG29:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, [rbp-0x360]
       mov      esi, 951
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG31:
       jmp      G_M000_IG13
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x288], xmm0
       jmp      SHORT G_M000_IG29
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x360]
       mov      esi, 969
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rdx, gword ptr [rbp-0x150]
       mov      rcx, gword ptr [rbp-0x158]
       mov      edi, 7
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x160], rax
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x160]
       mov      edi, 8
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x168], rax
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x58], rax
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x28C], eax
       cmp      dword ptr [rbp-0x28C], 0
       jne      G_M000_IG32
       jmp      G_M000_IG29
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x348], rax
       mov      rax, gword ptr [rbp-0x348]
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x348]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG33
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x410
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG39:
       cmp      gword ptr [rbp-0x58], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x290], eax
       mov      eax, dword ptr [rbp-0x28C]
       and      eax, dword ptr [rbp-0x290]
       movzx    rax, al
       mov      dword ptr [rbp-0x294], eax
       cmp      dword ptr [rbp-0x294], 0
       jne      G_M000_IG36
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x429
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       vmovsd   xmm0, qword ptr [rbp-0x2A8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2B8]
       vmovsd   qword ptr [rbp-0x2C0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2C0]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vmovsd   qword ptr [rbp-0x288], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG43:
       mov      rsi, gword ptr [rbp-0x2A0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       mov      rsi, gword ptr [rbp-0x2B0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2B8], xmm0
       jmp      G_M000_IG40
 
G_M000_IG44:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG46
 
G_M000_IG45:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x459
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG46:
       mov      rsi, gword ptr [rbp-0x2B0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2CC], eax
       cmp      dword ptr [rbp-0x2CC], 0
       jne      G_M000_IG43
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG37
 
G_M000_IG47:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG49
 
G_M000_IG48:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x474
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG49:
       mov      rsi, gword ptr [rbp-0x2A0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2D0], eax
       cmp      dword ptr [rbp-0x2D0], 0
       jne      G_M000_IG44
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG37
 
G_M000_IG50:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG52
 
G_M000_IG51:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x48F
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG52:
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x150], rax
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x158], rax
       mov      rax, gword ptr [rbp-0x150]
       mov      gword ptr [rbp-0x2A0], rax
       mov      rax, gword ptr [rbp-0x158]
       mov      gword ptr [rbp-0x2B0], rax
       cmp      dword ptr [rbp-0x28C], 0
       jne      G_M000_IG47
       jmp      G_M000_IG37
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x2B8], xmm0
       jmp      G_M000_IG40
 
G_M000_IG54:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG56
 
G_M000_IG55:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x4CC
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG56:
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0x2D8], eax
       cmp      dword ptr [rbp-0x2D8], 0
       jne      G_M000_IG53
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG50
 
G_M000_IG57:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG59
 
G_M000_IG58:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x4E1
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG59:
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0x2D4], eax
       cmp      dword ptr [rbp-0x2D4], 0
       jne      G_M000_IG54
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG50
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x288], xmm0
       jmp      G_M000_IG09
 
G_M000_IG61:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG63
 
G_M000_IG62:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x503
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG63:
       jmp      G_M000_IG07
 
G_M000_IG64:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x48]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x170], rax
       jmp      SHORT G_M000_IG61
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x48]
       vmovsd   xmm0, qword ptr [rbp-0x288]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       mov      gword ptr [rbp-0x170], rax
       jmp      G_M000_IG61
 
G_M000_IG66:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG68
 
G_M000_IG67:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x53A
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG68:
       jmp      G_M000_IG25
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2E0], xmm0
       jmp      SHORT G_M000_IG66
 
G_M000_IG70:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG72
 
G_M000_IG71:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x54D
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG72:
       mov      rdx, gword ptr [rbp-0x248]
       mov      rcx, gword ptr [rbp-0x250]
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x258], rax
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x258]
       mov      edi, 11
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x260], rax
       mov      rax, gword ptr [rbp-0x260]
       mov      gword ptr [rbp-0x70], rax
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E4], eax
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG69
       jmp      G_M000_IG66
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x348], rax
       mov      rax, gword ptr [rbp-0x348]
       vmovsd   xmm0, qword ptr [rbp-0x2E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x348]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG70
 
G_M000_IG74:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG76
 
G_M000_IG75:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x59A
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG76:
       cmp      gword ptr [rbp-0x70], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E8], eax
       mov      eax, dword ptr [rbp-0x2E4]
       and      eax, dword ptr [rbp-0x2E8]
       movzx    rax, al
       mov      dword ptr [rbp-0x2EC], eax
       cmp      dword ptr [rbp-0x2EC], 0
       jne      G_M000_IG73
       jmp      G_M000_IG70
 
G_M000_IG77:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG79
 
G_M000_IG78:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x5B4
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rbp-0x318], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2E0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rbp-0x320], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x320]
       vmovsd   qword ptr [rbp-0x2E0], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG66
 
G_M000_IG80:
       mov      rsi, gword ptr [rbp-0x2F8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x300], xmm0
       mov      rsi, gword ptr [rbp-0x308]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x310], xmm0
       jmp      G_M000_IG77
 
G_M000_IG81:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG83
 
G_M000_IG82:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x5E5
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG83:
       mov      rsi, gword ptr [rbp-0x308]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x324], eax
       cmp      dword ptr [rbp-0x324], 0
       jne      G_M000_IG80
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG74
 
G_M000_IG84:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG86
 
G_M000_IG85:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x600
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG86:
       mov      rsi, gword ptr [rbp-0x2F8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x328], eax
       cmp      dword ptr [rbp-0x328], 0
       jne      G_M000_IG81
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG74
 
G_M000_IG87:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG89
 
G_M000_IG88:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x61B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG89:
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x248], rax
       mov      rdi, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x250], rax
       mov      rax, gword ptr [rbp-0x248]
       mov      gword ptr [rbp-0x2F8], rax
       mov      rax, gword ptr [rbp-0x250]
       mov      gword ptr [rbp-0x308], rax
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG84
       jmp      G_M000_IG74
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x300], xmm0
       mov      rdi, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x310], xmm0
       jmp      G_M000_IG77
 
G_M000_IG91:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG93
 
G_M000_IG92:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x658
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG93:
       mov      rdi, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0x330], eax
       cmp      dword ptr [rbp-0x330], 0
       jne      G_M000_IG90
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG87
 
G_M000_IG94:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG96
 
G_M000_IG95:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x66D
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG96:
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0x32C], eax
       cmp      dword ptr [rbp-0x32C], 0
       jne      G_M000_IG91
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG87
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2E0], xmm0
       jmp      G_M000_IG21
 
G_M000_IG98:
       mov      eax, dword ptr [rbp-0x360]
       dec      eax
       mov      dword ptr [rbp-0x360], eax
       cmp      dword ptr [rbp-0x360], 0
       jg       SHORT G_M000_IG100
 
G_M000_IG99:
       lea      rdi, [rbp-0x360]
       mov      esi, 0x690
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG100:
       jmp      G_M000_IG19
 
G_M000_IG101:
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x68]
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x268], rax
       jmp      SHORT G_M000_IG98
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x68]
       vmovsd   xmm0, qword ptr [rbp-0x2E0]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       mov      gword ptr [rbp-0x268], rax
       jmp      G_M000_IG98
 
G_M000_IG103:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 5318

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x429
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 42 inlinees with PGO data; 76 single block inlinees; 106 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 672
       mov      qword ptr [rsp+0x618], r15
       mov      qword ptr [rsp+0x610], r14
       mov      qword ptr [rsp+0x608], r13
       mov      qword ptr [rsp+0x600], r12
       mov      qword ptr [rsp+0x5F8], rbx
       lea      rbp, [rsp+0x2A0]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -480
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x350]
       mov      r15, gword ptr [rbp+0x348]
       mov      r14, gword ptr [rbp+0x340]
       mov      r13, qword ptr [rbp+0x270]
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       mov      r12d, dword ptr [rbp+0xF4]
       vmovsd   xmm1, qword ptr [rbp+0xD8]
       vmovsd   xmm2, qword ptr [rbp+0xC8]
 
G_M000_IG02:
       jmp      G_M000_IG63
 
G_M000_IG03:
       mov      esi, dword ptr [rax+0x10]
       jmp      G_M000_IG53
 
G_M000_IG04:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG55
 
G_M000_IG05:
       mov      dword ptr [rbp-0x134], ecx
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG12
 
G_M000_IG06:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rbp-0x134]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG310
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       test     rsi, rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG13
 
G_M000_IG08:
       xor      esi, esi
 
G_M000_IG09:
       test     esi, esi
       je       G_M000_IG91
 
G_M000_IG10:
       test     r15, r15
       jne      SHORT G_M000_IG14
 
G_M000_IG11:
       mov      rax, gword ptr [rbp-0x240]
       jmp      G_M000_IG57
 
G_M000_IG12:
       mov      esi, dword ptr [rbp-0x134]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG310
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       shl      rsi, 4
       mov      rsi, gword ptr [rdi+rsi+0x10]
       test     rsi, rsi
       je       G_M000_IG309
 
G_M000_IG13:
       mov      esi, 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG14:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r15], rsi
       je       SHORT G_M000_IG16
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x240]
       jmp      G_M000_IG57
 
G_M000_IG16:
       mov      rax, gword ptr [rbp-0x240]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x120], rcx
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG22
 
G_M000_IG17:
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG18:
       mov      rax, qword ptr [rbp-0x120]
       test     rax, rax
       jl       SHORT G_M000_IG23
 
G_M000_IG19:
       mov      rcx, rax
 
G_M000_IG20:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       SHORT G_M000_IG24
 
G_M000_IG21:
       mov      rax, gword ptr [rbp-0x240]
       jmp      G_M000_IG57
 
G_M000_IG22:
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG18
 
G_M000_IG23:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      SHORT G_M000_IG20
 
G_M000_IG24:
       mov      dword ptr [rbp-0x124], ecx
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG89
 
G_M000_IG25:
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      edi, dword ptr [rbp-0x124]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG310
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG90
 
G_M000_IG27:
       xor      edi, edi
 
G_M000_IG28:
       test     edi, edi
       mov      rax, gword ptr [rbp-0x240]
       je       G_M000_IG57
 
G_M000_IG29:
       mov      rdi, rbx
       mov      gword ptr [rbp-0x240], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0xD8], xmm0
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm2, xmm0
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   xmm1, qword ptr [rbp+0xD8]
       jmp      G_M000_IG63
 
G_M000_IG30:
       mov      gword ptr [rbp+0x328], rsi
 
G_M000_IG31:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r12, rax
 
G_M000_IG32:
       mov      rax, gword ptr [rbp+0x328]
       test     rax, rax
       je       SHORT G_M000_IG36
 
G_M000_IG33:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG123
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG134
 
G_M000_IG35:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       je       G_M000_IG139
 
G_M000_IG36:
       mov      rdx, r12
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG37:
       mov      r12, rax
       test     r12, r12
       jne      G_M000_IG149
 
G_M000_IG38:
       xor      eax, eax
       mov      gword ptr [rbp+0x328], r12
       mov      r12d, eax
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
 
G_M000_IG39:
       add      r13, rcx
       jo       G_M000_IG312
 
G_M000_IG40:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       cmp      r13, rax
       setl     r9b
       movzx    r9, r9b
       and      r9d, edi
       cmp      r13, rax
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, r9d
       je       G_M000_IG70
 
G_M000_IG41:
       lea      rsi, [r13+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG88
 
G_M000_IG42:
       mov      qword ptr [rbp+0x268], rax
       mov      qword ptr [rbp+0x260], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG43:
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r9, rdi
       dec      esi
       mov      r8d, esi
       cmp      r8d, dword ptr [r9+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [r9+4*r8+0x10], 44
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       mov      esi, esi
       mov      dword ptr [rdi+4*rsi+0x10], 44
 
G_M000_IG46:
       test     r12d, r12d
       je       G_M000_IG57
 
G_M000_IG47:
       test     rbx, rbx
       je       G_M000_IG57
 
G_M000_IG48:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG57
 
G_M000_IG49:
       test     rax, rax
       je       SHORT G_M000_IG57
 
G_M000_IG50:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG57
 
G_M000_IG51:
       mov      gword ptr [rbp-0x240], rax
       mov      r9, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x130], r9
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG03
 
G_M000_IG52:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG53:
       mov      rax, qword ptr [rbp-0x130]
       test     rax, rax
       jl       G_M000_IG04
 
G_M000_IG54:
       mov      rcx, rax
 
G_M000_IG55:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       G_M000_IG05
 
G_M000_IG56:
       mov      rax, gword ptr [rbp-0x240]
 
G_M000_IG57:
       mov      rdi, rbx
       mov      gword ptr [rbp-0x240], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x230], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x228], rax
       test     r12d, r12d
       je       G_M000_IG64
 
G_M000_IG58:
       mov      rcx, gword ptr [rbp+0x230]
       test     rcx, rcx
       je       SHORT G_M000_IG64
 
G_M000_IG59:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG64
 
G_M000_IG60:
       test     rax, rax
       je       SHORT G_M000_IG64
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      rax, gword ptr [rbp+0x228]
       jne      SHORT G_M000_IG64
 
G_M000_IG62:
       mov      rdi, gword ptr [rbp+0x230]
       vmovsd   xmm1, qword ptr [rdi+0x08]
       vmovsd   xmm2, qword ptr [rax+0x08]
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
 
G_M000_IG63:
       vmulsd   xmm1, xmm1, xmm2
       vaddsd   xmm0, xmm1, xmm0
       vmovsd   qword ptr [rbp+0xF8], xmm0
       xor      r11, r11
       mov      gword ptr [rbp+0x328], r11
       jmp      G_M000_IG39
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp+0x328]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       je       SHORT G_M000_IG66
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x328], rsi
       mov      rax, gword ptr [rbp+0x228]
       mov      rsi, gword ptr [rbp+0x328]
 
G_M000_IG66:
       mov      rcx, gword ptr [rbp+0x230]
       test     rcx, rcx
       je       G_M000_IG30
 
G_M000_IG67:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG92
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG117
 
G_M000_IG69:
       mov      gword ptr [rbp+0x328], rsi
       jmp      G_M000_IG31
 
G_M000_IG70:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r13, gword ptr [rbp+0x328]
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG84
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG73
 
G_M000_IG72:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x270], rax
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x270], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG74:
       mov      rdi, gword ptr [rbp-0x270]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x270]
       mov      rsi, gword ptr [rbp+0x338]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG75:
       mov      r12, qword ptr [rbp+0x2D8]
       mov      r13, qword ptr [rbp+0x2C8]
       add      r12, r13
       jo       G_M000_IG312
       mov      qword ptr [rbp+0x2C8], r13
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x2D0]
       cmp      r12, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2D0], rax
       cmp      r12, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG152
 
G_M000_IG76:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG79
 
G_M000_IG77:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       cmp      r8d, dword ptr [rcx+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rcx+4*r8+0x10], 42
       je       SHORT G_M000_IG79
 
G_M000_IG78:
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       mov      ecx, edx
       mov      dword ptr [rsi+4*rcx+0x10], 42
 
G_M000_IG79:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x328], rsi
       test     edi, edi
       jle      SHORT G_M000_IG82
 
G_M000_IG80:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rdx+4*r8+0x10], 43
       je       SHORT G_M000_IG82
 
G_M000_IG81:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp+0x290], rdx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x230], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x230]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x288], rax
       mov      rdi, gword ptr [rbp+0x290]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x280], rax
       mov      rdi, gword ptr [rbp+0x288]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x280]
       mov      rcx, rdi
       mov      qword ptr [rbp+0x268], rax
       mov      qword ptr [rbp+0x260], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x328]
       test     rsi, rsi
       jne      SHORT G_M000_IG85
 
G_M000_IG83:
       xor      r8d, r8d
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG40
 
G_M000_IG84:
       mov      rdi, gword ptr [rbp+0x338]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       jmp      G_M000_IG75
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG87
 
G_M000_IG86:
       mov      r8d, 1
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG40
 
G_M000_IG87:
       xor      r8d, r8d
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG40
 
G_M000_IG88:
       test     rsi, rsi
       jl       G_M000_IG312
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG311
       mov      rsi, gword ptr [r9+8*rsi+0x10]
       mov      rdi, rsi
       mov      qword ptr [rbp+0x268], rax
       mov      qword ptr [rbp+0x260], rcx
       mov      rax, rdi
       jmp      G_M000_IG43
 
G_M000_IG89:
       mov      edi, dword ptr [rbp-0x124]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG310
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG308
 
G_M000_IG90:
       mov      edi, 1
       jmp      G_M000_IG28
 
G_M000_IG91:
       mov      rax, gword ptr [rbp-0x240]
       jmp      G_M000_IG57
 
G_M000_IG92:
       mov      rdx, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG94
 
G_M000_IG93:
       mov      gword ptr [rbp+0x328], rsi
       jmp      G_M000_IG31
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG115
 
G_M000_IG95:
       mov      r12, qword ptr [rax+0x08]
       lea      rdi, [rbp-0x90]
       mulx     rcx, rax, r12
       mov      qword ptr [rdi], rax
       mov      rax, qword ptr [rbp-0x90]
       mov      qword ptr [rbp-0x88], rax
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, r12
       sub      rcx, rdi
       mov      rdi, r12
       sar      rdi, 63
       and      rdi, rdx
       sub      rcx, rdi
       mov      rdi, rax
       sar      rdi, 63
       cmp      rcx, rdi
       je       G_M000_IG111
 
G_M000_IG96:
       mov      gword ptr [rbp+0x328], rsi
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x80], xmm1
       lea      rdi, [rbp-0x80]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x280], rax
       mov      ecx, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x214], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       lea      rdi, [rbp-0xA0]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xA0]
       mov      r9d, dword ptr [rbp-0x98]
       mov      rax, gword ptr [rbp-0x280]
       test     rax, rax
       jne      G_M000_IG104
 
G_M000_IG97:
       test     r12, r12
       jne      G_M000_IG288
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       movsxd   rsi, dword ptr [rbp-0x214]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xB0]
       mov      eax, dword ptr [rbp-0xA8]
 
G_M000_IG98:
       test     r12, r12
       jne      G_M000_IG289
 
G_M000_IG99:
       test     r12, r12
       jne      G_M000_IG298
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG109
 
G_M000_IG100:
       mov      edi, -1
 
G_M000_IG101:
       test     edi, edi
       mov      dword ptr [rbp-0x218], eax
       jg       SHORT G_M000_IG110
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x258], rax
       mov      rdi, r12
       mov      esi, dword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x258]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG103:
       jmp      G_M000_IG32
 
G_M000_IG104:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG105:
       test     r12, r12
       je       SHORT G_M000_IG108
 
G_M000_IG106:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG107:
       mov      edx, dword ptr [rbp-0x214]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      eax, edx
       jmp      G_M000_IG98
 
G_M000_IG108:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG107
 
G_M000_IG109:
       xor      edi, edi
       jmp      SHORT G_M000_IG101
 
G_M000_IG110:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x250], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x250]
       mov      edi, dword ptr [rbp-0x218]
       mov      dword ptr [r12+0x10], edi
       jmp      SHORT G_M000_IG103
 
G_M000_IG111:
       lea      r12, [rax+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG114
 
G_M000_IG112:
       mov      gword ptr [rbp+0x328], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG113:
       jmp      G_M000_IG103
 
G_M000_IG114:
       test     r12, r12
       jl       G_M000_IG312
       mov      rdi, r12
       mov      rax, 0xD1FFAB1E
       mov      r9, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG311
       mov      r12, gword ptr [r9+8*rdi+0x10]
       mov      gword ptr [rbp+0x328], rsi
       jmp      SHORT G_M000_IG113
 
G_M000_IG115:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x328], rsi
       jne      G_M000_IG31
 
G_M000_IG116:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rdx
       vmovsd   qword ptr [rbp-0xB8], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xB8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG32
 
G_M000_IG117:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       test     rax, rax
       je       G_M000_IG122
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG120
 
G_M000_IG119:
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG32
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x328], rsi
       jne      G_M000_IG31
 
G_M000_IG121:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG32
 
G_M000_IG122:
       mov      gword ptr [rbp+0x328], rsi
       jmp      G_M000_IG31
 
G_M000_IG123:
       mov      rsi, qword ptr [rax+0x08]
       test     r12, r12
       je       G_M000_IG36
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG132
 
G_M000_IG125:
       mov      r12, qword ptr [r12+0x08]
       lea      rax, [rsi+r12]
       mov      qword ptr [rbp-0xE0], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r12
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG128
 
G_M000_IG126:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF0]
       mov      gword ptr [rbp-0x288], rax
       mov      ecx, dword ptr [rbp-0xE8]
       mov      dword ptr [rbp-0x21C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x100], xmm0
       lea      rdi, [rbp-0x100]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x288]
       mov      esi, dword ptr [rbp-0x21C]
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG127:
       jmp      G_M000_IG37
 
G_M000_IG128:
       lea      r12, [rax+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG131
 
G_M000_IG129:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp-0xE0]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG130:
       jmp      SHORT G_M000_IG127
 
G_M000_IG131:
       test     r12, r12
       jl       G_M000_IG312
       mov      rax, r12
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG311
       mov      rax, gword ptr [r9+8*rax+0x10]
       jmp      SHORT G_M000_IG130
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG36
 
G_M000_IG133:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x108]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG37
 
G_M000_IG134:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       test     r12, r12
       je       G_M000_IG36
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG137
 
G_M000_IG136:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG37
 
G_M000_IG137:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG36
 
G_M000_IG138:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG37
 
G_M000_IG139:
       mov      rsi, rax
       mov      gword ptr [rbp-0x260], rsi
       test     r12, r12
       je       G_M000_IG36
 
G_M000_IG140:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG36
 
G_M000_IG141:
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG146
 
G_M000_IG142:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG145
 
G_M000_IG143:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x114], eax
       mov      edi, eax
       add      edi, dword ptr [r12+0x08]
       js       G_M000_IG307
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x268], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x260]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x114]
       mov      rax, gword ptr [rbp-0x268]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r12, gword ptr [rbp-0x268]
       mov      rax, r12
 
G_M000_IG144:
       jmp      G_M000_IG37
 
G_M000_IG145:
       mov      rax, rsi
       jmp      SHORT G_M000_IG144
 
G_M000_IG146:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rax, r12
       jmp      SHORT G_M000_IG144
 
G_M000_IG148:
       mov      rax, 0xD1FFAB1E
       jmp      SHORT G_M000_IG144
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG151
 
G_M000_IG150:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      gword ptr [rbp+0x328], r12
       mov      r12d, eax
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG39
 
G_M000_IG151:
       xor      eax, eax
       mov      gword ptr [rbp+0x328], r12
       mov      r12d, eax
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG39
 
G_M000_IG152:
       mov      edi, dword ptr [(reloc)]
       mov      ebx, edi
       test     ebx, ebx
       jle      SHORT G_M000_IG155
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rdi+4*rcx+0x10], 46
       je       SHORT G_M000_IG155
 
G_M000_IG154:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 46
 
G_M000_IG155:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       test     ebx, ebx
       jle      SHORT G_M000_IG158
 
G_M000_IG156:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rdi+4*rcx+0x10], 48
       je       SHORT G_M000_IG158
 
G_M000_IG157:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 48
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rbx, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG274
 
G_M000_IG159:
       mov      rdi, r15
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       jmp      G_M000_IG273
 
G_M000_IG160:
       mov      gword ptr [rbp+0x338], r8
       jmp      G_M000_IG242
 
G_M000_IG161:
       mov      rcx, qword ptr [rdx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG163
 
G_M000_IG162:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG213
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG178
 
G_M000_IG164:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x158], rax
       lea      rdi, [rbp-0x178]
       mov      rdx, rcx
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x178]
       mov      qword ptr [rbp-0x170], r8
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG174
 
G_M000_IG165:
       mov      gword ptr [rbp+0x310], rsi
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x168], xmm1
       lea      rdi, [rbp-0x168]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x290], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x220], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       lea      rdi, [rbp-0x188]
       mov      rsi, qword ptr [rbp-0x158]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x188]
       mov      r9d, dword ptr [rbp-0x180]
       mov      rcx, gword ptr [rbp-0x290]
       test     rcx, rcx
       jne      SHORT G_M000_IG169
 
G_M000_IG166:
       test     rax, rax
       jne      G_M000_IG306
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x198], xmm0
       movsxd   rsi, dword ptr [rbp-0x220]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x198]
       mov      esi, dword ptr [rbp-0x190]
 
G_M000_IG167:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG168:
       jmp      G_M000_IG214
 
G_M000_IG169:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG170:
       test     rax, rax
       je       SHORT G_M000_IG173
 
G_M000_IG171:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG172:
       mov      edx, dword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG167
 
G_M000_IG173:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG172
 
G_M000_IG174:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG177
 
G_M000_IG175:
       mov      gword ptr [rbp+0x310], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x170]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG176:
       jmp      SHORT G_M000_IG168
 
G_M000_IG177:
       test     rax, rax
       jl       G_M000_IG312
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG311
       test     rax, rax
       jl       G_M000_IG312
       mov      rdi, rax
       mov      rdx, gword ptr [r9+8*rdi+0x10]
       mov      gword ptr [rbp+0x310], rsi
       jmp      SHORT G_M000_IG176
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rsi
       jne      G_M000_IG213
 
G_M000_IG179:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rcx
       vmovsd   qword ptr [rbp-0x1A0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x1A0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG214
 
G_M000_IG180:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm1
       test     rax, rax
       je       G_M000_IG185
 
G_M000_IG181:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG183
 
G_M000_IG182:
       mov      gword ptr [rbp+0x310], rsi
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG214
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rsi
       jne      G_M000_IG213
 
G_M000_IG184:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG214
 
G_M000_IG185:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG213
 
G_M000_IG186:
       test     rax, rax
       je       SHORT G_M000_IG188
 
G_M000_IG187:
       cmp      dword ptr [rax], eax
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG213
 
G_M000_IG188:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG213
 
G_M000_IG189:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG218
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG198
 
G_M000_IG191:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1C8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1D0], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG194
 
G_M000_IG192:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1E0]
       mov      gword ptr [rbp-0x298], rax
       mov      ecx, dword ptr [rbp-0x1D8]
       mov      dword ptr [rbp-0x224], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       lea      rdi, [rbp-0x1F0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x298]
       mov      esi, dword ptr [rbp-0x224]
       mov      rdx, gword ptr [rbp-0x1F0]
       mov      rcx, qword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG193:
       jmp      G_M000_IG219
 
G_M000_IG194:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG197
 
G_M000_IG195:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1D0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG196:
       jmp      SHORT G_M000_IG193
 
G_M000_IG197:
       test     rax, rax
       jl       G_M000_IG312
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r9, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG311
       test     rax, rax
       jl       G_M000_IG312
       mov      rdi, rax
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG196
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG218
 
G_M000_IG199:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1C0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG219
 
G_M000_IG200:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       test     rdx, rdx
       je       G_M000_IG218
 
G_M000_IG201:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG203
 
G_M000_IG202:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1B0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG219
 
G_M000_IG203:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG218
 
G_M000_IG204:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1B0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x200]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG219
 
G_M000_IG205:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG218
 
G_M000_IG206:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG218
 
G_M000_IG207:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG219
 
G_M000_IG208:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      G_M000_IG267
 
G_M000_IG209:
       test     rax, rax
       setne    sil
       movzx    rsi, sil
       mov      dword ptr [rbp+0x9C], esi
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       SHORT G_M000_IG211
 
G_M000_IG210:
       mov      gword ptr [rbp+0x310], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x310]
 
G_M000_IG211:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rcx, gword ptr [rbp+0x310]
       jmp      G_M000_IG221
 
G_M000_IG212:
       mov      gword ptr [rbp+0x310], rsi
 
G_M000_IG213:
       mov      rdx, rax
       mov      rsi, gword ptr [rbp+0x138]
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG214:
       mov      rax, gword ptr [rbp+0x310]
       test     rax, rax
       je       SHORT G_M000_IG218
 
G_M000_IG215:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG189
 
G_M000_IG216:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG200
 
G_M000_IG217:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG205
 
G_M000_IG218:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG219:
       mov      rcx, rax
       test     rcx, rcx
       jne      G_M000_IG208
 
G_M000_IG220:
       xor      eax, eax
       mov      dword ptr [rbp+0x9C], eax
 
G_M000_IG221:
       add      r12, 1
       jo       G_M000_IG312
       vmovsd   xmm0, qword ptr [rbp+0xA0]
 
G_M000_IG222:
       cmp      r12, qword ptr [rbp+0x170]
       jge      G_M000_IG268
 
G_M000_IG223:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG266
 
G_M000_IG224:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG225:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG228
 
G_M000_IG226:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rsi+4*rdi+0x10], 51
       je       SHORT G_M000_IG228
 
G_M000_IG227:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       mov      dword ptr [rsi+4*rdi+0x10], 51
 
G_M000_IG228:
       cmp      dword ptr [rbp+0x9C], 0
       je       G_M000_IG242
 
G_M000_IG229:
       xor      esi, esi
       mov      dword ptr [rbp-0x208], esi
 
G_M000_IG230:
       mov      qword ptr [rbp-0x210], rsi
       mov      r8, gword ptr [rbp+0x338]
       test     r8, r8
       je       G_M000_IG160
 
G_M000_IG231:
       test     rax, rax
       je       G_M000_IG241
 
G_M000_IG232:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG240
 
G_M000_IG233:
       mov      gword ptr [rbp-0x248], rax
       mov      rsi, qword ptr [rax+0x08]
       lea      rdx, [rbp-0x208]
       mov      gword ptr [rbp+0x338], r8
       mov      rdi, r8
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryNumericPosition(long,byref):bool:this]
       test     eax, eax
       je       G_M000_IG239
 
G_M000_IG234:
       lea      rdx, [rbp-0x210]
       mov      rdi, gword ptr [rbp+0x338]
       mov      esi, dword ptr [rbp-0x208]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryReadF64(int,byref):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG236
 
G_M000_IG235:
       mov      rax, gword ptr [rbp-0x248]
       jmp      SHORT G_M000_IG242
 
G_M000_IG236:
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       jne      SHORT G_M000_IG238
 
G_M000_IG237:
       mov      rax, gword ptr [rbp-0x248]
       jmp      SHORT G_M000_IG242
 
G_M000_IG238:
       mov      rdi, gword ptr [rbp+0x338]
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm1, xmm0
       mov      ecx, dword ptr [rbp+0x9C]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       jmp      G_M000_IG250
 
G_M000_IG239:
       mov      rax, gword ptr [rbp-0x248]
       jmp      SHORT G_M000_IG242
 
G_M000_IG240:
       mov      gword ptr [rbp+0x338], r8
       jmp      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      gword ptr [rbp+0x338], r8
 
G_M000_IG242:
       mov      rdi, gword ptr [rbp+0x338]
       mov      gword ptr [rbp-0x248], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x138], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x130], rax
       mov      ecx, dword ptr [rbp+0x9C]
       test     ecx, ecx
       je       G_M000_IG259
 
G_M000_IG243:
       mov      rdx, gword ptr [rbp+0x138]
       test     rdx, rdx
       je       G_M000_IG259
 
G_M000_IG244:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG246
 
G_M000_IG245:
       jmp      G_M000_IG259
 
G_M000_IG246:
       test     rax, rax
       je       G_M000_IG258
 
G_M000_IG247:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG249
 
G_M000_IG248:
       jmp      G_M000_IG259
 
G_M000_IG249:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
 
G_M000_IG250:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      rdi, rdi
       xor      rsi, rsi
       mov      dword ptr [rbp+0x9C], ecx
       mov      rcx, rsi
       jmp      G_M000_IG221
 
G_M000_IG251:
       mov      r12d, dword ptr [(reloc)]
       test     r12d, r12d
       jle      SHORT G_M000_IG254
 
G_M000_IG252:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       mov      rsi, rdi
       dec      r12d
       mov      eax, r12d
       cmp      eax, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rsi+4*rax+0x10], 53
       je       SHORT G_M000_IG254
 
G_M000_IG253:
       mov      rsi, rdi
       mov      edi, r12d
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG254:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG257
 
G_M000_IG255:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG257
 
G_M000_IG256:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG257:
       add      rsp, 0x5F8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG258:
 
G_M000_IG259:
       mov      rsi, gword ptr [rbp+0x310]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       SHORT G_M000_IG261
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x310], rsi
       mov      rax, gword ptr [rbp+0x130]
       mov      rsi, gword ptr [rbp+0x310]
 
G_M000_IG261:
       mov      rdx, gword ptr [rbp+0x138]
       test     rdx, rdx
       je       G_M000_IG212
 
G_M000_IG262:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG161
 
G_M000_IG263:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG180
 
G_M000_IG264:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG186
 
G_M000_IG265:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG213
 
G_M000_IG266:
       test     rsi, rsi
       jl       G_M000_IG312
       mov      rdi, rsi
       mov      r8, 0xD1FFAB1E
       mov      r9, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG311
       test     rsi, rsi
       jl       G_M000_IG312
       mov      rsi, gword ptr [r9+8*rsi+0x10]
       mov      r8, rsi
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, r8
       jmp      G_M000_IG225
 
G_M000_IG267:
       xor      eax, eax
       mov      dword ptr [rbp+0x9C], eax
       jmp      G_M000_IG221
 
G_M000_IG268:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r12, gword ptr [rbp+0x310]
       test     r12, r12
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0x9C], edi
       jne      G_M000_IG159
 
G_M000_IG269:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, rdi
       mov      gword ptr [rbp-0x2A0], rax
       mov      rcx, gword ptr [rax+0x70]
       test     rcx, rcx
       je       SHORT G_M000_IG271
 
G_M000_IG270:
       cmp      dword ptr [rcx+0x08], 1
       mov      gword ptr [rbp-0x278], rcx
       je       SHORT G_M000_IG272
 
G_M000_IG271:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x2A0]
       lea      rdi, bword ptr [rdi+0x70]
       mov      gword ptr [rbp-0x278], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG272:
       mov      rdi, gword ptr [rbp-0x278]
       mov      rdx, r12
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x278]
       mov      rsi, r15
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG273:
       add      r13, 1
       jo       G_M000_IG312
 
G_M000_IG274:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG251
 
G_M000_IG275:
       mov      r12d, dword ptr [(reloc)]
       test     r12d, r12d
       jle      SHORT G_M000_IG278
 
G_M000_IG276:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       lea      eax, [r12-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rsi+4*rcx+0x10], 49
       je       SHORT G_M000_IG278
 
G_M000_IG277:
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       mov      esi, eax
       mov      dword ptr [rdi+4*rsi+0x10], 49
 
G_M000_IG278:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x310], rax
       test     r12d, r12d
       jle      SHORT G_M000_IG281
 
G_M000_IG279:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      r12d
       mov      ecx, r12d
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG311
       cmp      dword ptr [rsi+4*rcx+0x10], 50
       je       SHORT G_M000_IG281
 
G_M000_IG280:
       mov      esi, r12d
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG311
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG281:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x238], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x338]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x238]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x190], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, gword ptr [rbp+0x190]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x170], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       je       G_M000_IG287
 
G_M000_IG282:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      SHORT G_M000_IG286
 
G_M000_IG283:
       test     rcx, rcx
       setne    dl
       movzx    rdx, dl
       mov      dword ptr [rbp+0x9C], edx
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       SHORT G_M000_IG285
 
G_M000_IG284:
       mov      gword ptr [rbp+0x310], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x310]
 
G_M000_IG285:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rcx, gword ptr [rbp+0x310]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       jmp      G_M000_IG222
 
G_M000_IG286:
       xor      edx, edx
       mov      dword ptr [rbp+0x9C], edx
       jmp      G_M000_IG222
 
G_M000_IG287:
       xor      edx, edx
       mov      dword ptr [rbp+0x9C], edx
       jmp      G_M000_IG222
 
G_M000_IG288:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG105
 
G_M000_IG289:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG290
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG291
 
G_M000_IG290:
       mov      ecx, eax
       jmp      SHORT G_M000_IG297
 
G_M000_IG291:
       cmp      edi, 2
       je       SHORT G_M000_IG292
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG311
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG293
 
G_M000_IG292:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG293:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG294
       mov      ecx, -1
       jmp      SHORT G_M000_IG296
 
G_M000_IG294:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG295
       mov      ecx, 1
       jmp      SHORT G_M000_IG296
 
G_M000_IG295:
       xor      ecx, ecx
 
G_M000_IG296:
       imul     ecx, eax
 
G_M000_IG297:
       test     ecx, ecx
       jge      G_M000_IG99
       mov      dword ptr [rbp-0x218], eax
       jmp      G_M000_IG110
 
G_M000_IG298:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG299
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG300
 
G_M000_IG299:
       mov      dword ptr [rbp-0x218], eax
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x218]
       jmp      G_M000_IG101
 
G_M000_IG300:
       cmp      edi, 2
       je       SHORT G_M000_IG301
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG311
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG302
 
G_M000_IG301:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG302:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG303
       mov      ecx, -1
       jmp      SHORT G_M000_IG305
 
G_M000_IG303:
       test     rdi, rdi
       jge      SHORT G_M000_IG304
       mov      ecx, 1
       jmp      SHORT G_M000_IG305
 
G_M000_IG304:
       xor      ecx, ecx
 
G_M000_IG305:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG101
 
G_M000_IG306:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG170
 
G_M000_IG307:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG308:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG309:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG310:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG311:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG312:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8298

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x410
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 42 inlinees with PGO data; 76 single block inlinees; 106 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 704
       mov      qword ptr [rsp+0x638], r15
       mov      qword ptr [rsp+0x630], r14
       mov      qword ptr [rsp+0x628], r13
       mov      qword ptr [rsp+0x620], r12
       mov      qword ptr [rsp+0x618], rbx
       lea      rbp, [rsp+0x2C0]
       xor      eax, eax
       mov      qword ptr [rbp-0x218], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -480
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x350]
       mov      r15, gword ptr [rbp+0x348]
       mov      r14, gword ptr [rbp+0x340]
       mov      r13, gword ptr [rbp+0x328]
       mov      r12, qword ptr [rbp+0x270]
       mov      ecx, dword ptr [rbp+0xF4]
 
G_M000_IG02:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       jne      G_M000_IG46
 
G_M000_IG03:
       mov      rax, gword ptr [rbp+0x230]
       test     rax, rax
       je       G_M000_IG13
 
G_M000_IG04:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG47
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG13
 
G_M000_IG06:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       mov      rdx, gword ptr [rbp+0x228]
       test     rdx, rdx
       je       SHORT G_M000_IG12
 
G_M000_IG07:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG09
 
G_M000_IG08:
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG14
 
G_M000_IG09:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG11
 
G_M000_IG10:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG14
 
G_M000_IG11:
       mov      gword ptr [rbp+0x228], rdx
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      gword ptr [rbp+0x228], rdx
 
G_M000_IG13:
       mov      rdx, gword ptr [rbp+0x228]
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG14:
       test     r13, r13
       je       SHORT G_M000_IG18
 
G_M000_IG15:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG73
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG84
 
G_M000_IG17:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       je       G_M000_IG89
 
G_M000_IG18:
       mov      rdx, rax
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG19:
       test     r13, r13
       jne      G_M000_IG99
 
G_M000_IG20:
       xor      eax, eax
       mov      dword ptr [rbp+0xF4], eax
 
G_M000_IG21:
       mov      rcx, qword ptr [rbp+0x260]
       add      r12, rcx
       jo       G_M000_IG313
 
G_M000_IG22:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0x268]
       cmp      r12, rdx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r12, rdx
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG102
 
G_M000_IG23:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG120
 
G_M000_IG24:
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG25:
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG28
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8, rdi
       dec      esi
       mov      r9d, esi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG312
       cmp      dword ptr [r8+4*r9+0x10], 44
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       mov      esi, esi
       mov      dword ptr [rdi+4*rsi+0x10], 44
 
G_M000_IG28:
       cmp      dword ptr [rbp+0xF4], 0
       je       G_M000_IG39
 
G_M000_IG29:
       test     rbx, rbx
       je       G_M000_IG39
 
G_M000_IG30:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG39
 
G_M000_IG31:
       test     rax, rax
       je       SHORT G_M000_IG39
 
G_M000_IG32:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG39
 
G_M000_IG33:
       mov      gword ptr [rbp-0x250], rax
       mov      r9, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x138], r9
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG121
 
G_M000_IG34:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG35:
       mov      rax, qword ptr [rbp-0x138]
       test     rax, rax
       jl       G_M000_IG122
 
G_M000_IG36:
       mov      rcx, rax
 
G_M000_IG37:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       G_M000_IG123
 
G_M000_IG38:
       mov      rax, gword ptr [rbp-0x250]
 
G_M000_IG39:
       mov      rdi, rbx
       mov      gword ptr [rbp-0x250], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x230], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x250]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      r8d, dword ptr [rbp+0xF4]
       test     r8d, r8d
       mov      gword ptr [rbp+0x228], rdx
       mov      ecx, r8d
       je       G_M000_IG02
 
G_M000_IG40:
       mov      rax, gword ptr [rbp+0x230]
       test     rax, rax
       mov      gword ptr [rbp+0x230], rax
       je       G_M000_IG02
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rbp+0x230]
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x230], rax
       mov      qword ptr [rbp-0x238], rdi
       jne      G_M000_IG02
 
G_M000_IG42:
       mov      rdx, gword ptr [rbp+0x228]
       test     rdx, rdx
       mov      gword ptr [rbp+0x228], rdx
       je       G_M000_IG02
 
G_M000_IG43:
       mov      rdx, gword ptr [rbp+0x228]
       mov      rdi, qword ptr [rbp-0x238]
       cmp      qword ptr [rdx], rdi
       mov      gword ptr [rbp+0x228], rdx
       jne      G_M000_IG02
 
G_M000_IG44:
       mov      rax, gword ptr [rbp+0x230]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      rdx, gword ptr [rbp+0x228]
       vmovsd   xmm2, qword ptr [rdx+0x08]
 
G_M000_IG45:
       vmulsd   xmm1, xmm1, xmm2
       vaddsd   xmm0, xmm1, qword ptr [rbp+0xF8]
       xor      r13, r13
       mov      dword ptr [rbp+0xF4], ecx
       vmovsd   qword ptr [rbp+0xF8], xmm0
       jmp      G_M000_IG21
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [r13+0x08], xmm0
       jmp      G_M000_IG03
 
G_M000_IG47:
       mov      rcx, qword ptr [rax+0x08]
       mov      rdx, gword ptr [rbp+0x228]
       test     rdx, rdx
       jne      SHORT G_M000_IG49
 
G_M000_IG48:
       mov      gword ptr [rbp+0x228], rdx
       jmp      G_M000_IG13
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG70
 
G_M000_IG50:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x78], rax
       lea      rdi, [rbp-0x98]
       mov      rdx, rcx
       mulx     rdx, r10, rax
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x98]
       mov      qword ptr [rbp-0x90], r10
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, r10
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG66
 
G_M000_IG51:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x88], xmm1
       lea      rdi, [rbp-0x88]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x290], rax
       mov      ecx, dword ptr [rbp-0x80]
       mov      dword ptr [rbp-0x21C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       mov      rsi, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      r9d, dword ptr [rbp-0xA0]
       mov      rcx, gword ptr [rbp-0x290]
       test     rcx, rcx
       jne      G_M000_IG59
 
G_M000_IG52:
       test     rax, rax
       jne      G_M000_IG289
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       movsxd   rsi, dword ptr [rbp-0x21C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r10d, dword ptr [rbp-0xB0]
 
G_M000_IG53:
       test     rax, rax
       jne      G_M000_IG290
 
G_M000_IG54:
       test     rax, rax
       jne      G_M000_IG299
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG64
 
G_M000_IG55:
       mov      edi, -1
 
G_M000_IG56:
       test     edi, edi
       mov      gword ptr [rbp-0x298], rax
       mov      dword ptr [rbp-0x220], r10d
       jg       SHORT G_M000_IG65
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x268], rax
       mov      rdi, gword ptr [rbp-0x298]
       mov      esi, dword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x268]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG58:
       mov      rcx, rdx
       mov      rax, rcx
       jmp      G_M000_IG14
 
G_M000_IG59:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG60:
       test     rax, rax
       je       SHORT G_M000_IG63
 
G_M000_IG61:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG62:
       mov      edx, dword ptr [rbp-0x21C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG53
 
G_M000_IG63:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG62
 
G_M000_IG64:
       xor      edi, edi
       jmp      G_M000_IG56
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x260], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x298]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x260]
       mov      edi, dword ptr [rbp-0x220]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG58
 
G_M000_IG66:
       lea      rax, [r10+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x90]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG68:
       jmp      G_M000_IG58
 
G_M000_IG69:
       test     rax, rax
       jl       G_M000_IG313
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG312
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG68
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG72
 
G_M000_IG71:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rcx
       vmovsd   qword ptr [rbp-0xC0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xC0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG14
 
G_M000_IG72:
       mov      gword ptr [rbp+0x228], rdx
       jmp      G_M000_IG13
 
G_M000_IG73:
       mov      rsi, qword ptr [r13+0x08]
       test     rax, rax
       je       G_M000_IG18
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG82
 
G_M000_IG75:
       mov      r13, qword ptr [rax+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xE8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG78
 
G_M000_IG76:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x2A0], rax
       mov      ecx, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0x224], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x108], xmm0
       lea      rdi, [rbp-0x108]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2A0]
       mov      esi, dword ptr [rbp-0x224]
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG77:
       mov      r13, rax
       jmp      G_M000_IG19
 
G_M000_IG78:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG81
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xE8]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG80:
       jmp      SHORT G_M000_IG77
 
G_M000_IG81:
       test     r13, r13
       jl       G_M000_IG313
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG312
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG80
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG18
 
G_M000_IG83:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x110], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x110]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG19
 
G_M000_IG84:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rax, rax
       je       G_M000_IG18
 
G_M000_IG85:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG87
 
G_M000_IG86:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG19
 
G_M000_IG87:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG18
 
G_M000_IG88:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG19
 
G_M000_IG89:
       mov      rsi, r13
       mov      gword ptr [rbp-0x270], rsi
       test     rax, rax
       je       G_M000_IG18
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG18
 
G_M000_IG91:
       mov      r13, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG97
 
G_M000_IG92:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG96
 
G_M000_IG93:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x11C], eax
       mov      edi, eax
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG152
 
G_M000_IG94:
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x278], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x270]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x11C]
       mov      rax, gword ptr [rbp-0x278]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0x278]
 
G_M000_IG95:
       jmp      G_M000_IG19
 
G_M000_IG96:
       mov      r13, rsi
       jmp      SHORT G_M000_IG95
 
G_M000_IG97:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG307
 
G_M000_IG98:
       jmp      SHORT G_M000_IG95
 
G_M000_IG99:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG101
 
G_M000_IG100:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      dword ptr [rbp+0xF4], eax
       jmp      G_M000_IG21
 
G_M000_IG101:
       xor      eax, eax
       mov      dword ptr [rbp+0xF4], eax
       jmp      G_M000_IG21
 
G_M000_IG102:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0xF4], edi
       jne      G_M000_IG116
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG105
 
G_M000_IG104:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x280], rax
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x280], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG106:
       mov      rdi, gword ptr [rbp-0x280]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x280]
       mov      rsi, gword ptr [rbp+0x338]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG107:
       mov      r12, qword ptr [rbp+0x2D8]
       mov      r13, qword ptr [rbp+0x2C8]
       add      r12, r13
       jo       G_M000_IG313
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x2D0]
       cmp      r12, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2D0], rax
       cmp      r12, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG153
 
G_M000_IG108:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       cmp      r8d, dword ptr [rcx+0x08]
       jae      G_M000_IG312
       cmp      dword ptr [rcx+4*r8+0x10], 42
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG312
       mov      ecx, edx
       mov      dword ptr [rsi+4*rcx+0x10], 42
 
G_M000_IG111:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x328], rsi
       test     edi, edi
       jle      SHORT G_M000_IG114
 
G_M000_IG112:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG312
       cmp      dword ptr [rdx+4*r8+0x10], 43
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG312
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp+0x290], rdx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x240], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x288], rax
       mov      rdi, gword ptr [rbp+0x290]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x280], rax
       mov      rdi, gword ptr [rbp+0x288]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      rdi, qword ptr [rbp+0x280]
       mov      rax, rdi
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x328]
       test     rsi, rsi
       jne      SHORT G_M000_IG117
 
G_M000_IG115:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2C8], r13
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG22
 
G_M000_IG116:
       mov      rdi, gword ptr [rbp+0x338]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       jmp      G_M000_IG107
 
G_M000_IG117:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG119
 
G_M000_IG118:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2C8], r13
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG22
 
G_M000_IG119:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2C8], r13
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG22
 
G_M000_IG120:
       test     rsi, rsi
       jl       G_M000_IG313
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG312
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      r8, rsi
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       mov      rax, r8
       jmp      G_M000_IG25
 
G_M000_IG121:
       mov      esi, dword ptr [rax+0x10]
       jmp      G_M000_IG35
 
G_M000_IG122:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG37
 
G_M000_IG123:
       mov      dword ptr [rbp-0x13C], ecx
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG129
 
G_M000_IG124:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rbp-0x13C]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG311
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       test     rsi, rsi
       je       SHORT G_M000_IG126
 
G_M000_IG125:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG130
 
G_M000_IG126:
       xor      esi, esi
 
G_M000_IG127:
       test     esi, esi
       jne      SHORT G_M000_IG131
 
G_M000_IG128:
       mov      rax, gword ptr [rbp-0x250]
       jmp      G_M000_IG39
 
G_M000_IG129:
       mov      esi, dword ptr [rbp-0x13C]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG311
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       shl      rsi, 4
       mov      rsi, gword ptr [rdi+rsi+0x10]
       test     rsi, rsi
       je       G_M000_IG310
 
G_M000_IG130:
       mov      esi, 1
       jmp      SHORT G_M000_IG127
 
G_M000_IG131:
       test     r15, r15
       je       G_M000_IG151
 
G_M000_IG132:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r15], rsi
       je       SHORT G_M000_IG134
 
G_M000_IG133:
       mov      rax, gword ptr [rbp-0x250]
       jmp      G_M000_IG39
 
G_M000_IG134:
       mov      rax, gword ptr [rbp-0x250]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x128], rcx
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG140
 
G_M000_IG135:
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG136:
       mov      rax, qword ptr [rbp-0x128]
       test     rax, rax
       jl       SHORT G_M000_IG141
 
G_M000_IG137:
       mov      rcx, rax
 
G_M000_IG138:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       SHORT G_M000_IG142
 
G_M000_IG139:
       mov      rax, gword ptr [rbp-0x250]
       jmp      G_M000_IG39
 
G_M000_IG140:
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG136
 
G_M000_IG141:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      SHORT G_M000_IG138
 
G_M000_IG142:
       mov      dword ptr [rbp-0x12C], ecx
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG148
 
G_M000_IG143:
       mov      rsi, gword ptr [r15+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      edi, dword ptr [rbp-0x12C]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG311
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG312
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG145
 
G_M000_IG144:
       mov      rsi, 0xD1FFAB1E
       mov      rax, rsi
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG149
 
G_M000_IG145:
       xor      edi, edi
 
G_M000_IG146:
       test     edi, edi
       je       SHORT G_M000_IG150
 
G_M000_IG147:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x250]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0xD8], xmm0
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x250]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm2, xmm0
       mov      ecx, dword ptr [rbp+0xF4]
       vmovsd   xmm1, qword ptr [rbp+0xD8]
       jmp      G_M000_IG45
 
G_M000_IG148:
       mov      edi, dword ptr [rbp-0x12C]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG311
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG312
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG309
 
G_M000_IG149:
       mov      edi, 1
       jmp      SHORT G_M000_IG146
 
G_M000_IG150:
       mov      rax, gword ptr [rbp-0x250]
       jmp      G_M000_IG39
 
G_M000_IG151:
       mov      rax, gword ptr [rbp-0x250]
       jmp      G_M000_IG39
 
G_M000_IG152:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG153:
       mov      edi, dword ptr [(reloc)]
       mov      ebx, edi
       test     ebx, ebx
       jle      SHORT G_M000_IG156
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       cmp      dword ptr [rdi+4*rcx+0x10], 46
       je       SHORT G_M000_IG156
 
G_M000_IG155:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 46
 
G_M000_IG156:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       test     ebx, ebx
       jle      SHORT G_M000_IG159
 
G_M000_IG157:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       cmp      dword ptr [rdi+4*rcx+0x10], 48
       je       SHORT G_M000_IG159
 
G_M000_IG158:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG312
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 48
 
G_M000_IG159:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rbx, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG275
 
G_M000_IG160:
       mov      rdi, r15
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       jmp      G_M000_IG274
 
G_M000_IG161:
       test     rax, rax
       je       G_M000_IG207
 
G_M000_IG162:
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG164
 
G_M000_IG163:
       jmp      G_M000_IG208
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
 
G_M000_IG165:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      rdi, rdi
       xor      rsi, rsi
       mov      dword ptr [rbp+0x9C], ecx
       mov      rcx, rsi
       jmp      G_M000_IG175
 
G_M000_IG166:
       mov      gword ptr [rbp+0x310], rsi
 
G_M000_IG167:
       mov      rdx, rax
       mov      rsi, gword ptr [rbp+0x138]
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG168:
       mov      rax, gword ptr [rbp+0x310]
       test     rax, rax
       je       SHORT G_M000_IG172
 
G_M000_IG169:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG245
 
G_M000_IG170:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG256
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG261
 
G_M000_IG172:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG173:
       mov      rcx, rax
       test     rcx, rcx
       jne      G_M000_IG264
 
G_M000_IG174:
       xor      eax, eax
       mov      dword ptr [rbp+0x9C], eax
 
G_M000_IG175:
       add      r12, 1
       jo       G_M000_IG313
       vmovsd   xmm0, qword ptr [rbp+0xA0]
 
G_M000_IG176:
       cmp      r12, qword ptr [rbp+0x170]
       jge      G_M000_IG269
 
G_M000_IG177:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG215
 
G_M000_IG178:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG179:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG182
 
G_M000_IG180:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG312
       cmp      dword ptr [rsi+4*rdi+0x10], 51
       je       SHORT G_M000_IG182
 
G_M000_IG181:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG312
       mov      dword ptr [rsi+4*rdi+0x10], 51
 
G_M000_IG182:
       cmp      dword ptr [rbp+0x9C], 0
       je       G_M000_IG196
 
G_M000_IG183:
       xor      esi, esi
       mov      dword ptr [rbp-0x210], esi
 
G_M000_IG184:
       mov      qword ptr [rbp-0x218], rsi
       mov      r8, gword ptr [rbp+0x338]
       test     r8, r8
       je       G_M000_IG216
 
G_M000_IG185:
       test     rax, rax
       je       G_M000_IG195
 
G_M000_IG186:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG194
 
G_M000_IG187:
       mov      gword ptr [rbp-0x258], rax
       mov      rsi, qword ptr [rax+0x08]
       lea      rdx, [rbp-0x210]
       mov      gword ptr [rbp+0x338], r8
       mov      rdi, r8
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryNumericPosition(long,byref):bool:this]
       test     eax, eax
       je       G_M000_IG193
 
G_M000_IG188:
       lea      rdx, [rbp-0x218]
       mov      rdi, gword ptr [rbp+0x338]
       mov      esi, dword ptr [rbp-0x210]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryReadF64(int,byref):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG190
 
G_M000_IG189:
       mov      rax, gword ptr [rbp-0x258]
       jmp      SHORT G_M000_IG196
 
G_M000_IG190:
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x258]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       jne      SHORT G_M000_IG192
 
G_M000_IG191:
       mov      rax, gword ptr [rbp-0x258]
       jmp      SHORT G_M000_IG196
 
G_M000_IG192:
       mov      rdi, gword ptr [rbp+0x338]
       mov      rsi, gword ptr [rbp-0x258]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x258]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm1, xmm0
       mov      ecx, dword ptr [rbp+0x9C]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       jmp      G_M000_IG165
 
G_M000_IG193:
       mov      rax, gword ptr [rbp-0x258]
       jmp      SHORT G_M000_IG196
 
G_M000_IG194:
       mov      gword ptr [rbp+0x338], r8
       jmp      SHORT G_M000_IG196
 
G_M000_IG195:
       mov      gword ptr [rbp+0x338], r8
 
G_M000_IG196:
       mov      rdi, gword ptr [rbp+0x338]
       mov      gword ptr [rbp-0x258], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x138], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x258]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x130], rax
       mov      ecx, dword ptr [rbp+0x9C]
       test     ecx, ecx
       je       G_M000_IG208
 
G_M000_IG197:
       mov      rdx, gword ptr [rbp+0x138]
       test     rdx, rdx
       je       G_M000_IG208
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG161
 
G_M000_IG199:
       jmp      G_M000_IG208
 
G_M000_IG200:
       mov      r12d, dword ptr [(reloc)]
       test     r12d, r12d
       jle      SHORT G_M000_IG203
 
G_M000_IG201:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       mov      rsi, rdi
       dec      r12d
       mov      eax, r12d
       mov      edx, dword ptr [rsi+0x08]
       cmp      eax, edx
       jae      G_M000_IG312
       cmp      dword ptr [rsi+4*rax+0x10], 53
       je       SHORT G_M000_IG203
 
G_M000_IG202:
       mov      rsi, rdi
       mov      edi, r12d
       cmp      edi, edx
       jae      G_M000_IG312
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG203:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG206
 
G_M000_IG204:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG206
 
G_M000_IG205:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG206:
       add      rsp, 0x618
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG207:
 
G_M000_IG208:
       mov      rsi, gword ptr [rbp+0x310]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       SHORT G_M000_IG210
 
G_M000_IG209:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x310], rsi
       mov      rax, gword ptr [rbp+0x130]
       mov      rsi, gword ptr [rbp+0x310]
 
G_M000_IG210:
       mov      rdx, gword ptr [rbp+0x138]
       test     rdx, rdx
       je       G_M000_IG166
 
G_M000_IG211:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG217
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG236
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG242
 
G_M000_IG214:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG167
 
G_M000_IG215:
       test     rsi, rsi
       jl       G_M000_IG313
       mov      rdi, rsi
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG312
       test     rsi, rsi
       jl       G_M000_IG313
       mov      rsi, gword ptr [r8+8*rsi+0x10]
       mov      r8, rsi
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, r8
       jmp      G_M000_IG179
 
G_M000_IG216:
       mov      gword ptr [rbp+0x338], r8
       jmp      G_M000_IG196
 
G_M000_IG217:
       mov      rcx, qword ptr [rdx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG219
 
G_M000_IG218:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG167
 
G_M000_IG219:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG234
 
G_M000_IG220:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x160], rax
       lea      rdi, [rbp-0x180]
       mov      rdx, rcx
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x180]
       mov      qword ptr [rbp-0x178], r8
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG230
 
G_M000_IG221:
       mov      gword ptr [rbp+0x310], rsi
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x170], xmm1
       lea      rdi, [rbp-0x170]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x170]
       mov      gword ptr [rbp-0x2A8], rax
       mov      ecx, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x228], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
       lea      rdi, [rbp-0x190]
       mov      rsi, qword ptr [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x190]
       mov      r9d, dword ptr [rbp-0x188]
       mov      rcx, gword ptr [rbp-0x2A8]
       test     rcx, rcx
       jne      SHORT G_M000_IG225
 
G_M000_IG222:
       test     rax, rax
       jne      G_M000_IG308
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
       movsxd   rsi, dword ptr [rbp-0x228]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1A0]
       mov      esi, dword ptr [rbp-0x198]
 
G_M000_IG223:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG224:
       jmp      G_M000_IG168
 
G_M000_IG225:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG226:
       test     rax, rax
       je       SHORT G_M000_IG229
 
G_M000_IG227:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG228:
       mov      edx, dword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG223
 
G_M000_IG229:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG228
 
G_M000_IG230:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG233
 
G_M000_IG231:
       mov      gword ptr [rbp+0x310], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG232:
       jmp      SHORT G_M000_IG224
 
G_M000_IG233:
       test     rax, rax
       jl       G_M000_IG313
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG312
       test     rax, rax
       jl       G_M000_IG313
       mov      rdi, rax
       mov      rdx, gword ptr [r8+8*rdi+0x10]
       mov      gword ptr [rbp+0x310], rsi
       jmp      SHORT G_M000_IG232
 
G_M000_IG234:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rsi
       jne      G_M000_IG167
 
G_M000_IG235:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rcx
       vmovsd   qword ptr [rbp-0x1A8], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x1A8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG168
 
G_M000_IG236:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       test     rax, rax
       je       G_M000_IG241
 
G_M000_IG237:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG239
 
G_M000_IG238:
       mov      gword ptr [rbp+0x310], rsi
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG168
 
G_M000_IG239:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       mov      gword ptr [rbp+0x310], rsi
       jne      G_M000_IG167
 
G_M000_IG240:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG168
 
G_M000_IG241:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG167
 
G_M000_IG242:
       test     rax, rax
       je       SHORT G_M000_IG244
 
G_M000_IG243:
       cmp      dword ptr [rax], eax
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG167
 
G_M000_IG244:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG167
 
G_M000_IG245:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG172
 
G_M000_IG246:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG254
 
G_M000_IG247:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1D0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1D8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG250
 
G_M000_IG248:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1E8], xmm0
       lea      rdi, [rbp-0x1E8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1E8]
       mov      gword ptr [rbp-0x2B0], rax
       mov      ecx, dword ptr [rbp-0x1E0]
       mov      dword ptr [rbp-0x22C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1F8], xmm0
       lea      rdi, [rbp-0x1F8]
       mov      rsi, qword ptr [rbp-0x1D0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2B0]
       mov      esi, dword ptr [rbp-0x22C]
       mov      rdx, gword ptr [rbp-0x1F8]
       mov      rcx, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG249:
       jmp      G_M000_IG173
 
G_M000_IG250:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG253
 
G_M000_IG251:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1D8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG252:
       jmp      SHORT G_M000_IG249
 
G_M000_IG253:
       test     rax, rax
       jl       G_M000_IG313
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r8, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG312
       test     rax, rax
       jl       G_M000_IG313
       mov      rdi, rax
       mov      rax, gword ptr [r8+8*rdi+0x10]
       jmp      SHORT G_M000_IG252
 
G_M000_IG254:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG172
 
G_M000_IG255:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1C8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x200], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG173
 
G_M000_IG256:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm0
       test     rdx, rdx
       je       G_M000_IG172
 
G_M000_IG257:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG259
 
G_M000_IG258:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1C0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG173
 
G_M000_IG259:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG172
 
G_M000_IG260:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG173
 
G_M000_IG261:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG172
 
G_M000_IG262:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG172
 
G_M000_IG263:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG173
 
G_M000_IG264:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG268
 
G_M000_IG265:
       test     rax, rax
       setne    sil
       movzx    rsi, sil
       mov      dword ptr [rbp+0x9C], esi
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG267
 
G_M000_IG266:
       mov      gword ptr [rbp+0x310], rcx
       mov      rsi, rcx
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x310]
 
G_M000_IG267:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rcx, gword ptr [rbp+0x310]
       jmp      G_M000_IG175
 
G_M000_IG268:
       xor      eax, eax
       mov      dword ptr [rbp+0x9C], eax
       jmp      G_M000_IG175
 
G_M000_IG269:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r12, gword ptr [rbp+0x310]
       test     r12, r12
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0x9C], edi
       jne      G_M000_IG160
 
G_M000_IG270:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, rdi
       mov      gword ptr [rbp-0x2B8], rax
       mov      rcx, gword ptr [rax+0x70]
       test     rcx, rcx
       je       SHORT G_M000_IG272
 
G_M000_IG271:
       cmp      dword ptr [rcx+0x08], 1
       mov      gword ptr [rbp-0x288], rcx
       je       SHORT G_M000_IG273
 
G_M000_IG272:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x2B8]
       lea      rdi, bword ptr [rdi+0x70]
       mov      gword ptr [rbp-0x288], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG273:
       mov      rdi, gword ptr [rbp-0x288]
       mov      rdx, r12
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x288]
       mov      rsi, r15
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG274:
       add      r13, 1
       jo       G_M000_IG313
 
G_M000_IG275:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG200
 
G_M000_IG276:
       mov      r12d, dword ptr [(reloc)]
       test     r12d, r12d
       jle      SHORT G_M000_IG279
 
G_M000_IG277:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       lea      eax, [r12-0x01]
       mov      ecx, eax
       mov      edx, dword ptr [rsi+0x08]
       cmp      ecx, edx
       jae      G_M000_IG312
       cmp      dword ptr [rsi+4*rcx+0x10], 49
       je       SHORT G_M000_IG279
 
G_M000_IG278:
       cmp      eax, edx
       jae      G_M000_IG312
       mov      edx, eax
       mov      dword ptr [rdi+4*rdx+0x10], 49
 
G_M000_IG279:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x310], rax
       test     r12d, r12d
       jle      SHORT G_M000_IG282
 
G_M000_IG280:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      r12d
       mov      ecx, r12d
       mov      edx, dword ptr [rsi+0x08]
       cmp      ecx, edx
       jae      G_M000_IG312
       cmp      dword ptr [rsi+4*rcx+0x10], 50
       je       SHORT G_M000_IG282
 
G_M000_IG281:
       mov      esi, r12d
       cmp      esi, edx
       jae      G_M000_IG312
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG282:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x248], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x338]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x190], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, gword ptr [rbp+0x190]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x170], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       je       SHORT G_M000_IG288
 
G_M000_IG283:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG287
 
G_M000_IG284:
       test     rcx, rcx
       setne    dl
       movzx    rdx, dl
       mov      dword ptr [rbp+0x9C], edx
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG286
 
G_M000_IG285:
       mov      gword ptr [rbp+0x310], rcx
       mov      rsi, rcx
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x310]
 
G_M000_IG286:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rcx, gword ptr [rbp+0x310]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       jmp      G_M000_IG176
 
G_M000_IG287:
       xor      edx, edx
       mov      dword ptr [rbp+0x9C], edx
       jmp      G_M000_IG176
 
G_M000_IG288:
       xor      edx, edx
       mov      dword ptr [rbp+0x9C], edx
       jmp      G_M000_IG176
 
G_M000_IG289:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG60
 
G_M000_IG290:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG291
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG292
 
G_M000_IG291:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG298
 
G_M000_IG292:
       cmp      edi, 2
       je       SHORT G_M000_IG293
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG312
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG294
 
G_M000_IG293:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG294:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG295
       mov      ecx, -1
       jmp      SHORT G_M000_IG297
 
G_M000_IG295:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG296
       mov      ecx, 1
       jmp      SHORT G_M000_IG297
 
G_M000_IG296:
       xor      ecx, ecx
 
G_M000_IG297:
       imul     ecx, r10d
 
G_M000_IG298:
       test     ecx, ecx
       jge      G_M000_IG54
       mov      gword ptr [rbp-0x298], rax
       mov      dword ptr [rbp-0x220], r10d
       jmp      G_M000_IG65
 
G_M000_IG299:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG300
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG301
 
G_M000_IG300:
       mov      dword ptr [rbp-0x220], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x220]
       jmp      G_M000_IG56
 
G_M000_IG301:
       cmp      edi, 2
       je       SHORT G_M000_IG302
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG312
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG303
 
G_M000_IG302:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x298], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x298]
 
G_M000_IG303:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG304
       mov      ecx, -1
       jmp      SHORT G_M000_IG306
 
G_M000_IG304:
       test     rdi, rdi
       jge      SHORT G_M000_IG305
       mov      ecx, 1
       jmp      SHORT G_M000_IG306
 
G_M000_IG305:
       xor      ecx, ecx
 
G_M000_IG306:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG56
 
G_M000_IG307:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG95
 
G_M000_IG308:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG226
 
G_M000_IG309:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG310:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG311:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG312:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG313:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8046

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4e1
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 42 inlinees with PGO data; 76 single block inlinees; 106 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 688
       mov      qword ptr [rsp+0x628], r15
       mov      qword ptr [rsp+0x620], r14
       mov      qword ptr [rsp+0x618], r13
       mov      qword ptr [rsp+0x610], r12
       mov      qword ptr [rsp+0x608], rbx
       lea      rbp, [rsp+0x2B0]
       xor      eax, eax
       mov      qword ptr [rbp-0x218], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -480
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x350]
       mov      r15, gword ptr [rbp+0x348]
       mov      r14, gword ptr [rbp+0x340]
       mov      r13, gword ptr [rbp+0x328]
       mov      rcx, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x270]
 
G_M000_IG02:
       jmp      G_M000_IG19
 
G_M000_IG03:
       mov      rdx, rax
       mov      rsi, gword ptr [rbp+0x230]
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG04:
       test     r13, r13
       je       SHORT G_M000_IG08
 
G_M000_IG05:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG108
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG119
 
G_M000_IG07:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       je       G_M000_IG124
 
G_M000_IG08:
       mov      rdx, rax
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG09:
       test     r13, r13
       jne      G_M000_IG133
 
G_M000_IG10:
       xor      eax, eax
       mov      dword ptr [rbp+0xF4], eax
 
G_M000_IG11:
       mov      rcx, qword ptr [rbp+0x260]
       add      r12, rcx
       jo       G_M000_IG318
 
G_M000_IG12:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0x268]
       cmp      r12, rdx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r12, rdx
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG136
 
G_M000_IG13:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG154
 
G_M000_IG14:
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      rsi, rax
 
G_M000_IG15:
       mov      rdi, rsi
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG18
 
G_M000_IG16:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      esi
       mov      r10d, esi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG317
       cmp      dword ptr [r9+4*r10+0x10], 44
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       cmp      esi, dword ptr [r8+0x08]
       jae      G_M000_IG317
       mov      esi, esi
       mov      dword ptr [r8+4*rsi+0x10], 44
 
G_M000_IG18:
       mov      eax, dword ptr [rbp+0xF4]
       test     eax, eax
       mov      dword ptr [rbp+0xF4], eax
       mov      rcx, rdi
       je       G_M000_IG29
 
G_M000_IG19:
       test     rbx, rbx
       je       G_M000_IG29
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG29
 
G_M000_IG21:
       test     rcx, rcx
       je       SHORT G_M000_IG29
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG29
 
G_M000_IG23:
       mov      gword ptr [rbp+0x320], rcx
       mov      rdi, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x138], rdi
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG36
 
G_M000_IG24:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG25:
       mov      rax, qword ptr [rbp-0x138]
       test     rax, rax
       jl       G_M000_IG37
 
G_M000_IG26:
       mov      rcx, rax
 
G_M000_IG27:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       G_M000_IG38
 
G_M000_IG28:
       mov      rcx, gword ptr [rbp+0x320]
 
G_M000_IG29:
       mov      rdi, rbx
       mov      gword ptr [rbp+0x320], rcx
       mov      rsi, rcx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x230], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x228], rax
       mov      ecx, dword ptr [rbp+0xF4]
       test     ecx, ecx
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       mov      rdx, gword ptr [rbp+0x230]
       test     rdx, rdx
       jne      G_M000_IG69
 
G_M000_IG31:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       jne      G_M000_IG77
 
G_M000_IG32:
       mov      rdx, gword ptr [rbp+0x230]
       test     rdx, rdx
       je       G_M000_IG03
 
G_M000_IG33:
       mov      rsi, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG78
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG103
 
G_M000_IG35:
       jmp      G_M000_IG03
 
G_M000_IG36:
       mov      esi, dword ptr [rax+0x10]
       jmp      G_M000_IG25
 
G_M000_IG37:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG27
 
G_M000_IG38:
       mov      dword ptr [rbp-0x13C], ecx
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG44
 
G_M000_IG39:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      esi, dword ptr [rbp-0x13C]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG316
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG317
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       test     rsi, rsi
       je       SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG45
 
G_M000_IG41:
       xor      esi, esi
 
G_M000_IG42:
       test     esi, esi
       jne      SHORT G_M000_IG46
 
G_M000_IG43:
       mov      rcx, gword ptr [rbp+0x320]
       jmp      G_M000_IG29
 
G_M000_IG44:
       mov      esi, dword ptr [rbp-0x13C]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG316
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG317
       shl      rsi, 4
       mov      rsi, gword ptr [rdi+rsi+0x10]
       test     rsi, rsi
       je       G_M000_IG315
 
G_M000_IG45:
       mov      esi, 1
       jmp      SHORT G_M000_IG42
 
G_M000_IG46:
       test     r15, r15
       je       G_M000_IG68
 
G_M000_IG47:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r15], rsi
       je       SHORT G_M000_IG49
 
G_M000_IG48:
       mov      rcx, gword ptr [rbp+0x320]
       jmp      G_M000_IG29
 
G_M000_IG49:
       mov      rax, gword ptr [rbp+0x320]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x128], rcx
       mov      rdx, gword ptr [r15+0x08]
       mov      gword ptr [rbp-0x2A8], rdx
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG57
 
G_M000_IG50:
       mov      rcx, gword ptr [rbp-0x2A8]
       mov      rax, rcx
       test     rax, rax
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG233
 
G_M000_IG52:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG53:
       mov      rax, qword ptr [rbp-0x128]
       test     rax, rax
       jl       SHORT G_M000_IG58
 
G_M000_IG54:
       mov      rdx, rax
 
G_M000_IG55:
       movsxd   rsi, esi
       cmp      rdx, rsi
       jb       SHORT G_M000_IG59
 
G_M000_IG56:
       mov      rcx, gword ptr [rbp+0x320]
       jmp      G_M000_IG29
 
G_M000_IG57:
       mov      rcx, gword ptr [rbp-0x2A8]
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG53
 
G_M000_IG58:
       movsxd   rdx, esi
       add      rdx, rax
       mov      rax, rdx
       jmp      SHORT G_M000_IG55
 
G_M000_IG59:
       mov      dword ptr [rbp-0x12C], edx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG234
 
G_M000_IG60:
       mov      rsi, gword ptr [rbp-0x2A8]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG62
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG235
 
G_M000_IG62:
       mov      edi, dword ptr [rbp-0x12C]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG316
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG317
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG64
 
G_M000_IG63:
       mov      rsi, 0xD1FFAB1E
       mov      rax, rsi
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG67
 
G_M000_IG64:
       xor      edi, edi
 
G_M000_IG65:
       test     edi, edi
       je       G_M000_IG236
 
G_M000_IG66:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0xD8], xmm0
       mov      rdi, r15
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm1, xmm0
       mov      ecx, dword ptr [rbp+0xF4]
       vmovsd   xmm0, qword ptr [rbp+0xD8]
       jmp      SHORT G_M000_IG75
 
G_M000_IG67:
       mov      edi, 1
       jmp      SHORT G_M000_IG65
 
G_M000_IG68:
       mov      rcx, gword ptr [rbp+0x320]
       jmp      G_M000_IG29
 
G_M000_IG69:
       mov      rsi, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       SHORT G_M000_IG71
 
G_M000_IG70:
       jmp      G_M000_IG31
 
G_M000_IG71:
       test     rax, rax
       je       SHORT G_M000_IG76
 
G_M000_IG72:
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       jmp      G_M000_IG31
 
G_M000_IG74:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
 
G_M000_IG75:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       xor      r13, r13
       mov      dword ptr [rbp+0xF4], ecx
       jmp      G_M000_IG11
 
G_M000_IG76:
       jmp      G_M000_IG31
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [r13+0x08], xmm0
       mov      rax, gword ptr [rbp+0x228]
       jmp      G_M000_IG32
 
G_M000_IG78:
       mov      rsi, qword ptr [rdx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG80
 
G_M000_IG79:
       jmp      G_M000_IG03
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG101
 
G_M000_IG81:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x78], rax
       lea      rdi, [rbp-0x98]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x98]
       mov      qword ptr [rbp-0x90], rcx
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, rcx
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG97
 
G_M000_IG82:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x88], xmm1
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x280], rax
       mov      ecx, dword ptr [rbp-0x80]
       mov      dword ptr [rbp-0x21C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       mov      rsi, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      r9d, dword ptr [rbp-0xA0]
       mov      rcx, gword ptr [rbp-0x280]
       test     rcx, rcx
       jne      G_M000_IG90
 
G_M000_IG83:
       test     rax, rax
       jne      G_M000_IG214
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       movsxd   rsi, dword ptr [rbp-0x21C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r10d, dword ptr [rbp-0xB0]
 
G_M000_IG84:
       test     rax, rax
       jne      G_M000_IG215
 
G_M000_IG85:
       test     rax, rax
       jne      G_M000_IG224
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG95
 
G_M000_IG86:
       mov      edi, -1
 
G_M000_IG87:
       test     edi, edi
       mov      gword ptr [rbp-0x288], rax
       mov      dword ptr [rbp-0x220], r10d
       jg       SHORT G_M000_IG96
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x258], rax
       mov      rdi, gword ptr [rbp-0x288]
       mov      esi, dword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x258]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG89:
       mov      rcx, rdx
       mov      rax, rcx
       jmp      G_M000_IG04
 
G_M000_IG90:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG91:
       test     rax, rax
       je       SHORT G_M000_IG94
 
G_M000_IG92:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG93:
       mov      edx, dword ptr [rbp-0x21C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG84
 
G_M000_IG94:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG93
 
G_M000_IG95:
       xor      edi, edi
       jmp      G_M000_IG87
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x250], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x288]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x250]
       mov      edi, dword ptr [rbp-0x220]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG89
 
G_M000_IG97:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG100
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x90]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG99:
       jmp      G_M000_IG89
 
G_M000_IG100:
       test     rax, rax
       jl       G_M000_IG318
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG317
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG99
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG03
 
G_M000_IG102:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0xC0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xC0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG04
 
G_M000_IG103:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG104:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG106
 
G_M000_IG105:
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG04
 
G_M000_IG106:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG03
 
G_M000_IG107:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG04
 
G_M000_IG108:
       mov      rsi, qword ptr [r13+0x08]
       test     rax, rax
       je       G_M000_IG08
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG117
 
G_M000_IG110:
       mov      r13, qword ptr [rax+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xE8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG113
 
G_M000_IG111:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x290], rax
       mov      ecx, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0x224], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x108], xmm0
       lea      rdi, [rbp-0x108]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x290]
       mov      esi, dword ptr [rbp-0x224]
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG112:
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG113:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG116
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xE8]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG115:
       jmp      SHORT G_M000_IG112
 
G_M000_IG116:
       test     r13, r13
       jl       G_M000_IG318
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG317
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG115
 
G_M000_IG117:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG08
 
G_M000_IG118:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x110], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x110]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG119:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rax, rax
       je       G_M000_IG08
 
G_M000_IG120:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG122
 
G_M000_IG121:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG122:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG08
 
G_M000_IG123:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG124:
       mov      rsi, r13
       mov      gword ptr [rbp-0x260], rsi
       test     rax, rax
       je       G_M000_IG08
 
G_M000_IG125:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG08
 
G_M000_IG126:
       mov      r13, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG131
 
G_M000_IG127:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG130
 
G_M000_IG128:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x11C], eax
       mov      edi, eax
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG313
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x268], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x260]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x11C]
       mov      rax, gword ptr [rbp-0x268]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0x268]
 
G_M000_IG129:
       jmp      G_M000_IG09
 
G_M000_IG130:
       mov      r13, rsi
       jmp      SHORT G_M000_IG129
 
G_M000_IG131:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG232
 
G_M000_IG132:
       jmp      SHORT G_M000_IG129
 
G_M000_IG133:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG135
 
G_M000_IG134:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      dword ptr [rbp+0xF4], eax
       jmp      G_M000_IG11
 
G_M000_IG135:
       xor      eax, eax
       mov      dword ptr [rbp+0xF4], eax
       jmp      G_M000_IG11
 
G_M000_IG136:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0xF4], edi
       jne      G_M000_IG150
 
G_M000_IG137:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG139
 
G_M000_IG138:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x270], rax
       je       SHORT G_M000_IG140
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x270], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG140:
       mov      rdi, gword ptr [rbp-0x270]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x270]
       mov      rsi, gword ptr [rbp+0x338]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG141:
       mov      r12, qword ptr [rbp+0x2D8]
       mov      r13, qword ptr [rbp+0x2C8]
       add      r12, r13
       jo       G_M000_IG318
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x2D0]
       cmp      r12, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2D0], rax
       cmp      r12, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG237
 
G_M000_IG142:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG145
 
G_M000_IG143:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       mov      r9d, dword ptr [rcx+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG317
       mov      r9d, r8d
       cmp      dword ptr [rcx+4*r9+0x10], 42
       je       SHORT G_M000_IG145
 
G_M000_IG144:
       mov      ecx, edx
       mov      dword ptr [rsi+4*rcx+0x10], 42
 
G_M000_IG145:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x328], rsi
       test     edi, edi
       jle      SHORT G_M000_IG148
 
G_M000_IG146:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       dec      edi
       mov      r8d, edi
       mov      r9d, dword ptr [rdx+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG317
       cmp      dword ptr [rdx+4*r8+0x10], 43
       je       SHORT G_M000_IG148
 
G_M000_IG147:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG148:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp+0x290], rdx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x238], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x238]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x288], rax
       mov      rdi, gword ptr [rbp+0x290]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x280], rax
       mov      rdi, gword ptr [rbp+0x288]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      rdi, qword ptr [rbp+0x280]
       mov      rax, rdi
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x328]
       test     rsi, rsi
       jne      SHORT G_M000_IG151
 
G_M000_IG149:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2C8], r13
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG12
 
G_M000_IG150:
       mov      rdi, gword ptr [rbp+0x338]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       jmp      G_M000_IG141
 
G_M000_IG151:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG153
 
G_M000_IG152:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2C8], r13
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG12
 
G_M000_IG153:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2C8], r13
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG12
 
G_M000_IG154:
       test     rsi, rsi
       jl       G_M000_IG318
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG317
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       jmp      G_M000_IG15
 
G_M000_IG155:
       add      r13, 1
       jo       G_M000_IG318
 
G_M000_IG156:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG306
 
G_M000_IG157:
       mov      r12d, dword ptr [(reloc)]
       test     r12d, r12d
       jle      SHORT G_M000_IG160
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       lea      eax, [r12-0x01]
       mov      ecx, eax
       mov      edx, dword ptr [rsi+0x08]
       cmp      ecx, edx
       jae      G_M000_IG317
       cmp      dword ptr [rsi+4*rcx+0x10], 49
       je       SHORT G_M000_IG160
 
G_M000_IG159:
       cmp      eax, edx
       jae      G_M000_IG317
       mov      dword ptr [rdi+4*rcx+0x10], 49
 
G_M000_IG160:
       mov      rdx, 0xD1FFAB1E
       mov      rax, gword ptr [rdx]
       mov      gword ptr [rbp+0x310], rax
       test     r12d, r12d
       jle      SHORT G_M000_IG163
 
G_M000_IG161:
       mov      rcx, 0xD1FFAB1E
       mov      rdi, gword ptr [rcx]
       mov      rsi, rdi
       dec      r12d
       mov      ecx, r12d
       mov      edx, dword ptr [rsi+0x08]
       cmp      ecx, edx
       jae      G_M000_IG317
       cmp      dword ptr [rsi+4*rcx+0x10], 50
       je       SHORT G_M000_IG163
 
G_M000_IG162:
       mov      esi, r12d
       cmp      esi, edx
       jae      G_M000_IG317
       mov      dword ptr [rdi+4*rcx+0x10], 50
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x240], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x338]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x190], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, gword ptr [rbp+0x190]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x170], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       jne      G_M000_IG245
       jmp      G_M000_IG244
 
G_M000_IG164:
       test     rsi, rsi
       jl       G_M000_IG318
       mov      rdi, rsi
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG317
       test     rsi, rsi
       jl       G_M000_IG318
       mov      rsi, gword ptr [r8+8*rsi+0x10]
       mov      r8, rsi
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, r8
       jmp      G_M000_IG198
 
G_M000_IG165:
       xor      esi, esi
       mov      dword ptr [rbp-0x210], esi
 
G_M000_IG166:
       mov      qword ptr [rbp-0x218], rsi
       mov      r8, gword ptr [rbp+0x338]
       test     r8, r8
       je       SHORT G_M000_IG172
 
G_M000_IG167:
       test     rax, rax
       je       SHORT G_M000_IG171
 
G_M000_IG168:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG170
 
G_M000_IG169:
       mov      gword ptr [rbp-0x248], rax
       mov      rsi, qword ptr [rax+0x08]
       lea      rdx, [rbp-0x210]
       mov      gword ptr [rbp+0x338], r8
       mov      rdi, r8
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryNumericPosition(long,byref):bool:this]
       test     eax, eax
       je       G_M000_IG298
       jmp      G_M000_IG296
 
G_M000_IG170:
       mov      gword ptr [rbp+0x338], r8
       jmp      G_M000_IG202
 
G_M000_IG171:
       mov      gword ptr [rbp+0x338], r8
       jmp      G_M000_IG202
 
G_M000_IG172:
       mov      gword ptr [rbp+0x338], r8
       jmp      G_M000_IG202
 
G_M000_IG173:
       mov      gword ptr [rbp+0x310], rsi
 
G_M000_IG174:
       mov      rdx, rax
       mov      rsi, gword ptr [rbp+0x138]
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG175:
       mov      rax, gword ptr [rbp+0x310]
       test     rax, rax
       je       G_M000_IG186
 
G_M000_IG176:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG183
 
G_M000_IG177:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG185
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG285
       jmp      SHORT G_M000_IG186
 
G_M000_IG179:
       mov      rcx, qword ptr [rdx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG181
 
G_M000_IG180:
       mov      gword ptr [rbp+0x310], rsi
       jmp      SHORT G_M000_IG174
 
G_M000_IG181:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG249
       jmp      G_M000_IG248
 
G_M000_IG182:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       test     rax, rax
       jne      G_M000_IG266
       jmp      G_M000_IG265
 
G_M000_IG183:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       SHORT G_M000_IG186
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG274
       jmp      G_M000_IG273
 
G_M000_IG185:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm0
       test     rdx, rdx
       jne      G_M000_IG281
 
G_M000_IG186:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG187:
       mov      rcx, rax
       test     rcx, rcx
       je       SHORT G_M000_IG193
 
G_M000_IG188:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG192
 
G_M000_IG189:
       test     rax, rax
       setne    sil
       movzx    rsi, sil
       mov      dword ptr [rbp+0x9C], esi
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG191
 
G_M000_IG190:
       mov      gword ptr [rbp+0x310], rcx
       mov      rsi, rcx
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x310]
 
G_M000_IG191:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rcx, gword ptr [rbp+0x310]
       jmp      SHORT G_M000_IG194
 
G_M000_IG192:
       xor      eax, eax
       mov      dword ptr [rbp+0x9C], eax
       jmp      SHORT G_M000_IG194
 
G_M000_IG193:
       xor      eax, eax
       mov      dword ptr [rbp+0x9C], eax
 
G_M000_IG194:
       add      r12, 1
       jo       G_M000_IG318
       vmovsd   xmm0, qword ptr [rbp+0xA0]
 
G_M000_IG195:
       cmp      r12, qword ptr [rbp+0x170]
       jge      G_M000_IG213
 
G_M000_IG196:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG164
 
G_M000_IG197:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG198:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG201
 
G_M000_IG199:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG317
       cmp      dword ptr [rsi+4*rdi+0x10], 51
       je       SHORT G_M000_IG201
 
G_M000_IG200:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG317
       mov      dword ptr [rsi+4*rdi+0x10], 51
 
G_M000_IG201:
       cmp      dword ptr [rbp+0x9C], 0
       jne      G_M000_IG165
 
G_M000_IG202:
       mov      rdi, gword ptr [rbp+0x338]
       mov      gword ptr [rbp-0x248], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x138], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x130], rax
       mov      ecx, dword ptr [rbp+0x9C]
       test     ecx, ecx
       je       SHORT G_M000_IG207
 
G_M000_IG203:
       mov      rdx, gword ptr [rbp+0x138]
       test     rdx, rdx
       je       SHORT G_M000_IG207
 
G_M000_IG204:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG206
 
G_M000_IG205:
       jmp      SHORT G_M000_IG207
 
G_M000_IG206:
       test     rax, rax
       jne      G_M000_IG289
       jmp      G_M000_IG292
 
G_M000_IG207:
       mov      rsi, gword ptr [rbp+0x310]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       SHORT G_M000_IG209
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x310], rsi
       mov      rax, gword ptr [rbp+0x130]
       mov      rsi, gword ptr [rbp+0x310]
 
G_M000_IG209:
       mov      rdx, gword ptr [rbp+0x138]
       test     rdx, rdx
       je       G_M000_IG173
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG179
 
G_M000_IG211:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG182
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG270
       jmp      G_M000_IG264
 
G_M000_IG213:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r12, gword ptr [rbp+0x310]
       test     r12, r12
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0x9C], edi
       jne      G_M000_IG305
       jmp      G_M000_IG299
 
G_M000_IG214:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG91
 
G_M000_IG215:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG216
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG217
 
G_M000_IG216:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG223
 
G_M000_IG217:
       cmp      edi, 2
       je       SHORT G_M000_IG218
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG317
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG219
 
G_M000_IG218:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG219:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG220
       mov      ecx, -1
       jmp      SHORT G_M000_IG222
 
G_M000_IG220:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG221
       mov      ecx, 1
       jmp      SHORT G_M000_IG222
 
G_M000_IG221:
       xor      ecx, ecx
 
G_M000_IG222:
       imul     ecx, r10d
 
G_M000_IG223:
       test     ecx, ecx
       jge      G_M000_IG85
       mov      gword ptr [rbp-0x288], rax
       mov      dword ptr [rbp-0x220], r10d
       jmp      G_M000_IG96
 
G_M000_IG224:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG225
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG226
 
G_M000_IG225:
       mov      dword ptr [rbp-0x220], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x220]
       jmp      G_M000_IG87
 
G_M000_IG226:
       cmp      edi, 2
       je       SHORT G_M000_IG227
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG317
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG228
 
G_M000_IG227:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x288], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x288]
 
G_M000_IG228:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG229
       mov      ecx, -1
       jmp      SHORT G_M000_IG231
 
G_M000_IG229:
       test     rdi, rdi
       jge      SHORT G_M000_IG230
       mov      ecx, 1
       jmp      SHORT G_M000_IG231
 
G_M000_IG230:
       xor      ecx, ecx
 
G_M000_IG231:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG87
 
G_M000_IG232:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG129
 
G_M000_IG233:
       mov      rsi, rcx
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
       mov      rcx, gword ptr [rbp-0x2A8]
       jmp      G_M000_IG52
 
G_M000_IG234:
       mov      edi, dword ptr [rbp-0x12C]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG316
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG317
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       jne      G_M000_IG67
       jmp      G_M000_IG314
 
G_M000_IG235:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
       jmp      G_M000_IG62
 
G_M000_IG236:
       mov      rcx, gword ptr [rbp+0x320]
       jmp      G_M000_IG29
 
G_M000_IG237:
       mov      edi, dword ptr [(reloc)]
       mov      ebx, edi
       test     ebx, ebx
       jle      SHORT G_M000_IG240
 
G_M000_IG238:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       mov      r9d, dword ptr [rdi+0x08]
       cmp      ecx, r9d
       jae      G_M000_IG317
       cmp      dword ptr [rdi+4*rcx+0x10], 46
       je       SHORT G_M000_IG240
 
G_M000_IG239:
       mov      rdi, rsi
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 46
 
G_M000_IG240:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       test     ebx, ebx
       jle      SHORT G_M000_IG243
 
G_M000_IG241:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       mov      r9d, dword ptr [rdi+0x08]
       cmp      ecx, r9d
       jae      G_M000_IG317
       cmp      dword ptr [rdi+4*rcx+0x10], 48
       je       SHORT G_M000_IG243
 
G_M000_IG242:
       mov      rdi, rsi
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 48
 
G_M000_IG243:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rbx, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG156
 
G_M000_IG244:
       xor      edx, edx
       mov      dword ptr [rbp+0x9C], edx
       jmp      G_M000_IG195
 
G_M000_IG245:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG247
 
G_M000_IG246:
       xor      edx, edx
       mov      dword ptr [rbp+0x9C], edx
       jmp      G_M000_IG195
 
G_M000_IG247:
       test     rcx, rcx
       setne    dl
       movzx    rdx, dl
       mov      dword ptr [rbp+0x9C], edx
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG301
       jmp      G_M000_IG300
 
G_M000_IG248:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rsi
       je       G_M000_IG263
       jmp      G_M000_IG174
 
G_M000_IG249:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x160], rax
       lea      rdi, [rbp-0x180]
       mov      rdx, rcx
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x180]
       mov      qword ptr [rbp-0x178], r8
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG259
 
G_M000_IG250:
       mov      gword ptr [rbp+0x310], rsi
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x170], xmm1
       lea      rdi, [rbp-0x170]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x170]
       mov      gword ptr [rbp-0x298], rax
       mov      ecx, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x228], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
       lea      rdi, [rbp-0x190]
       mov      rsi, qword ptr [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x190]
       mov      r9d, dword ptr [rbp-0x188]
       mov      rcx, gword ptr [rbp-0x298]
       test     rcx, rcx
       jne      SHORT G_M000_IG253
 
G_M000_IG251:
       test     rax, rax
       jne      SHORT G_M000_IG252
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
       movsxd   rsi, dword ptr [rbp-0x228]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1A0]
       mov      esi, dword ptr [rbp-0x198]
       jmp      SHORT G_M000_IG258
 
G_M000_IG252:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG254
 
G_M000_IG253:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG254:
       test     rax, rax
       jne      SHORT G_M000_IG256
 
G_M000_IG255:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG257
 
G_M000_IG256:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG257:
       mov      edx, dword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
 
G_M000_IG258:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
       jmp      SHORT G_M000_IG262
 
G_M000_IG259:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG261
 
G_M000_IG260:
       mov      gword ptr [rbp+0x310], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
       jmp      SHORT G_M000_IG262
 
G_M000_IG261:
       test     rax, rax
       jl       G_M000_IG318
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG317
       test     rax, rax
       jl       G_M000_IG318
       mov      rdi, rax
       mov      rdx, gword ptr [r8+8*rdi+0x10]
       mov      gword ptr [rbp+0x310], rsi
 
G_M000_IG262:
       jmp      G_M000_IG175
 
G_M000_IG263:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rcx
       vmovsd   qword ptr [rbp-0x1A8], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x1A8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG175
 
G_M000_IG264:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG174
 
G_M000_IG265:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG174
 
G_M000_IG266:
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG268
 
G_M000_IG267:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       mov      gword ptr [rbp+0x310], rsi
       je       SHORT G_M000_IG269
       jmp      G_M000_IG174
 
G_M000_IG268:
       mov      gword ptr [rbp+0x310], rsi
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG175
 
G_M000_IG269:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG175
 
G_M000_IG270:
       test     rax, rax
       jne      SHORT G_M000_IG272
 
G_M000_IG271:
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG174
 
G_M000_IG272:
       cmp      dword ptr [rax], eax
       mov      gword ptr [rbp+0x310], rsi
       jmp      G_M000_IG174
 
G_M000_IG273:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG280
       jmp      G_M000_IG186
 
G_M000_IG274:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1D0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1D8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG276
 
G_M000_IG275:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1E8], xmm0
       lea      rdi, [rbp-0x1E8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1E8]
       mov      gword ptr [rbp-0x2A0], rax
       mov      ecx, dword ptr [rbp-0x1E0]
       mov      dword ptr [rbp-0x22C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1F8], xmm0
       lea      rdi, [rbp-0x1F8]
       mov      rsi, qword ptr [rbp-0x1D0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2A0]
       mov      esi, dword ptr [rbp-0x22C]
       mov      rdx, gword ptr [rbp-0x1F8]
       mov      rcx, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      SHORT G_M000_IG279
 
G_M000_IG276:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG278
 
G_M000_IG277:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1D8]
       mov      qword ptr [rax+0x08], rdi
       jmp      SHORT G_M000_IG279
 
G_M000_IG278:
       test     rax, rax
       jl       G_M000_IG318
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r8, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG317
       test     rax, rax
       jl       G_M000_IG318
       mov      rdi, rax
       mov      rax, gword ptr [r8+8*rdi+0x10]
 
G_M000_IG279:
       jmp      G_M000_IG187
 
G_M000_IG280:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1C8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x200], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG187
 
G_M000_IG281:
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG283
 
G_M000_IG282:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       je       SHORT G_M000_IG284
       jmp      G_M000_IG186
 
G_M000_IG283:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1C0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG187
 
G_M000_IG284:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG187
 
G_M000_IG285:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG186
 
G_M000_IG286:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG186
 
G_M000_IG287:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG187
 
G_M000_IG288:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      rdi, rdi
       xor      rsi, rsi
       mov      dword ptr [rbp+0x9C], ecx
       mov      rcx, rsi
       jmp      G_M000_IG194
 
G_M000_IG289:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG291
 
G_M000_IG290:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG288
 
G_M000_IG291:
       jmp      G_M000_IG207
 
G_M000_IG292:
       jmp      G_M000_IG207
 
G_M000_IG293:
       mov      rdi, gword ptr [rbp+0x338]
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm1, xmm0
       mov      ecx, dword ptr [rbp+0x9C]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       jmp      SHORT G_M000_IG288
 
G_M000_IG294:
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       jne      SHORT G_M000_IG293
 
G_M000_IG295:
       mov      rax, gword ptr [rbp-0x248]
       jmp      G_M000_IG202
 
G_M000_IG296:
       lea      rdx, [rbp-0x218]
       mov      rdi, gword ptr [rbp+0x338]
       mov      esi, dword ptr [rbp-0x210]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryReadF64(int,byref):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG294
 
G_M000_IG297:
       mov      rax, gword ptr [rbp-0x248]
       jmp      G_M000_IG202
 
G_M000_IG298:
       mov      rax, gword ptr [rbp-0x248]
       jmp      G_M000_IG202
 
G_M000_IG299:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, rdi
       mov      gword ptr [rbp-0x2B0], rax
       mov      rcx, gword ptr [rax+0x70]
       test     rcx, rcx
       je       SHORT G_M000_IG303
       jmp      SHORT G_M000_IG302
 
G_M000_IG300:
       mov      gword ptr [rbp+0x310], rcx
       mov      rsi, rcx
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x310]
 
G_M000_IG301:
       mov      gword ptr [rbp+0x310], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rcx, gword ptr [rbp+0x310]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       jmp      G_M000_IG195
 
G_M000_IG302:
       cmp      dword ptr [rcx+0x08], 1
       mov      gword ptr [rbp-0x278], rcx
       je       SHORT G_M000_IG304
 
G_M000_IG303:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x2B0]
       lea      rdi, bword ptr [rdi+0x70]
       mov      gword ptr [rbp-0x278], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG304:
       mov      rdi, gword ptr [rbp-0x278]
       mov      rdx, r12
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x278]
       mov      rsi, r15
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       jmp      G_M000_IG155
 
G_M000_IG305:
       mov      rdi, r15
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       jmp      G_M000_IG155
 
G_M000_IG306:
       mov      r12d, dword ptr [(reloc)]
       test     r12d, r12d
       jle      SHORT G_M000_IG309
 
G_M000_IG307:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       mov      rsi, rdi
       dec      r12d
       mov      eax, r12d
       mov      edx, dword ptr [rsi+0x08]
       cmp      eax, edx
       jae      G_M000_IG317
       mov      ecx, eax
       cmp      dword ptr [rsi+4*rcx+0x10], 53
       je       SHORT G_M000_IG309
 
G_M000_IG308:
       mov      rsi, rdi
       mov      edi, r12d
       cmp      edi, edx
       jae      G_M000_IG317
       mov      dword ptr [rsi+4*rcx+0x10], 53
 
G_M000_IG309:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG312
 
G_M000_IG310:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG312
 
G_M000_IG311:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG312:
       add      rsp, 0x608
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG313:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG314:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG315:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG316:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG317:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG318:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8065

