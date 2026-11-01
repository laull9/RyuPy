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

; Assembly listing for method RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 256
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqu32 zmmword ptr [rbp-0x50], zmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rax, gword ptr [rbp-0x08]
       mov      gword ptr [rbp-0x60], rax
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x68], rax
       cmp      gword ptr [rbp-0x68], 0
       jne      G_M000_IG04
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x70], rax
       cmp      gword ptr [rbp-0x70], 0
       jne      G_M000_IG05
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x78], rax
       cmp      gword ptr [rbp-0x78], 0
       jne      G_M000_IG09
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x80], rax
       cmp      gword ptr [rbp-0x80], 0
       jne      G_M000_IG10
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x88], rax
       cmp      gword ptr [rbp-0x88], 0
       jne      G_M000_IG11
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x90], rax
       cmp      gword ptr [rbp-0x90], 0
       jne      G_M000_IG12
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x98], rax
       cmp      gword ptr [rbp-0x98], 0
       jne      G_M000_IG13
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
 
G_M000_IG03:
       mov      gword ptr [rbp-0xA0], rax
       cmp      gword ptr [rbp-0xA0], 0
       jne      G_M000_IG14
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xA8], rax
       cmp      gword ptr [rbp-0xA8], 0
       jne      G_M000_IG17
       jmp      G_M000_IG20
 
G_M000_IG04:
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x18], rax
       mov      rdi, gword ptr [rbp-0x18]
       call     [RyuPy.Runtime.PyEnum:IsEnumClass(System.Object):bool]
       test     eax, eax
       je       G_M000_IG20
       mov      rdi, gword ptr [rbp-0x18]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyEnum:GetItem(RyuPy.Runtime.PyClass,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG05:
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x20], rax
       mov      rdi, gword ptr [rbp-0x20]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      gword ptr [rbp-0xB0], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG06
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG06
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG06
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG06
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       mov      dword ptr [rbp-0xB4], 1
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       xor      eax, eax
       mov      dword ptr [rbp-0xB4], eax
 
G_M000_IG08:
       cmp      dword ptr [rbp-0xB4], 0
       je       G_M000_IG20
       mov      rdi, gword ptr [rbp-0x20]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG09:
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0x28], rax
       mov      rdi, gword ptr [rbp-0x28]
       call     [RyuPy.Runtime.PyAbc:IsAbc(RyuPy.Runtime.PyNativeType):bool]
       test     eax, eax
       je       G_M000_IG20
       mov      rdi, gword ptr [rbp-0x28]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG10:
       mov      rdi, gword ptr [rbp-0x80]
       mov      rsi, gword ptr [rbp-0x10]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG11:
       mov      rdi, gword ptr [rbp-0x88]
       mov      rsi, gword ptr [rbp-0x10]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyTuple:GetItem(System.Object):System.Object:this]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG12:
       mov      rdi, gword ptr [rbp-0x90]
       mov      rsi, gword ptr [rbp-0x10]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyDict:Get(System.Object):System.Object:this]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG13:
       mov      rdi, gword ptr [rbp-0x98]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyOps:GetString(System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG14:
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x30], rax
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x38], rax
       cmp      gword ptr [rbp-0x38], 0
       jne      SHORT G_M000_IG15
       mov      rdi, gword ptr [rbp-0x10]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      qword ptr [rbp-0xD8], rax
       mov      rsi, qword ptr [rbp-0xD8]
       mov      rdi, gword ptr [rbp-0x30]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBytes:GetItem(long):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x38]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBytes:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyBytes:this]
       mov      gword ptr [rbp-0xC8], rax
 
G_M000_IG16:
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG17:
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x48], rax
       cmp      gword ptr [rbp-0x48], 0
       jne      SHORT G_M000_IG18
       mov      rdi, gword ptr [rbp-0x10]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      qword ptr [rbp-0xE0], rax
       mov      rsi, qword ptr [rbp-0xE0]
       mov      rdi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyByteArray:GetItem(long):System.Object:this]
       mov      gword ptr [rbp-0xD0], rax
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x48]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyByteArray:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyByteArray:this]
       mov      gword ptr [rbp-0xD0], rax
 
G_M000_IG19:
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG20:
       lea      rcx, [rbp-0x50]
       mov      rdi, gword ptr [rbp-0x08]
       mov      rdx, gword ptr [rbp-0x10]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:TryMethod1(System.Object,System.String,System.Object,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG21
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG22
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xC0], rax
       mov      edi, 0x129E
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x3930
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rsi, gword ptr [rbp-0xF0]
       mov      rdx, gword ptr [rbp-0xF8]
       call     [System.String:Concat(System.String,System.String,System.String):System.String]
       mov      gword ptr [rbp-0x100], rax
       mov      rsi, gword ptr [rbp-0x100]
       mov      rdi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xC0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG22:
       mov      rax, gword ptr [rbp-0x58]
 
G_M000_IG23:
       add      rsp, 256
       pop      rbp
       ret      
 
; Total bytes of code 1333

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 48
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
       mov      qword ptr [rbp-0x20], rax
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x18], rax
       cmp      gword ptr [rbp-0x18], 0
       je       SHORT G_M000_IG03
       mov      rdi, gword ptr [rbp-0x08]
       mov      rsi, gword ptr [rbp-0x18]
       call     [RyuPy.Runtime.PyList:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyList:this]
       mov      gword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyList:ToIndex(System.Object):long]
       mov      qword ptr [rbp-0x28], rax
       mov      rsi, qword ptr [rbp-0x28]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyList:GetItem(long):System.Object:this]
       mov      gword ptr [rbp-0x20], rax
 
G_M000_IG04:
       mov      rax, gword ptr [rbp-0x20]
 
G_M000_IG05:
       add      rsp, 48
       pop      rbp
       ret      
 
; Total bytes of code 121

; Assembly listing for method RyuPy.Runtime.PyList:ToIndex(System.Object):long (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      qword ptr [rbp-0x10], rax
       mov      gword ptr [rbp-0x08], rdi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG03
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       movzx    rax, byte  ptr [rax]
       mov      dword ptr [rbp-0x0C], eax
       jmp      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG04
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x18], rax
       jmp      SHORT G_M000_IG06
 
G_M000_IG04:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG08
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x1C], eax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       cmp      dword ptr [rbp-0x0C], 0
       setne    al
       movzx    rax, al
       mov      eax, eax
       mov      qword ptr [rbp-0x28], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG06:
       mov      rax, qword ptr [rbp-0x18]
       mov      qword ptr [rbp-0x28], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       movsxd   rax, dword ptr [rbp-0x1C]
       mov      qword ptr [rbp-0x28], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x30], rax
       mov      edi, 0x5CBF
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x40]
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, gword ptr [rbp-0x48]
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x30]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG09:
       mov      rax, qword ptr [rbp-0x28]
 
