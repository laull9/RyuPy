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
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       xor      eax, eax
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
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x10], rax
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
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x14], eax
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
       movzx    rax, byte  ptr [rax]
       mov      dword ptr [rbp-0x18], eax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x10]
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG06:
       movsxd   rax, dword ptr [rbp-0x14]
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       cmp      dword ptr [rbp-0x18], 0
       setne    al
       movzx    rax, al
       mov      eax, eax
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyList:ToIndexSlow(System.Object):long]
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG09:
       mov      rax, qword ptr [rbp-0x20]
 
G_M000_IG10:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 251

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(long):System.Object:this (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 48
       lea      rbp, [rsp+0x30]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x30], ymm8
       mov      gword ptr [rbp-0x08], rdi
       mov      qword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rax, gword ptr [rbp-0x08]
       mov      rax, gword ptr [rax+0x08]
       mov      gword ptr [rbp-0x18], rax
       cmp      gword ptr [rbp-0x18], 0
       jne      SHORT G_M000_IG04
       mov      rdi, gword ptr [rbp-0x08]
       mov      rsi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyList:ReadNumeric(long):System.Object:this]
       nop      
 
G_M000_IG03:
       add      rsp, 48
       pop      rbp
       ret      
 
G_M000_IG04:
       mov      rdi, gword ptr [rbp-0x18]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Count():int:this]
       mov      dword ptr [rbp-0x1C], eax
       cmp      qword ptr [rbp-0x10], 0
       jl       SHORT G_M000_IG05
       mov      rax, qword ptr [rbp-0x10]
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       movsxd   rax, dword ptr [rbp-0x1C]
       add      rax, qword ptr [rbp-0x10]
       mov      qword ptr [rbp-0x30], rax
 
G_M000_IG06:
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x28], rax
       movsxd   rax, dword ptr [rbp-0x1C]
       cmp      rax, qword ptr [rbp-0x28]
       ja       SHORT G_M000_IG08
       call     [RyuPy.Runtime.PyList:ListIndexError():System.Object]
       nop      
 
G_M000_IG07:
       add      rsp, 48
       pop      rbp
       ret      
 
G_M000_IG08:
       mov      rdi, gword ptr [rbp-0x18]
       mov      esi, dword ptr [rbp-0x28]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       nop      
 
G_M000_IG09:
       add      rsp, 48
       pop      rbp
       ret      
 
; Total bytes of code 165

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
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 46 inlinees with PGO data; 81 single block inlinees; 125 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 784
       mov      qword ptr [rsp+0x688], r15
       mov      qword ptr [rsp+0x680], r14
       mov      qword ptr [rsp+0x678], r13
       mov      qword ptr [rsp+0x670], r12
       mov      qword ptr [rsp+0x668], rbx
       lea      rbp, [rsp+0x310]
       xor      eax, eax
       mov      qword ptr [rbp-0x288], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x280], xmm8
       mov      rax, -576
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
       jmp      G_M000_IG42
 
G_M000_IG03:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       vmovaps  xmm2, xmm0
       jmp      G_M000_IG30
 
G_M000_IG04:
       mov      rsi, gword ptr [rbx+0x10]
       test     rsi, rsi
       jne      G_M000_IG33
 
G_M000_IG05:
       mov      rsi, gword ptr [rbx+0x08]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG396
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edi, edi
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG34
 
G_M000_IG07:
       xor      edi, edi
 
G_M000_IG08:
       test     edi, edi
       je       G_M000_IG68
 
G_M000_IG09:
       test     r15, r15
       je       G_M000_IG68
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG68
 
G_M000_IG11:
       mov      rdi, qword ptr [rax+0x08]
       mov      rsi, gword ptr [r15+0x08]
       test     rsi, rsi
       jne      G_M000_IG35
 
G_M000_IG12:
       mov      rsi, gword ptr [r15+0x10]
       mov      esi, dword ptr [rsi+0x10]
 
G_M000_IG13:
       test     rdi, rdi
       jl       G_M000_IG36
 
G_M000_IG14:
       movsxd   rsi, esi
       cmp      rdi, rsi
       jae      G_M000_IG68
 
G_M000_IG15:
       mov      rsi, gword ptr [r15+0x10]
       test     rsi, rsi
       jne      G_M000_IG37
 
G_M000_IG16:
       mov      rsi, gword ptr [r15+0x08]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG396
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edi, edi
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG38
 
G_M000_IG18:
       xor      edi, edi
 
G_M000_IG19:
       test     edi, edi
       je       G_M000_IG68
 
G_M000_IG20:
       mov      gword ptr [rbp+0x320], rax
       mov      rsi, qword ptr [rax+0x08]
       mov      rdi, gword ptr [rbx+0x08]
       test     rdi, rdi
       jne      G_M000_IG169
 
G_M000_IG21:
       mov      rdi, gword ptr [rbx+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG22:
       test     rsi, rsi
       jl       G_M000_IG170
 
G_M000_IG23:
       movsxd   rdi, edi
       cmp      rsi, rdi
       jb       G_M000_IG171
 
G_M000_IG24:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       vmovaps  xmm1, xmm0
 
G_M000_IG25:
       vmovsd   qword ptr [rbp+0xD8], xmm1
       mov      rax, gword ptr [rbp+0x320]
       mov      rax, qword ptr [rax+0x08]
       mov      rdi, gword ptr [r15+0x08]
       test     rdi, rdi
       jne      G_M000_IG178
 
G_M000_IG26:
       mov      rdi, gword ptr [r15+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG27:
       test     rax, rax
       jl       G_M000_IG179
 
G_M000_IG28:
       movsxd   rdi, edi
       cmp      rax, rdi
       jb       G_M000_IG180
 
G_M000_IG29:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       vmovaps  xmm2, xmm0
 
G_M000_IG30:
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   xmm1, qword ptr [rbp+0xD8]
       jmp      G_M000_IG42
 
G_M000_IG31:
       mov      esi, dword ptr [rsi+0x10]
       jmp      G_M000_IG66
 
G_M000_IG32:
       movsxd   r9, esi
       add      rdi, r9
       jmp      G_M000_IG67
 
G_M000_IG33:
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG396
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edi, edi
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG395
 
G_M000_IG34:
       mov      edi, 1
       jmp      G_M000_IG08
 
G_M000_IG35:
       mov      esi, dword ptr [rsi+0x10]
       jmp      G_M000_IG13
 
G_M000_IG36:
       movsxd   r9, esi
       add      rdi, r9
       jmp      G_M000_IG14
 
G_M000_IG37:
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG396
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edi, edi
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG394
 
G_M000_IG38:
       mov      edi, 1
       jmp      G_M000_IG19
 
G_M000_IG39:
       test     rax, rax
       je       G_M000_IG106
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      rax, gword ptr [rbp+0x228]
       mov      rcx, gword ptr [rbp+0x230]
       jne      G_M000_IG72
 
G_M000_IG41:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   xmm2, qword ptr [rsi+0x08]
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
 
G_M000_IG42:
       vmulsd   xmm1, xmm1, xmm2
       vaddsd   xmm0, xmm1, xmm0
       vmovsd   qword ptr [rbp+0xF8], xmm0
       xor      r11, r11
       mov      gword ptr [rbp+0x328], r11
       jmp      G_M000_IG52
 
G_M000_IG43:
       mov      gword ptr [rbp+0x328], rdx
 
G_M000_IG44:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r12, rax
 
G_M000_IG45:
       mov      rax, gword ptr [rbp+0x328]
       test     rax, rax
       je       SHORT G_M000_IG49
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG140
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG151
 
G_M000_IG48:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       G_M000_IG156
 
G_M000_IG49:
       mov      rdx, r12
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG50:
       mov      r12, rax
       test     r12, r12
       jne      G_M000_IG166
 
G_M000_IG51:
       xor      eax, eax
       mov      gword ptr [rbp+0x328], r12
       mov      r12d, eax
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
 
G_M000_IG52:
       add      r13, rcx
       jo       G_M000_IG398
 
G_M000_IG53:
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
       je       G_M000_IG78
 
G_M000_IG54:
       lea      rsi, [r13+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG105
 
G_M000_IG55:
       mov      qword ptr [rbp+0x268], rax
       mov      qword ptr [rbp+0x260], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG56:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r9, rsi
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [r9+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [r9+4*r8+0x10], 44
       je       SHORT G_M000_IG59
 
G_M000_IG58:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 44
 
G_M000_IG59:
       test     r12d, r12d
       je       SHORT G_M000_IG68
 
G_M000_IG60:
       test     rbx, rbx
       je       SHORT G_M000_IG68
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG68
 
G_M000_IG62:
       test     rax, rax
       je       SHORT G_M000_IG68
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG68
 
G_M000_IG64:
       mov      rdi, qword ptr [rax+0x08]
       mov      rsi, gword ptr [rbx+0x08]
       test     rsi, rsi
       jne      G_M000_IG31
 
G_M000_IG65:
       mov      rsi, gword ptr [rbx+0x10]
       mov      esi, dword ptr [rsi+0x10]
 
G_M000_IG66:
       test     rdi, rdi
       jl       G_M000_IG32
 
G_M000_IG67:
       movsxd   rsi, esi
       cmp      rdi, rsi
       jb       G_M000_IG04
 
G_M000_IG68:
       mov      rdi, rbx
       mov      gword ptr [rbp+0x320], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x230], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x228], rax
       mov      rcx, gword ptr [rbp+0x230]
       mov      rdx, rcx
       mov      rsi, rax
       test     r12d, r12d
       je       SHORT G_M000_IG72
 
G_M000_IG69:
       test     rcx, rcx
       je       SHORT G_M000_IG72
 
G_M000_IG70:
       mov      r8, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      r8, rdi
       je       G_M000_IG39
 
G_M000_IG71:
       mov      rcx, gword ptr [rbp+0x230]
 
G_M000_IG72:
       mov      rdx, gword ptr [rbp+0x328]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x328], rdx
       mov      rax, gword ptr [rbp+0x228]
       mov      rcx, gword ptr [rbp+0x230]
       mov      rdx, gword ptr [rbp+0x328]
 
G_M000_IG74:
       test     rcx, rcx
       je       G_M000_IG43
 
G_M000_IG75:
       mov      r8, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      r8, rdi
       je       G_M000_IG107
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       cmp      r8, rdi
       je       G_M000_IG134
 
G_M000_IG77:
       mov      gword ptr [rbp+0x328], rdx
       jmp      G_M000_IG44
 
G_M000_IG78:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r13, gword ptr [rbp+0x328]
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG92
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG81
 
G_M000_IG80:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x2D0], rax
       je       SHORT G_M000_IG82
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG82:
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x2D0]
       mov      rsi, gword ptr [rbp+0x338]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG83:
       mov      r12, qword ptr [rbp+0x2D8]
       mov      rax, qword ptr [rbp+0x2C8]
       add      r12, rax
       jo       G_M000_IG398
       mov      qword ptr [rbp+0x2C8], rax
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       mov      rcx, qword ptr [rbp+0x2D0]
       cmp      r12, rcx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2D8], r12
       mov      qword ptr [rbp+0x2D0], rcx
       cmp      r12, rcx
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG186
 
G_M000_IG84:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG87
 
G_M000_IG85:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       lea      r8d, [rdi-0x01]
       mov      r9d, r8d
       cmp      r9d, dword ptr [rdx+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rdx+4*r9+0x10], 42
       je       SHORT G_M000_IG87
 
G_M000_IG86:
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edx, r8d
       mov      dword ptr [rsi+4*rdx+0x10], 42
 
G_M000_IG87:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x328], rsi
       test     edi, edi
       jle      SHORT G_M000_IG90
 
G_M000_IG88:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [r8+4*r9+0x10], 43
       je       SHORT G_M000_IG90
 
G_M000_IG89:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       mov      gword ptr [rbp+0x290], r8
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2A0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x2A0]
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
       jne      G_M000_IG102
 
G_M000_IG91:
       xor      r8d, r8d
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG53
 
G_M000_IG92:
       mov      r13, gword ptr [rbp+0x338]
       mov      r12, r13
       test     r12, r12
       jne      SHORT G_M000_IG94
 
G_M000_IG93:
       call     [RyuPy.Runtime.PyNumericListOps:AppendError():System.Object]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG83
 
G_M000_IG94:
       mov      rax, gword ptr [r12+0x10]
       mov      gword ptr [rbp-0x2D8], rax
       test     rax, rax
       je       G_M000_IG99
 
G_M000_IG95:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG368
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      r12, qword ptr [rbp+0xF8]
       mov      rax, gword ptr [rbp-0x2D8]
       inc      dword ptr [rax+0x14]
       mov      rdi, gword ptr [rax+0x08]
       mov      ecx, dword ptr [rax+0x10]
       cmp      dword ptr [rdi+0x08], ecx
       jbe      SHORT G_M000_IG98
 
G_M000_IG97:
       lea      edx, [rcx+0x01]
       mov      dword ptr [rax+0x10], edx
       mov      eax, ecx
       shl      rax, 4
       lea      rax, bword ptr [rdi+rax+0x10]
       mov      bword ptr [rbp-0x310], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x310]
       mov      qword ptr [rsi+0x08], r12
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG83
 
G_M000_IG98:
       mov      rdx, r12
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG83
 
G_M000_IG99:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r12+0x08]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG101
 
G_M000_IG100:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       mov      esi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG83
 
G_M000_IG101:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG83
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      r8d, 1
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG53
 
G_M000_IG104:
       xor      r8d, r8d
       mov      gword ptr [rbp+0x328], rsi
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG53
 
G_M000_IG105:
       test     rsi, rsi
       jl       G_M000_IG398
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG397
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      rsi, rdi
       mov      qword ptr [rbp+0x268], rax
       mov      qword ptr [rbp+0x260], rcx
       mov      rax, rsi
       jmp      G_M000_IG56
 
G_M000_IG106:
       mov      rcx, gword ptr [rbp+0x230]
       jmp      G_M000_IG72
 
G_M000_IG107:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG109
 
G_M000_IG108:
       mov      gword ptr [rbp+0x328], rdx
       jmp      G_M000_IG44
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG132
 
G_M000_IG110:
       mov      gword ptr [rbp+0x328], rdx
       mov      r12, qword ptr [rax+0x08]
       lea      rdi, [rbp-0x88]
       mov      rdx, rsi
       mulx     rcx, rax, r12
       mov      qword ptr [rdi], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r12
       sub      rcx, rdi
       mov      rdi, r12
       sar      rdi, 63
       and      rdi, rsi
       sub      rcx, rdi
       mov      rdi, rax
       sar      rdi, 63
       cmp      rcx, rdi
       je       G_M000_IG128
 
G_M000_IG111:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x80], xmm1
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x2F8], rax
       mov      ecx, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x28C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       lea      rdi, [rbp-0x98]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x98]
       mov      r9d, dword ptr [rbp-0x90]
       mov      rax, gword ptr [rbp-0x2F8]
       mov      rsi, rax
       or       rsi, r12
       jne      G_M000_IG119
 
G_M000_IG112:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       movsxd   rsi, dword ptr [rbp-0x28C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xA8]
       mov      eax, dword ptr [rbp-0xA0]
 
G_M000_IG113:
       mov      dword ptr [rbp-0x290], eax
       test     r12, r12
       jne      G_M000_IG369
 
G_M000_IG114:
       mov      rdi, r12
       test     rdi, rdi
       jne      G_M000_IG378
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG126
 
G_M000_IG115:
       mov      edi, -1
 
G_M000_IG116:
       test     edi, edi
       jg       SHORT G_M000_IG127
 
G_M000_IG117:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2B8], rax
       mov      rdi, r12
       mov      esi, dword ptr [rbp-0x290]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x2B8]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG118:
       jmp      G_M000_IG45
 
G_M000_IG119:
       test     rax, rax
       je       SHORT G_M000_IG124
 
G_M000_IG120:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG121:
       test     r12, r12
       je       SHORT G_M000_IG125
 
G_M000_IG122:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG123:
       mov      edx, dword ptr [rbp-0x28C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      eax, edx
       jmp      G_M000_IG113
 
G_M000_IG124:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG121
 
G_M000_IG125:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG123
 
G_M000_IG126:
       xor      edi, edi
       jmp      SHORT G_M000_IG116
 
G_M000_IG127:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2B0], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x2B0]
       mov      edi, dword ptr [rbp-0x290]
       mov      dword ptr [r12+0x10], edi
       jmp      SHORT G_M000_IG118
 
G_M000_IG128:
       mov      r12, rax
       lea      rax, [r12+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG131
 
G_M000_IG129:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG130:
       jmp      G_M000_IG118
 
G_M000_IG131:
       test     rax, rax
       jl       G_M000_IG398
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      r9, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG397
       mov      r12, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG130
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x328], rdx
       jne      G_M000_IG44
 
G_M000_IG133:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0xB0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG45
 
G_M000_IG134:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       test     rax, rax
       je       G_M000_IG139
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      gword ptr [rbp+0x328], rdx
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG45
 
G_M000_IG137:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x328], rdx
       jne      G_M000_IG44
 
G_M000_IG138:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG45
 
G_M000_IG139:
       mov      gword ptr [rbp+0x328], rdx
       jmp      G_M000_IG44
 
G_M000_IG140:
       mov      rsi, qword ptr [rax+0x08]
       test     r12, r12
       je       G_M000_IG49
 
G_M000_IG141:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG149
 
G_M000_IG142:
       mov      r12, qword ptr [r12+0x08]
       lea      rax, [rsi+r12]
       mov      qword ptr [rbp-0xD8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r12
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG145
 
G_M000_IG143:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       lea      rdi, [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x300], rax
       mov      ecx, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0x294], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x300]
       mov      esi, dword ptr [rbp-0x294]
       mov      rdx, gword ptr [rbp-0xF8]
       mov      rcx, qword ptr [rbp-0xF0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG144:
       jmp      G_M000_IG50
 
G_M000_IG145:
       lea      r12, [rax+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG148
 
G_M000_IG146:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp-0xD8]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG147:
       jmp      SHORT G_M000_IG144
 
G_M000_IG148:
       test     r12, r12
       jl       G_M000_IG398
       mov      rax, r12
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG397
       test     r12, r12
       jl       G_M000_IG398
       mov      rdi, r12
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG147
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG49
 
G_M000_IG150:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xD0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG50
 
G_M000_IG151:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       test     r12, r12
       je       G_M000_IG49
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG154
 
G_M000_IG153:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG50
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG49
 
G_M000_IG155:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG50
 
G_M000_IG156:
       mov      rcx, rax
       mov      gword ptr [rbp-0x2C0], rcx
       test     r12, r12
       je       G_M000_IG49
 
G_M000_IG157:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r12], rsi
       jne      G_M000_IG49
 
G_M000_IG158:
       cmp      dword ptr [rcx+0x08], 0
       je       G_M000_IG163
 
G_M000_IG159:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG162
 
G_M000_IG160:
       mov      eax, dword ptr [rcx+0x08]
       mov      dword ptr [rbp-0x10C], eax
       mov      esi, eax
       add      esi, dword ptr [r12+0x08]
       js       G_M000_IG391
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x2C8], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rcx, gword ptr [rbp-0x2C0]
       lea      rsi, bword ptr [rcx+0x0C]
       mov      edx, dword ptr [rcx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x10C]
       mov      rax, gword ptr [rbp-0x2C8]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       lea      rsi, bword ptr [r12+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r12, gword ptr [rbp-0x2C8]
       mov      rax, r12
 
G_M000_IG161:
       jmp      G_M000_IG50
 
G_M000_IG162:
       mov      rax, rcx
       jmp      SHORT G_M000_IG161
 
G_M000_IG163:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG165
 
G_M000_IG164:
       mov      rax, r12
       jmp      SHORT G_M000_IG161
 
G_M000_IG165:
       mov      rax, 0xD1FFAB1E
       jmp      SHORT G_M000_IG161
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG168
 
G_M000_IG167:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      gword ptr [rbp+0x328], r12
       mov      r12d, eax
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG52
 
G_M000_IG168:
       xor      eax, eax
       mov      gword ptr [rbp+0x328], r12
       mov      r12d, eax
       mov      rax, qword ptr [rbp+0x268]
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG52
 
G_M000_IG169:
       mov      edi, dword ptr [rdi+0x10]
       jmp      G_M000_IG22
 
G_M000_IG170:
       movsxd   r9, edi
       add      rsi, r9
       jmp      G_M000_IG23
 
G_M000_IG171:
       mov      r9, gword ptr [rbx+0x10]
       test     r9, r9
       jne      SHORT G_M000_IG176
 
G_M000_IG172:
       mov      rdi, gword ptr [rbx+0x08]
       cmp      esi, dword ptr [rdi+0x10]
       jae      G_M000_IG396
       mov      rdi, gword ptr [rdi+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      esi, esi
       mov      rdi, gword ptr [rdi+8*rsi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG175
 
G_M000_IG173:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       jne      SHORT G_M000_IG175
 
G_M000_IG174:
       vmovsd   xmm1, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG177
 
G_M000_IG175:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       vmovaps  xmm1, xmm0
       jmp      G_M000_IG25
 
G_M000_IG176:
       cmp      esi, dword ptr [r9+0x10]
       jae      G_M000_IG396
       mov      rdi, gword ptr [r9+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      esi, esi
       shl      rsi, 4
       lea      rdi, bword ptr [rdi+rsi+0x10]
       mov      rsi, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     rsi, rsi
       je       G_M000_IG392
       vmovq    xmm1, rdi
 
G_M000_IG177:
       jmp      G_M000_IG25
 
G_M000_IG178:
       mov      edi, dword ptr [rdi+0x10]
       jmp      G_M000_IG27
 
G_M000_IG179:
       movsxd   rsi, edi
       add      rax, rsi
       jmp      G_M000_IG28
 
G_M000_IG180:
       mov      rsi, gword ptr [r15+0x10]
       test     rsi, rsi
       jne      SHORT G_M000_IG184
 
G_M000_IG181:
       mov      rdi, gword ptr [r15+0x08]
       cmp      eax, dword ptr [rdi+0x10]
       jae      G_M000_IG396
       mov      rdi, gword ptr [rdi+0x08]
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      eax, eax
       mov      rdi, gword ptr [rdi+8*rax+0x10]
       test     rdi, rdi
       je       G_M000_IG03
 
G_M000_IG182:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      G_M000_IG03
 
G_M000_IG183:
       vmovsd   xmm2, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG185
 
G_M000_IG184:
       cmp      eax, dword ptr [rsi+0x10]
       jae      G_M000_IG396
       mov      rdi, gword ptr [rsi+0x08]
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      eax, eax
       shl      rax, 4
       lea      rdi, bword ptr [rdi+rax+0x10]
       mov      rax, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     rax, rax
       je       G_M000_IG393
       vmovq    xmm2, rdi
 
G_M000_IG185:
       jmp      G_M000_IG30
 
G_M000_IG186:
       mov      r13, gword ptr [rbp+0x338]
       mov      edi, dword ptr [(reloc)]
       mov      ebx, edi
       test     ebx, ebx
       jle      SHORT G_M000_IG189
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rdi+4*rcx+0x10], 46
       je       SHORT G_M000_IG189
 
G_M000_IG188:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 46
 
G_M000_IG189:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       lea      rdi, bword ptr [r15+0x10]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       test     ebx, ebx
       jle      SHORT G_M000_IG192
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rdi+4*rcx+0x10], 48
       je       SHORT G_M000_IG192
 
G_M000_IG191:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 48
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rbx, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG354
 
G_M000_IG193:
       mov      rax, gword ptr [r15+0x10]
       mov      gword ptr [rbp-0x2F0], rax
       test     rax, rax
       je       G_M000_IG282
 
G_M000_IG194:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x288], xmm0
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG390
 
G_M000_IG195:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       lea      rdi, [rbp-0x288]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       call     [RyuPy.Runtime.PyNumericSlot:.ctor(System.Object,double):this]
 
G_M000_IG196:
       vmovdqu  xmm0, xmmword ptr [rbp-0x288]
       vmovdqu  xmmword ptr [rbp-0x278], xmm0
 
G_M000_IG197:
       mov      r8, gword ptr [rbp-0x2F0]
       inc      dword ptr [r8+0x14]
       mov      rdi, gword ptr [r8+0x08]
       mov      esi, dword ptr [r8+0x10]
       cmp      dword ptr [rdi+0x08], esi
       jbe      G_M000_IG281
 
G_M000_IG198:
       lea      eax, [rsi+0x01]
       mov      dword ptr [r8+0x10], eax
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      esi, esi
       shl      rsi, 4
       lea      rdi, bword ptr [rdi+rsi+0x10]
       lea      rsi, bword ptr [rbp-0x278]
       call     CORINFO_HELP_ASSIGN_BYREF
       movsq    
       jmp      G_M000_IG353
 
G_M000_IG199:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x268], xmm0
       mov      rdi, gword ptr [rbp-0x2E8]
       mov      rax, gword ptr [rdi+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG205
 
G_M000_IG200:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG202
 
G_M000_IG201:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG206
 
G_M000_IG202:
       xor      eax, eax
 
G_M000_IG203:
       test     eax, eax
       jne      SHORT G_M000_IG207
 
G_M000_IG204:
       mov      rax, gword ptr [rbp+0x320]
       jmp      G_M000_IG269
 
G_M000_IG205:
       mov      rdi, rax
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x268], rax
       mov      qword ptr [rbp-0x260], rdx
       lea      rdi, [rbp-0x268]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG206:
       mov      eax, 1
       jmp      SHORT G_M000_IG203
 
G_M000_IG207:
       test     r14, r14
       je       G_M000_IG241
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       je       SHORT G_M000_IG210
 
G_M000_IG209:
       mov      rax, gword ptr [rbp+0x320]
       jmp      G_M000_IG269
 
G_M000_IG210:
       mov      rax, gword ptr [rbp+0x320]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x240], rcx
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x240]
       test     rsi, rsi
       jl       SHORT G_M000_IG213
 
G_M000_IG211:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG214
 
G_M000_IG212:
       mov      rax, gword ptr [rbp+0x320]
       jmp      G_M000_IG269
 
G_M000_IG213:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      SHORT G_M000_IG211
 
G_M000_IG214:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       jne      SHORT G_M000_IG220
 
G_M000_IG215:
       mov      rdi, gword ptr [r14+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG217
 
G_M000_IG216:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG221
 
G_M000_IG217:
       xor      eax, eax
 
G_M000_IG218:
       test     eax, eax
       jne      SHORT G_M000_IG222
 
G_M000_IG219:
       mov      rax, gword ptr [rbp+0x320]
       jmp      G_M000_IG269
 
G_M000_IG220:
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x250], rax
       mov      qword ptr [rbp-0x248], rdx
       lea      rdi, [rbp-0x250]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG221:
       mov      eax, 1
       jmp      SHORT G_M000_IG218
 
G_M000_IG222:
       mov      rax, r13
       mov      gword ptr [rbp-0x2E0], rax
       mov      rcx, gword ptr [rbp+0x320]
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x210], rdx
       mov      rdi, rax
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x210]
       test     rsi, rsi
       jl       SHORT G_M000_IG229
 
G_M000_IG223:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG230
 
G_M000_IG224:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG225:
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rax, gword ptr [rbp+0x320]
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x228], rax
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x228]
       test     rsi, rsi
       jl       G_M000_IG235
 
G_M000_IG226:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       G_M000_IG236
 
G_M000_IG227:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG228:
       vmovaps  xmm1, xmm0
       mov      esi, dword ptr [rbp+0x9C]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       jmp      G_M000_IG245
 
G_M000_IG229:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      SHORT G_M000_IG223
 
G_M000_IG230:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
       mov      rdi, gword ptr [rbp-0x2E0]
       mov      rax, gword ptr [rdi+0x10]
       test     rax, rax
       jne      G_M000_IG386
 
G_M000_IG231:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG233
 
G_M000_IG232:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG387
 
G_M000_IG233:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      G_M000_IG225
 
G_M000_IG234:
       jmp      G_M000_IG225
 
G_M000_IG235:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      G_M000_IG226
 
G_M000_IG236:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       jne      G_M000_IG388
 
G_M000_IG237:
       mov      rdi, gword ptr [r14+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG239
 
G_M000_IG238:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG389
 
G_M000_IG239:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      G_M000_IG228
 
G_M000_IG240:
       jmp      G_M000_IG228
 
G_M000_IG241:
       mov      rax, gword ptr [rbp+0x320]
       jmp      G_M000_IG269
 
G_M000_IG242:
       test     rax, rax
       je       G_M000_IG293
 
G_M000_IG243:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       mov      rax, gword ptr [rbp+0x130]
       mov      rcx, gword ptr [rbp+0x138]
       jne      G_M000_IG273
 
G_M000_IG244:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
 
G_M000_IG245:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      rdx, rdx
       mov      dword ptr [rbp+0x9C], esi
       mov      rax, rdx
       jmp      G_M000_IG255
 
G_M000_IG246:
       mov      gword ptr [rbp+0x310], rdx
 
G_M000_IG247:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG248:
       mov      rax, gword ptr [rbp+0x310]
       test     rax, rax
       je       SHORT G_M000_IG252
 
G_M000_IG249:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG325
 
G_M000_IG250:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG338
 
G_M000_IG251:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG343
 
G_M000_IG252:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG253:
       test     rax, rax
       jne      G_M000_IG346
 
G_M000_IG254:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x9C], ecx
 
G_M000_IG255:
       mov      rdi, qword ptr [rbp+0x178]
       add      rdi, 1
       jo       G_M000_IG398
       mov      qword ptr [rbp+0x178], rdi
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
 
G_M000_IG256:
       mov      rcx, qword ptr [rbp+0x178]
       cmp      rcx, qword ptr [rbp+0x170]
       jge      G_M000_IG351
 
G_M000_IG257:
       mov      qword ptr [rbp+0x178], rcx
       lea      r8, [rcx+0x05]
       cmp      r8, 261
       jbe      G_M000_IG280
 
G_M000_IG258:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, qword ptr [rbp+0x178]
       mov      qword ptr [rax+0x08], rcx
 
G_M000_IG259:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG262
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rdi+4*r8+0x10], 51
       je       SHORT G_M000_IG262
 
G_M000_IG261:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      dword ptr [rdi+4*r8+0x10], 51
 
G_M000_IG262:
       cmp      dword ptr [rbp+0x9C], 0
       je       SHORT G_M000_IG269
 
G_M000_IG263:
       mov      r8, r13
       mov      gword ptr [rbp-0x2E8], r8
       test     r8, r8
       je       SHORT G_M000_IG269
 
G_M000_IG264:
       test     rax, rax
       je       SHORT G_M000_IG269
 
G_M000_IG265:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG269
 
G_M000_IG266:
       mov      gword ptr [rbp+0x320], rax
       mov      r9, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x258], r9
       mov      rdi, r8
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x258]
       test     rsi, rsi
       jl       G_M000_IG284
 
G_M000_IG267:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       G_M000_IG199
 
G_M000_IG268:
       mov      rax, gword ptr [rbp+0x320]
 
G_M000_IG269:
       mov      rdi, r13
       mov      gword ptr [rbp+0x320], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x138], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x130], rax
       mov      rcx, gword ptr [rbp+0x138]
       mov      rdi, rcx
       mov      rdx, rax
       mov      esi, dword ptr [rbp+0x9C]
       test     esi, esi
       je       SHORT G_M000_IG273
 
G_M000_IG270:
       test     rcx, rcx
       je       SHORT G_M000_IG273
 
G_M000_IG271:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       je       G_M000_IG242
 
G_M000_IG272:
       mov      rcx, gword ptr [rbp+0x138]
 
G_M000_IG273:
       mov      rdx, gword ptr [rbp+0x310]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     esi, edi
       je       SHORT G_M000_IG275
 
G_M000_IG274:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x310], rdx
       mov      rax, gword ptr [rbp+0x130]
       mov      rcx, gword ptr [rbp+0x138]
       mov      rdx, gword ptr [rbp+0x310]
 
G_M000_IG275:
       test     rcx, rcx
       je       G_M000_IG246
 
G_M000_IG276:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG294
 
G_M000_IG277:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG316
 
G_M000_IG278:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG322
 
G_M000_IG279:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG247
 
G_M000_IG280:
       test     r8, r8
       jl       G_M000_IG398
       mov      rdi, r8
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG397
       test     r8, r8
       jl       G_M000_IG398
       mov      rdi, r8
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, rdi
       jmp      G_M000_IG259
 
G_M000_IG281:
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdx, qword ptr [rbp-0x270]
       mov      rdi, r8
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       jmp      G_M000_IG353
 
G_M000_IG282:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r15+0x08]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG285
 
G_M000_IG283:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       movsxd   rsi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       jmp      G_M000_IG353
 
G_M000_IG284:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      G_M000_IG267
 
G_M000_IG285:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      G_M000_IG353
 
G_M000_IG286:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG289
 
G_M000_IG287:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rsi+4*rdi+0x10], 53
       je       SHORT G_M000_IG289
 
G_M000_IG288:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG397
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG289:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG292
 
G_M000_IG290:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG292
 
G_M000_IG291:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG292:
       add      rsp, 0x668
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG293:
       mov      rcx, gword ptr [rbp+0x138]
       jmp      G_M000_IG273
 
G_M000_IG294:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG296
 
G_M000_IG295:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG247
 
G_M000_IG296:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG314
 
G_M000_IG297:
       mov      gword ptr [rbp+0x310], rdx
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x130], rax
       lea      rdi, [rbp-0x148]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x148]
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
       je       G_M000_IG310
 
G_M000_IG298:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x140], xmm1
       lea      rdi, [rbp-0x140]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0x308], rax
       mov      ecx, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x298], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x158], xmm0
       lea      rdi, [rbp-0x158]
       mov      rsi, qword ptr [rbp-0x130]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x158]
       mov      r9d, dword ptr [rbp-0x150]
       mov      rcx, gword ptr [rbp-0x308]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG303
 
G_M000_IG299:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x178], xmm0
       movsxd   rsi, dword ptr [rbp-0x298]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x178]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG300:
       vmovdqu  xmm0, xmmword ptr [rbp-0x178]
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
 
G_M000_IG301:
       mov      rdi, gword ptr [rbp-0x168]
       mov      rsi, qword ptr [rbp-0x160]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG302:
       mov      rdx, rax
       jmp      G_M000_IG248
 
G_M000_IG303:
       test     rcx, rcx
       je       SHORT G_M000_IG308
 
G_M000_IG304:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG305:
       test     rax, rax
       je       SHORT G_M000_IG309
 