G_M000_IG10:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 346

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(long):System.Object:this (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
       mov      gword ptr [rbp-0x08], rdi
       mov      qword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyList:get_Items():System.Collections.Generic.List`1[System.Object]:this]
       mov      gword ptr [rbp-0x18], rax
       mov      rdi, gword ptr [rbp-0x08]
       mov      rsi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyList:Normalize(long):long:this]
       mov      dword ptr [rbp-0x1C], eax
       mov      esi, dword ptr [rbp-0x1C]
       mov      rdi, gword ptr [rbp-0x18]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       nop      
 
G_M000_IG03:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 77

; Assembly listing for method RyuPy.Generated.Module:m0_read_refs(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x106
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 26 inlinees with PGO data; 47 single block inlinees; 30 inlinees without PGO data

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
       sub      rsp, 864
       lea      rbp, [rsp+0x360]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x360], xmm8
       vmovdqa32 xmmword ptr [rbp-0x350], xmm8
       mov      rax, -768
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x350], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x78], rax
       mov      edi, 40
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
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
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x350]
       mov      esi, 102
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
       mov      gword ptr [rbp-0x328], rax
       mov      rdi, gword ptr [rbp-0x328]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x330], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x358], rax
       mov      rcx, gword ptr [rbp-0x358]
       mov      rsi, gword ptr [rbp-0x328]
       mov      rdx, gword ptr [rbp-0x330]
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
       jne      G_M000_IG47
       jmp      G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xA8]
       add      rax, qword ptr [rbp-0xB8]
       jo       G_M000_IG75
       mov      qword ptr [rbp-0xA8], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 46
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
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
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x350]
       mov      esi, 412
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
       jmp      G_M000_IG14
 
G_M000_IG12:
       mov      rdi, qword ptr [rbp-0x110]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 44
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
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
       jne      G_M000_IG44
       jmp      G_M000_IG37
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x110]
       add      rax, qword ptr [rbp-0x120]
       jo       G_M000_IG75
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
       jne      G_M000_IG51
       jmp      G_M000_IG48
 
G_M000_IG15:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG17
 
G_M000_IG16:
       lea      rdi, [rbp-0x350]
       mov      esi, 577
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
       mov      gword ptr [rbp-0x340], rax
       mov      rdi, gword ptr [rbp-0x340]
       mov      rdx, gword ptr [rbp-0x48]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x348], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x360], rax
       mov      rcx, gword ptr [rbp-0x360]
       mov      rsi, gword ptr [rbp-0x340]
       mov      rdx, gword ptr [rbp-0x348]
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
       mov      qword ptr [rbp-0x2D8], rax
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2DC], eax
       cmp      dword ptr [rbp-0x2DC], 0
       jne      G_M000_IG70
       jmp      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x1A0]
       add      rax, qword ptr [rbp-0x1B0]
       jo       G_M000_IG75
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
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x350]
       mov      esi, 837
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
       jmp      G_M000_IG26
 
G_M000_IG24:
       mov      rdi, qword ptr [rbp-0x208]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x240], rax
       mov      rax, gword ptr [rbp-0x240]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 51
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x248], rax
       mov      rdi, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x250], rax
       mov      rax, gword ptr [rbp-0x248]
       mov      gword ptr [rbp-0x2F0], rax
       mov      rax, gword ptr [rbp-0x250]
       mov      gword ptr [rbp-0x300], rax
       cmp      dword ptr [rbp-0x2DC], 0
       jne      G_M000_IG67
       jmp      G_M000_IG60
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x208]
       add      rax, qword ptr [rbp-0x218]
       jo       G_M000_IG75
       mov      qword ptr [rbp-0x208], rax
       jmp      G_M000_IG21
 
G_M000_IG26:
       cmp      gword ptr [rbp-0x70], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E0], eax
       mov      eax, dword ptr [rbp-0x2DC]
       and      eax, dword ptr [rbp-0x2E0]
       movzx    rax, al
       mov      dword ptr [rbp-0x2E4], eax
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG74
       jmp      G_M000_IG71
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x270]
 
G_M000_IG28:
       add      rsp, 864
       pop      rbp
       ret      
 
G_M000_IG29:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x3F5
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
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x407
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
       mov      gword ptr [rbp-0x338], rax
       mov      rax, gword ptr [rbp-0x338]
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x338]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG33
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x44E
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
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
 
G_M000_IG41:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG43
 
G_M000_IG42:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x492
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG43:
       mov      rsi, gword ptr [rbp-0x2B0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2CC], eax
       cmp      dword ptr [rbp-0x2CC], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG37
 
G_M000_IG44:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG46
 
G_M000_IG45:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x4AD
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG46:
       mov      rsi, gword ptr [rbp-0x2A0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2D0], eax
       cmp      dword ptr [rbp-0x2D0], 0
       jne      G_M000_IG41
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG37
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x288], xmm0
       jmp      G_M000_IG09
 
G_M000_IG48:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG50
 
G_M000_IG49:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x4D5
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG50:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x48]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x170], rax
       jmp      G_M000_IG07
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x338], rax
       mov      rax, gword ptr [rbp-0x338]
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x338]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG48
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x4FE
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG54:
       jmp      G_M000_IG25
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       jmp      SHORT G_M000_IG52
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x511
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
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
       mov      dword ptr [rbp-0x2DC], eax
       cmp      dword ptr [rbp-0x2DC], 0
       jne      G_M000_IG55
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x338], rax
       mov      rax, gword ptr [rbp-0x338]
       vmovsd   xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x338]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG56
 
G_M000_IG60:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG62
 
G_M000_IG61:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x55E
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG62:
       cmp      gword ptr [rbp-0x70], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E0], eax
       mov      eax, dword ptr [rbp-0x2DC]
       and      eax, dword ptr [rbp-0x2E0]
       movzx    rax, al
       mov      dword ptr [rbp-0x2E4], eax
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG59
       jmp      G_M000_IG56
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x2F0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       mov      rsi, gword ptr [rbp-0x300]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2D8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rbp-0x318], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG52
 
G_M000_IG64:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG66
 
G_M000_IG65:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x5A4
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG66:
       mov      rsi, gword ptr [rbp-0x300]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x31C], eax
       cmp      dword ptr [rbp-0x31C], 0
       jne      G_M000_IG63
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG60
 
G_M000_IG67:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG69
 
G_M000_IG68:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x5BF
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG69:
       mov      rsi, gword ptr [rbp-0x2F0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x320], eax
       cmp      dword ptr [rbp-0x320], 0
       jne      G_M000_IG64
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG60
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       jmp      G_M000_IG21
 
G_M000_IG71:
       mov      eax, dword ptr [rbp-0x350]
       dec      eax
       mov      dword ptr [rbp-0x350], eax
       cmp      dword ptr [rbp-0x350], 0
       jg       SHORT G_M000_IG73
 
G_M000_IG72:
       lea      rdi, [rbp-0x350]
       mov      esi, 0x5E8
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG73:
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x68]
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x268], rax
       jmp      G_M000_IG19
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x338], rax
       mov      rax, gword ptr [rbp-0x338]
       vmovsd   xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x338]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG71
 
G_M000_IG75:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4684

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x492
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 53 inlinees with PGO data; 73 single block inlinees; 84 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 624
       mov      qword ptr [rsp+0x5D8], r15
       mov      qword ptr [rsp+0x5D0], r14
       mov      qword ptr [rsp+0x5C8], r13
       mov      qword ptr [rsp+0x5C0], r12
       mov      qword ptr [rsp+0x5B8], rbx
       lea      rbp, [rsp+0x270]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x1D0], xmm8
       vmovdqa32 xmmword ptr [rbp-0x1C0], xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x340]
       mov      r15, gword ptr [rbp+0x338]
       mov      r14, gword ptr [rbp+0x330]
       mov      r13, gword ptr [rbp+0x318]
       mov      r12, qword ptr [rbp+0x260]
       mov      r8, gword ptr [rbp+0xD0]
       mov      r11, gword ptr [rbp+0xC0]
 
G_M000_IG02:
       test     r11, r11
       je       G_M000_IG28
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0xC0], r11
       cmp      qword ptr [r11], rsi
       jne      G_M000_IG28
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0xD0], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0xD0]
 
G_M000_IG06:
       vmovsd   xmm1, qword ptr [r8+0x08]
       mov      r11, gword ptr [rbp+0xC0]
       vmulsd   xmm1, xmm1, qword ptr [r11+0x08]
       vaddsd   xmm0, xmm1, qword ptr [rbp+0xE8]
       vmovsd   qword ptr [rbp+0xE8], xmm0
       xor      r13, r13
       mov      eax, dword ptr [rbp+0xE4]
       jmp      G_M000_IG18
 
G_M000_IG07:
       mov      rdx, 0xD1FFAB1E
       cmp      r8, rdx
       je       G_M000_IG56
 
G_M000_IG08:
       mov      rcx, gword ptr [rbp+0x218]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:
       mov      rcx, gword ptr [rbp+0x218]
 
G_M000_IG10:
       mov      rdx, rcx
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG11:
       test     r13, r13
       je       SHORT G_M000_IG15
 
G_M000_IG12:
       mov      rax, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG61
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG78
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG83
 
G_M000_IG15:
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG16:
       test     r13, r13
       jne      G_M000_IG86
 
G_M000_IG17:
       xor      eax, eax
 
G_M000_IG18:
       mov      rcx, qword ptr [rbp+0x250]
       add      r12, rcx
       jo       G_M000_IG250
 
G_M000_IG19:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0x258]
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
       je       G_M000_IG89
 
G_M000_IG20:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG106
 
G_M000_IG21:
       mov      dword ptr [rbp+0xE4], eax
       mov      qword ptr [rbp+0x258], rdx
       mov      qword ptr [rbp+0x250], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG22:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG249
       cmp      dword ptr [r8+4*r9+0x10], 44
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG249
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 44
 
G_M000_IG25:
       mov      rdi, rbx
       mov      gword ptr [rbp-0x210], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x220], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x210]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rax, gword ptr [rbp+0x220]
       mov      r8, rax
       mov      r11, rdi
       mov      ecx, dword ptr [rbp+0xE4]
       test     ecx, ecx
       mov      gword ptr [rbp+0x220], rax
       mov      gword ptr [rbp+0x218], rdi
       mov      dword ptr [rbp+0xE4], ecx
       je       SHORT G_M000_IG28
 
G_M000_IG26:
       test     r8, r8
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       je       G_M000_IG02
 
G_M000_IG28:
       test     r13, r13
       sete     r8b
       movzx    r8, r8b
       test     dword ptr [rbp+0xE4], r8d
       je       SHORT G_M000_IG30
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xE8]
       vmovsd   qword ptr [r13+0x08], xmm0
 
G_M000_IG30:
       mov      rax, gword ptr [rbp+0x220]
       test     rax, rax
       je       G_M000_IG09
 
G_M000_IG31:
       mov      r8, qword ptr [rax]
       mov      rdx, 0xD1FFAB1E
       cmp      r8, rdx
       jne      G_M000_IG07
 
G_M000_IG32:
       mov      rdx, qword ptr [rax+0x08]
       mov      rcx, gword ptr [rbp+0x218]
       test     rcx, rcx
       je       G_M000_IG10
 
G_M000_IG33:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       jne      G_M000_IG54
 
G_M000_IG34:
       mov      rax, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x78], rax
       lea      rcx, [rbp-0x98]
       mulx     r11, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0x98]
       mov      qword ptr [rbp-0x90], rcx
       mov      r8, rdx
       sar      r8, 63
       and      r8, rax
       sub      r11, r8
       mov      r8, rax
       sar      r8, 63
       and      r8, rdx
       sub      r11, r8
       mov      r8, rcx
       sar      r8, 63
       cmp      r11, r8
       je       G_M000_IG50
 
G_M000_IG35:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x88], xmm1
       lea      rdi, [rbp-0x88]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x248], rax
       mov      ecx, dword ptr [rbp-0x80]
       mov      dword ptr [rbp-0x1E4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       mov      rsi, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      r9d, dword ptr [rbp-0xA0]
       mov      rcx, gword ptr [rbp-0x248]
       test     rcx, rcx
       jne      G_M000_IG43
 
G_M000_IG36:
       test     rax, rax
       jne      G_M000_IG155
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       movsxd   rsi, dword ptr [rbp-0x1E4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r10d, dword ptr [rbp-0xB0]
 
G_M000_IG37:
       test     rax, rax
       jne      G_M000_IG156
 
G_M000_IG38:
       test     rax, rax
       jne      G_M000_IG165
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG48
 
G_M000_IG39:
       mov      edi, -1
 
G_M000_IG40:
       test     edi, edi
       mov      gword ptr [rbp-0x250], rax
       mov      dword ptr [rbp-0x1E8], r10d
       jg       SHORT G_M000_IG49
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x228], rax
       mov      rdi, gword ptr [rbp-0x250]
       mov      esi, dword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x228]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG42:
       jmp      G_M000_IG11
 
G_M000_IG43:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG44:
       test     rax, rax
       je       SHORT G_M000_IG47
 
G_M000_IG45:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG46:
       mov      edx, dword ptr [rbp-0x1E4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG37
 
G_M000_IG47:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG46
 
G_M000_IG48:
       xor      edi, edi
       jmp      SHORT G_M000_IG40
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x220], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x250]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x220]
       mov      edi, dword ptr [rbp-0x1E8]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG42
 
G_M000_IG50:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG53
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x90]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG52:
       jmp      G_M000_IG42
 
G_M000_IG53:
       test     rax, rax
       jl       G_M000_IG250
       mov      rdx, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdx, 262
       jae      G_M000_IG249
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG52
 
G_M000_IG54:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       jne      G_M000_IG10
 
G_M000_IG55:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rdx
       vmovsd   qword ptr [rbp-0xC0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xC0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG11
 
G_M000_IG56:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       mov      rcx, gword ptr [rbp+0x218]
       test     rcx, rcx
       je       G_M000_IG10
 
G_M000_IG57:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG59
 
G_M000_IG58:
       vmovsd   xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG11
 
G_M000_IG59:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG10
 
G_M000_IG60:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG11
 
G_M000_IG61:
       mov      rsi, qword ptr [r13+0x08]
       test     rdx, rdx
       je       G_M000_IG15
 
G_M000_IG62:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG76
 
G_M000_IG63:
       mov      r13, qword ptr [rdx+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xE8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG72
 
G_M000_IG64:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x258], rax
       mov      ecx, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0x1EC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x108], xmm0
       lea      rdi, [rbp-0x108]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x258]
       mov      esi, dword ptr [rbp-0x1EC]
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r13, rax
       mov      eax, edx
       mov      dword ptr [rbp-0x1F0], edx
       test     r13, r13
       jne      G_M000_IG173
 
G_M000_IG65:
       test     r13, r13
       jne      G_M000_IG182
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG70
 
G_M000_IG66:
       mov      edi, -1
 
G_M000_IG67:
       test     edi, edi
       jg       SHORT G_M000_IG71
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x238], rax
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x238]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG69:
       jmp      G_M000_IG16
 
G_M000_IG70:
       xor      edi, edi
       jmp      SHORT G_M000_IG67
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x230], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      r13, gword ptr [rbp-0x230]
       mov      edi, dword ptr [rbp-0x1F0]
       mov      dword ptr [r13+0x10], edi
       jmp      SHORT G_M000_IG69
 
G_M000_IG72:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG75
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xE8]
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG74:
       jmp      SHORT G_M000_IG69
 
G_M000_IG75:
       test     r13, r13
       jl       G_M000_IG250
       mov      rdi, r13
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG249
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG74
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG15
 
G_M000_IG77:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x110], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x110]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG78:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rdx, rdx
       je       G_M000_IG15
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG81
 
G_M000_IG80:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG15
 
G_M000_IG82:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG83:
       mov      rdi, r13
       test     rdx, rdx
       je       G_M000_IG15
 
G_M000_IG84:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG15
 
G_M000_IG85:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG88
 
G_M000_IG87:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0xE8], xmm0
       jmp      G_M000_IG18
 
G_M000_IG88:
       xor      eax, eax
       jmp      G_M000_IG18
 
G_M000_IG89:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     eax, edi
       je       SHORT G_M000_IG91
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xE8]
       vmovsd   qword ptr [r13+0x08], xmm0
 
G_M000_IG91:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG93
 
G_M000_IG92:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x240], rax
       je       SHORT G_M000_IG94
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x240], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG94:
       mov      rdi, gword ptr [rbp-0x240]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x240]
       mov      rsi, gword ptr [rbp+0x328]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r13, qword ptr [rbp+0x2C8]
       mov      r12, qword ptr [rbp+0x2B8]
       add      r13, r12
       jo       G_M000_IG250
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x2C0]
       cmp      r13, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2C8], r13
       mov      qword ptr [rbp+0x2C0], rax
       cmp      r13, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG190
 
G_M000_IG95:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG98
 
G_M000_IG96:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       mov      r9d, dword ptr [rcx+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG249
       mov      r9d, r8d
       cmp      dword ptr [rcx+4*r9+0x10], 42
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       mov      ecx, edx
       mov      dword ptr [rsi+4*rcx+0x10], 42
 
G_M000_IG98:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x318], rsi
       test     edi, edi
       jle      SHORT G_M000_IG101
 
G_M000_IG99:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       dec      edi
       mov      r8d, edi
       mov      r9d, dword ptr [rdx+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG249
       cmp      dword ptr [rdx+4*r8+0x10], 43
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp+0x280], rdx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x200], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rax, gword ptr [rcx]
       mov      rcx, rax
       mov      rdx, rax
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x200]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x278], rax
       mov      rdi, gword ptr [rbp+0x280]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x270], rax
       mov      rdi, gword ptr [rbp+0x278]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      rdi, qword ptr [rbp+0x270]
       mov      rax, rdi
       mov      qword ptr [rbp+0x258], rdx
       mov      qword ptr [rbp+0x250], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x318]
       test     rsi, rsi
       jne      SHORT G_M000_IG103
 
G_M000_IG102:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xE4], r8d
       mov      qword ptr [rbp+0x2B8], r12
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      eax, dword ptr [rbp+0xE4]
       mov      rcx, qword ptr [rbp+0x250]
       jmp      G_M000_IG19
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG105
 
G_M000_IG104:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      qword ptr [rbp+0x2B8], r12
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x250]
       jmp      G_M000_IG19
 
G_M000_IG105:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x2B8], r12
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x250]
       jmp      G_M000_IG19
 
G_M000_IG106:
       test     rsi, rsi
       jl       G_M000_IG250
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG249
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       mov      r8, rdi
       mov      dword ptr [rbp+0xE4], eax
       mov      qword ptr [rbp+0x258], rdx
       mov      qword ptr [rbp+0x250], rcx
       mov      rax, r8
       jmp      G_M000_IG22
 
G_M000_IG107:
       mov      gword ptr [rbp+0x300], rdx
 
G_M000_IG108:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG109:
       mov      rax, gword ptr [rbp+0x300]
       test     rax, rax
       je       G_M000_IG121
 
G_M000_IG110:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG118
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG120
 
G_M000_IG112:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG234
       jmp      G_M000_IG121
 
G_M000_IG113:
       test     r8, r8
       jl       G_M000_IG250
       mov      rdi, r8
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG249
       test     r8, r8
       jl       G_M000_IG250
       mov      rdi, r8
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      r8, rdi
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      rax, r8
       jmp      G_M000_IG131
 
G_M000_IG114:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG116
 
G_M000_IG115:
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG108
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG202
       jmp      G_M000_IG201
 
G_M000_IG117:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm1
       test     rax, rax
       jne      G_M000_IG218
       jmp      G_M000_IG217
 
G_M000_IG118:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       SHORT G_M000_IG121
 
G_M000_IG119:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG223
       jmp      G_M000_IG222
 
G_M000_IG120:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       test     rdx, rdx
       jne      G_M000_IG230
 
G_M000_IG121:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG122:
       test     rax, rax
       je       SHORT G_M000_IG126
 
G_M000_IG123:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG125
 
G_M000_IG124:
       mov      edi, 1
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0x98], xmm0
       jmp      SHORT G_M000_IG127
 
G_M000_IG125:
       xor      edi, edi
       jmp      SHORT G_M000_IG127
 
G_M000_IG126:
       xor      edi, edi
 
G_M000_IG127:
       mov      rcx, qword ptr [rbp+0x168]
       add      rcx, 1
       jo       G_M000_IG250
       mov      dword ptr [rbp+0x94], edi
       mov      rdx, rax
       mov      rax, qword ptr [rbp+0x160]
       vmovsd   xmm0, qword ptr [rbp+0x98]
 
G_M000_IG128:
       cmp      rcx, rax
       jge      G_M000_IG145
 
G_M000_IG129:
       mov      qword ptr [rbp+0x168], rcx
       lea      r8, [rcx+0x05]
       cmp      r8, 261
       jbe      G_M000_IG113
 
G_M000_IG130:
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, qword ptr [rbp+0x168]
       mov      qword ptr [rax+0x08], rcx
 
G_M000_IG131:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG134
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG249
       cmp      dword ptr [rdi+4*r8+0x10], 51
       je       SHORT G_M000_IG134
 
G_M000_IG133:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG249
       mov      dword ptr [rdi+4*r8+0x10], 51
 
G_M000_IG134:
       mov      rdi, gword ptr [rbp+0x328]
       mov      gword ptr [rbp-0x218], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x128], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x218]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x120], rax
       mov      ecx, dword ptr [rbp+0x94]
       test     ecx, ecx
       je       SHORT G_M000_IG139
 
G_M000_IG135:
       mov      rdx, gword ptr [rbp+0x128]
       test     rdx, rdx
       je       SHORT G_M000_IG139
 
G_M000_IG136:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG138
 
G_M000_IG137:
       jmp      SHORT G_M000_IG139
 
G_M000_IG138:
       test     rax, rax
       jne      G_M000_IG237
       jmp      G_M000_IG240
 
G_M000_IG139:
       mov      rdx, gword ptr [rbp+0x300]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       SHORT G_M000_IG141
 
G_M000_IG140:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x98]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x300], rdx
       mov      rax, gword ptr [rbp+0x120]
       mov      rdx, gword ptr [rbp+0x300]
 
G_M000_IG141:
       mov      rcx, gword ptr [rbp+0x128]
       test     rcx, rcx
       je       G_M000_IG107
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG114
 
G_M000_IG143:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG117
 
G_M000_IG144:
       mov      rsi, qword ptr [rcx]
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG108
 
G_M000_IG145:
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0x94], edi
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   qword ptr [rbp+0x98], xmm0
       jne      G_M000_IG241
 
G_M000_IG146:
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp+0x300]
       mov      rsi, rbx
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       add      r12, 1
       jo       G_M000_IG250
 
G_M000_IG147:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG242
 
G_M000_IG148:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG249
       cmp      dword ptr [rdi+4*rsi+0x10], 49
       je       SHORT G_M000_IG151
 
G_M000_IG150:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG249
       mov      dword ptr [rdi+4*rsi+0x10], 49
 
G_M000_IG151:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x300], rax
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG154
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG249
       cmp      dword ptr [rdi+4*rsi+0x10], 50
       je       SHORT G_M000_IG154
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG249
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x188], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x208], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x328]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, r13
       mov      rdx, r13
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x208]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x180], rax
       mov      rdi, gword ptr [rbp+0x188]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x178], rax
       mov      rdi, gword ptr [rbp+0x180]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0x178]
       mov      qword ptr [rbp+0x160], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp+0x300]
       test     rdx, rdx
       jne      G_M000_IG198
       jmp      G_M000_IG197
 
G_M000_IG155:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG44
 
G_M000_IG156:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG157
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG158
 
G_M000_IG157:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG164
 
G_M000_IG158:
       cmp      edi, 2
       je       SHORT G_M000_IG159
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG249
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG160
 
G_M000_IG159:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG160:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG161
       mov      ecx, -1
       jmp      SHORT G_M000_IG163
 
G_M000_IG161:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG162
       mov      ecx, 1
       jmp      SHORT G_M000_IG163
 
G_M000_IG162:
       xor      ecx, ecx
 
G_M000_IG163:
       imul     ecx, r10d
 
G_M000_IG164:
       test     ecx, ecx
       jge      G_M000_IG38
       mov      gword ptr [rbp-0x250], rax
       mov      dword ptr [rbp-0x1E8], r10d
       jmp      G_M000_IG49
 
G_M000_IG165:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG166
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG167
 
G_M000_IG166:
       mov      dword ptr [rbp-0x1E8], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x1E8]
       jmp      G_M000_IG40
 
G_M000_IG167:
       cmp      edi, 2
       je       SHORT G_M000_IG168
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG249
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG169
 
G_M000_IG168:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x250], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x250]
 
G_M000_IG169:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG170
       mov      ecx, -1
       jmp      SHORT G_M000_IG172
 
G_M000_IG170:
       test     rdi, rdi
       jge      SHORT G_M000_IG171
       mov      ecx, 1
       jmp      SHORT G_M000_IG172
 
G_M000_IG171:
       xor      ecx, ecx
 
G_M000_IG172:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG40
 
G_M000_IG173:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG174
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG175
 
G_M000_IG174:
       mov      ecx, eax
       jmp      SHORT G_M000_IG181
 
G_M000_IG175:
       cmp      edi, 2
       je       SHORT G_M000_IG176
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG249
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG177
 
G_M000_IG176:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG177:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG178
       mov      ecx, -1
       jmp      SHORT G_M000_IG180
 
G_M000_IG178:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG179
       mov      ecx, 1
       jmp      SHORT G_M000_IG180
 
G_M000_IG179:
       xor      ecx, ecx
 
G_M000_IG180:
       imul     ecx, eax
 
G_M000_IG181:
       test     ecx, ecx
       jge      G_M000_IG65
       jmp      G_M000_IG71
 
G_M000_IG182:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG183
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG184
 
G_M000_IG183:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x1F0]
       jmp      G_M000_IG67
 
G_M000_IG184:
       cmp      edi, 2
       je       SHORT G_M000_IG185
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG249
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG186
 
G_M000_IG185:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG186:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG187
       mov      ecx, -1
       jmp      SHORT G_M000_IG189
 
G_M000_IG187:
       test     rdi, rdi
       jge      SHORT G_M000_IG188
       mov      ecx, 1
       jmp      SHORT G_M000_IG189
 
G_M000_IG188:
       xor      ecx, ecx
 
G_M000_IG189:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG67
 
G_M000_IG190:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG193
 
G_M000_IG191:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       mov      r9d, dword ptr [rax+0x08]
       cmp      ecx, r9d
       jae      G_M000_IG249
       cmp      dword ptr [rax+4*rcx+0x10], 46
       je       SHORT G_M000_IG193
 
G_M000_IG192:
       mov      rax, rsi
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 46
 
G_M000_IG193:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rsi, 0xD1FFAB1E
       mov      rax, gword ptr [rsi]
       mov      r13, rax
       mov      rsi, r13
       mov      rdi, r15
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG196
 
G_M000_IG194:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, dword ptr [rcx+0x08]
       cmp      edi, edx
       jae      G_M000_IG249
       cmp      dword ptr [rcx+4*rdi+0x10], 48
       je       SHORT G_M000_IG196
 
G_M000_IG195:
       mov      dword ptr [rax+4*rdi+0x10], 48
 
G_M000_IG196:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      r15, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       jmp      G_M000_IG147
 
G_M000_IG197:
       xor      esi, esi
       mov      dword ptr [rbp+0x94], esi
       jmp      G_M000_IG128
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG200
 
G_M000_IG199:
       xor      esi, esi
       mov      dword ptr [rbp+0x94], esi
       jmp      G_M000_IG128
 
G_M000_IG200:
       mov      esi, 1
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      dword ptr [rbp+0x94], esi
       mov      rdx, gword ptr [rbp+0x300]
       vmovsd   xmm0, qword ptr [rbp+0x98]
       jmp      G_M000_IG128
 
G_M000_IG201:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x300], rdx
       je       G_M000_IG216
       jmp      G_M000_IG108
 
G_M000_IG202:
       mov      gword ptr [rbp+0x300], rdx
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x138], rax
       lea      rdi, [rbp-0x158]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x158]
       mov      qword ptr [rbp-0x150], rcx
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
       je       G_M000_IG212
 
G_M000_IG203:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x148], xmm1
       lea      rdi, [rbp-0x148]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x260], rax
       mov      ecx, dword ptr [rbp-0x140]
       mov      dword ptr [rbp-0x1F4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, qword ptr [rbp-0x138]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      r9d, dword ptr [rbp-0x160]
       mov      rcx, gword ptr [rbp-0x260]
       test     rcx, rcx
       jne      SHORT G_M000_IG206
 
G_M000_IG204:
       test     rax, rax
       jne      SHORT G_M000_IG205
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x178], xmm0
       movsxd   rsi, dword ptr [rbp-0x1F4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x178]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x178]
       mov      esi, dword ptr [rbp-0x170]
       jmp      SHORT G_M000_IG211
 
G_M000_IG205:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG207
 
G_M000_IG206:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG207:
       test     rax, rax
       jne      SHORT G_M000_IG209
 
G_M000_IG208:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG210
 
G_M000_IG209:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG210:
       mov      edx, dword ptr [rbp-0x1F4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
 
G_M000_IG211:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
       jmp      SHORT G_M000_IG215
 
G_M000_IG212:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG214
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x150]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
       jmp      SHORT G_M000_IG215
 
G_M000_IG214:
       test     rax, rax
       jl       G_M000_IG250
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG249
       test     rax, rax
       jl       G_M000_IG250
       mov      rdi, rax
       mov      rdx, gword ptr [r9+8*rdi+0x10]
 
G_M000_IG215:
       jmp      G_M000_IG109
 
G_M000_IG216:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x180], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG109
 
G_M000_IG217:
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG108
 
G_M000_IG218:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG220
 
G_M000_IG219:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x300], rdx
       je       SHORT G_M000_IG221
       jmp      G_M000_IG108
 
G_M000_IG220:
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x128], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x128]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG109
 
G_M000_IG221:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG109
 
G_M000_IG222:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG229
       jmp      G_M000_IG121
 
G_M000_IG223:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1A8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1B0], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG225
 
G_M000_IG224:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1C0]
       mov      gword ptr [rbp-0x268], rax
       mov      ecx, dword ptr [rbp-0x1B8]
       mov      dword ptr [rbp-0x1F8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
       lea      rdi, [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x268]
       mov      esi, dword ptr [rbp-0x1F8]
       mov      rdx, gword ptr [rbp-0x1D0]
       mov      rcx, qword ptr [rbp-0x1C8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      SHORT G_M000_IG228
 
G_M000_IG225:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG227
 
G_M000_IG226:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1B0]
       mov      qword ptr [rax+0x08], rdi
       jmp      SHORT G_M000_IG228
 
G_M000_IG227:
       test     rax, rax
       jl       G_M000_IG250
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r9, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG249
       test     rax, rax
       jl       G_M000_IG250
       mov      rdi, rax
       mov      rax, gword ptr [r9+8*rdi+0x10]
 
G_M000_IG228:
       jmp      G_M000_IG122
 
G_M000_IG229:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1D8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1D8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG122
 
G_M000_IG230:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG232
 
G_M000_IG231:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG233
       jmp      G_M000_IG121
 
G_M000_IG232:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG122
 
G_M000_IG233:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1E0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG122
 
G_M000_IG234:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG121
 
G_M000_IG235:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG121
 
G_M000_IG236:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG122
 
G_M000_IG237:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG239
 
G_M000_IG238:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rax+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x98]
       vmovsd   qword ptr [rbp+0x98], xmm0
       xor      rdx, rdx
       mov      rax, rdx
       mov      edi, ecx
       jmp      G_M000_IG127
 
G_M000_IG239:
       jmp      G_M000_IG139
 
G_M000_IG240:
       jmp      G_M000_IG139
 
G_M000_IG241:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x98]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG146
 
G_M000_IG242:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG245
 
G_M000_IG243:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      SHORT G_M000_IG249
       cmp      dword ptr [rsi+4*rdi+0x10], 53
       je       SHORT G_M000_IG245
 
G_M000_IG244:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      SHORT G_M000_IG249
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG245:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x2F8]
       test     rbx, rbx
       jle      SHORT G_M000_IG248
 
G_M000_IG246:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG248
 
G_M000_IG247:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG248:
       add      rsp, 0x5B8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG249:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG250:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7061

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x407
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 53 inlinees with PGO data; 73 single block inlinees; 84 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 624
       mov      qword ptr [rsp+0x5D8], r15
       mov      qword ptr [rsp+0x5D0], r14
       mov      qword ptr [rsp+0x5C8], r13
       mov      qword ptr [rsp+0x5C0], r12
       mov      qword ptr [rsp+0x5B8], rbx
       lea      rbp, [rsp+0x270]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x1D0], xmm8
       vmovdqa32 xmmword ptr [rbp-0x1C0], xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x340]
       mov      r15, gword ptr [rbp+0x338]
       mov      r14, gword ptr [rbp+0x330]
       mov      r13, gword ptr [rbp+0x318]
       mov      rdi, qword ptr [rbp+0x2B8]
       mov      r12, qword ptr [rbp+0x260]
       mov      rax, gword ptr [rbp+0x220]
       mov      rcx, gword ptr [rbp+0x218]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x2B8], rdi
       test     rdi, rdi
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0x1FC], edx
       jmp      G_M000_IG71
 
G_M000_IG03:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG04:
       test     rax, rax
       je       SHORT G_M000_IG07
 
G_M000_IG05:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG06:
       mov      edx, dword ptr [rbp-0x1E4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG78
 
G_M000_IG07:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG06
 
G_M000_IG08:
       xor      edi, edi
       jmp      G_M000_IG81
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x228], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x258]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x228]
       mov      edi, dword ptr [rbp-0x1E8]
       mov      dword ptr [rdx+0x10], edi
       jmp      G_M000_IG83
 
G_M000_IG10:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x90]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG12:
       jmp      G_M000_IG83
 
G_M000_IG13:
       test     rax, rax
       jl       G_M000_IG255
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG254
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG12
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG22
 
G_M000_IG15:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rdx
       vmovsd   qword ptr [rbp-0xC0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xC0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG23
 
G_M000_IG16:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      SHORT G_M000_IG22
 
G_M000_IG17:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       test     rcx, rcx
       je       SHORT G_M000_IG22
 
G_M000_IG18:
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       vmovsd   xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG23
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG22
 
G_M000_IG21:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      rdx, rcx
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG23:
       test     r13, r13
       je       G_M000_IG53
 
G_M000_IG24:
       mov      rax, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG43
 
G_M000_IG25:
       mov      rsi, qword ptr [r13+0x08]
       test     rdx, rdx
       je       G_M000_IG53
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG41
 
G_M000_IG27:
       mov      r13, qword ptr [rdx+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xE8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG37
 
G_M000_IG28:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x260], rax
       mov      ecx, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0x1EC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x108], xmm0
       lea      rdi, [rbp-0x108]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x260]
       mov      esi, dword ptr [rbp-0x1EC]
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r13, rax
       mov      eax, edx
       mov      dword ptr [rbp-0x1F0], edx
       test     r13, r13
       jne      G_M000_IG236
 
G_M000_IG29:
       test     r13, r13
       jne      G_M000_IG245
 
G_M000_IG30:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG35
 
G_M000_IG31:
       mov      edi, -1
 
G_M000_IG32:
       test     edi, edi
       jg       SHORT G_M000_IG36
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x240], rax
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x240]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG34:
       jmp      G_M000_IG54
 
G_M000_IG35:
       xor      edi, edi
       jmp      SHORT G_M000_IG32
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x238], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      r13, gword ptr [rbp-0x238]
       mov      edi, dword ptr [rbp-0x1F0]
       mov      dword ptr [r13+0x10], edi
       jmp      SHORT G_M000_IG34
 
G_M000_IG37:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xE8]
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG39:
       jmp      SHORT G_M000_IG34
 
G_M000_IG40:
       test     r13, r13
       jl       G_M000_IG255
       mov      rdi, r13
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG254
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG39
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG53
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x110], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x110]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG54
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG49
 
G_M000_IG44:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rdx, rdx
       je       G_M000_IG53
 
G_M000_IG45:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG54
 
G_M000_IG47:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdx], rax
       jne      SHORT G_M000_IG53
 
G_M000_IG48:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG54
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG53
 
G_M000_IG50:
       mov      rdi, r13
       test     rdx, rdx
       je       SHORT G_M000_IG53
 
G_M000_IG51:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG53
 
G_M000_IG52:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
       jmp      SHORT G_M000_IG54
 
G_M000_IG53:
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG54:
       test     r13, r13
       je       SHORT G_M000_IG58
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG57
 
G_M000_IG56:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0xE8], xmm0
       jmp      SHORT G_M000_IG59
 
G_M000_IG57:
       xor      eax, eax
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:
       xor      eax, eax
 
G_M000_IG59:
       mov      rcx, qword ptr [rbp+0x250]
       add      r12, rcx
       jo       G_M000_IG255
 
G_M000_IG60:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0x258]
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
       je       G_M000_IG84
 
G_M000_IG61:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG101
 
G_M000_IG62:
       mov      dword ptr [rbp+0xE4], eax
       mov      qword ptr [rbp+0x258], rdx
       mov      qword ptr [rbp+0x250], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG63:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG66
 
G_M000_IG64:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [r8+4*r9+0x10], 44
       je       SHORT G_M000_IG66
 
G_M000_IG65:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG254
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 44
 
G_M000_IG66:
       mov      rdi, rbx
       mov      gword ptr [rbp-0x218], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x220], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x218]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      edi, dword ptr [rbp+0xE4]
       test     edi, edi
       je       SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rcx, gword ptr [rbp+0x220]
       test     rcx, rcx
       jne      G_M000_IG102
 
G_M000_IG68:
       mov      gword ptr [rbp+0x220], rcx
 
G_M000_IG69:
       test     r13, r13
       sete     dl
       movzx    rdx, dl
       test     edi, edx
       jne      G_M000_IG109
 
G_M000_IG70:
       mov      rcx, rax
       mov      rax, gword ptr [rbp+0x220]
 
G_M000_IG71:
       test     rax, rax
       je       G_M000_IG22
 
G_M000_IG72:
       mov      rdi, qword ptr [rax]
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      G_M000_IG16
 
G_M000_IG73:
       mov      rdx, qword ptr [rax+0x08]
       test     rcx, rcx
       je       G_M000_IG22
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG14
 
G_M000_IG75:
       mov      rax, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x78], rax
       lea      rdi, [rbp-0x98]
       mulx     rsi, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x98]
       mov      qword ptr [rbp-0x90], rcx
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      rsi, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      rsi, rdi
       mov      rdi, rcx
       sar      rdi, 63
       cmp      rsi, rdi
       je       G_M000_IG10
 
G_M000_IG76:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x88], xmm1
       lea      rdi, [rbp-0x88]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x250], rax
       mov      ecx, dword ptr [rbp-0x80]
       mov      dword ptr [rbp-0x1E4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       mov      rsi, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      r9d, dword ptr [rbp-0xA0]
       mov      rcx, gword ptr [rbp-0x250]
       test     rcx, rcx
       jne      G_M000_IG03
 
G_M000_IG77:
       test     rax, rax
       jne      G_M000_IG218
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       movsxd   rsi, dword ptr [rbp-0x1E4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r10d, dword ptr [rbp-0xB0]
 
G_M000_IG78:
       test     rax, rax
       jne      G_M000_IG219
 
G_M000_IG79:
       test     rax, rax
       jne      G_M000_IG228
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG08
 
G_M000_IG80:
       mov      edi, -1
 
G_M000_IG81:
       test     edi, edi
       mov      gword ptr [rbp-0x258], rax
       mov      dword ptr [rbp-0x1E8], r10d
       jg       G_M000_IG09
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x230], rax
       mov      rdi, gword ptr [rbp-0x258]
       mov      esi, dword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x230]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG83:
       jmp      G_M000_IG23
 
G_M000_IG84:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     eax, edi
       je       SHORT G_M000_IG86
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xE8]
       vmovsd   qword ptr [r13+0x08], xmm0
 
G_M000_IG86:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG88
 
G_M000_IG87:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x248], rax
       je       SHORT G_M000_IG89
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x248], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG89:
       mov      rdi, gword ptr [rbp-0x248]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x248]
       mov      rsi, gword ptr [rbp+0x328]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r13, qword ptr [rbp+0x2C8]
       add      r13, qword ptr [rbp+0x2B8]
       jo       G_M000_IG255
       mov      eax, dword ptr [rbp-0x1FC]
       mov      edi, eax
       mov      rcx, qword ptr [rbp+0x2C0]
       cmp      r13, rcx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2C8], r13
       mov      qword ptr [rbp+0x2C0], rcx
       cmp      r13, rcx
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG110
 
G_M000_IG90:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG93
 
G_M000_IG91:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       lea      r8d, [rdi-0x01]
       mov      r9d, r8d
       cmp      r9d, dword ptr [rdx+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rdx+4*r9+0x10], 42
       je       SHORT G_M000_IG93
 
G_M000_IG92:
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG254
       mov      edx, r8d
       mov      dword ptr [rsi+4*rdx+0x10], 42
 
G_M000_IG93:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x318], rsi
       test     edi, edi
       jle      SHORT G_M000_IG96
 
G_M000_IG94:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [r8+4*r9+0x10], 43
       je       SHORT G_M000_IG96
 
G_M000_IG95:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG254
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       mov      gword ptr [rbp+0x280], r8
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x208], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rax, gword ptr [rcx]
       mov      rcx, rax
       mov      rdx, rax
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x208]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x278], rax
       mov      rdi, gword ptr [rbp+0x280]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x270], rax
       mov      rdi, gword ptr [rbp+0x278]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      rdi, qword ptr [rbp+0x270]
       mov      rax, rdi
       mov      qword ptr [rbp+0x258], rdx
       mov      qword ptr [rbp+0x250], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x318]
       test     rsi, rsi
       jne      SHORT G_M000_IG98
 
G_M000_IG97:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xE4], r8d
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      eax, dword ptr [rbp+0xE4]
       mov      rcx, qword ptr [rbp+0x250]
       jmp      G_M000_IG60
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG100
 
G_M000_IG99:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x250]
       jmp      G_M000_IG60
 
G_M000_IG100:
       xor      r8d, r8d
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x250]
       jmp      G_M000_IG60
 
G_M000_IG101:
       test     rsi, rsi
       jl       G_M000_IG255
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG254
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       mov      r8, rdi
       mov      dword ptr [rbp+0xE4], eax
       mov      qword ptr [rbp+0x258], rdx
       mov      qword ptr [rbp+0x250], rcx
       mov      rax, r8
       jmp      G_M000_IG63
 
G_M000_IG102:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG108
 
G_M000_IG103:
       test     rax, rax
       je       SHORT G_M000_IG107
 
G_M000_IG104:
       cmp      qword ptr [rax], rdx
       jne      SHORT G_M000_IG106
 
G_M000_IG105:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rax+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xE8]
       vmovsd   qword ptr [rbp+0xE8], xmm0
       xor      r13, r13
       mov      eax, edi
       jmp      G_M000_IG59
 
G_M000_IG106:
       mov      gword ptr [rbp+0x220], rcx
       jmp      G_M000_IG69
 
G_M000_IG107:
       mov      gword ptr [rbp+0x220], rcx
       jmp      G_M000_IG69
 
G_M000_IG108:
       mov      gword ptr [rbp+0x220], rcx
       jmp      G_M000_IG69
 
G_M000_IG109:
       mov      gword ptr [rbp+0x218], rax
       mov      r13, 0xD1FFAB1E
       mov      rdi, r13
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xE8]
       vmovsd   qword ptr [r13+0x08], xmm0
       vmovsd   qword ptr [rbp+0xE8], xmm0
       mov      rax, gword ptr [rbp+0x220]
       mov      rcx, gword ptr [rbp+0x218]
       jmp      G_M000_IG71
 
G_M000_IG110:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG113
 
G_M000_IG111:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rax+4*rcx+0x10], 46
       je       SHORT G_M000_IG113
 
G_M000_IG112:
       mov      rax, rsi
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG254
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 46
 
G_M000_IG113:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rsi, 0xD1FFAB1E
       mov      rax, gword ptr [rsi]
       mov      r13, rax
       mov      rsi, r13
       mov      rdi, r15
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG116
 
G_M000_IG114:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rcx+4*rdx+0x10], 48
       je       SHORT G_M000_IG116
 
G_M000_IG115:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG254
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 48
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      r15, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       jmp      G_M000_IG177
 
G_M000_IG117:
       test     rax, rax
       je       SHORT G_M000_IG120
 
G_M000_IG118:
       cmp      qword ptr [rax], rdi
       je       G_M000_IG189
 
G_M000_IG119:
       jmp      SHORT G_M000_IG121
 
G_M000_IG120:
 
G_M000_IG121:
       mov      rdx, gword ptr [rbp+0x300]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       SHORT G_M000_IG123
 
G_M000_IG122:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x98]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x300], rdx
       mov      rax, gword ptr [rbp+0x120]
       mov      rdx, gword ptr [rbp+0x300]
 
G_M000_IG123:
       mov      rcx, gword ptr [rbp+0x128]
       test     rcx, rcx
       je       G_M000_IG191
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG128
 
G_M000_IG125:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG147
 
G_M000_IG126:
       mov      rsi, qword ptr [rcx]
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG192
 
G_M000_IG127:
       test     r8, r8
       jl       G_M000_IG255
       mov      rdi, r8
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG254
       test     r8, r8
       jl       G_M000_IG255
       mov      rdi, r8
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      r8, rdi
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      rax, r8
       jmp      G_M000_IG204
 
G_M000_IG128:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG192
 
G_M000_IG130:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG145
 
G_M000_IG131:
       mov      gword ptr [rbp+0x300], rdx
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x138], rax
       lea      rdi, [rbp-0x158]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x158]
       mov      qword ptr [rbp-0x150], rcx
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
       je       G_M000_IG141
 
G_M000_IG132:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x148], xmm1
       lea      rdi, [rbp-0x148]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x268], rax
       mov      ecx, dword ptr [rbp-0x140]
       mov      dword ptr [rbp-0x1F4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, qword ptr [rbp-0x138]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      r9d, dword ptr [rbp-0x160]
       mov      rcx, gword ptr [rbp-0x268]
       test     rcx, rcx
       jne      SHORT G_M000_IG136
 
G_M000_IG133:
       test     rax, rax
       jne      G_M000_IG253
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x178], xmm0
       movsxd   rsi, dword ptr [rbp-0x1F4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x178]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x178]
       mov      esi, dword ptr [rbp-0x170]
 
G_M000_IG134:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG135:
       jmp      G_M000_IG193
 
G_M000_IG136:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG137:
       test     rax, rax
       je       SHORT G_M000_IG140
 
G_M000_IG138:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG139:
       mov      edx, dword ptr [rbp-0x1F4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG134
 
G_M000_IG140:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG139
 
G_M000_IG141:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG144
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x150]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG143:
       jmp      SHORT G_M000_IG135
 
G_M000_IG144:
       test     rax, rax
       jl       G_M000_IG255
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG254
       test     rax, rax
       jl       G_M000_IG255
       mov      rdi, rax
       mov      rdx, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG143
 
G_M000_IG145:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x300], rdx
       jne      G_M000_IG192
 
G_M000_IG146:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x180], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG193
 
G_M000_IG147:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm1
       test     rax, rax
       je       G_M000_IG152
 
G_M000_IG148:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG150
 
G_M000_IG149:
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x128], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x128]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG193
 
G_M000_IG150:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       mov      gword ptr [rbp+0x300], rdx
       jne      G_M000_IG192
 
G_M000_IG151:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG193
 
G_M000_IG152:
       mov      gword ptr [rbp+0x300], rdx
       jmp      G_M000_IG192
 
G_M000_IG153:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG197
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG162
 
G_M000_IG155:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1A8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1B0], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG158
 
G_M000_IG156:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1C0]
       mov      gword ptr [rbp-0x270], rax
       mov      ecx, dword ptr [rbp-0x1B8]
       mov      dword ptr [rbp-0x1F8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
       lea      rdi, [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x270]
       mov      esi, dword ptr [rbp-0x1F8]
       mov      rdx, gword ptr [rbp-0x1D0]
       mov      rcx, qword ptr [rbp-0x1C8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG157:
       jmp      G_M000_IG198
 
G_M000_IG158:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG161
 
G_M000_IG159:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1B0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG160:
       jmp      SHORT G_M000_IG157
 
G_M000_IG161:
       test     rax, rax
       jl       G_M000_IG255
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r9, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG254
       test     rax, rax
       jl       G_M000_IG255
       mov      rdi, rax
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG160
 
G_M000_IG162:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG197
 
G_M000_IG163:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1D8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1D8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG198
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       test     rdx, rdx
       je       G_M000_IG197
 
G_M000_IG165:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG167
 
G_M000_IG166:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG198
 
G_M000_IG167:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG197
 
G_M000_IG168:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1E0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG198
 
G_M000_IG169:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG197
 
G_M000_IG170:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG197
 
G_M000_IG171:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG198
 
G_M000_IG172:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG190
 
G_M000_IG173:
       xor      edi, edi
       jmp      G_M000_IG200
 
G_M000_IG174:
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0x94], edi
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   qword ptr [rbp+0x98], xmm0
       je       SHORT G_M000_IG176
 
G_M000_IG175:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x98]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x300], rdx
 
G_M000_IG176:
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp+0x300]
       mov      rsi, rbx
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       add      r12, 1
       jo       G_M000_IG255
 
G_M000_IG177:
       cmp      r12, r15
       jge      G_M000_IG211
 
G_M000_IG178:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG181
 
G_M000_IG179:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rdi+4*rsi+0x10], 49
       je       SHORT G_M000_IG181
 
G_M000_IG180:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG254
       mov      dword ptr [rdi+4*rsi+0x10], 49
 
G_M000_IG181:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x300], rax
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG184
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rdi+4*rsi+0x10], 50
       je       SHORT G_M000_IG184
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG254
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x188], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x210], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x328]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, r13
       mov      rdx, r13
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x210]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x180], rax
       mov      rdi, gword ptr [rbp+0x188]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x178], rax
       mov      rdi, gword ptr [rbp+0x180]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0x178]
       mov      qword ptr [rbp+0x160], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp+0x300]
       test     rdx, rdx
       je       SHORT G_M000_IG188
 
G_M000_IG185:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG187
 
G_M000_IG186:
       mov      esi, 1
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      dword ptr [rbp+0x94], esi
       mov      rdx, gword ptr [rbp+0x300]
       vmovsd   xmm0, qword ptr [rbp+0x98]
       jmp      G_M000_IG201
 
G_M000_IG187:
       xor      esi, esi
       mov      dword ptr [rbp+0x94], esi
       jmp      G_M000_IG201
 
G_M000_IG188:
       xor      esi, esi
       mov      dword ptr [rbp+0x94], esi
       jmp      G_M000_IG201
 
G_M000_IG189:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rax+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x98]
       vmovsd   qword ptr [rbp+0x98], xmm0
       xor      rdx, rdx
       mov      rax, rdx
       mov      edi, ecx
       jmp      G_M000_IG200
 
G_M000_IG190:
       mov      edi, 1
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0x98], xmm0
       jmp      SHORT G_M000_IG200
 
G_M000_IG191:
       mov      gword ptr [rbp+0x300], rdx
 
G_M000_IG192:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG193:
       mov      rax, gword ptr [rbp+0x300]
       test     rax, rax
       je       SHORT G_M000_IG197
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG153
 
G_M000_IG195:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG164
 
G_M000_IG196:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG169
 
G_M000_IG197:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG198:
       test     rax, rax
       jne      G_M000_IG172
 
G_M000_IG199:
       xor      edi, edi
 
G_M000_IG200:
       mov      rcx, qword ptr [rbp+0x168]
       add      rcx, 1
       jo       G_M000_IG255
       mov      dword ptr [rbp+0x94], edi
       mov      rdx, rax
       mov      rax, qword ptr [rbp+0x160]
       vmovsd   xmm0, qword ptr [rbp+0x98]
 
G_M000_IG201:
       cmp      rcx, rax
       jge      G_M000_IG174
 
G_M000_IG202:
       mov      qword ptr [rbp+0x168], rcx
       lea      r8, [rcx+0x05]
       cmp      r8, 261
       jbe      G_M000_IG127
 
G_M000_IG203:
       mov      gword ptr [rbp+0x300], rdx
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, qword ptr [rbp+0x168]
       mov      qword ptr [rax+0x08], rcx
 
G_M000_IG204:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG207
 
G_M000_IG205:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rdi+4*r8+0x10], 51
       je       SHORT G_M000_IG207
 
G_M000_IG206:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG254
       mov      dword ptr [rdi+4*r8+0x10], 51
 
G_M000_IG207:
       mov      rdi, gword ptr [rbp+0x328]
       mov      gword ptr [rbp-0x220], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x128], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x220]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x120], rax
       mov      ecx, dword ptr [rbp+0x94]
       test     ecx, ecx
       je       G_M000_IG121
 
G_M000_IG208:
       mov      rdx, gword ptr [rbp+0x128]
       test     rdx, rdx
       je       G_M000_IG121
 
G_M000_IG209:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG117
 
G_M000_IG210:
       jmp      G_M000_IG121
 
G_M000_IG211:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG214
 
G_M000_IG212:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG254
       cmp      dword ptr [rsi+4*rdi+0x10], 53
       je       SHORT G_M000_IG214
 
G_M000_IG213:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG254
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG214:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x2F8]
       test     rbx, rbx
       jle      SHORT G_M000_IG217
 
G_M000_IG215:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG217
 
G_M000_IG216:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG217:
       add      rsp, 0x5B8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG218:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG04
 
G_M000_IG219:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG220
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG221
 
G_M000_IG220:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG227
 
G_M000_IG221:
       cmp      edi, 2
       je       SHORT G_M000_IG222
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG254
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG223
 
G_M000_IG222:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG223:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG224
       mov      ecx, -1
       jmp      SHORT G_M000_IG226
 
G_M000_IG224:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG225
       mov      ecx, 1
       jmp      SHORT G_M000_IG226
 
G_M000_IG225:
       xor      ecx, ecx
 
G_M000_IG226:
       imul     ecx, r10d
 
G_M000_IG227:
       test     ecx, ecx
       jge      G_M000_IG79
       mov      gword ptr [rbp-0x258], rax
       mov      dword ptr [rbp-0x1E8], r10d
       jmp      G_M000_IG09
 
G_M000_IG228:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG229
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG230
 
G_M000_IG229:
       mov      dword ptr [rbp-0x1E8], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x1E8]
       jmp      G_M000_IG81
 
G_M000_IG230:
       cmp      edi, 2
       je       SHORT G_M000_IG231
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG254
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG232
 
G_M000_IG231:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x258], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x258]
 
G_M000_IG232:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG233
       mov      ecx, -1
       jmp      SHORT G_M000_IG235
 
G_M000_IG233:
       test     rdi, rdi
       jge      SHORT G_M000_IG234
       mov      ecx, 1
       jmp      SHORT G_M000_IG235
 
G_M000_IG234:
       xor      ecx, ecx
 
G_M000_IG235:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG81
 
G_M000_IG236:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG237
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG238
 
G_M000_IG237:
       mov      ecx, eax
       jmp      SHORT G_M000_IG244
 
G_M000_IG238:
       cmp      edi, 2
       je       SHORT G_M000_IG239
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG254
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG240
 
G_M000_IG239:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG240:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG241
       mov      ecx, -1
       jmp      SHORT G_M000_IG243
 
G_M000_IG241:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG242
       mov      ecx, 1
       jmp      SHORT G_M000_IG243
 
G_M000_IG242:
       xor      ecx, ecx
 
G_M000_IG243:
       imul     ecx, eax
 
G_M000_IG244:
       test     ecx, ecx
       jge      G_M000_IG29
       jmp      G_M000_IG36
 
G_M000_IG245:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG246
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG247
 
G_M000_IG246:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x1F0]
       jmp      G_M000_IG32
 
G_M000_IG247:
       cmp      edi, 2
       je       SHORT G_M000_IG248
       cmp      dword ptr [r13+0x08], 0
       jbe      SHORT G_M000_IG254
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG249
 
G_M000_IG248:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG249:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG250
       mov      ecx, -1
       jmp      SHORT G_M000_IG252
 
G_M000_IG250:
       test     rdi, rdi
       jge      SHORT G_M000_IG251
       mov      ecx, 1
       jmp      SHORT G_M000_IG252
 
G_M000_IG251:
       xor      ecx, ecx
 
G_M000_IG252:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG32
 
G_M000_IG253:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG137
 
G_M000_IG254:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG255:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 6790

; Assembly listing for method RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 256
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqu32 zmmword ptr [rbp-0x50], zmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rax, gword ptr [rbp-0x08]
       mov      gword ptr [rbp-0x60], rax
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x68], rax
       cmp      gword ptr [rbp-0x68], 0
       jne      G_M000_IG04
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x70], rax
       cmp      gword ptr [rbp-0x70], 0
       jne      G_M000_IG06
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x78], rax
       cmp      gword ptr [rbp-0x78], 0
       jne      G_M000_IG15
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x80], rax
       cmp      gword ptr [rbp-0x80], 0
       jne      G_M000_IG17
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x88], rax
       cmp      gword ptr [rbp-0x88], 0
       jne      G_M000_IG18
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x90], rax
       cmp      gword ptr [rbp-0x90], 0
       jne      G_M000_IG19
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x98], rax
       cmp      gword ptr [rbp-0x98], 0
       jne      G_M000_IG20
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
 
G_M000_IG03:
       mov      gword ptr [rbp-0xA0], rax
       cmp      gword ptr [rbp-0xA0], 0
       jne      G_M000_IG21
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0xA8], rax
       cmp      gword ptr [rbp-0xA8], 0
       jne      G_M000_IG24
       jmp      G_M000_IG27
 
G_M000_IG04:
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x18], rax
       mov      rdi, gword ptr [rbp-0x18]
       call     [RyuPy.Runtime.PyEnum:IsEnumClass(System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x18]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyEnum:GetItem(RyuPy.Runtime.PyClass,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG06:
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x20], rax
       mov      rdi, gword ptr [rbp-0x20]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyType:get_Name():System.String:this]
       mov      gword ptr [rbp-0xB0], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG08
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG11
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG10
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG09
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, 0xD1FFAB1E
       call     [System.String:op_Equality(System.String,System.String):bool]
       test     eax, eax
       je       SHORT G_M000_IG12
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
 
G_M000_IG08:
       mov      dword ptr [rbp-0xB4], 1
       jmp      SHORT G_M000_IG13
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG08
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG08
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG08
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       xor      eax, eax
       mov      dword ptr [rbp-0xB4], eax
 
G_M000_IG13:
       cmp      dword ptr [rbp-0xB4], 0
       je       SHORT G_M000_IG14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x20]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0x28], rax
       mov      rdi, gword ptr [rbp-0x28]
       call     [RyuPy.Runtime.PyAbc:IsAbc(RyuPy.Runtime.PyNativeType):bool]
       test     eax, eax
       je       SHORT G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x28]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x80]
       mov      rsi, gword ptr [rbp-0x10]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x88]
       mov      rsi, gword ptr [rbp-0x10]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyTuple:GetItem(System.Object):System.Object:this]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x90]
       mov      rsi, gword ptr [rbp-0x10]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyDict:Get(System.Object):System.Object:this]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x98]
       mov      rsi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyOps:GetString(System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG21:
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x30], rax
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x38], rax
       cmp      gword ptr [rbp-0x38], 0
       jne      SHORT G_M000_IG22
       mov      rdi, gword ptr [rbp-0x10]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      qword ptr [rbp-0xD8], rax
       mov      rsi, qword ptr [rbp-0xD8]
       mov      rdi, gword ptr [rbp-0x30]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBytes:GetItem(long):System.Object:this]
       mov      gword ptr [rbp-0xC8], rax
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x38]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyBytes:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyBytes:this]
       mov      gword ptr [rbp-0xC8], rax
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG24:
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x48], rax
       cmp      gword ptr [rbp-0x48], 0
       jne      SHORT G_M000_IG25
       mov      rdi, gword ptr [rbp-0x10]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      qword ptr [rbp-0xE0], rax
       mov      rsi, qword ptr [rbp-0xE0]
       mov      rdi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyByteArray:GetItem(long):System.Object:this]
       mov      gword ptr [rbp-0xD0], rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x48]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyByteArray:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyByteArray:this]
       mov      gword ptr [rbp-0xD0], rax
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG27:
       lea      rcx, [rbp-0x50]
       mov      rdi, gword ptr [rbp-0x08]
       mov      rdx, gword ptr [rbp-0x10]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:TryMethod1(System.Object,System.String,System.Object,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG28
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG29
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xC0], rax
       mov      edi, 0x129E
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x3930
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rsi, gword ptr [rbp-0xF0]
       mov      rdx, gword ptr [rbp-0xF8]
       call     [System.String:Concat(System.String,System.String,System.String):System.String]
       mov      gword ptr [rbp-0x100], rax
       mov      rsi, gword ptr [rbp-0x100]
       mov      rdi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xC0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x58]
 
G_M000_IG30:
       add      rsp, 256
       pop      rbp
       ret      
 
; Total bytes of code 1650

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 48
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
       mov      qword ptr [rbp-0x20], rax
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x18], rax
       cmp      gword ptr [rbp-0x18], 0
       je       SHORT G_M000_IG03
       mov      rdi, gword ptr [rbp-0x08]
       mov      rsi, gword ptr [rbp-0x18]
       call     [RyuPy.Runtime.PyList:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyList:this]
       mov      gword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyList:ToIndex(System.Object):long]
       mov      qword ptr [rbp-0x28], rax
       mov      rsi, qword ptr [rbp-0x28]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyList:GetItem(long):System.Object:this]
       mov      gword ptr [rbp-0x20], rax
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x20]
 
G_M000_IG05:
       add      rsp, 48
       pop      rbp
       ret      
 
; Total bytes of code 151

; Assembly listing for method RyuPy.Runtime.PyList:ToIndex(System.Object):long (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      qword ptr [rbp-0x10], rax
       mov      gword ptr [rbp-0x08], rdi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG03
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       movzx    rax, byte  ptr [rax]
       mov      dword ptr [rbp-0x0C], eax
       jmp      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG04
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x18], rax
       jmp      SHORT G_M000_IG06
 
G_M000_IG04:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG08
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x1C], eax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       cmp      dword ptr [rbp-0x0C], 0
       setne    al
       movzx    rax, al
       mov      eax, eax
       mov      qword ptr [rbp-0x28], rax
       jmp      G_M000_IG09
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x18]
       mov      qword ptr [rbp-0x28], rax
       jmp      G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       movsxd   rax, dword ptr [rbp-0x1C]
       mov      qword ptr [rbp-0x28], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x30], rax
       mov      edi, 0x5CBF
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x40]
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, gword ptr [rbp-0x48]
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x30]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x28]
 
G_M000_IG10:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 397

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(long):System.Object:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
       mov      gword ptr [rbp-0x08], rdi
       mov      qword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyList:get_Items():System.Collections.Generic.List`1[System.Object]:this]
       mov      gword ptr [rbp-0x18], rax
       mov      rdi, gword ptr [rbp-0x08]
       mov      rsi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyList:Normalize(long):long:this]
       mov      dword ptr [rbp-0x1C], eax
       mov      esi, dword ptr [rbp-0x1C]
       mov      rdi, gword ptr [rbp-0x18]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       nop      
 