G_M000_IG306:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG307:
       mov      edx, dword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x168], rax
       mov      qword ptr [rbp-0x160], rdx
       jmp      SHORT G_M000_IG301
 
G_M000_IG308:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG305
 
G_M000_IG309:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG307
 
G_M000_IG310:
       mov      rax, rcx
       mov      qword ptr [rbp-0x180], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG313
 
G_M000_IG311:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x180]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG312:
       jmp      SHORT G_M000_IG302
 
G_M000_IG313:
       test     rcx, rcx
       jl       G_M000_IG398
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG397
       test     rcx, rcx
       jl       G_M000_IG398
       mov      rdi, rcx
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG312
 
G_M000_IG314:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rdx
       jne      G_M000_IG247
 
G_M000_IG315:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x128], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x188], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x128]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG248
 
G_M000_IG316:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       test     rax, rax
       je       G_M000_IG321
 
G_M000_IG317:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG319
 
G_M000_IG318:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x118]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x120]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG248
 
G_M000_IG319:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rdx
       jne      G_M000_IG247
 
G_M000_IG320:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x118]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG248
 
G_M000_IG321:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG247
 
G_M000_IG322:
       test     rax, rax
       je       SHORT G_M000_IG324
 
G_M000_IG323:
       mov      rsi, qword ptr [rax]
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG247
 
G_M000_IG324:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG247
 
G_M000_IG325:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG252
 
G_M000_IG326:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG336
 
G_M000_IG327:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1B0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1B8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG332
 
G_M000_IG328:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
       lea      rdi, [rbp-0x1C8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG329:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1C8]
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
 
G_M000_IG330:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1F8], xmm0
       lea      rdi, [rbp-0x1F8]
       mov      rsi, qword ptr [rbp-0x1B0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1D8]
       mov      rsi, qword ptr [rbp-0x1D0]
       mov      rdx, gword ptr [rbp-0x1F8]
       mov      rcx, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1E8], rax
       mov      qword ptr [rbp-0x1E0], rdx
       mov      rdi, gword ptr [rbp-0x1E8]
       mov      rsi, qword ptr [rbp-0x1E0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG331:
       jmp      G_M000_IG253
 
G_M000_IG332:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG335
 
G_M000_IG333:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1B8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG334:
       jmp      SHORT G_M000_IG331
 
G_M000_IG335:
       test     rax, rax
       jl       G_M000_IG398
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r9, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG397
       test     rax, rax
       jl       G_M000_IG398
       mov      rdi, rax
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG334
 
G_M000_IG336:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG252
 
G_M000_IG337:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG253
 
G_M000_IG338:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm0
       test     rdx, rdx
       je       G_M000_IG252
 
G_M000_IG339:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG341
 
G_M000_IG340:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG253
 
G_M000_IG341:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG252
 
G_M000_IG342:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG253
 
G_M000_IG343:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG252
 
G_M000_IG344:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG252
 
G_M000_IG345:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG253
 
G_M000_IG346:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG350
 
G_M000_IG347:
       test     rax, rax
       setne    sil
       movzx    rsi, sil
       mov      dword ptr [rbp+0x9C], esi
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG349
 
G_M000_IG348:
       mov      gword ptr [rbp+0x310], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp+0x310]
 
G_M000_IG349:
       mov      gword ptr [rbp+0x310], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, gword ptr [rbp+0x310]
       jmp      G_M000_IG255
 
G_M000_IG350:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x9C], ecx
       jmp      G_M000_IG255
 
G_M000_IG351:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       sete     sil
       movzx    rsi, sil
       test     dword ptr [rbp+0x9C], esi
       jne      G_M000_IG193
 
G_M000_IG352:
       mov      rsi, r15
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
 
G_M000_IG353:
       add      r12, 1
       jo       G_M000_IG398
 
G_M000_IG354:
       cmp      r12, rbx
       jge      G_M000_IG286
 
G_M000_IG355:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG358
 
G_M000_IG356:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rdi+4*rsi+0x10], 49
       je       SHORT G_M000_IG358
 
G_M000_IG357:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      dword ptr [rdi+4*rsi+0x10], 49
 
G_M000_IG358:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x310], rax
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG361
 
G_M000_IG359:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       cmp      dword ptr [rdi+4*rsi+0x10], 50
       je       SHORT G_M000_IG361
 
G_M000_IG360:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG397
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG361:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x198], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2A8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x2A8]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x190], rax
       mov      rdi, gword ptr [rbp+0x198]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x188], rax
       mov      rdi, gword ptr [rbp+0x190]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0x188]
       mov      qword ptr [rbp+0x178], rcx
       mov      qword ptr [rbp+0x170], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp+0x310]
       test     rdx, rdx
       je       G_M000_IG367
 
G_M000_IG362:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG366
 
G_M000_IG363:
       test     rdx, rdx
       setne    r8b
       movzx    r8, r8b
       mov      dword ptr [rbp+0x9C], r8d
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG365
 
G_M000_IG364:
       mov      gword ptr [rbp+0x310], rdx
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rdx, gword ptr [rbp+0x310]
 
G_M000_IG365:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdx, gword ptr [rbp+0x310]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       jmp      G_M000_IG256
 
G_M000_IG366:
       xor      r8d, r8d
       xor      esi, esi
       mov      dword ptr [rbp+0x9C], esi
       jmp      G_M000_IG256
 
G_M000_IG367:
       xor      esi, esi
       mov      dword ptr [rbp+0x9C], esi
       jmp      G_M000_IG256
 
G_M000_IG368:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_GCSTATIC_BASE]
       jmp      G_M000_IG96
 
G_M000_IG369:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG370
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG371
 
G_M000_IG370:
       mov      ecx, eax
       jmp      SHORT G_M000_IG377
 
G_M000_IG371:
       cmp      edi, 2
       je       SHORT G_M000_IG372
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG397
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG373
 
G_M000_IG372:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG373:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG374
       mov      ecx, -1
       jmp      SHORT G_M000_IG376
 
G_M000_IG374:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG375
       mov      ecx, 1
       jmp      SHORT G_M000_IG376
 
G_M000_IG375:
       xor      ecx, ecx
 
G_M000_IG376:
       imul     ecx, eax
 
G_M000_IG377:
       test     ecx, ecx
       jge      G_M000_IG114
       jmp      G_M000_IG127
 
G_M000_IG378:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG379
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG380
 
G_M000_IG379:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x290]
       jmp      G_M000_IG116
 
G_M000_IG380:
       cmp      edi, 2
       je       SHORT G_M000_IG381
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG397
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG382
 
G_M000_IG381:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG382:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG383
       mov      ecx, -1
       jmp      SHORT G_M000_IG385
 
G_M000_IG383:
       test     rdi, rdi
       jge      SHORT G_M000_IG384
       mov      ecx, 1
       jmp      SHORT G_M000_IG385
 
G_M000_IG384:
       xor      ecx, ecx
 
G_M000_IG385:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG116
 
G_M000_IG386:
       mov      rdi, rax
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x220], rax
       mov      qword ptr [rbp-0x218], rdx
       lea      rdi, [rbp-0x220]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
       jmp      G_M000_IG234
 
G_M000_IG387:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      G_M000_IG234
 
G_M000_IG388:
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x238], rax
       mov      qword ptr [rbp-0x230], rdx
       lea      rdi, [rbp-0x238]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
       jmp      G_M000_IG240
 
G_M000_IG389:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      G_M000_IG240
 
G_M000_IG390:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_GCSTATIC_BASE]
       jmp      G_M000_IG195
 
G_M000_IG391:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG392:
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
 
G_M000_IG393:
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
 
G_M000_IG394:
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
 
G_M000_IG395:
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
 
G_M000_IG396:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG397:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG398:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 9889

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x410
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 46 inlinees with PGO data; 81 single block inlinees; 125 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 752
       mov      qword ptr [rsp+0x668], r15
       mov      qword ptr [rsp+0x660], r14
       mov      qword ptr [rsp+0x658], r13
       mov      qword ptr [rsp+0x650], r12
       mov      qword ptr [rsp+0x648], rbx
       lea      rbp, [rsp+0x2F0]
       xor      eax, eax
       mov      qword ptr [rbp-0x278], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -576
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
       mov      r12, gword ptr [rbp+0x230]
       mov      edx, dword ptr [rbp+0xF4]
 
G_M000_IG02:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     edx, edi
       jne      G_M000_IG44
 
G_M000_IG03:
       test     r12, r12
       je       SHORT G_M000_IG07
 
G_M000_IG04:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r12], rdx
       je       G_M000_IG45
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       je       G_M000_IG71
 
G_M000_IG06:
       mov      rcx, gword ptr [rbp+0x228]
       mov      rdx, qword ptr [r12]
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rcx, gword ptr [rbp+0x228]
 
G_M000_IG08:
       mov      rdx, rcx
       mov      rsi, r12
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG09:
       mov      r12, rdx
       test     r13, r13
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       G_M000_IG76
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       G_M000_IG87
 
G_M000_IG12:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       je       G_M000_IG92
 
G_M000_IG13:
       mov      rdx, r12
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG14:
       test     r13, r13
       jne      G_M000_IG102
 
G_M000_IG15:
       xor      r12d, r12d
 
G_M000_IG16:
       mov      rax, qword ptr [rbp+0x270]
       mov      rcx, qword ptr [rbp+0x260]
       add      rax, rcx
       jo       G_M000_IG393
 
G_M000_IG17:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0x268]
       cmp      rax, rdx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      rax, rdx
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG105
 
G_M000_IG18:
       mov      qword ptr [rbp+0x270], rax
       lea      rsi, [rax+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG131
 
G_M000_IG19:
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, qword ptr [rbp+0x270]
       mov      qword ptr [rax+0x08], rcx
 
G_M000_IG20:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [r8+4*r9+0x10], 44
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 44
 
G_M000_IG23:
       test     r12d, r12d
       je       G_M000_IG37
 
G_M000_IG24:
       test     rbx, rbx
       je       G_M000_IG37
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG37
 
G_M000_IG26:
       test     rax, rax
       je       G_M000_IG37
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG37
 
G_M000_IG28:
       mov      rdi, qword ptr [rax+0x08]
       mov      rsi, gword ptr [rbx+0x08]
       test     rsi, rsi
       jne      G_M000_IG154
 
G_M000_IG29:
       mov      rsi, gword ptr [rbx+0x10]
       mov      esi, dword ptr [rsi+0x10]
 
G_M000_IG30:
       test     rdi, rdi
       jl       G_M000_IG155
 
G_M000_IG31:
       movsxd   rsi, esi
       cmp      rdi, rsi
       jae      SHORT G_M000_IG37
 
G_M000_IG32:
       mov      rsi, gword ptr [rbx+0x10]
       test     rsi, rsi
       jne      G_M000_IG156
 
G_M000_IG33:
       mov      rsi, gword ptr [rbx+0x08]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG391
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG35
 
G_M000_IG34:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG157
 
G_M000_IG35:
       xor      edi, edi
 
G_M000_IG36:
       test     edi, edi
       jne      G_M000_IG132
 
G_M000_IG37:
       mov      rdi, rbx
       mov      gword ptr [rbp+0x320], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x230], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rax, gword ptr [rbp+0x230]
       mov      rdx, rax
       mov      rdi, rcx
       test     r12d, r12d
       mov      gword ptr [rbp+0x228], rcx
       mov      gword ptr [rbp+0xE0], rdx
       mov      gword ptr [rbp+0xD0], rdi
       mov      edx, r12d
       mov      r12, rax
       je       G_M000_IG02
 
G_M000_IG38:
       test     r12, r12
       je       G_M000_IG02
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG02
 
G_M000_IG40:
       mov      rcx, gword ptr [rbp+0x228]
       test     rcx, rcx
       mov      gword ptr [rbp+0x228], rcx
       je       G_M000_IG02
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rbp+0x228]
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp+0x228], rcx
       jne      G_M000_IG02
 
G_M000_IG42:
       mov      rcx, gword ptr [rbp+0xE0]
       mov      r13, gword ptr [rbp+0xD0]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   xmm2, qword ptr [r13+0x08]
 
G_M000_IG43:
       vmulsd   xmm1, xmm1, xmm2
       vaddsd   xmm0, xmm1, qword ptr [rbp+0xF8]
       xor      r13, r13
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, edx
       jmp      G_M000_IG16
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [r13+0x08], xmm0
       jmp      G_M000_IG03
 
G_M000_IG45:
       mov      rdx, qword ptr [r12+0x08]
       mov      rcx, gword ptr [rbp+0x228]
       test     rcx, rcx
       je       G_M000_IG08
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG69
 
G_M000_IG47:
       mov      r12, qword ptr [rcx+0x08]
       lea      rdi, [rbp-0x88]
       mulx     r10, rcx, r12
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x88]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, r12
       sub      r10, rdi
       mov      rdi, r12
       sar      rdi, 63
       and      rdi, rdx
       sub      r10, rdi
       mov      rdi, rcx
       sar      rdi, 63
       cmp      r10, rdi
       je       G_M000_IG65
 
G_M000_IG48:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x80], xmm1
       lea      rdi, [rbp-0x80]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x2E0], rax
       mov      ecx, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x27C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       lea      rdi, [rbp-0x98]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x98]
       mov      r9d, dword ptr [rbp-0x90]
       mov      rax, gword ptr [rbp-0x2E0]
       mov      rsi, rax
       or       rsi, r12
       jne      G_M000_IG56
 
G_M000_IG49:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       movsxd   rsi, dword ptr [rbp-0x27C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xA8]
       mov      eax, dword ptr [rbp-0xA0]
 
G_M000_IG50:
       mov      dword ptr [rbp-0x280], eax
       mov      rdi, r12
       test     rdi, rdi
       jne      G_M000_IG333
 
G_M000_IG51:
       mov      rdi, r12
       test     rdi, rdi
       jne      G_M000_IG342
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG63
 
G_M000_IG52:
       mov      edi, -1
 
G_M000_IG53:
       test     edi, edi
       jg       G_M000_IG64
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2A8], rax
       mov      rdi, r12
       mov      esi, dword ptr [rbp-0x280]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x2A8]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG55:
       mov      rdx, r12
       jmp      G_M000_IG09
 
G_M000_IG56:
       test     rax, rax
       je       SHORT G_M000_IG61
 
G_M000_IG57:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG58:
       test     r12, r12
       je       SHORT G_M000_IG62
 
G_M000_IG59:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG60:
       mov      edx, dword ptr [rbp-0x27C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      eax, edx
       jmp      G_M000_IG50
 
G_M000_IG61:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG58
 
G_M000_IG62:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG60
 
G_M000_IG63:
       xor      edi, edi
       jmp      G_M000_IG53
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2A0], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x2A0]
       mov      edi, dword ptr [rbp-0x280]
       mov      dword ptr [r12+0x10], edi
       jmp      G_M000_IG55
 
G_M000_IG65:
       mov      r12, rcx
       lea      rcx, [r12+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG68
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG67:
       jmp      G_M000_IG55
 
G_M000_IG68:
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rdi, rcx
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG392
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rdi, rcx
       mov      r12, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG67
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG08
 
G_M000_IG70:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rdx
       vmovsd   qword ptr [rbp-0xB0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       mov      rdx, r12
       jmp      G_M000_IG09
 
G_M000_IG71:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       mov      rcx, gword ptr [rbp+0x228]
       test     rcx, rcx
       je       G_M000_IG08
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG74
 
G_M000_IG73:
       vmovsd   xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       mov      rdx, r12
       jmp      G_M000_IG09
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG08
 
G_M000_IG75:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       mov      rdx, r12
       jmp      G_M000_IG09
 
G_M000_IG76:
       mov      rsi, qword ptr [r13+0x08]
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG85
 
G_M000_IG78:
       mov      r12, qword ptr [r12+0x08]
       lea      r13, [rsi+r12]
       mov      rdi, rsi
       xor      rdi, r13
       mov      rax, r12
       xor      rax, r13
       and      rdi, rax
       jge      SHORT G_M000_IG81
 
G_M000_IG79:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0xE0]
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0x284], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0x284]
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG80:
       mov      r13, rax
       jmp      G_M000_IG14
 
G_M000_IG81:
       lea      r12, [r13+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG84
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG83:
       jmp      SHORT G_M000_IG80
 
G_M000_IG84:
       test     r12, r12
       jl       G_M000_IG393
       mov      rax, r12
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG392
       test     r12, r12
       jl       G_M000_IG393
       mov      rdi, r12
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG83
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG13
 
G_M000_IG86:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xD0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG14
 
G_M000_IG87:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG90
 
G_M000_IG89:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG14
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG13
 
G_M000_IG91:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG14
 
G_M000_IG92:
       mov      rax, r13
       mov      gword ptr [rbp-0x2B0], rax
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG93:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r12], rsi
       jne      G_M000_IG13
 
G_M000_IG94:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG100
 
G_M000_IG95:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG99
 
G_M000_IG96:
       mov      r13d, dword ptr [rax+0x08]
       mov      esi, r13d
       add      esi, dword ptr [r12+0x08]
       js       G_M000_IG180
 
G_M000_IG97:
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x2B8], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rcx, gword ptr [rbp-0x2B0]
       lea      rsi, bword ptr [rcx+0x0C]
       mov      edx, dword ptr [rcx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       movsxd   rdi, r13d
       mov      r13, gword ptr [rbp-0x2B8]
       lea      rdi, bword ptr [r13+2*rdi+0x0C]
       lea      rsi, bword ptr [r12+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG98:
       jmp      G_M000_IG14
 
G_M000_IG99:
       mov      r13, rax
       jmp      SHORT G_M000_IG98
 
G_M000_IG100:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG350
 
G_M000_IG101:
       mov      r13, r12
       jmp      SHORT G_M000_IG98
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      r12d, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       jmp      G_M000_IG16
 
G_M000_IG104:
       xor      r12d, r12d
       jmp      G_M000_IG16
 
G_M000_IG105:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG119
 
G_M000_IG106:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG108
 
G_M000_IG107:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x2C0], rax
       je       SHORT G_M000_IG109
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG109:
       mov      rdi, gword ptr [rbp-0x2C0]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x2C0]
       mov      rsi, gword ptr [rbp+0x338]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG110:
       mov      r12, qword ptr [rbp+0x2D8]
       mov      rax, qword ptr [rbp+0x2C8]
       add      r12, rax
       jo       G_M000_IG393
       mov      qword ptr [rbp+0x2C8], rax
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       mov      rcx, qword ptr [rbp+0x2D0]
       cmp      r12, rcx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2D0], rcx
       cmp      r12, rcx
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG181
 
G_M000_IG111:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG114
 
G_M000_IG112:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       lea      r8d, [rdi-0x01]
       mov      r9d, r8d
       cmp      r9d, dword ptr [rdx+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdx+4*r9+0x10], 42
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edx, r8d
       mov      dword ptr [rsi+4*rdx+0x10], 42
 
G_M000_IG114:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x328], rsi
       test     edi, edi
       jle      SHORT G_M000_IG117
 
G_M000_IG115:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [r8+4*r9+0x10], 43
       je       SHORT G_M000_IG117
 
G_M000_IG116:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG117:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       mov      gword ptr [rbp+0x290], r8
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x290], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x290]
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
       mov      rax, qword ptr [rbp+0x280]
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x328]
       test     rsi, rsi
       jne      G_M000_IG128
 
G_M000_IG118:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x2D8], r12
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG17
 
G_M000_IG119:
       mov      r13, gword ptr [rbp+0x338]
       mov      r12, r13
       test     r12, r12
       jne      SHORT G_M000_IG121
 
G_M000_IG120:
       call     [RyuPy.Runtime.PyNumericListOps:AppendError():System.Object]
       jmp      G_M000_IG110
 
G_M000_IG121:
       mov      rdi, gword ptr [r12+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG125
 
G_M000_IG122:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r12, qword ptr [rbp+0xF8]
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      ecx, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], ecx
       jbe      SHORT G_M000_IG124
 
G_M000_IG123:
       lea      edx, [rcx+0x01]
       mov      dword ptr [rdi+0x10], edx
       mov      edi, ecx
       shl      rdi, 4
       lea      rax, bword ptr [rax+rdi+0x10]
       mov      bword ptr [rbp-0x2F0], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x2F0]
       mov      qword ptr [rsi+0x08], r12
       jmp      G_M000_IG110
 
G_M000_IG124:
       mov      rdx, r12
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       jmp      G_M000_IG110
 
G_M000_IG125:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r12+0x08]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG127
 
G_M000_IG126:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       mov      esi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       jmp      G_M000_IG110
 
G_M000_IG127:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      G_M000_IG110
 
G_M000_IG128:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      qword ptr [rbp+0x2D8], r12
       mov      r12d, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG17
 
G_M000_IG130:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x2D8], r12
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12d, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG17
 
G_M000_IG131:
       test     rsi, rsi
       jl       G_M000_IG393
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG392
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       mov      r8, rdi
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       mov      rax, r8
       jmp      G_M000_IG20
 
G_M000_IG132:
       test     r15, r15
       je       G_M000_IG37
 
G_M000_IG133:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG37
 
G_M000_IG134:
       mov      rdi, qword ptr [rax+0x08]
       mov      rsi, gword ptr [r15+0x08]
       test     rsi, rsi
       jne      G_M000_IG158
 
G_M000_IG135:
       mov      rsi, gword ptr [r15+0x10]
       mov      esi, dword ptr [rsi+0x10]
 
G_M000_IG136:
       test     rdi, rdi
       jl       G_M000_IG159
 
G_M000_IG137:
       movsxd   rsi, esi
       cmp      rdi, rsi
       jae      G_M000_IG37
 
G_M000_IG138:
       mov      rsi, gword ptr [r15+0x10]
       test     rsi, rsi
       jne      G_M000_IG160
 
G_M000_IG139:
       mov      rsi, gword ptr [r15+0x08]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG391
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG141
 
G_M000_IG140:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG161
 
G_M000_IG141:
       xor      edi, edi
 
G_M000_IG142:
       test     edi, edi
       je       G_M000_IG37
 
G_M000_IG143:
       mov      gword ptr [rbp+0x320], rax
       mov      rsi, qword ptr [rax+0x08]
       mov      rdi, gword ptr [rbx+0x08]
       test     rdi, rdi
       jne      G_M000_IG162
 
G_M000_IG144:
       mov      rdi, gword ptr [rbx+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG145:
       test     rsi, rsi
       jl       G_M000_IG163
 
G_M000_IG146:
       movsxd   rdi, edi
       cmp      rsi, rdi
       jb       G_M000_IG164
 
G_M000_IG147:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG148:
       vmovsd   qword ptr [rbp+0xD8], xmm0
       mov      rax, gword ptr [rbp+0x320]
       mov      rax, qword ptr [rax+0x08]
       mov      r13, gword ptr [r15+0x08]
       mov      rdi, r13
       test     rdi, rdi
       jne      G_M000_IG171
 
G_M000_IG149:
       mov      rdi, gword ptr [r15+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG150:
       test     rax, rax
       jl       G_M000_IG172
 
G_M000_IG151:
       movsxd   rdi, edi
       cmp      rax, rdi
       jb       G_M000_IG173
 
G_M000_IG152:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG153:
       mov      edx, r12d
       vmovaps  xmm2, xmm0
       vmovsd   xmm1, qword ptr [rbp+0xD8]
       jmp      G_M000_IG43
 
G_M000_IG154:
       mov      esi, dword ptr [rsi+0x10]
       jmp      G_M000_IG30
 
G_M000_IG155:
       movsxd   r8, esi
       add      rdi, r8
       jmp      G_M000_IG31
 
G_M000_IG156:
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG391
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG390
 
G_M000_IG157:
       mov      edi, 1
       jmp      G_M000_IG36
 
G_M000_IG158:
       mov      esi, dword ptr [rsi+0x10]
       jmp      G_M000_IG136
 
G_M000_IG159:
       movsxd   r8, esi
       add      rdi, r8
       jmp      G_M000_IG137
 
G_M000_IG160:
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG391
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG389
 
G_M000_IG161:
       mov      edi, 1
       jmp      G_M000_IG142
 
G_M000_IG162:
       mov      edi, dword ptr [rdi+0x10]
       jmp      G_M000_IG145
 
G_M000_IG163:
       movsxd   r8, edi
       add      rsi, r8
       jmp      G_M000_IG146
 
G_M000_IG164:
       mov      r13, gword ptr [rbx+0x10]
       test     r13, r13
       jne      SHORT G_M000_IG169
 
G_M000_IG165:
       mov      rdi, gword ptr [rbx+0x08]
       cmp      esi, dword ptr [rdi+0x10]
       jae      G_M000_IG391
       mov      rdi, gword ptr [rdi+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      esi, esi
       mov      rdi, gword ptr [rdi+8*rsi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG168
 
G_M000_IG166:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       jne      SHORT G_M000_IG168
 
G_M000_IG167:
       vmovsd   xmm1, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG170
 
G_M000_IG168:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      G_M000_IG148
 
G_M000_IG169:
       cmp      esi, dword ptr [r13+0x10]
       jae      G_M000_IG391
       mov      rdi, gword ptr [r13+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      esi, esi
       shl      rsi, 4
       lea      rdi, bword ptr [rdi+rsi+0x10]
       mov      rsi, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     rsi, rsi
       je       G_M000_IG387
       vmovq    xmm1, rdi
 
G_M000_IG170:
       vmovaps  xmm0, xmm1
       jmp      G_M000_IG148
 
G_M000_IG171:
       mov      edi, dword ptr [rdi+0x10]
       jmp      G_M000_IG150
 
G_M000_IG172:
       movsxd   rsi, edi
       add      rax, rsi
       jmp      G_M000_IG151
 
G_M000_IG173:
       mov      rsi, gword ptr [r15+0x10]
       test     rsi, rsi
       jne      SHORT G_M000_IG178
 
G_M000_IG174:
       cmp      eax, dword ptr [r13+0x10]
       jae      G_M000_IG391
       mov      rdi, gword ptr [r13+0x08]
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      eax, eax
       mov      rdi, gword ptr [rdi+8*rax+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG177
 
G_M000_IG175:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG177
 
G_M000_IG176:
       vmovsd   xmm1, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG179
 
G_M000_IG177:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      G_M000_IG153
 
G_M000_IG178:
       cmp      eax, dword ptr [rsi+0x10]
       jae      G_M000_IG391
       mov      rdi, gword ptr [rsi+0x08]
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      esi, eax
       shl      rsi, 4
       lea      rdi, bword ptr [rdi+rsi+0x10]
       mov      rsi, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     rsi, rsi
       je       G_M000_IG388
       vmovq    xmm1, rdi
 
G_M000_IG179:
       vmovaps  xmm0, xmm1
       jmp      G_M000_IG153
 
G_M000_IG180:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG181:
       mov      r13, gword ptr [rbp+0x338]
       mov      edi, dword ptr [(reloc)]
       mov      ebx, edi
       test     ebx, ebx
       jle      SHORT G_M000_IG184
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rcx+0x10], 46
       je       SHORT G_M000_IG184
 
G_M000_IG183:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 46
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       lea      rdi, bword ptr [r15+0x10]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       test     ebx, ebx
       jle      SHORT G_M000_IG187
 
G_M000_IG185:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rcx+0x10], 48
       je       SHORT G_M000_IG187
 
G_M000_IG186:
       mov      rdi, rsi
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 48
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rbx, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG220
 
G_M000_IG188:
       mov      gword ptr [rbp+0x310], rdx
 
G_M000_IG189:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG190:
       mov      rax, gword ptr [rbp+0x310]
       test     rax, rax
       je       SHORT G_M000_IG194
 
G_M000_IG191:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG297
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG310
 
G_M000_IG193:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG315
 
G_M000_IG194:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG195:
       test     rax, rax
       jne      G_M000_IG318
 
G_M000_IG196:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x9C], ecx
 
G_M000_IG197:
       mov      rdi, qword ptr [rbp+0x178]
       add      rdi, 1
       jo       G_M000_IG393
       mov      qword ptr [rbp+0x178], rdi
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
 
G_M000_IG198:
       mov      rcx, qword ptr [rbp+0x178]
       cmp      rcx, qword ptr [rbp+0x170]
       jge      G_M000_IG217
 
G_M000_IG199:
       mov      qword ptr [rbp+0x178], rcx
       lea      r8, [rcx+0x05]
       cmp      r8, 261
       jbe      G_M000_IG243
 
G_M000_IG200:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, qword ptr [rbp+0x178]
       mov      qword ptr [rax+0x08], rcx
       mov      rsi, rax
 
G_M000_IG201:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG204
 
G_M000_IG202:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*r8+0x10], 51
       je       SHORT G_M000_IG204
 
G_M000_IG203:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rdi+4*r8+0x10], 51
 
G_M000_IG204:
       cmp      dword ptr [rbp+0x9C], 0
       jne      G_M000_IG244
 
G_M000_IG205:
       mov      rdi, r13
       mov      gword ptr [rbp+0x320], rsi
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x138], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x130], rax
       mov      rcx, gword ptr [rbp+0x138]
       mov      rdi, rcx
       mov      rdx, rax
       mov      esi, dword ptr [rbp+0x9C]
       test     esi, esi
       je       SHORT G_M000_IG209
 
G_M000_IG206:
       test     rcx, rcx
       je       SHORT G_M000_IG209
 
G_M000_IG207:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       je       G_M000_IG239
 
G_M000_IG208:
       mov      rcx, gword ptr [rbp+0x138]
 
G_M000_IG209:
       mov      rdx, gword ptr [rbp+0x310]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     esi, edi
       je       SHORT G_M000_IG211
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x310], rdx
       mov      rax, gword ptr [rbp+0x130]
       mov      rcx, gword ptr [rbp+0x138]
       mov      rdx, gword ptr [rbp+0x310]
 
G_M000_IG211:
       test     rcx, rcx
       je       G_M000_IG188
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG266
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG288
 
G_M000_IG214:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG294
 
G_M000_IG215:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG189
 
G_M000_IG216:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x9C], ecx
       jmp      G_M000_IG197
 
G_M000_IG217:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       sete     sil
       movzx    rsi, sil
       test     dword ptr [rbp+0x9C], esi
       jne      G_M000_IG232
 
G_M000_IG218:
       mov      rsi, r15
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
 
G_M000_IG219:
       add      r12, 1
       jo       G_M000_IG393
 
G_M000_IG220:
       cmp      r12, rbx
       jge      G_M000_IG326
 
G_M000_IG221:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG224
 
G_M000_IG222:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rsi+0x10], 49
       je       SHORT G_M000_IG224
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rdi+4*rsi+0x10], 49
 
G_M000_IG224:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x310], rax
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG227
 
G_M000_IG225:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rsi+0x10], 50
       je       SHORT G_M000_IG227
 
G_M000_IG226:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG227:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x198], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x298], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x298]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x190], rax
       mov      rdi, gword ptr [rbp+0x198]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x188], rax
       mov      rdi, gword ptr [rbp+0x190]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0x188]
       mov      qword ptr [rbp+0x178], rcx
       mov      qword ptr [rbp+0x170], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp+0x310]
       test     rdx, rdx
       je       G_M000_IG238
 
G_M000_IG228:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG237
 
G_M000_IG229:
       test     rdx, rdx
       setne    r8b
       movzx    r8, r8b
       mov      dword ptr [rbp+0x9C], r8d
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG231
 
G_M000_IG230:
       mov      gword ptr [rbp+0x310], rdx
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rdx, gword ptr [rbp+0x310]
 
G_M000_IG231:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdx, gword ptr [rbp+0x310]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       jmp      G_M000_IG198
 
G_M000_IG232:
       mov      rax, gword ptr [r15+0x10]
       mov      gword ptr [rbp-0x2D8], rax
       test     rax, rax
       je       G_M000_IG323
 
G_M000_IG233:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x278], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       lea      rdi, [rbp-0x278]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       call     [RyuPy.Runtime.PyNumericSlot:.ctor(System.Object,double):this]
 
G_M000_IG234:
       vmovdqu  xmm0, xmmword ptr [rbp-0x278]
       vmovdqu  xmmword ptr [rbp-0x268], xmm0
 
G_M000_IG235:
       mov      r8, gword ptr [rbp-0x2D8]
       inc      dword ptr [r8+0x14]
       mov      rdi, gword ptr [r8+0x08]
       mov      esi, dword ptr [r8+0x10]
       cmp      dword ptr [rdi+0x08], esi
       jbe      G_M000_IG322
 
G_M000_IG236:
       lea      eax, [rsi+0x01]
       mov      dword ptr [r8+0x10], eax
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      esi, esi
       shl      rsi, 4
       lea      rdi, bword ptr [rdi+rsi+0x10]
       lea      rsi, bword ptr [rbp-0x268]
       call     CORINFO_HELP_ASSIGN_BYREF
       movsq    
       jmp      G_M000_IG219
 
G_M000_IG237:
       xor      r8d, r8d
       xor      esi, esi
       mov      dword ptr [rbp+0x9C], esi
       jmp      G_M000_IG198
 
G_M000_IG238:
       xor      esi, esi
       mov      dword ptr [rbp+0x9C], esi
       jmp      G_M000_IG198
 
G_M000_IG239:
       test     rax, rax
       je       G_M000_IG265
 
G_M000_IG240:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       mov      rax, gword ptr [rbp+0x130]
       mov      rcx, gword ptr [rbp+0x138]
       jne      G_M000_IG209
 
G_M000_IG241:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
 
G_M000_IG242:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      rdx, rdx
       mov      dword ptr [rbp+0x9C], esi
       mov      rax, rdx
       jmp      G_M000_IG197
 
G_M000_IG243:
       test     r8, r8
       jl       G_M000_IG393
       mov      rdi, r8
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG392
       test     r8, r8
       jl       G_M000_IG393
       mov      rdi, r8
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rsi, rdi
       jmp      G_M000_IG201
 
G_M000_IG244:
       mov      r9, r13
       mov      gword ptr [rbp-0x2D0], r9
       test     r9, r9
       je       G_M000_IG205
 
G_M000_IG245:
       test     rsi, rsi
       je       G_M000_IG205
 
G_M000_IG246:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG205
 
G_M000_IG247:
       mov      gword ptr [rbp+0x320], rsi
       mov      r10, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x248], r10
       mov      rdi, r9
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x248]
       test     rsi, rsi
       jl       SHORT G_M000_IG250
 
G_M000_IG248:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG251
 
G_M000_IG249:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG205
 
G_M000_IG250:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      SHORT G_M000_IG248
 