G_M000_IG03:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 77

; Assembly listing for method RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 6354
; 3 inlinees with PGO data; 12 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 16
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x28], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       test     rbx, rbx
       je       SHORT G_M000_IG06
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG06
 
G_M000_IG04:
       mov      r14, rbx
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyEnum:IsEnumClass(System.Object):bool]
       test     eax, eax
       je       G_M000_IG41
 
G_M000_IG05:
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyEnum:GetItem(RyuPy.Runtime.PyClass,System.Object):System.Object]
       jmp      G_M000_IG42
 
G_M000_IG06:
       test     rbx, rbx
       je       G_M000_IG43
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG43
 
G_M000_IG08:
       mov      r14, rbx
       mov      r13, gword ptr [rbx+0x08]
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       call     [System.String:Equals(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG13
 
G_M000_IG09:
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       call     [System.String:Equals(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG13
 
G_M000_IG10:
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       call     [System.String:Equals(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       call     [System.String:Equals(System.String,System.String):bool]
       test     eax, eax
       jne      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       call     [System.String:Equals(System.String,System.String):bool]
       test     eax, eax
       je       G_M000_IG41
 
G_M000_IG13:
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       jmp      G_M000_IG42
 
G_M000_IG14:
       test     rbx, rbx
       je       G_M000_IG22
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG22
 
G_M000_IG16:
       test     r15, r15
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rsi, r15
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyList:this]
       jmp      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyList:ToIndex(System.Object):long]
       mov      rcx, gword ptr [rbx+0x08]
       mov      rdx, rcx
       test     rax, rax
       jl       G_M000_IG62
 
G_M000_IG20:
       test     rax, rax
       jl       G_M000_IG63
       mov      edi, dword ptr [rcx+0x10]
       movsxd   rcx, edi
       cmp      rax, rcx
       jge      G_M000_IG63
       cmp      eax, edi
       jae      G_M000_IG64
       mov      rcx, gword ptr [rdx+0x08]
       cmp      eax, dword ptr [rcx+0x08]
       jae      G_M000_IG69
       mov      eax, eax
       mov      rax, gword ptr [rcx+8*rax+0x10]
 
G_M000_IG21:
       jmp      G_M000_IG42
 
G_M000_IG22:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       xor      rdi, rdi
 
G_M000_IG25:
       test     rdi, rdi
       jne      G_M000_IG58
 
G_M000_IG26:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG29
 
G_M000_IG28:
       xor      rdi, rdi
 
G_M000_IG29:
       test     rdi, rdi
       jne      G_M000_IG57
 
G_M000_IG30:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG33
 
G_M000_IG31:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG33
 
G_M000_IG32:
       xor      rdi, rdi
 
G_M000_IG33:
       test     rdi, rdi
       jne      G_M000_IG56
 
G_M000_IG34:
       mov      r14, rbx
       test     r14, r14
       je       SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       xor      r14, r14
 
G_M000_IG37:
       test     r14, r14
       jne      G_M000_IG50
 
G_M000_IG38:
       mov      r14, rbx
       test     r14, r14
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG59
 
G_M000_IG40:
       test     r14, r14
       jne      SHORT G_M000_IG47
 
G_M000_IG41:
       lea      rcx, [rbp-0x28]
       mov      rdi, rbx
       mov      rdx, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:TryMethod1(System.Object,System.String,System.Object,byref):bool]
       test     eax, eax
       je       G_M000_IG68
       mov      rax, gword ptr [rbp-0x28]
 
G_M000_IG42:
       add      rsp, 16
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG43:
       test     rbx, rbx
       je       G_M000_IG14
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG14
 
G_M000_IG45:
       mov      r14, rbx
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyAbc:IsAbc(RyuPy.Runtime.PyNativeType):bool]
       test     eax, eax
       je       SHORT G_M000_IG41
 
G_M000_IG46:
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       jmp      SHORT G_M000_IG42
 
G_M000_IG47:
       mov      rsi, r15
       test     rsi, rsi
       jne      G_M000_IG60
 
G_M000_IG48:
       test     rsi, rsi
       jne      G_M000_IG67
       jmp      G_M000_IG66
 
G_M000_IG49:
       jmp      SHORT G_M000_IG42
 
G_M000_IG50:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG65
 
G_M000_IG52:
       test     rsi, rsi
       jne      SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      rsi, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyBytes:GetItem(long):System.Object:this]
 
G_M000_IG54:
       jmp      G_M000_IG42
 
G_M000_IG55:
       mov      rdi, r14
       call     [RyuPy.Runtime.PyBytes:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyBytes:this]
       jmp      SHORT G_M000_IG54
 
G_M000_IG56:
       mov      rsi, r15
       call     [RyuPy.Runtime.PyOps:GetString(System.String,System.Object):System.Object]
       jmp      G_M000_IG42
 
G_M000_IG57:
       mov      rsi, r15
       call     [RyuPy.Runtime.PyDict:Get(System.Object):System.Object:this]
       jmp      G_M000_IG42
 
G_M000_IG58:
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTuple:GetItem(System.Object):System.Object:this]
       jmp      G_M000_IG42
 