G_M000_IG251:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rax, gword ptr [rdi+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG257
 
G_M000_IG252:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG254
 
G_M000_IG253:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG258
 
G_M000_IG254:
       xor      eax, eax
 
G_M000_IG255:
       test     eax, eax
       jne      SHORT G_M000_IG259
 
G_M000_IG256:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG205
 
G_M000_IG257:
       mov      rdi, rax
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x258], rax
       mov      qword ptr [rbp-0x250], rdx
       lea      rdi, [rbp-0x258]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG258:
       mov      eax, 1
       jmp      SHORT G_M000_IG255
 
G_M000_IG259:
       test     r14, r14
       je       SHORT G_M000_IG264
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       je       SHORT G_M000_IG262
 
G_M000_IG261:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG205
 
G_M000_IG262:
       mov      rax, gword ptr [rbp+0x320]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x230], rcx
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x230]
       test     rsi, rsi
       jl       G_M000_IG377
       jmp      G_M000_IG376
 
G_M000_IG263:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jae      G_M000_IG380
       jmp      G_M000_IG378
 
G_M000_IG264:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG205
 
G_M000_IG265:
       mov      rcx, gword ptr [rbp+0x138]
       jmp      G_M000_IG209
 
G_M000_IG266:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG268
 
G_M000_IG267:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG189
 
G_M000_IG268:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG286
 
G_M000_IG269:
       mov      gword ptr [rbp+0x310], rdx
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x120], rax
       lea      rdi, [rbp-0x138]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x138]
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
       je       G_M000_IG282
 
G_M000_IG270:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x130], xmm1
       lea      rdi, [rbp-0x130]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x2E8], rax
       mov      ecx, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x288], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x148], xmm0
       lea      rdi, [rbp-0x148]
       mov      rsi, qword ptr [rbp-0x120]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x148]
       mov      r9d, dword ptr [rbp-0x140]
       mov      rcx, gword ptr [rbp-0x2E8]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG275
 
G_M000_IG271:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       movsxd   rsi, dword ptr [rbp-0x288]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x168]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG272:
       vmovdqu  xmm0, xmmword ptr [rbp-0x168]
       vmovdqu  xmmword ptr [rbp-0x158], xmm0
 
G_M000_IG273:
       mov      rdi, gword ptr [rbp-0x158]
       mov      rsi, qword ptr [rbp-0x150]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG274:
       mov      rdx, rax
       jmp      G_M000_IG190
 
G_M000_IG275:
       test     rcx, rcx
       je       SHORT G_M000_IG280
 
G_M000_IG276:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG277:
       test     rax, rax
       je       SHORT G_M000_IG281
 
G_M000_IG278:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG279:
       mov      edx, dword ptr [rbp-0x288]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x158], rax
       mov      qword ptr [rbp-0x150], rdx
       jmp      SHORT G_M000_IG273
 
G_M000_IG280:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG277
 
G_M000_IG281:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG279
 
G_M000_IG282:
       mov      rax, rcx
       mov      qword ptr [rbp-0x170], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG285
 
G_M000_IG283:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x170]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG284:
       jmp      SHORT G_M000_IG274
 
G_M000_IG285:
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG392
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rdi, rcx
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG284
 
G_M000_IG286:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rdx
       jne      G_M000_IG189
 
G_M000_IG287:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x178], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x178]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG190
 
G_M000_IG288:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       test     rax, rax
       je       G_M000_IG293
 
G_M000_IG289:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG291
 
G_M000_IG290:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x108]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG190
 
G_M000_IG291:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rdx
       jne      G_M000_IG189
 
G_M000_IG292:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x108]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG190
 
G_M000_IG293:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG189
 
G_M000_IG294:
       test     rax, rax
       je       SHORT G_M000_IG296
 
G_M000_IG295:
       mov      rsi, qword ptr [rax]
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG189
 
G_M000_IG296:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG189
 
G_M000_IG297:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG194
 
G_M000_IG298:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG308
 
G_M000_IG299:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1A0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1A8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG304
 
G_M000_IG300:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG301:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1B8]
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
 
G_M000_IG302:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1E8], xmm0
       lea      rdi, [rbp-0x1E8]
       mov      rsi, qword ptr [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C8]
       mov      rsi, qword ptr [rbp-0x1C0]
       mov      rdx, gword ptr [rbp-0x1E8]
       mov      rcx, qword ptr [rbp-0x1E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D8], rax
       mov      qword ptr [rbp-0x1D0], rdx
       mov      rdi, gword ptr [rbp-0x1D8]
       mov      rsi, qword ptr [rbp-0x1D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG303:
       jmp      G_M000_IG195
 
G_M000_IG304:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG307
 
G_M000_IG305:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG306:
       jmp      SHORT G_M000_IG303
 
G_M000_IG307:
       test     rax, rax
       jl       G_M000_IG393
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r9, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG392
       test     rax, rax
       jl       G_M000_IG393
       mov      rdi, rax
       mov      rax, gword ptr [r9+8*rdi+0x10]
       jmp      SHORT G_M000_IG306
 
G_M000_IG308:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG194
 
G_M000_IG309:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG195
 
G_M000_IG310:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm0
       test     rdx, rdx
       je       G_M000_IG194
 
G_M000_IG311:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG313
 
G_M000_IG312:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG195
 
G_M000_IG313:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG194
 
G_M000_IG314:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG195
 
G_M000_IG315:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG194
 
G_M000_IG316:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG194
 
G_M000_IG317:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG195
 
G_M000_IG318:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG216
 
G_M000_IG319:
       test     rax, rax
       setne    sil
       movzx    rsi, sil
       mov      dword ptr [rbp+0x9C], esi
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG321
 
G_M000_IG320:
       mov      gword ptr [rbp+0x310], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp+0x310]
 
G_M000_IG321:
       mov      gword ptr [rbp+0x310], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, gword ptr [rbp+0x310]
       jmp      G_M000_IG197
 
G_M000_IG322:
       mov      rsi, gword ptr [rbp-0x268]
       mov      rdx, qword ptr [rbp-0x260]
       mov      rdi, r8
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       jmp      G_M000_IG219
 
G_M000_IG323:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r15+0x08]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG325
 
G_M000_IG324:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       movsxd   rsi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       jmp      G_M000_IG219
 
G_M000_IG325:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      G_M000_IG219
 
G_M000_IG326:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG329
 
G_M000_IG327:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rsi+4*rdi+0x10], 53
       je       SHORT G_M000_IG329
 
G_M000_IG328:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG329:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG332
 
G_M000_IG330:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG332
 
G_M000_IG331:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG332:
       add      rsp, 0x648
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG333:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG334
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG335
 
G_M000_IG334:
       mov      ecx, eax
       jmp      SHORT G_M000_IG341
 
G_M000_IG335:
       cmp      edi, 2
       je       SHORT G_M000_IG336
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG392
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG337
 
G_M000_IG336:
       cmp      dword ptr [r12+0x08], 1
       jbe      G_M000_IG392
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG392
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG337:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG338
       mov      ecx, -1
       jmp      SHORT G_M000_IG340
 
G_M000_IG338:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG339
       mov      ecx, 1
       jmp      SHORT G_M000_IG340
 
G_M000_IG339:
       xor      ecx, ecx
 
G_M000_IG340:
       imul     ecx, eax
 
G_M000_IG341:
       test     ecx, ecx
       jge      G_M000_IG51
       jmp      G_M000_IG64
 
G_M000_IG342:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG343
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG344
 
G_M000_IG343:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x280]
       jmp      G_M000_IG53
 
G_M000_IG344:
       cmp      edi, 2
       je       SHORT G_M000_IG345
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG392
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG346
 
G_M000_IG345:
       cmp      dword ptr [r12+0x08], 1
       jbe      G_M000_IG392
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG392
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG346:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG347
       mov      ecx, -1
       jmp      SHORT G_M000_IG349
 
G_M000_IG347:
       test     rdi, rdi
       jge      SHORT G_M000_IG348
       mov      ecx, 1
       jmp      SHORT G_M000_IG349
 
G_M000_IG348:
       xor      ecx, ecx
 
G_M000_IG349:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG53
 
G_M000_IG350:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG98
 
G_M000_IG351:
       mov      rax, r13
       mov      gword ptr [rbp-0x2C8], rax
       mov      rcx, gword ptr [rbp+0x320]
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x200], rdx
       mov      rdi, rax
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x200]
       test     rsi, rsi
       jl       SHORT G_M000_IG353
 
G_M000_IG352:
       jmp      SHORT G_M000_IG354
 
G_M000_IG353:
       movsxd   rdi, eax
       add      rsi, rdi
 
G_M000_IG354:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG356
 
G_M000_IG355:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG363
 
G_M000_IG356:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x210], xmm0
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rax, gword ptr [rdi+0x10]
       test     rax, rax
       je       SHORT G_M000_IG359
 
G_M000_IG357:
       mov      rdi, rax
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x210], rax
       mov      qword ptr [rbp-0x208], rdx
       lea      rdi, [rbp-0x210]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG358:
       jmp      SHORT G_M000_IG363
 
G_M000_IG359:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG361
 
G_M000_IG360:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG362
 
G_M000_IG361:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG363
 
G_M000_IG362:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG358
 
G_M000_IG363:
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rax, gword ptr [rbp+0x320]
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x218], rax
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x218]
       test     rsi, rsi
       jl       SHORT G_M000_IG365
 
G_M000_IG364:
       jmp      SHORT G_M000_IG366
 
G_M000_IG365:
       movsxd   rdi, eax
       add      rsi, rdi
 
G_M000_IG366:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG368
 
G_M000_IG367:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG375
 
G_M000_IG368:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG371
 
G_M000_IG369:
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x228], rax
       mov      qword ptr [rbp-0x220], rdx
       lea      rdi, [rbp-0x228]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG370:
       jmp      SHORT G_M000_IG375
 
G_M000_IG371:
       mov      rdi, gword ptr [r14+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG373
 
G_M000_IG372:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG374
 
G_M000_IG373:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG375
 
G_M000_IG374:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG370
 
G_M000_IG375:
       vmovaps  xmm1, xmm0
       mov      esi, dword ptr [rbp+0x9C]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       jmp      G_M000_IG242
 
G_M000_IG376:
       jmp      G_M000_IG263
 
G_M000_IG377:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      G_M000_IG263
 
G_M000_IG378:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG381
 
G_M000_IG379:
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x240], rax
       mov      qword ptr [rbp-0x238], rdx
       lea      rdi, [rbp-0x240]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
       jmp      SHORT G_M000_IG384
 
G_M000_IG380:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG205
 
G_M000_IG381:
       mov      rdi, gword ptr [r14+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG383
 
G_M000_IG382:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG384
 
G_M000_IG383:
       xor      eax, eax
       jmp      SHORT G_M000_IG385
 
G_M000_IG384:
       mov      eax, 1
 
G_M000_IG385:
       test     eax, eax
       jne      G_M000_IG351
 
G_M000_IG386:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG205
 
G_M000_IG387:
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
 
G_M000_IG388:
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
 
G_M000_IG389:
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
 
G_M000_IG390:
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
 
G_M000_IG391:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG392:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG393:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 9640

; Assembly listing for method RyuPy.Generated.Module:m0_produce_consume(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4e1
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 46 inlinees with PGO data; 81 single block inlinees; 125 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 800
       mov      qword ptr [rsp+0x698], r15
       mov      qword ptr [rsp+0x690], r14
       mov      qword ptr [rsp+0x688], r13
       mov      qword ptr [rsp+0x680], r12
       mov      qword ptr [rsp+0x678], rbx
       lea      rbp, [rsp+0x320]
       xor      eax, eax
       mov      qword ptr [rbp-0x298], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x290], xmm8
       vmovdqa32 xmmword ptr [rbp-0x280], xmm8
       mov      rax, -576
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
       jmp      G_M000_IG35
 
G_M000_IG03:
       mov      r9, gword ptr [rbx+0x10]
       mov      r9d, dword ptr [r9+0x10]
 
G_M000_IG04:
       test     r11, r11
       jl       G_M000_IG41
 
G_M000_IG05:
       movsxd   r9, r9d
       cmp      r11, r9
       jae      SHORT G_M000_IG11
 
G_M000_IG06:
       mov      r9, gword ptr [rbx+0x10]
       test     r9, r9
       jne      G_M000_IG64
 
G_M000_IG07:
       mov      r9, gword ptr [rbx+0x08]
       cmp      r11d, dword ptr [r9+0x10]
       jae      G_M000_IG391
       mov      r9, gword ptr [r9+0x08]
       cmp      r11d, dword ptr [r9+0x08]
       jae      G_M000_IG392
       mov      r11d, r11d
       mov      r9, gword ptr [r9+8*r11+0x10]
       test     r9, r9
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [r9], r11
       je       G_M000_IG65
 
G_M000_IG09:
       xor      r9d, r9d
 
G_M000_IG10:
       test     r9d, r9d
       jne      G_M000_IG42
 
G_M000_IG11:
       mov      rdi, rbx
       mov      gword ptr [rbp+0x320], rcx
       mov      rsi, rcx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x230], rax
       mov      rdi, r15
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x228], rax
       mov      rcx, gword ptr [rbp+0x230]
       mov      rdi, rcx
       mov      rdx, rax
       mov      esi, dword ptr [rbp+0xF4]
       test     esi, esi
       je       SHORT G_M000_IG15
 
G_M000_IG12:
       test     rcx, rcx
       je       SHORT G_M000_IG15
 
G_M000_IG13:
       mov      r8, qword ptr [rcx]
       mov      r9, 0xD1FFAB1E
       cmp      r8, r9
       je       G_M000_IG77
 
G_M000_IG14:
       mov      rcx, gword ptr [rbp+0x230]
 
G_M000_IG15:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     esi, edi
       jne      G_M000_IG82
 
G_M000_IG16:
       test     rcx, rcx
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      r8, qword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       cmp      r8, rdx
       je       G_M000_IG83
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      r8, rdi
       je       G_M000_IG109
 
G_M000_IG19:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG20:
       mov      rax, rdx
       test     r13, r13
       je       SHORT G_M000_IG24
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       G_M000_IG114
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       G_M000_IG125
 
G_M000_IG23:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       je       G_M000_IG130
 
G_M000_IG24:
       mov      rdx, rax
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG25:
       test     r13, r13
       jne      G_M000_IG139
 
G_M000_IG26:
       xor      eax, eax
       mov      dword ptr [rbp+0xF4], eax
 
G_M000_IG27:
       mov      rcx, qword ptr [rbp+0x260]
       add      r12, rcx
       jo       G_M000_IG393
 
G_M000_IG28:
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
       je       G_M000_IG142
 
G_M000_IG29:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG167
 
G_M000_IG30:
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      rsi, rax
 
G_M000_IG31:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG34
 
G_M000_IG32:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [r9+4*r10+0x10], 44
       je       SHORT G_M000_IG34
 
G_M000_IG33:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG392
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 44
 
G_M000_IG34:
       mov      eax, dword ptr [rbp+0xF4]
       test     eax, eax
       mov      dword ptr [rbp+0xF4], eax
       mov      rcx, rsi
       je       G_M000_IG11
 
G_M000_IG35:
       test     rbx, rbx
       je       G_M000_IG11
 
G_M000_IG36:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [rbx], r11
       jne      G_M000_IG11
 
G_M000_IG37:
       test     rcx, rcx
       je       G_M000_IG11
 
G_M000_IG38:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [rcx], r11
       jne      G_M000_IG11
 
G_M000_IG39:
       mov      r11, qword ptr [rcx+0x08]
       mov      r9, gword ptr [rbx+0x08]
       test     r9, r9
       je       G_M000_IG03
 
G_M000_IG40:
       mov      r9d, dword ptr [r9+0x10]
       jmp      G_M000_IG04
 
G_M000_IG41:
       movsxd   r10, r9d
       add      r11, r10
       jmp      G_M000_IG05
 
G_M000_IG42:
       test     r15, r15
       je       G_M000_IG11
 
G_M000_IG43:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [r15], r9
       jne      G_M000_IG11
 
G_M000_IG44:
       mov      r9, qword ptr [rcx+0x08]
       mov      r11, gword ptr [r15+0x08]
       test     r11, r11
       jne      G_M000_IG66
 
G_M000_IG45:
       mov      r11, gword ptr [r15+0x10]
       mov      r11d, dword ptr [r11+0x10]
 
G_M000_IG46:
       mov      dword ptr [rbp-0x120], r11d
       test     r9, r9
       jl       G_M000_IG67
 
G_M000_IG47:
       movsxd   r11, r11d
       cmp      r9, r11
       jae      G_M000_IG11
 
G_M000_IG48:
       mov      r11, gword ptr [r15+0x10]
       test     r11, r11
       jne      G_M000_IG253
 
G_M000_IG49:
       mov      r11, gword ptr [r15+0x08]
       cmp      r9d, dword ptr [r11+0x10]
       jae      G_M000_IG391
       mov      r11, gword ptr [r11+0x08]
       cmp      r9d, dword ptr [r11+0x08]
       jae      G_M000_IG392
       mov      r9d, r9d
       mov      r9, gword ptr [r11+8*r9+0x10]
       test     r9, r9
       je       SHORT G_M000_IG51
 
G_M000_IG50:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [r9], r11
       je       G_M000_IG68
 
G_M000_IG51:
       xor      r9d, r9d
 
G_M000_IG52:
       test     r9d, r9d
       je       G_M000_IG11
 
G_M000_IG53:
       mov      gword ptr [rbp+0x320], rcx
       mov      r9, qword ptr [rcx+0x08]
       mov      r11, gword ptr [rbx+0x08]
       test     r11, r11
       jne      G_M000_IG69
 
G_M000_IG54:
       mov      r11, gword ptr [rbx+0x10]
       mov      r11d, dword ptr [r11+0x10]
 
G_M000_IG55:
       test     r9, r9
       jl       G_M000_IG70
 
G_M000_IG56:
       movsxd   r11, r11d
       cmp      r9, r11
       jb       G_M000_IG71
 
G_M000_IG57:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG58:
       vmovsd   qword ptr [rbp+0xD8], xmm0
       mov      rcx, gword ptr [rbp+0x320]
       mov      rcx, qword ptr [rcx+0x08]
       mov      r9, gword ptr [r15+0x08]
       test     r9, r9
       jne      G_M000_IG73
 
G_M000_IG59:
       mov      r9, gword ptr [r15+0x10]
       mov      r9d, dword ptr [r9+0x10]
 
G_M000_IG60:
       test     rcx, rcx
       jl       G_M000_IG74
 
G_M000_IG61:
       movsxd   r9, r9d
       cmp      rcx, r9
       jb       G_M000_IG75
 
G_M000_IG62:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
 
G_M000_IG63:
       vmovaps  xmm1, xmm0
       mov      esi, dword ptr [rbp+0xF4]
       vmovsd   xmm0, qword ptr [rbp+0xD8]
       jmp      G_M000_IG80
 
G_M000_IG64:
       cmp      r11d, dword ptr [r9+0x10]
       jae      G_M000_IG391
       mov      r9, gword ptr [r9+0x08]
       cmp      r11d, dword ptr [r9+0x08]
       jae      G_M000_IG392
       mov      r11d, r11d
       shl      r11, 4
       mov      r9, gword ptr [r9+r11+0x10]
       test     r9, r9
       je       G_M000_IG390
 
G_M000_IG65:
       mov      r9d, 1
       jmp      G_M000_IG10
 
G_M000_IG66:
       mov      r11d, dword ptr [r11+0x10]
       jmp      G_M000_IG46
 
G_M000_IG67:
       movsxd   r11, r11d
       add      r9, r11
       mov      r11d, dword ptr [rbp-0x120]
       jmp      G_M000_IG47
 
G_M000_IG68:
       mov      r9d, 1
       jmp      G_M000_IG52
 
G_M000_IG69:
       mov      r11d, dword ptr [r11+0x10]
       jmp      G_M000_IG55
 
G_M000_IG70:
       movsxd   r13, r11d
       add      r9, r13
       jmp      G_M000_IG56
 
G_M000_IG71:
       mov      r13, gword ptr [rbx+0x10]
       test     r13, r13
       jne      G_M000_IG243
 
G_M000_IG72:
       mov      r11, gword ptr [rbx+0x08]
       cmp      r9d, dword ptr [r11+0x10]
       jae      G_M000_IG391
       mov      r11, gword ptr [r11+0x08]
       cmp      r9d, dword ptr [r11+0x08]
       jae      G_M000_IG392
       mov      r9d, r9d
       mov      r9, gword ptr [r11+8*r9+0x10]
       test     r9, r9
       je       G_M000_IG245
       jmp      G_M000_IG244
 
G_M000_IG73:
       mov      r9d, dword ptr [r9+0x10]
       jmp      G_M000_IG60
 
G_M000_IG74:
       movsxd   r11, r9d
       add      rcx, r11
       jmp      G_M000_IG61
 
G_M000_IG75:
       mov      r13, gword ptr [r15+0x10]
       test     r13, r13
       jne      G_M000_IG248
 
G_M000_IG76:
       mov      r9, gword ptr [r15+0x08]
       cmp      ecx, dword ptr [r9+0x10]
       jae      G_M000_IG391
       mov      r9, gword ptr [r9+0x08]
       cmp      ecx, dword ptr [r9+0x08]
       jae      G_M000_IG392
       mov      ecx, ecx
       mov      rcx, gword ptr [r9+8*rcx+0x10]
       test     rcx, rcx
       je       G_M000_IG250
       jmp      G_M000_IG249
 
G_M000_IG77:
       test     rax, rax
       je       SHORT G_M000_IG81
 
G_M000_IG78:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       mov      rax, gword ptr [rbp+0x228]
       mov      rcx, gword ptr [rbp+0x230]
       jne      G_M000_IG15
 
G_M000_IG79:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
 
G_M000_IG80:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       xor      r13, r13
       mov      dword ptr [rbp+0xF4], esi
       jmp      G_M000_IG27
 
G_M000_IG81:
       mov      rcx, gword ptr [rbp+0x230]
       jmp      G_M000_IG15
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [r13+0x08], xmm0
       mov      rax, gword ptr [rbp+0x228]
       mov      rcx, gword ptr [rbp+0x230]
       jmp      G_M000_IG16
 
G_M000_IG83:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG19
 
G_M000_IG84:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG107
 
G_M000_IG85:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x78], rax
       lea      rdi, [rbp-0x90]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x90]
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
       je       G_M000_IG103
 
G_M000_IG86:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x88], xmm1
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x300], rax
       mov      ecx, dword ptr [rbp-0x80]
       mov      dword ptr [rbp-0x29C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       lea      rdi, [rbp-0xA0]
       mov      rsi, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA0]
       mov      r9d, dword ptr [rbp-0x98]
       mov      rcx, gword ptr [rbp-0x300]
       mov      rsi, rcx
       or       rsi, rax
       jne      G_M000_IG94
 
G_M000_IG87:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       movsxd   rsi, dword ptr [rbp-0x29C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB0]
       mov      r10d, dword ptr [rbp-0xA8]
 
G_M000_IG88:
       mov      gword ptr [rbp-0x308], rax
       mov      ecx, r10d
       mov      dword ptr [rbp-0x2A0], ecx
       test     rax, rax
       jne      G_M000_IG225
       movsxd   rdi, ecx
 
G_M000_IG89:
       mov      rdx, rax
       test     rdx, rdx
       jne      G_M000_IG234
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      G_M000_IG101
 
G_M000_IG90:
       mov      edi, -1
 
G_M000_IG91:
       test     edi, edi
       jg       G_M000_IG102
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2C8], rax
       mov      rdi, gword ptr [rbp-0x308]
       mov      esi, dword ptr [rbp-0x2A0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x2C8]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG93:
       jmp      G_M000_IG20
 
G_M000_IG94:
       test     rcx, rcx
       je       SHORT G_M000_IG99
 
G_M000_IG95:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG96:
       test     rax, rax
       je       SHORT G_M000_IG100
 
G_M000_IG97:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG98:
       mov      edx, dword ptr [rbp-0x29C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG88
 
G_M000_IG99:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG96
 
G_M000_IG100:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG98
 
G_M000_IG101:
       xor      edi, edi
       jmp      G_M000_IG91
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x2C0], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x308]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x2C0]
       mov      edi, dword ptr [rbp-0x2A0]
       mov      dword ptr [rdx+0x10], edi
       jmp      G_M000_IG93
 
G_M000_IG103:
       mov      rax, rcx
       mov      qword ptr [rbp-0xB8], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG106
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG105:
       jmp      G_M000_IG93
 
G_M000_IG106:
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rdx, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG392
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG105
 
G_M000_IG107:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG19
 
G_M000_IG108:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0xC0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xC0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG20
 
G_M000_IG109:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm1
       test     rax, rax
       je       G_M000_IG19
 
G_M000_IG110:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG112
 
G_M000_IG111:
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG20
 
G_M000_IG112:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG19
 
G_M000_IG113:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG20
 
G_M000_IG114:
       mov      rsi, qword ptr [r13+0x08]
       test     rax, rax
       je       G_M000_IG24
 
G_M000_IG115:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG123
 
G_M000_IG116:
       mov      r13, qword ptr [rax+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xE8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG119
 
G_M000_IG117:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x310], rax
       mov      ecx, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0x2A4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x108], xmm0
       lea      rdi, [rbp-0x108]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x310]
       mov      esi, dword ptr [rbp-0x2A4]
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG118:
       mov      r13, rax
       jmp      G_M000_IG25
 
G_M000_IG119:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG122
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xE8]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG121:
       jmp      SHORT G_M000_IG118
 
G_M000_IG122:
       test     r13, r13
       jl       G_M000_IG393
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG392
       test     r13, r13
       jl       G_M000_IG393
       mov      rax, r13
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG121
 
G_M000_IG123:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG24
 
G_M000_IG124:
       vmovsd   xmm0, qword ptr [rax+0x08]
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
       jmp      G_M000_IG25
 
G_M000_IG125:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rax, rax
       je       G_M000_IG24
 
G_M000_IG126:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG128
 
G_M000_IG127:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG25
 
G_M000_IG128:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG24
 
G_M000_IG129:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG25
 
G_M000_IG130:
       mov      rcx, r13
       mov      gword ptr [rbp-0x2D0], rcx
       test     rax, rax
       je       G_M000_IG24
 
G_M000_IG131:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG24
 
G_M000_IG132:
       mov      r13, rax
       cmp      dword ptr [rcx+0x08], 0
       je       G_M000_IG137
 
G_M000_IG133:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG136
 
G_M000_IG134:
       mov      eax, dword ptr [rcx+0x08]
       mov      dword ptr [rbp-0x11C], eax
       mov      esi, eax
       add      esi, dword ptr [r13+0x08]
       js       G_M000_IG386
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x2D8], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rcx, gword ptr [rbp-0x2D0]
       lea      rsi, bword ptr [rcx+0x0C]
       mov      edx, dword ptr [rcx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x11C]
       mov      rcx, gword ptr [rbp-0x2D8]
       lea      rdi, bword ptr [rcx+2*rdi+0x0C]
       lea      rsi, bword ptr [r13+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0x2D8]
 
G_M000_IG135:
       jmp      G_M000_IG25
 
G_M000_IG136:
       mov      r13, rcx
       jmp      SHORT G_M000_IG135
 
G_M000_IG137:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG242
 
G_M000_IG138:
       jmp      SHORT G_M000_IG135
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG141
 
G_M000_IG140:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      dword ptr [rbp+0xF4], eax
       jmp      G_M000_IG27
 
G_M000_IG141:
       xor      eax, eax
       mov      dword ptr [rbp+0xF4], eax
       jmp      G_M000_IG27
 
G_M000_IG142:
       mov      edi, 45
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp+0xF4], edi
       jne      G_M000_IG156
 
G_M000_IG143:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x58]
       test     rax, rax
       je       SHORT G_M000_IG145
 
G_M000_IG144:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x2E0], rax
       je       SHORT G_M000_IG146
 
G_M000_IG145:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x58]
       mov      gword ptr [rbp-0x2E0], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG146:
       mov      rdi, gword ptr [rbp-0x2E0]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x2E0]
       mov      rsi, gword ptr [rbp+0x338]
       mov      edi, 9
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
 
G_M000_IG147:
       mov      r12, qword ptr [rbp+0x2D8]
       mov      rax, qword ptr [rbp+0x2C8]
       add      r12, rax
       jo       G_M000_IG393
       mov      qword ptr [rbp+0x2C8], rax
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       mov      rcx, qword ptr [rbp+0x2D0]
       cmp      r12, rcx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x2D0], rcx
       cmp      r12, rcx
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG254
 
G_M000_IG148:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       lea      r8d, [rdi-0x01]
       mov      r9d, r8d
       mov      r10d, dword ptr [rdx+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG392
       mov      r10d, r9d
       cmp      dword ptr [rdx+4*r10+0x10], 42
       je       SHORT G_M000_IG151
 
G_M000_IG150:
       mov      edx, r8d
       mov      dword ptr [rsi+4*rdx+0x10], 42
 
G_M000_IG151:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x328], rsi
       test     edi, edi
       jle      SHORT G_M000_IG154
 
G_M000_IG152:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG392
       cmp      dword ptr [r8+4*r9+0x10], 43
       je       SHORT G_M000_IG154
 
G_M000_IG153:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 43
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       mov      gword ptr [rbp+0x290], r8
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2B0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x2B0]
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
       jne      G_M000_IG164
 
G_M000_IG155:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG28
 
G_M000_IG156:
       mov      r13, gword ptr [rbp+0x338]
       mov      r12, r13
       test     r12, r12
       jne      SHORT G_M000_IG158
 
G_M000_IG157:
       call     [RyuPy.Runtime.PyNumericListOps:AppendError():System.Object]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG147
 
G_M000_IG158:
       mov      rdi, gword ptr [r12+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG162
 
G_M000_IG159:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r12, qword ptr [rbp+0xF8]
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      ecx, dword ptr [rdi+0x10]
       mov      edx, dword ptr [rax+0x08]
       cmp      edx, ecx
       jbe      SHORT G_M000_IG161
 
G_M000_IG160:
       lea      edx, [rcx+0x01]
       mov      dword ptr [rdi+0x10], edx
       mov      edi, ecx
       shl      rdi, 4
       lea      rax, bword ptr [rax+rdi+0x10]
       mov      bword ptr [rbp-0x320], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x320]
       mov      qword ptr [rsi+0x08], r12
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG147
 
G_M000_IG161:
       mov      rdx, r12
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG147
 
G_M000_IG162:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r12+0x08]
       vmovsd   xmm0, qword ptr [rbp+0xF8]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      G_M000_IG224
 
G_M000_IG163:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       mov      esi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG147
 
G_M000_IG164:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG166
 
G_M000_IG165:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG28
 
G_M000_IG166:
       xor      r8d, r8d
       mov      dword ptr [rbp+0xF4], r8d
       mov      qword ptr [rbp+0x2D8], r12
       vmovsd   qword ptr [rbp+0xF8], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0x260]
       jmp      G_M000_IG28
 
G_M000_IG167:
       test     rsi, rsi
       jl       G_M000_IG393
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG392
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      qword ptr [rbp+0x268], rdx
       mov      qword ptr [rbp+0x260], rcx
       jmp      G_M000_IG31
 
G_M000_IG168:
       add      r12, 1
       jo       G_M000_IG393
 
G_M000_IG169:
       cmp      r12, rbx
       jge      G_M000_IG379
 
G_M000_IG170:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG173
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rsi+0x10], 49
       je       SHORT G_M000_IG173
 
G_M000_IG172:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rdi+4*rsi+0x10], 49
 
G_M000_IG173:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x310], rax
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG176
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rsi+0x10], 50
       je       SHORT G_M000_IG176
 
G_M000_IG175:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [(reloc)]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rdi+4*rsi+0x10], 50
 
G_M000_IG176:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x198], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2B8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x2B8]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       mov      rdx, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x190], rax
       mov      rdi, gword ptr [rbp+0x198]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x188], rax
       mov      rdi, gword ptr [rbp+0x190]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0x188]
       mov      qword ptr [rbp+0x178], rcx
       mov      qword ptr [rbp+0x170], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp+0x310]
       test     rdx, rdx
       jne      G_M000_IG262
       jmp      G_M000_IG261
 
G_M000_IG177:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jae      G_M000_IG360
       jmp      G_M000_IG358
 
G_M000_IG178:
       test     rax, rax
       jne      G_M000_IG311
       jmp      G_M000_IG313
 
G_M000_IG179:
       mov      gword ptr [rbp+0x310], rdx
 
G_M000_IG180:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG181:
       mov      rax, gword ptr [rbp+0x310]
       test     rax, rax
       je       G_M000_IG192
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG189
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG191
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG307
       jmp      SHORT G_M000_IG192
 
G_M000_IG185:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG187
 
G_M000_IG186:
       mov      gword ptr [rbp+0x310], rdx
       jmp      SHORT G_M000_IG180
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG266
       jmp      G_M000_IG265
 
G_M000_IG188:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x128], xmm1
       test     rax, rax
       jne      G_M000_IG286
       jmp      G_M000_IG285
 
G_M000_IG189:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       SHORT G_M000_IG192
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG294
       jmp      G_M000_IG293
 
G_M000_IG191:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       test     rdx, rdx
       jne      G_M000_IG303
 
G_M000_IG192:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG193:
       test     rax, rax
       je       SHORT G_M000_IG199
 
G_M000_IG194:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG198
 
G_M000_IG195:
       test     rax, rax
       setne    sil
       movzx    rsi, sil
       mov      dword ptr [rbp+0x9C], esi
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG197
 
G_M000_IG196:
       mov      gword ptr [rbp+0x310], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp+0x310]
 
G_M000_IG197:
       mov      gword ptr [rbp+0x310], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rax, gword ptr [rbp+0x310]
       jmp      SHORT G_M000_IG200
 
G_M000_IG198:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x9C], ecx
       jmp      SHORT G_M000_IG200
 
G_M000_IG199:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x9C], ecx
 
G_M000_IG200:
       mov      rdi, qword ptr [rbp+0x178]
       add      rdi, 1
       jo       G_M000_IG393
       mov      qword ptr [rbp+0x178], rdi
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
 
G_M000_IG201:
       mov      rcx, qword ptr [rbp+0x178]
       cmp      rcx, qword ptr [rbp+0x170]
       jge      G_M000_IG223
 
G_M000_IG202:
       mov      qword ptr [rbp+0x178], rcx
       lea      r8, [rcx+0x05]
       cmp      r8, 261
       jbe      G_M000_IG218
 