G_M000_IG59:
       xor      r14, r14
       jmp      G_M000_IG40
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       G_M000_IG48
 
G_M000_IG61:
       xor      rsi, rsi
       jmp      G_M000_IG48
 
G_M000_IG62:
       mov      edi, dword ptr [rcx+0x10]
       movsxd   rdi, edi
       add      rax, rdi
       jmp      G_M000_IG20
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x5C8F
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyIndexError:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG64:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG65:
       xor      rsi, rsi
       jmp      G_M000_IG52
 
G_M000_IG66:
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      rsi, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyByteArray:GetItem(long):System.Object:this]
       jmp      G_M000_IG49
 
G_M000_IG67:
       mov      rdi, r14
       call     [RyuPy.Runtime.PyByteArray:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyByteArray:this]
       jmp      G_M000_IG49
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x129E
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      rbx, rax
       mov      edi, 0x3930
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rdi, r15
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String,System.String):System.String]
       mov      rsi, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG69:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 1113

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 11116
; 2 inlinees with PGO data; 6 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     rbx
       push     rax
       lea      rbp, [rsp+0x10]
       mov      rbx, rdi
 
G_M000_IG02:
       test     rsi, rsi
       je       SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyList:this]
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rdi, rsi
       call     [RyuPy.Runtime.PyList:ToIndex(System.Object):long]
       mov      rcx, gword ptr [rbx+0x08]
       mov      rdx, rcx
       test     rax, rax
       jl       SHORT G_M000_IG08
 
G_M000_IG06:
       test     rax, rax
       jl       SHORT G_M000_IG09
       mov      edi, dword ptr [rcx+0x10]
       movsxd   rcx, edi
       cmp      rax, rcx
       jge      SHORT G_M000_IG09
       cmp      eax, edi
       jae      SHORT G_M000_IG10
       mov      rcx, gword ptr [rdx+0x08]
       cmp      eax, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG11
       mov      eax, eax
       mov      rax, gword ptr [rcx+8*rax+0x10]
 
G_M000_IG07:
       add      rsp, 8
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG08:
       mov      edi, dword ptr [rcx+0x10]
       movsxd   rdi, edi
       add      rax, rdi
       jmp      SHORT G_M000_IG06
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x5C8F
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyIndexError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG10:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG11:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 190

; Assembly listing for method RyuPy.Runtime.PyList:ToIndex(System.Object):long (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 11958

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     rbx
       push     rax
       lea      rbp, [rsp+0x20]
       mov      rbx, rdi
 
G_M000_IG02:
       test     rbx, rbx
       je       SHORT G_M000_IG07
 
G_M000_IG03:
       mov      rsi, qword ptr [rbx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       cmp      byte  ptr [rbx+0x08], 0
       setne    sil
       movzx    rsi, sil
       mov      eax, esi
       jmp      SHORT G_M000_IG11
 
G_M000_IG05:
       mov      rax, 0xD1FFAB1E
       cmp      rsi, rax
       jne      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rax, qword ptr [rbx+0x08]
       jmp      SHORT G_M000_IG11
 
G_M000_IG07:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       xor      rsi, rsi
 
G_M000_IG10:
       test     rsi, rsi
       je       SHORT G_M000_IG12
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       movsxd   rax, dword ptr [rax]
 
G_M000_IG11:
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x5CBF
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
; Total bytes of code 221

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(long):System.Object:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data
; 2 inlinees with PGO data; 5 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     rbx
       push     rax
       lea      rbp, [rsp+0x10]
 
G_M000_IG02:
       mov      rax, gword ptr [rdi+0x08]
       mov      rcx, rax
       test     rsi, rsi
       jl       SHORT G_M000_IG05
 
G_M000_IG03:
       test     rsi, rsi
       jl       SHORT G_M000_IG06
       mov      edx, dword ptr [rax+0x10]
       movsxd   rax, edx
       cmp      rsi, rax
       jge      SHORT G_M000_IG06
       cmp      esi, edx
       jae      SHORT G_M000_IG07
       mov      rax, gword ptr [rcx+0x08]
       cmp      esi, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG08
       mov      ecx, esi
       mov      rax, gword ptr [rax+8*rcx+0x10]
 
G_M000_IG04:
       add      rsp, 8
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG05:
       mov      edx, dword ptr [rax+0x10]
       movsxd   rdx, edx
       add      rsi, rdx
       jmp      SHORT G_M000_IG03
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x5C8F
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyIndexError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG07:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG08:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 147