G_M000_IG203:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, qword ptr [rbp+0x178]
       mov      qword ptr [rax+0x08], rcx
       mov      rsi, rax
 
G_M000_IG204:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG207
 
G_M000_IG205:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*r8+0x10], 51
       je       SHORT G_M000_IG207
 
G_M000_IG206:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [(reloc)]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rdi+4*r8+0x10], 51
 
G_M000_IG207:
       cmp      dword ptr [rbp+0x9C], 0
       jne      G_M000_IG219
 
G_M000_IG208:
       mov      rdi, r13
       mov      gword ptr [rbp+0x320], rsi
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x138], rax
       mov      rdi, r14
       mov      rsi, gword ptr [rbp+0x320]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x130], rax
       mov      rcx, gword ptr [rbp+0x138]
       mov      rdi, rcx
       mov      rdx, rax
       mov      esi, dword ptr [rbp+0x9C]
       test     esi, esi
       je       SHORT G_M000_IG212
 
G_M000_IG209:
       test     rcx, rcx
       je       SHORT G_M000_IG212
 
G_M000_IG210:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       je       G_M000_IG178
 
G_M000_IG211:
       mov      rcx, gword ptr [rbp+0x138]
 
G_M000_IG212:
       mov      rdx, gword ptr [rbp+0x310]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     esi, edi
       je       SHORT G_M000_IG214
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp+0x310], rdx
       mov      rax, gword ptr [rbp+0x130]
       mov      rcx, gword ptr [rbp+0x138]
       mov      rdx, gword ptr [rbp+0x310]
 
G_M000_IG214:
       test     rcx, rcx
       je       G_M000_IG179
 
G_M000_IG215:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG185
 
G_M000_IG216:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG188
 
G_M000_IG217:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG290
       jmp      G_M000_IG284
 
G_M000_IG218:
       test     r8, r8
       jl       G_M000_IG393
       mov      rdi, r8
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       cmp      rdi, 262
       jae      G_M000_IG392
       test     r8, r8
       jl       G_M000_IG393
       mov      rdi, r8
       mov      rdi, gword ptr [r9+8*rdi+0x10]
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rsi, rdi
       jmp      G_M000_IG204
 
G_M000_IG219:
       mov      r9, r13
       mov      gword ptr [rbp-0x2F0], r9
       test     r9, r9
       je       G_M000_IG208
 
G_M000_IG220:
       test     rsi, rsi
       je       G_M000_IG208
 
G_M000_IG221:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG208
 
G_M000_IG222:
       mov      gword ptr [rbp+0x320], rsi
       mov      r10, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x268], r10
       mov      rdi, r9
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x268]
       test     rsi, rsi
       jl       G_M000_IG357
       jmp      G_M000_IG356
 
G_M000_IG223:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      edi, 52
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       sete     sil
       movzx    rsi, sil
       test     dword ptr [rbp+0x9C], esi
       jne      G_M000_IG370
       jmp      G_M000_IG367
 
G_M000_IG224:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       mov      gword ptr [rbp+0x338], r13
       jmp      G_M000_IG147
 
G_M000_IG225:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdx, rdi
       jl       SHORT G_M000_IG226
       mov      edx, dword ptr [rax+0x08]
       cmp      edx, 2
       jle      SHORT G_M000_IG227
 
G_M000_IG226:
       mov      esi, ecx
       jmp      SHORT G_M000_IG233
 
G_M000_IG227:
       cmp      edx, 2
       je       SHORT G_M000_IG228
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG392
       mov      edx, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG229
 
G_M000_IG228:
       mov      edx, dword ptr [rax+0x14]
       shl      rdx, 32
       mov      esi, dword ptr [rax+0x10]
       or       rdx, rsi
 
G_M000_IG229:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jae      SHORT G_M000_IG230
       mov      esi, -1
       jmp      SHORT G_M000_IG232
 
G_M000_IG230:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jbe      SHORT G_M000_IG231
       mov      esi, 1
       jmp      SHORT G_M000_IG232
 
G_M000_IG231:
       xor      esi, esi
 
G_M000_IG232:
       imul     esi, ecx
 
G_M000_IG233:
       test     esi, esi
       jge      G_M000_IG89
       jmp      G_M000_IG102
 
G_M000_IG234:
       mov      rdx, 0xD1FFAB1E
       xor      rdx, rdi
       jl       SHORT G_M000_IG235
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG236
 
G_M000_IG235:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x2A0]
       jmp      G_M000_IG91
 
G_M000_IG236:
       cmp      edi, 2
       je       SHORT G_M000_IG237
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG392
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG238
 
G_M000_IG237:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x308]
 
G_M000_IG238:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG239
       mov      edx, -1
       jmp      SHORT G_M000_IG241
 
G_M000_IG239:
       test     rdi, rdi
       jge      SHORT G_M000_IG240
       mov      edx, 1
       jmp      SHORT G_M000_IG241
 
G_M000_IG240:
       xor      edx, edx
 
G_M000_IG241:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG91
 
G_M000_IG242:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG135
 
G_M000_IG243:
       cmp      r9d, dword ptr [r13+0x10]
       jae      G_M000_IG391
       mov      r11, gword ptr [r13+0x08]
       cmp      r9d, dword ptr [r11+0x08]
       jae      G_M000_IG392
       mov      r9d, r9d
       shl      r9, 4
       lea      r9, bword ptr [r11+r9+0x10]
       mov      r11, gword ptr [r9]
       mov      r9, qword ptr [r9+0x08]
       test     r11, r11
       je       G_M000_IG387
       vmovq    xmm1, r9
       jmp      SHORT G_M000_IG247
 
G_M000_IG244:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [r9], r11
       je       SHORT G_M000_IG246
 
G_M000_IG245:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      G_M000_IG58
 
G_M000_IG246:
       vmovsd   xmm1, qword ptr [r9+0x08]
 
G_M000_IG247:
       vmovaps  xmm0, xmm1
       jmp      G_M000_IG58
 
G_M000_IG248:
       cmp      ecx, dword ptr [r13+0x10]
       jae      G_M000_IG391
       mov      r9, gword ptr [r13+0x08]
       cmp      ecx, dword ptr [r9+0x08]
       jae      G_M000_IG392
       mov      ecx, ecx
       shl      rcx, 4
       lea      rcx, bword ptr [r9+rcx+0x10]
       mov      r9, gword ptr [rcx]
       mov      rcx, qword ptr [rcx+0x08]
       test     r9, r9
       je       G_M000_IG388
       vmovq    xmm1, rcx
       jmp      SHORT G_M000_IG252
 
G_M000_IG249:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rcx], r9
       je       SHORT G_M000_IG251
 
G_M000_IG250:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      G_M000_IG63
 
G_M000_IG251:
       vmovsd   xmm1, qword ptr [rcx+0x08]
 
G_M000_IG252:
       vmovaps  xmm0, xmm1
       jmp      G_M000_IG63
 
G_M000_IG253:
       cmp      r9d, dword ptr [r11+0x10]
       jae      G_M000_IG391
       mov      r11, gword ptr [r11+0x08]
       cmp      r9d, dword ptr [r11+0x08]
       jae      G_M000_IG392
       mov      r9d, r9d
       shl      r9, 4
       mov      r9, gword ptr [r11+r9+0x10]
       test     r9, r9
       jne      G_M000_IG68
       jmp      G_M000_IG389
 
G_M000_IG254:
       mov      r13, gword ptr [rbp+0x338]
       mov      edi, dword ptr [(reloc)]
       mov      ebx, edi
       test     ebx, ebx
       jle      SHORT G_M000_IG257
 
G_M000_IG255:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       mov      r10d, dword ptr [rdi+0x08]
       cmp      ecx, r10d
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rcx+0x10], 46
       je       SHORT G_M000_IG257
 
G_M000_IG256:
       mov      rdi, rsi
       cmp      eax, r10d
       jae      G_M000_IG392
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 46
 
G_M000_IG257:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       lea      rdi, bword ptr [r15+0x10]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       test     ebx, ebx
       jle      SHORT G_M000_IG260
 
G_M000_IG258:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      eax, [rbx-0x01]
       mov      ecx, eax
       mov      r10d, dword ptr [rdi+0x08]
       cmp      ecx, r10d
       jae      G_M000_IG392
       cmp      dword ptr [rdi+4*rcx+0x10], 48
       je       SHORT G_M000_IG260
 
G_M000_IG259:
       mov      rdi, rsi
       cmp      eax, r10d
       jae      G_M000_IG392
       mov      eax, eax
       mov      dword ptr [rdi+4*rax+0x10], 48
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      rbx, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG169
 
G_M000_IG261:
       xor      esi, esi
       mov      dword ptr [rbp+0x9C], esi
       jmp      G_M000_IG201
 
G_M000_IG262:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG264
 
G_M000_IG263:
       xor      r8d, r8d
       xor      esi, esi
       mov      dword ptr [rbp+0x9C], esi
       jmp      G_M000_IG201
 
G_M000_IG264:
       test     rdx, rdx
       setne    r8b
       movzx    r8, r8b
       mov      dword ptr [rbp+0x9C], r8d
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       G_M000_IG369
       jmp      G_M000_IG368
 
G_M000_IG265:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rdx
       je       G_M000_IG283
       jmp      G_M000_IG180
 
G_M000_IG266:
       mov      gword ptr [rbp+0x310], rdx
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x140], rax
       lea      rdi, [rbp-0x158]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x158]
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
       je       G_M000_IG279
 
G_M000_IG267:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu32 xmmword ptr [rbp-0x150], xmm1
       lea      rdi, [rbp-0x150]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x150]
       mov      gword ptr [rbp-0x318], rax
       mov      ecx, dword ptr [rbp-0x148]
       mov      dword ptr [rbp-0x2A8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, qword ptr [rbp-0x140]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      r9d, dword ptr [rbp-0x160]
       mov      rcx, gword ptr [rbp-0x318]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG271
 
G_M000_IG268:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       movsxd   rsi, dword ptr [rbp-0x2A8]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x188]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG269:
       vmovdqu  xmm0, xmmword ptr [rbp-0x188]
       vmovdqu  xmmword ptr [rbp-0x178], xmm0
 
G_M000_IG270:
       jmp      SHORT G_M000_IG278
 
G_M000_IG271:
       test     rcx, rcx
       jne      SHORT G_M000_IG273
 
G_M000_IG272:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG274
 
G_M000_IG273:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG274:
       test     rax, rax
       jne      SHORT G_M000_IG276
 
G_M000_IG275:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG277
 
G_M000_IG276:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG277:
       mov      edx, dword ptr [rbp-0x2A8]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x178], rax
       mov      qword ptr [rbp-0x170], rdx
 
G_M000_IG278:
       mov      rdi, gword ptr [rbp-0x178]
       mov      rsi, qword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      SHORT G_M000_IG282
 
G_M000_IG279:
       mov      rax, rcx
       mov      qword ptr [rbp-0x190], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG281
 
G_M000_IG280:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x190]
       mov      qword ptr [rax+0x08], rdi
       jmp      SHORT G_M000_IG282
 
G_M000_IG281:
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      r9, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG392
       test     rcx, rcx
       jl       G_M000_IG393
       mov      rdi, rcx
       mov      rax, gword ptr [r9+8*rdi+0x10]
 
G_M000_IG282:
       mov      rdx, rax
       jmp      G_M000_IG181
 
G_M000_IG283:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x198], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x198]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x138]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG181
 
G_M000_IG284:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG180
 
G_M000_IG285:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG180
 
G_M000_IG286:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG288
 
G_M000_IG287:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0x310], rdx
       je       SHORT G_M000_IG289
       jmp      G_M000_IG180
 
G_M000_IG288:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x128]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG181
 
G_M000_IG289:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x128]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG181
 
G_M000_IG290:
       test     rax, rax
       jne      SHORT G_M000_IG292
 
G_M000_IG291:
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG180
 
G_M000_IG292:
       mov      rsi, qword ptr [rax]
       mov      gword ptr [rbp+0x310], rdx
       jmp      G_M000_IG180
 
G_M000_IG293:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG302
       jmp      G_M000_IG192
 
G_M000_IG294:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1C0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1C8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG298
 
G_M000_IG295:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
       lea      rdi, [rbp-0x1D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG296:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1D8]
       vmovdqu  xmmword ptr [rbp-0x1E8], xmm0
 
G_M000_IG297:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x208], xmm0
       lea      rdi, [rbp-0x208]
       mov      rsi, qword ptr [rbp-0x1C0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1E8]
       mov      rsi, qword ptr [rbp-0x1E0]
       mov      rdx, gword ptr [rbp-0x208]
       mov      rcx, qword ptr [rbp-0x200]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1F8], rax
       mov      qword ptr [rbp-0x1F0], rdx
       mov      rdi, gword ptr [rbp-0x1F8]
       mov      rsi, qword ptr [rbp-0x1F0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      SHORT G_M000_IG301
 
G_M000_IG298:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG300
 
G_M000_IG299:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1C8]
       mov      qword ptr [rax+0x08], rdi
       jmp      SHORT G_M000_IG301
 
G_M000_IG300:
       test     rax, rax
       jl       G_M000_IG393
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r9, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG392
       test     rax, rax
       jl       G_M000_IG393
       mov      rdi, rax
       mov      rax, gword ptr [r9+8*rdi+0x10]
 
G_M000_IG301:
       jmp      G_M000_IG193
 
G_M000_IG302:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x210], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x210]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG193
 
G_M000_IG303:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG305
 
G_M000_IG304:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG306
       jmp      G_M000_IG192
 
G_M000_IG305:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1A8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG193
 
G_M000_IG306:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1A8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG193
 
G_M000_IG307:
       mov      rdi, rax
       test     rdx, rdx
       je       G_M000_IG192
 
G_M000_IG308:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG192
 
G_M000_IG309:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG193
 
G_M000_IG310:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      rdx, rdx
       mov      dword ptr [rbp+0x9C], esi
       mov      rax, rdx
       jmp      G_M000_IG200
 
G_M000_IG311:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       mov      rax, gword ptr [rbp+0x130]
       mov      rcx, gword ptr [rbp+0x138]
       jne      G_M000_IG212
 
G_M000_IG312:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       jmp      SHORT G_M000_IG310
 
G_M000_IG313:
       mov      rcx, gword ptr [rbp+0x138]
       jmp      G_M000_IG212
 
G_M000_IG314:
       mov      rax, r13
       mov      gword ptr [rbp-0x2E8], rax
       mov      rcx, gword ptr [rbp+0x320]
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x220], rdx
       mov      rdi, rax
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x220]
       test     rsi, rsi
       jl       SHORT G_M000_IG316
 
G_M000_IG315:
       jmp      SHORT G_M000_IG317
 
G_M000_IG316:
       movsxd   rdi, eax
       add      rsi, rdi
 
G_M000_IG317:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG319
 
G_M000_IG318:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG326
 
G_M000_IG319:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x230], xmm0
       mov      rdi, gword ptr [rbp-0x2E8]
       mov      rax, gword ptr [rdi+0x10]
       test     rax, rax
       je       SHORT G_M000_IG322
 
G_M000_IG320:
       mov      rdi, rax
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x230], rax
       mov      qword ptr [rbp-0x228], rdx
       lea      rdi, [rbp-0x230]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG321:
       jmp      SHORT G_M000_IG326
 
G_M000_IG322:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG324
 
G_M000_IG323:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG325
 
G_M000_IG324:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG326
 
G_M000_IG325:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG321
 
G_M000_IG326:
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rax, gword ptr [rbp+0x320]
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x238], rax
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x238]
       test     rsi, rsi
       jl       SHORT G_M000_IG328
 
G_M000_IG327:
       jmp      SHORT G_M000_IG329
 
G_M000_IG328:
       movsxd   rdi, eax
       add      rsi, rdi
 
G_M000_IG329:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jb       SHORT G_M000_IG331
 
G_M000_IG330:
       mov      edi, 2
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG338
 
G_M000_IG331:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x248], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG334
 
G_M000_IG332:
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       lea      rdi, [rbp-0x248]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG333:
       jmp      SHORT G_M000_IG338
 
G_M000_IG334:
       mov      rdi, gword ptr [r14+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG336
 
G_M000_IG335:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG337
 
G_M000_IG336:
       mov      edi, 3
       call     [RyuPy.Runtime.PyNumericListOps:ReadError(int):double]
       jmp      SHORT G_M000_IG338
 
G_M000_IG337:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG333
 
G_M000_IG338:
       vmovaps  xmm1, xmm0
       mov      esi, dword ptr [rbp+0x9C]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       jmp      G_M000_IG310
 
G_M000_IG339:
       test     r14, r14
       jne      SHORT G_M000_IG341
 
G_M000_IG340:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG208
 
G_M000_IG341:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG344
 
G_M000_IG342:
       mov      rax, gword ptr [rbp+0x320]
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x250], rcx
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rsi, qword ptr [rbp-0x250]
       test     rsi, rsi
       jl       SHORT G_M000_IG345
 
G_M000_IG343:
       jmp      SHORT G_M000_IG346
 
G_M000_IG344:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG208
 
G_M000_IG345:
       movsxd   rdi, eax
       add      rsi, rdi
 
G_M000_IG346:
       movsxd   rdi, eax
       cmp      rsi, rdi
       jae      SHORT G_M000_IG349
 
G_M000_IG347:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x260], xmm0
       mov      rdi, gword ptr [r14+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG350
 
G_M000_IG348:
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x260], rax
       mov      qword ptr [rbp-0x258], rdx
       lea      rdi, [rbp-0x260]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
       jmp      SHORT G_M000_IG353
 
G_M000_IG349:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG208
 
G_M000_IG350:
       mov      rdi, gword ptr [r14+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG352
 
G_M000_IG351:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG353
 
G_M000_IG352:
       xor      eax, eax
       jmp      SHORT G_M000_IG354
 
G_M000_IG353:
       mov      eax, 1
 
G_M000_IG354:
       test     eax, eax
       jne      G_M000_IG314
 
G_M000_IG355:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG208
 
G_M000_IG356:
       jmp      G_M000_IG177
 
G_M000_IG357:
       movsxd   rdi, eax
       add      rsi, rdi
       jmp      G_M000_IG177
 
G_M000_IG358:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x278], xmm0
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rax, gword ptr [rdi+0x10]
       test     rax, rax
       je       SHORT G_M000_IG361
 
G_M000_IG359:
       mov      rdi, rax
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:get_Item(int):RyuPy.Runtime.PyNumericSlot:this]
       mov      gword ptr [rbp-0x278], rax
       mov      qword ptr [rbp-0x270], rdx
       lea      rdi, [rbp-0x278]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
       jmp      SHORT G_M000_IG364
 
G_M000_IG360:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG208
 
G_M000_IG361:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       test     rax, rax
       je       SHORT G_M000_IG363
 
G_M000_IG362:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG364
 
G_M000_IG363:
       xor      eax, eax
       jmp      SHORT G_M000_IG365
 
G_M000_IG364:
       mov      eax, 1
 
G_M000_IG365:
       test     eax, eax
       jne      G_M000_IG339
 
G_M000_IG366:
       mov      rsi, gword ptr [rbp+0x320]
       jmp      G_M000_IG208
 
G_M000_IG367:
       mov      rsi, r15
       mov      edi, 12
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG168
 
G_M000_IG368:
       mov      gword ptr [rbp+0x310], rdx
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rdx, gword ptr [rbp+0x310]
 
G_M000_IG369:
       mov      gword ptr [rbp+0x310], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdx, gword ptr [rbp+0x310]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       jmp      G_M000_IG201
 
G_M000_IG370:
       mov      rax, gword ptr [r15+0x10]
       mov      gword ptr [rbp-0x2F8], rax
       test     rax, rax
       je       G_M000_IG376
 
G_M000_IG371:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x298], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       lea      rdi, [rbp-0x298]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       call     [RyuPy.Runtime.PyNumericSlot:.ctor(System.Object,double):this]
 
G_M000_IG372:
       vmovdqu  xmm0, xmmword ptr [rbp-0x298]
       vmovdqu  xmmword ptr [rbp-0x288], xmm0
 
G_M000_IG373:
       mov      r8, gword ptr [rbp-0x2F8]
       inc      dword ptr [r8+0x14]
       mov      rdi, gword ptr [r8+0x08]
       mov      esi, dword ptr [r8+0x10]
       cmp      dword ptr [rdi+0x08], esi
       jbe      SHORT G_M000_IG375
 
G_M000_IG374:
       lea      eax, [rsi+0x01]
       mov      dword ptr [r8+0x10], eax
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG392
       mov      esi, esi
       shl      rsi, 4
       lea      rdi, bword ptr [rdi+rsi+0x10]
       lea      rsi, bword ptr [rbp-0x288]
       call     CORINFO_HELP_ASSIGN_BYREF
       movsq    
       jmp      G_M000_IG168
 
G_M000_IG375:
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdx, qword ptr [rbp-0x280]
       mov      rdi, r8
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       jmp      G_M000_IG168
 
G_M000_IG376:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r15+0x08]
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG378
 
G_M000_IG377:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       movsxd   rsi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       jmp      G_M000_IG168
 
G_M000_IG378:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      G_M000_IG168
 
G_M000_IG379:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG382
 
G_M000_IG380:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       cmp      dword ptr [rsi+4*rdi+0x10], 53
       je       SHORT G_M000_IG382
 
G_M000_IG381:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, dword ptr [(reloc)]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG392
       mov      dword ptr [rsi+4*rdi+0x10], 53
 
G_M000_IG382:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG385
 
G_M000_IG383:
       movsxd   rcx, dword ptr [(reloc)]
       cmp      rbx, rcx
       jg       SHORT G_M000_IG385
 
G_M000_IG384:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG385:
       add      rsp, 0x678
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG386:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG387:
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
 
G_M000_IG388:
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
 
G_M000_IG389:
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
 
G_M000_IG390:
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
 
G_M000_IG391:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG392:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG393:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 9782

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
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       xor      eax, eax
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
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x10], rax
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
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x14], eax
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
       movzx    rax, byte  ptr [rax]
       mov      dword ptr [rbp-0x18], eax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x10]
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       movsxd   rax, dword ptr [rbp-0x14]
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       cmp      dword ptr [rbp-0x18], 0
       setne    al
       movzx    rax, al
       mov      eax, eax
       mov      qword ptr [rbp-0x20], rax
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyList:ToIndexSlow(System.Object):long]
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x20]
 
G_M000_IG10:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 311

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(long):System.Object:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 48
       lea      rbp, [rsp+0x30]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x30], ymm8
       mov      gword ptr [rbp-0x08], rdi
       mov      qword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rax, gword ptr [rbp-0x08]
       mov      rax, gword ptr [rax+0x08]
       mov      gword ptr [rbp-0x18], rax
       cmp      gword ptr [rbp-0x18], 0
       jne      SHORT G_M000_IG04
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x08]
       mov      rsi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyList:ReadNumeric(long):System.Object:this]
       nop      
 
G_M000_IG03:
       add      rsp, 48
       pop      rbp
       ret      
 
G_M000_IG04:
       mov      rdi, gword ptr [rbp-0x18]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Count():int:this]
       mov      dword ptr [rbp-0x1C], eax
       cmp      qword ptr [rbp-0x10], 0
       jl       SHORT G_M000_IG05
       mov      rax, qword ptr [rbp-0x10]
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       movsxd   rax, dword ptr [rbp-0x1C]
       add      rax, qword ptr [rbp-0x10]
       mov      qword ptr [rbp-0x30], rax
 
G_M000_IG06:
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x28], rax
       movsxd   rax, dword ptr [rbp-0x1C]
       cmp      rax, qword ptr [rbp-0x28]
       ja       SHORT G_M000_IG08
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       call     [RyuPy.Runtime.PyList:ListIndexError():System.Object]
       nop      
 
G_M000_IG07:
       add      rsp, 48
       pop      rbp
       ret      
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x18]
       mov      esi, dword ptr [rbp-0x28]
       cmp      dword ptr [rdi], edi
       call     [System.Collections.Generic.List`1[System.__Canon]:get_Item(int):System.__Canon:this]
       nop      
 
G_M000_IG09:
       add      rsp, 48
       pop      rbp
       ret      
 
; Total bytes of code 225

; Assembly listing for method RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 13264
; 4 inlinees with PGO data; 7 single block inlinees; 0 inlinees without PGO data

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
       je       G_M000_IG64
 
G_M000_IG05:
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyEnum:GetItem(RyuPy.Runtime.PyClass,System.Object):System.Object]
       jmp      G_M000_IG65
 
G_M000_IG06:
       test     rbx, rbx
       je       G_M000_IG14
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG14
 
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
       je       G_M000_IG64
 
G_M000_IG13:
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       jmp      G_M000_IG65
 
G_M000_IG14:
       test     rbx, rbx
       je       SHORT G_M000_IG18
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG18
 
G_M000_IG16:
       mov      r14, rbx
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyAbc:IsAbc(RyuPy.Runtime.PyNativeType):bool]
       test     eax, eax
       je       G_M000_IG64
 
G_M000_IG17:
       mov      rdi, r14
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTyping:SubscribeNative(System.Object,System.Object):System.Object]
       jmp      G_M000_IG65
 
G_M000_IG18:
       test     rbx, rbx
       je       G_M000_IG39
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG39
 
G_M000_IG20:
       test     r15, r15
       je       SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      rsi, r15
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyList:this]
       jmp      G_M000_IG33
 
G_M000_IG23:
       test     r15, r15
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r15], rsi
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rsi, qword ptr [r15+0x08]
       jmp      SHORT G_M000_IG31
 
G_M000_IG26:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG29
 
G_M000_IG28:
       xor      rsi, rsi
 
G_M000_IG29:
       test     rsi, rsi
       je       SHORT G_M000_IG34
 
G_M000_IG30:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       movsxd   rsi, dword ptr [rax]
 
G_M000_IG31:
       mov      rax, gword ptr [rbx+0x08]
       test     rax, rax
       je       G_M000_IG69
       mov      ecx, dword ptr [rax+0x10]
       mov      edx, ecx
       test     rsi, rsi
       jl       G_M000_IG70
 
G_M000_IG32:
       movsxd   rdx, edx
       cmp      rsi, rdx
       jae      G_M000_IG71
       cmp      esi, ecx
       jae      G_M000_IG72
       mov      rax, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rax+0x08]
       jae      G_M000_IG74
       mov      ecx, esi
       mov      rax, gword ptr [rax+8*rcx+0x10]
 
G_M000_IG33:
       jmp      G_M000_IG65
 
G_M000_IG34:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG36
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG68
 
G_M000_IG36:
       test     rsi, rsi
       je       SHORT G_M000_IG38
 
G_M000_IG37:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      byte  ptr [rax], 0
       setne    dil
       movzx    rdi, dil
       mov      esi, edi
       jmp      G_M000_IG31
 
G_M000_IG38:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyList:ToIndexSlow(System.Object):long]
       mov      rsi, rax
       jmp      G_M000_IG31
 
G_M000_IG39:
       test     rbx, rbx
       je       SHORT G_M000_IG42
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG42
 
G_M000_IG41:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyTuple:GetItem(System.Object):System.Object:this]
       jmp      G_M000_IG65
 
G_M000_IG42:
       test     rbx, rbx
       je       SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG45
 
G_M000_IG44:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyDict:Get(System.Object):System.Object:this]
       jmp      G_M000_IG65
 
G_M000_IG45:
       test     rbx, rbx
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG48
 
G_M000_IG47:
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyOps:GetString(System.String,System.Object):System.Object]
       jmp      G_M000_IG65
 
G_M000_IG48:
       test     rbx, rbx
       je       SHORT G_M000_IG56
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG56
 
G_M000_IG50:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG66
 
G_M000_IG52:
       test     rsi, rsi
       jne      SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyBytes:GetItem(long):System.Object:this]
 
G_M000_IG54:
       jmp      G_M000_IG65
 
G_M000_IG55:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyBytes:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyBytes:this]
       jmp      SHORT G_M000_IG54
 
G_M000_IG56:
       test     rbx, rbx
       je       SHORT G_M000_IG64
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG64
 
G_M000_IG58:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG60
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG67
 
G_M000_IG60:
       test     rsi, rsi
       jne      SHORT G_M000_IG63
 
G_M000_IG61:
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ToIndex(System.Object,System.String):long]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyByteArray:GetItem(long):System.Object:this]
 
G_M000_IG62:
       jmp      SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyByteArray:GetSlice(RyuPy.Runtime.PySlice):RyuPy.Runtime.PyByteArray:this]
       jmp      SHORT G_M000_IG62
 
G_M000_IG64:
       lea      rcx, [rbp-0x28]
       mov      rdi, rbx
       mov      rdx, r15
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:TryMethod1(System.Object,System.String,System.Object,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG73
       mov      rax, gword ptr [rbp-0x28]
 
G_M000_IG65:
       add      rsp, 16
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG66:
       xor      rsi, rsi
       jmp      G_M000_IG52
 
G_M000_IG67:
       xor      rsi, rsi
       jmp      SHORT G_M000_IG60
 
G_M000_IG68:
       xor      rsi, rsi
       jmp      G_M000_IG36
 
G_M000_IG69:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:ReadNumeric(long):System.Object:this]
       jmp      G_M000_IG33
 
G_M000_IG70:
       movsxd   rdi, edx
       add      rsi, rdi
       jmp      G_M000_IG32
 
G_M000_IG71:
       call     [RyuPy.Runtime.PyList:ListIndexError():System.Object]
       jmp      G_M000_IG33
 
G_M000_IG72:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x129E
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRepr:TypeName(System.Object):System.String]
       mov      rbx, rax
       mov      edi, 0x3930
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rdi, r14
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String,System.String):System.String]
       mov      rsi, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTypeError:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG74:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 1140

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(System.Object):System.Object:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 24100
; 3 inlinees with PGO data; 1 single block inlinees; 0 inlinees without PGO data

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
       jmp      G_M000_IG15
 
G_M000_IG05:
       test     rsi, rsi
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rsi, qword ptr [rsi+0x08]
       jmp      SHORT G_M000_IG13
 
G_M000_IG08:
       mov      rdi, rsi
       test     rdi, rdi
       je       SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG11
 
G_M000_IG10:
       xor      rdi, rdi
 
G_M000_IG11:
       test     rdi, rdi
       je       SHORT G_M000_IG16
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       movsxd   rsi, dword ptr [rax]
 
G_M000_IG13:
       mov      rax, gword ptr [rbx+0x08]
       test     rax, rax
       je       G_M000_IG22
       mov      ecx, dword ptr [rax+0x10]
       mov      edx, ecx
       test     rsi, rsi
       jl       G_M000_IG23
 
G_M000_IG14:
       movsxd   rdx, edx
       cmp      rsi, rdx
       jae      G_M000_IG24
       cmp      esi, ecx
       jae      G_M000_IG25
       mov      rax, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rax+0x08]
       jae      G_M000_IG26
       mov      ecx, esi
       mov      rax, gword ptr [rax+8*rcx+0x10]
 
G_M000_IG15:
       add      rsp, 8
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG16:
       mov      rdi, rsi
       test     rdi, rdi
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG19
 
G_M000_IG18:
       xor      rdi, rdi
 
G_M000_IG19:
       test     rdi, rdi
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      byte  ptr [rax], 0
       setne    dil
       movzx    rdi, dil
       mov      esi, edi
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      rdi, rsi
       call     [RyuPy.Runtime.PyList:ToIndexSlow(System.Object):long]
       mov      rsi, rax
       jmp      G_M000_IG13
 
G_M000_IG22:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyList:ReadNumeric(long):System.Object:this]
       jmp      SHORT G_M000_IG15
 
G_M000_IG23:
       movsxd   rdi, edx
       add      rsi, rdi
       jmp      G_M000_IG14
 
G_M000_IG24:
       call     [RyuPy.Runtime.PyList:ListIndexError():System.Object]
       jmp      SHORT G_M000_IG15
 
G_M000_IG25:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG26:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 316

; Assembly listing for method RyuPy.Runtime.PyList:ToIndex(System.Object):long (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 23644

G_M000_IG01:
       push     rbp
       mov      rbp, rsp
 
G_M000_IG02:
       test     rdi, rdi
       je       SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      rax, qword ptr [rdi+0x08]
       jmp      SHORT G_M000_IG10
 
G_M000_IG05:
       mov      rsi, rdi
       test     rsi, rsi
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       xor      rsi, rsi
 
G_M000_IG08:
       test     rsi, rsi
       je       SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       movsxd   rax, dword ptr [rax]
 
G_M000_IG10:
       pop      rbp
       ret      
 
G_M000_IG11:
       mov      rsi, rdi
       test     rsi, rsi
       je       SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       xor      rsi, rsi
 
G_M000_IG14:
       test     rsi, rsi
       je       SHORT G_M000_IG16
 
G_M000_IG15:
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      byte  ptr [rax], 0
       setne    dil
       movzx    rdi, dil
       mov      eax, edi
       jmp      SHORT G_M000_IG10
 
G_M000_IG16:
       call     [RyuPy.Runtime.PyList:ToIndexSlow(System.Object):long]
       jmp      SHORT G_M000_IG10
 
; Total bytes of code 154

; Assembly listing for method RyuPy.Runtime.PyList:GetItem(long):System.Object:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Dynamic PGO
; rbp based frame
; fully interruptible
; with Dynamic PGO: fgCalledCount is 23252
; 1 inlinees with PGO data; 1 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       mov      rbp, rsp
 
G_M000_IG02:
       mov      rax, gword ptr [rdi+0x08]
       test     rax, rax
       je       SHORT G_M000_IG06
       mov      ecx, dword ptr [rax+0x10]
       mov      edx, ecx
       movsxd   rdi, edx
       add      rdi, rsi
       test     rsi, rsi
       cmovge   rdi, rsi
       movsxd   rdx, edx
       cmp      rdi, rdx
       jae      SHORT G_M000_IG07
 
G_M000_IG03:
       cmp      edi, ecx
       jae      SHORT G_M000_IG08
 
G_M000_IG04:
       mov      rax, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG09
       mov      ecx, edi
       mov      rax, gword ptr [rax+8*rcx+0x10]
 
G_M000_IG05:
       pop      rbp
       ret      
 
G_M000_IG06:
       pop      rbp
       tail.jmp [RyuPy.Runtime.PyList:ReadNumeric(long):System.Object:this]
 
G_M000_IG07:
       pop      rbp
       tail.jmp [RyuPy.Runtime.PyList:ListIndexError():System.Object]
 
G_M000_IG08:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG09:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 88

