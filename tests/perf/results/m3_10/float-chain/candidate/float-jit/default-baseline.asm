; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mul_add(System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 224
       lea      rbp, [rsp+0xE0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x50], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
 
G_M000_IG02:
       mov      dword ptr [rbp-0xD8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x60], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x50], rax
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x90], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xD8]
       dec      eax
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0xD8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xD8]
       mov      esi, 77
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
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       xor      edi, edi
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rcx, gword ptr [rbp-0x50]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x50], rax
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdi, qword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xD0]
 
G_M000_IG08:
       add      rsp, 224
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 597

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_norm(System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 336
       lea      rbp, [rsp+0x150]
       xor      eax, eax
       mov      qword ptr [rbp-0x148], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      qword ptr [rbp-0x50], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x150], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x60], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x68], rax
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x50], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      qword ptr [rbp-0x98], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x150]
       mov      esi, 86
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x98], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      rax, qword ptr [rbp-0x88]
       cmp      rax, qword ptr [rbp-0x90]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      rax, qword ptr [rbp-0x88]
       cmp      rax, qword ptr [rbp-0x90]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       cmp      dword ptr [rbp-0x9C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xAC]
       and      eax, dword ptr [rbp-0xA4]
       movzx    rax, al
       mov      dword ptr [rbp-0xB0], eax
       mov      eax, dword ptr [rbp-0xA8]
       or       eax, dword ptr [rbp-0xB0]
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       cmp      dword ptr [rbp-0xB4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x38]
       mov      edi, 2
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 3
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp-0xD0]
       mov      edi, 4
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0x48]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 5
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0xE0]
       mov      edi, 6
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x50], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x138], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x140], rax
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x108], rax
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0x118], rax
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x128], rax
       mov      rax, gword ptr [rbp-0x108]
       mov      gword ptr [rbp-0x38], rax
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x40], rax
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, qword ptr [rbp-0x88]
       add      rax, qword ptr [rbp-0x98]
       jo       SHORT G_M000_IG10
       mov      qword ptr [rbp-0x88], rax
 
G_M000_IG07:
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, qword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x130]
 
G_M000_IG09:
       add      rsp, 336
       pop      rbp
       ret      
 
G_M000_IG10:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 867

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (Instrumented Tier0)
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
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xF8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x90], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xF8]
       dec      eax
       mov      dword ptr [rbp-0xF8], eax
       cmp      dword ptr [rbp-0xF8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xF8]
       mov      esi, 88
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
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x80]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdx, gword ptr [rbp-0x48]
       mov      rcx, gword ptr [rbp-0xC0]
       mov      edi, 7
       mov      esi, 6
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0xC8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x50], rax
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0xD8]
       mov      edi, 8
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x40], rax
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 27
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG08:
       add      rsp, 256
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 770

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mul_add(System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4d
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 7
; 52 inlinees with PGO data; 46 single block inlinees; 42 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 336
       mov      qword ptr [rsp+0x238], r15
       mov      qword ptr [rsp+0x230], r14
       mov      qword ptr [rsp+0x228], r13
       mov      qword ptr [rsp+0x220], r12
       mov      qword ptr [rsp+0x218], rbx
       lea      rbp, [rsp+0x150]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      qword ptr [rbp-0x50], rax
       mov      rbx, gword ptr [rbp+0xB8]
       mov      r15, gword ptr [rbp+0xB0]
       mov      r13, gword ptr [rbp+0xA0]
       mov      r14, qword ptr [rbp+0x70]
       mov      r12, qword ptr [rbp+0x68]
       mov      rax, qword ptr [rbp+0x60]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x60], rax
       test     rax, rax
       setg     r8b
       movzx    r8, r8b
       mov      dword ptr [rbp-0x100], r8d
       jmp      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rdx, r13
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG04:
       mov      rax, qword ptr [rbp+0x60]
       add      r14, rax
       jo       G_M000_IG120
       mov      qword ptr [rbp+0x60], rax
       mov      r8d, dword ptr [rbp-0x100]
 
G_M000_IG05:
       mov      edx, r8d
       cmp      r14, r12
       setl     dil
       movzx    rdi, dil
       and      edi, edx
       cmp      r14, r12
       setg     sil
       movzx    rsi, sil
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, esi
       or       edx, edi
       je       G_M000_IG77
 
G_M000_IG06:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      edx
       mov      r9d, edx
       cmp      r9d, dword ptr [rsi+0x08]
       jae      G_M000_IG119
       cmp      dword ptr [rsi+4*r9+0x10], 10
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG119
       mov      edx, edx
       mov      dword ptr [rdi+4*rdx+0x10], 10
 
G_M000_IG09:
       test     rbx, rbx
       je       G_M000_IG41
 
G_M000_IG10:
       mov      r9, qword ptr [rbx]
       mov      rdx, 0xD1FFAB1E
       cmp      r9, rdx
       jne      G_M000_IG35
 
G_M000_IG11:
       mov      rsi, qword ptr [rbx+0x08]
       test     r15, r15
       je       G_M000_IG41
 
G_M000_IG12:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG33
 
G_M000_IG13:
       mov      r9, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x48], r9
       lea      rdi, [rbp-0x68]
       mov      rdx, rsi
       mulx     rdx, r10, r9
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x60], r10
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r9
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r10
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG29
 
G_M000_IG14:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x138], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0xF0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      r9d, dword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x138]
       test     rcx, rcx
       jne      G_M000_IG22
 
G_M000_IG15:
       test     rax, rax
       jne      G_M000_IG82
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       movsxd   rsi, dword ptr [rbp-0xF0]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r10d, dword ptr [rbp-0x80]
 
G_M000_IG16:
       test     rax, rax
       jne      G_M000_IG83
 
G_M000_IG17:
       test     rax, rax
       jne      G_M000_IG92
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG27
 
G_M000_IG18:
       mov      edi, -1
 
G_M000_IG19:
       test     edi, edi
       mov      gword ptr [rbp-0x140], rax
       mov      dword ptr [rbp-0xF4], r10d
       jg       SHORT G_M000_IG28
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, gword ptr [rbp-0x140]
       mov      esi, dword ptr [rbp-0xF4]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x110]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG21:
       jmp      G_M000_IG42
 
G_M000_IG22:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG23:
       test     rax, rax
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG25:
       mov      edx, dword ptr [rbp-0xF0]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG16
 
G_M000_IG26:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG25
 
G_M000_IG27:
       xor      edi, edi
       jmp      SHORT G_M000_IG19
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x108], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x140]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x108]
       mov      edi, dword ptr [rbp-0xF4]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG21
 
G_M000_IG29:
       lea      rdx, [r10+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x60]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG31:
       jmp      G_M000_IG21
 
G_M000_IG32:
       test     rdx, rdx
       jl       G_M000_IG120
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG119
       mov      rsi, gword ptr [rdx+8*rsi+0x10]
       jmp      SHORT G_M000_IG31
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG41
 
G_M000_IG34:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG42
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      r9, rdi
       jne      G_M000_IG41
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r15, r15
       je       G_M000_IG41
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG39
 
G_M000_IG38:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG42
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:
       mov      rdx, r15
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG42:
       test     rsi, rsi
       je       G_M000_IG03
 
G_M000_IG43:
       mov      rax, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG61
 
G_M000_IG44:
       mov      rax, qword ptr [rsi+0x08]
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG59
 
G_M000_IG46:
       mov      r13, qword ptr [r13+0x08]
       lea      rcx, [rax+r13]
       mov      qword ptr [rbp-0xB8], rcx
       mov      rdi, rax
       xor      rdi, rcx
       mov      rsi, r13
       xor      rsi, rcx
       and      rdi, rsi
       jge      G_M000_IG55
 
G_M000_IG47:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x148], rax
       mov      ecx, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xF8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x148]
       mov      esi, dword ptr [rbp-0xF8]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r13, rax
       mov      eax, edx
       mov      dword ptr [rbp-0xFC], edx
       test     r13, r13
       jne      G_M000_IG100
 
G_M000_IG48:
       test     r13, r13
       jne      G_M000_IG109
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG53
 
G_M000_IG49:
       mov      edi, -1
 
G_M000_IG50:
       test     edi, edi
       jg       SHORT G_M000_IG54
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x128], rax
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0xFC]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x128]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG52:
       jmp      G_M000_IG04
 
G_M000_IG53:
       xor      edi, edi
       jmp      SHORT G_M000_IG50
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x120], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      r13, gword ptr [rbp-0x120]
       mov      edi, dword ptr [rbp-0xFC]
       mov      dword ptr [r13+0x10], edi
       jmp      SHORT G_M000_IG52
 
G_M000_IG55:
       lea      r13, [rcx+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG57:
       jmp      SHORT G_M000_IG52
 
G_M000_IG58:
       test     r13, r13
       jl       G_M000_IG120
       mov      rdi, r13
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG119
       mov      r13, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG57
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG03
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG67
 
G_M000_IG62:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG65
 
G_M000_IG64:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG03
 
G_M000_IG66:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG03
 
G_M000_IG68:
       mov      rax, rsi
       mov      gword ptr [rbp-0x118], rax
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG03
 
G_M000_IG70:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG75
 
G_M000_IG71:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG74
 
G_M000_IG72:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xEC], ecx
       mov      esi, ecx
       add      esi, dword ptr [r13+0x08]
       js       G_M000_IG118
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x130], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x118]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xEC]
       mov      rax, gword ptr [rbp-0x130]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0x130]
 
G_M000_IG73:
       jmp      G_M000_IG04
 
G_M000_IG74:
       mov      r13, rax
       jmp      SHORT G_M000_IG73
 
G_M000_IG75:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG117
 
G_M000_IG76:
       jmp      SHORT G_M000_IG73
 
G_M000_IG77:
       mov      rcx, qword ptr [rbp+0x90]
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG80
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, rdi
       dec      edx
       mov      esi, edx
       cmp      esi, dword ptr [rax+0x08]
       jae      G_M000_IG119
       cmp      dword ptr [rax+4*rsi+0x10], 11
       je       SHORT G_M000_IG80
 
G_M000_IG79:
       mov      eax, edx
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG119
       mov      dword ptr [rdi+4*rax+0x10], 11
 
G_M000_IG80:
       mov      rdi, rcx
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG81:
       add      rsp, 536
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG82:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG23
 
G_M000_IG83:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG84
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG85
 
G_M000_IG84:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG91
 
G_M000_IG85:
       cmp      edi, 2
       je       SHORT G_M000_IG86
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG119
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG87
 
G_M000_IG86:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG87:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG88
       mov      ecx, -1
       jmp      SHORT G_M000_IG90
 
G_M000_IG88:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG89
       mov      ecx, 1
       jmp      SHORT G_M000_IG90
 
G_M000_IG89:
       xor      ecx, ecx
 
G_M000_IG90:
       imul     ecx, r10d
 
G_M000_IG91:
       test     ecx, ecx
       jge      G_M000_IG17
       mov      gword ptr [rbp-0x140], rax
       mov      dword ptr [rbp-0xF4], r10d
       jmp      G_M000_IG28
 
G_M000_IG92:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG93
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG94
 
G_M000_IG93:
       mov      dword ptr [rbp-0xF4], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0xF4]
       jmp      G_M000_IG19
 
G_M000_IG94:
       cmp      edi, 2
       je       SHORT G_M000_IG95
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG119
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG96
 
G_M000_IG95:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x140], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x140]
 
G_M000_IG96:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG97
       mov      ecx, -1
       jmp      SHORT G_M000_IG99
 
G_M000_IG97:
       test     rdi, rdi
       jge      SHORT G_M000_IG98
       mov      ecx, 1
       jmp      SHORT G_M000_IG99
 
G_M000_IG98:
       xor      ecx, ecx
 
G_M000_IG99:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG19
 
G_M000_IG100:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG101
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG102
 
G_M000_IG101:
       mov      ecx, eax
       jmp      SHORT G_M000_IG108
 
G_M000_IG102:
       cmp      edi, 2
       je       SHORT G_M000_IG103
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG119
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG104:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG105
       mov      ecx, -1
       jmp      SHORT G_M000_IG107
 
G_M000_IG105:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG106
       mov      ecx, 1
       jmp      SHORT G_M000_IG107
 
G_M000_IG106:
       xor      ecx, ecx
 
G_M000_IG107:
       imul     ecx, eax
 
G_M000_IG108:
       test     ecx, ecx
       jge      G_M000_IG48
       jmp      G_M000_IG54
 
G_M000_IG109:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG110
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG111
 
G_M000_IG110:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0xFC]
       jmp      G_M000_IG50
 
G_M000_IG111:
       cmp      edi, 2
       je       SHORT G_M000_IG112
       cmp      dword ptr [r13+0x08], 0
       jbe      SHORT G_M000_IG119
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG113
 
G_M000_IG112:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG113:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG114
       mov      ecx, -1
       jmp      SHORT G_M000_IG116
 
G_M000_IG114:
       test     rdi, rdi
       jge      SHORT G_M000_IG115
       mov      ecx, 1
       jmp      SHORT G_M000_IG116
 
G_M000_IG115:
       xor      ecx, ecx
 
G_M000_IG116:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG50
 
G_M000_IG117:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG73
 
G_M000_IG118:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG119:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG120:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2832

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_norm(System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x56
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 60 inlinees with PGO data; 64 single block inlinees; 85 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 640
       mov      qword ptr [rsp+0x3D8], r15
       mov      qword ptr [rsp+0x3D0], r14
       mov      qword ptr [rsp+0x3C8], r13
       mov      qword ptr [rsp+0x3C0], r12
       mov      qword ptr [rsp+0x3B8], rbx
       lea      rbp, [rsp+0x280]
       xor      eax, eax
       mov      qword ptr [rbp-0x1C8], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      rbx, gword ptr [rbp+0x128]
       mov      r15, gword ptr [rbp+0x120]
       mov      r14, gword ptr [rbp+0x118]
       mov      rax, gword ptr [rbp+0x110]
       mov      r13, qword ptr [rbp+0xD8]
       mov      r12, qword ptr [rbp+0xD0]
       mov      rcx, qword ptr [rbp+0xC8]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xC8], rcx
       test     rcx, rcx
       setg     r9b
       movzx    r9, r9b
       mov      dword ptr [rbp-0x1F8], r9d
       jmp      G_M000_IG105
 
G_M000_IG03:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      G_M000_IG124
 
G_M000_IG04:
       xor      edi, edi
       jmp      G_M000_IG117
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      gword ptr [rbp-0x208], rcx
       lea      rdi, bword ptr [rcx+0x08]
       mov      rsi, gword ptr [rbp-0x250]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x208]
       mov      edi, dword ptr [rbp-0x1E0]
       mov      dword ptr [rcx+0x10], edi
       jmp      G_M000_IG119
 
G_M000_IG06:
       lea      rax, [r10+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x58]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG08:
       jmp      G_M000_IG120
 
G_M000_IG09:
       test     rax, rax
       jl       G_M000_IG166
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG165
       mov      rax, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG08
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG12
 
G_M000_IG11:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x200], rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdx, rbx
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x200], rax
 
G_M000_IG13:
       test     r15, r15
       je       G_M000_IG37
 
G_M000_IG14:
       mov      rcx, qword ptr [r15]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       jne      G_M000_IG35
 
G_M000_IG15:
       mov      rcx, qword ptr [r15+0x08]
       mov      rsi, rcx
       mov      qword ptr [rbp-0x98], rcx
       lea      rdi, [rbp-0xB8]
       mov      rdx, rsi
       mulx     rdx, r8, rcx
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0xB8]
       mov      qword ptr [rbp-0xB0], r8
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG31
 
G_M000_IG16:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x258], rax
       mov      ecx, dword ptr [rbp-0xA0]
       mov      dword ptr [rbp-0x1E4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, qword ptr [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      r9d, dword ptr [rbp-0xC0]
       mov      rcx, gword ptr [rbp-0x258]
       test     rcx, rcx
       jne      G_M000_IG24
 
G_M000_IG17:
       test     rax, rax
       jne      G_M000_IG145
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       movsxd   rsi, dword ptr [rbp-0x1E4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xD8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      r10d, dword ptr [rbp-0xD0]
 
G_M000_IG18:
       test     rax, rax
       jne      G_M000_IG146
 
G_M000_IG19:
       test     rax, rax
       jne      G_M000_IG155
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG29
 
G_M000_IG20:
       mov      edi, -1
 
G_M000_IG21:
       test     edi, edi
       mov      gword ptr [rbp-0x260], rax
       mov      dword ptr [rbp-0x1E8], r10d
       jg       SHORT G_M000_IG30
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x220], rax
       mov      rdi, gword ptr [rbp-0x260]
       mov      esi, dword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x220]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG23:
       mov      rcx, rdx
       mov      rax, rcx
       jmp      G_M000_IG38
 
G_M000_IG24:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG25:
       test     rax, rax
       je       SHORT G_M000_IG28
 
G_M000_IG26:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG27:
       mov      edx, dword ptr [rbp-0x1E4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG18
 
G_M000_IG28:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG27
 
G_M000_IG29:
       xor      edi, edi
       jmp      G_M000_IG21
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x218], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x260]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x218]
       mov      edi, dword ptr [rbp-0x1E8]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG23
 
G_M000_IG31:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG34
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG33:
       jmp      G_M000_IG23
 
G_M000_IG34:
       test     rcx, rcx
       jl       G_M000_IG166
       mov      rdx, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG165
       mov      rdx, gword ptr [rdi+8*rdx+0x10]
       jmp      SHORT G_M000_IG33
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG37
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      rax, rcx
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:
       mov      rdx, r15
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG38:
       mov      rcx, gword ptr [rbp-0x200]
       test     rcx, rcx
       je       G_M000_IG67
 
G_M000_IG39:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG51
 
G_M000_IG40:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG67
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG49
 
G_M000_IG42:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0xF8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x100], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG45
 
G_M000_IG43:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x268], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x1EC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x120], xmm0
       lea      rdi, [rbp-0x120]
       mov      rsi, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x268]
       mov      esi, dword ptr [rbp-0x1EC]
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG44:
       mov      gword ptr [rbp-0x228], rax
       jmp      G_M000_IG68
 
G_M000_IG45:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG48
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x100]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG47:
       jmp      SHORT G_M000_IG44
 
G_M000_IG48:
       test     rax, rax
       jl       G_M000_IG166
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG165
       mov      rax, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG47
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG67
 
G_M000_IG50:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x128], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x128]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x228], rax
       jmp      G_M000_IG68
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG57
 
G_M000_IG52:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       test     rax, rax
       je       G_M000_IG67
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG55
 
G_M000_IG54:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x228], rax
       jmp      G_M000_IG68
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG67
 
G_M000_IG56:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x228], rax
       jmp      G_M000_IG68
 
G_M000_IG57:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG67
 
G_M000_IG58:
       mov      rsi, rcx
       mov      gword ptr [rbp-0x230], rsi
       test     rax, rax
       je       G_M000_IG67
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG67
 
G_M000_IG60:
       mov      gword ptr [rbp-0x238], rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG65
 
G_M000_IG61:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG64
 
G_M000_IG62:
       mov      ecx, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x134], ecx
       mov      edi, ecx
       add      edi, dword ptr [rax+0x08]
       js       G_M000_IG164
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x240], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x230]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x134]
       mov      rax, gword ptr [rbp-0x240]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rsi, gword ptr [rbp-0x238]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rax, gword ptr [rbp-0x240]
 
G_M000_IG63:
       mov      gword ptr [rbp-0x228], rax
       jmp      SHORT G_M000_IG68
 
G_M000_IG64:
       mov      rax, rsi
       jmp      SHORT G_M000_IG63
 
G_M000_IG65:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG163
 
G_M000_IG66:
       jmp      SHORT G_M000_IG63
 
G_M000_IG67:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x228], rax
 
G_M000_IG68:
       test     r14, r14
       je       G_M000_IG79
 
G_M000_IG69:
       mov      rcx, qword ptr [r14]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       jne      G_M000_IG77
 
G_M000_IG70:
       mov      rcx, qword ptr [r14+0x08]
       mov      rsi, rcx
       mov      qword ptr [rbp-0x150], rcx
       lea      rdi, [rbp-0x170]
       mov      rdx, rsi
       mulx     rdx, r8, rcx
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x170]
       mov      qword ptr [rbp-0x168], r8
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG73
 
G_M000_IG71:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x160]
       mov      gword ptr [rbp-0x270], rax
       mov      ecx, dword ptr [rbp-0x158]
       mov      dword ptr [rbp-0x1F0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x180], xmm0
       lea      rdi, [rbp-0x180]
       mov      rsi, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x270]
       mov      esi, dword ptr [rbp-0x1F0]
       mov      rdx, gword ptr [rbp-0x180]
       mov      rcx, qword ptr [rbp-0x178]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG72:
       jmp      G_M000_IG80
 
G_M000_IG73:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG75:
       jmp      SHORT G_M000_IG72
 
G_M000_IG76:
       test     rcx, rcx
       jl       G_M000_IG166
       mov      rdx, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG165
       mov      rdx, gword ptr [rdi+8*rdx+0x10]
       jmp      SHORT G_M000_IG75
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG79
 
G_M000_IG78:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm0
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x140]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:
       mov      rdx, r14
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG80:
       mov      rax, gword ptr [rbp-0x228]
       test     rax, rax
       je       G_M000_IG103
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG93
 
G_M000_IG82:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG103
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG91
 
G_M000_IG84:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1A0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1A8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG87
 
G_M000_IG85:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1B8]
       mov      gword ptr [rbp-0x278], rax
       mov      ecx, dword ptr [rbp-0x1B0]
       mov      dword ptr [rbp-0x1F4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
       lea      rdi, [rbp-0x1C8]
       mov      rsi, qword ptr [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x278]
       mov      esi, dword ptr [rbp-0x1F4]
       mov      rdx, gword ptr [rbp-0x1C8]
       mov      rcx, qword ptr [rbp-0x1C0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG86:
       jmp      G_M000_IG104
 
G_M000_IG87:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG90
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG89:
       jmp      SHORT G_M000_IG86
 
G_M000_IG90:
       test     rax, rax
       jl       G_M000_IG166
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG165
       test     rax, rax
       jl       G_M000_IG166
       mov      rdi, rax
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG89
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG103
 
G_M000_IG92:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1D0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1D0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG104
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG99
 
G_M000_IG94:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm0
       test     rdx, rdx
       je       G_M000_IG103
 
G_M000_IG95:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG97
 
G_M000_IG96:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG104
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG103
 
G_M000_IG98:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1D8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG104
 
G_M000_IG99:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG103
 
G_M000_IG100:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG103
 
G_M000_IG102:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG104:
       mov      gword ptr [rbp+0x110], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, qword ptr [rbp+0xC8]
       add      r13, rcx
       jo       G_M000_IG166
       mov      qword ptr [rbp+0xC8], rcx
       mov      rdx, rbx
       mov      rbx, r15
       mov      r15, r14
       mov      r14, rdx
       mov      rax, gword ptr [rbp+0x110]
       mov      r9d, dword ptr [rbp-0x1F8]
 
G_M000_IG105:
       mov      edx, r9d
       cmp      r13, r12
       setl     dil
       movzx    rdi, dil
       and      edi, edx
       cmp      r13, r12
       setg     sil
       movzx    rsi, sil
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, esi
       or       edx, edi
       je       G_M000_IG125
 
G_M000_IG106:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG109
 
G_M000_IG107:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      edx
       mov      eax, edx
       cmp      eax, dword ptr [rsi+0x08]
       jae      G_M000_IG165
       cmp      dword ptr [rsi+4*rax+0x10], 17
       je       SHORT G_M000_IG109
 
G_M000_IG108:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG165
       mov      edx, edx
       mov      dword ptr [rdi+4*rdx+0x10], 17
 
G_M000_IG109:
       test     rbx, rbx
       je       G_M000_IG12
 
G_M000_IG110:
       mov      rax, qword ptr [rbx]
       mov      rdx, 0xD1FFAB1E
       cmp      rax, rdx
       jne      G_M000_IG10
 
G_M000_IG111:
       mov      rax, qword ptr [rbx+0x08]
       mov      rsi, rax
       mov      qword ptr [rbp-0x40], rax
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r10, rax
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x58], r10
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       sub      rdx, rdi
       mov      rdi, r10
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG06
 
G_M000_IG112:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x248], rax
       mov      ecx, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x1DC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x40]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x248]
       test     rcx, rcx
       jne      G_M000_IG121
 
G_M000_IG113:
       test     rax, rax
       jne      G_M000_IG127
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x1DC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      r10d, dword ptr [rbp-0x78]
 
G_M000_IG114:
       test     rax, rax
       jne      G_M000_IG128
 
G_M000_IG115:
       test     rax, rax
       jne      G_M000_IG137
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG04
 
G_M000_IG116:
       mov      edi, -1
 
G_M000_IG117:
       test     edi, edi
       mov      gword ptr [rbp-0x250], rax
       mov      dword ptr [rbp-0x1E0], r10d
       jg       G_M000_IG05
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x210], rax
       mov      rdi, gword ptr [rbp-0x250]
       mov      esi, dword ptr [rbp-0x1E0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rcx, gword ptr [rbp-0x210]
       mov      qword ptr [rcx+0x08], rax
 
G_M000_IG119:
       mov      rax, rcx
 
G_M000_IG120:
       mov      gword ptr [rbp-0x200], rax
       jmp      G_M000_IG13
 
G_M000_IG121:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG122:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG123:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG124:
       mov      edx, dword ptr [rbp-0x1DC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG114
 
G_M000_IG125:
       mov      gword ptr [rbp+0x110], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp+0x100]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp+0x110]
 
G_M000_IG126:
       add      rsp, 952
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG127:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG122
 
G_M000_IG128:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG129
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG136
 
G_M000_IG130:
       cmp      edi, 2
       je       SHORT G_M000_IG131
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG132
 
G_M000_IG131:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG133
       mov      ecx, -1
       jmp      SHORT G_M000_IG135
 
G_M000_IG133:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG134
       mov      ecx, 1
       jmp      SHORT G_M000_IG135
 
G_M000_IG134:
       xor      ecx, ecx
 
G_M000_IG135:
       imul     ecx, r10d
 
G_M000_IG136:
       test     ecx, ecx
       jge      G_M000_IG115
       mov      gword ptr [rbp-0x250], rax
       mov      dword ptr [rbp-0x1E0], r10d
       jmp      G_M000_IG05
 
G_M000_IG137:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG138
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG139
 
G_M000_IG138:
       mov      dword ptr [rbp-0x1E0], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x1E0]
       jmp      G_M000_IG117
 
G_M000_IG139:
       cmp      edi, 2
       je       SHORT G_M000_IG140
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG141
 
G_M000_IG140:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x250], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x250]
 
G_M000_IG141:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG142
       mov      ecx, -1
       jmp      SHORT G_M000_IG144
 
G_M000_IG142:
       test     rdi, rdi
       jge      SHORT G_M000_IG143
       mov      ecx, 1
       jmp      SHORT G_M000_IG144
 
G_M000_IG143:
       xor      ecx, ecx
 
G_M000_IG144:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG117
 
G_M000_IG145:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG25
 
G_M000_IG146:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG147
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG148
 
G_M000_IG147:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG154
 
G_M000_IG148:
       cmp      edi, 2
       je       SHORT G_M000_IG149
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG150
 
G_M000_IG149:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG150:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG151
       mov      ecx, -1
       jmp      SHORT G_M000_IG153
 
G_M000_IG151:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG152
       mov      ecx, 1
       jmp      SHORT G_M000_IG153
 
G_M000_IG152:
       xor      ecx, ecx
 
G_M000_IG153:
       imul     ecx, r10d
 
G_M000_IG154:
       test     ecx, ecx
       jge      G_M000_IG19
       mov      gword ptr [rbp-0x260], rax
       mov      dword ptr [rbp-0x1E8], r10d
       jmp      G_M000_IG30
 
G_M000_IG155:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG156
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG157
 
G_M000_IG156:
       mov      dword ptr [rbp-0x1E8], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x1E8]
       jmp      G_M000_IG21
 
G_M000_IG157:
       cmp      edi, 2
       je       SHORT G_M000_IG158
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG165
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG159
 
G_M000_IG158:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x260], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x260]
 
G_M000_IG159:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG160
       mov      ecx, -1
       jmp      SHORT G_M000_IG162
 
G_M000_IG160:
       test     rdi, rdi
       jge      SHORT G_M000_IG161
       mov      ecx, 1
       jmp      SHORT G_M000_IG162
 
G_M000_IG161:
       xor      ecx, ecx
 
G_M000_IG162:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG21
 
G_M000_IG163:
       mov      rax, 0xD1FFAB1E
       jmp      G_M000_IG63
 
G_M000_IG164:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG165:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG166:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4405

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x58
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 37 inlinees with PGO data; 1 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 256
       mov      qword ptr [rsp+0x208], r15
       mov      qword ptr [rsp+0x200], r14
       mov      qword ptr [rsp+0x1F8], r13
       mov      qword ptr [rsp+0x1F0], r12
       mov      qword ptr [rsp+0x1E8], rbx
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       mov      rbx, gword ptr [rbp+0xD8]
       mov      r14, gword ptr [rbp+0xD0]
       mov      r15, qword ptr [rbp+0x90]
       mov      r13, qword ptr [rbp+0x88]
       mov      r12, qword ptr [rbp+0x80]
 
G_M000_IG02:
       test     r12, r12
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0xDC], ecx
       jmp      G_M000_IG53
 
G_M000_IG03:
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x50], rcx
       test     rsi, rsi
       je       G_M000_IG69
 
G_M000_IG04:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdx
       jne      G_M000_IG12
 
G_M000_IG05:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x60], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x68], rdx
       lea      rdx, [rbp-0x68]
       mov      rdi, rcx
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x68]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0xF0], rax
       mov      ecx, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0xD4], ecx
       mov      rdi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0xF0]
       mov      esi, dword ptr [rbp-0xD4]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG07:
       jmp      G_M000_IG70
 
G_M000_IG08:
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x80], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x80]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG10:
       jmp      SHORT G_M000_IG07
 
G_M000_IG11:
       test     rcx, rcx
       jl       G_M000_IG88
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG87
       mov      rsi, gword ptr [rdx+8*rsi+0x10]
       jmp      SHORT G_M000_IG10
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG69
 
G_M000_IG13:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x88], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG70
 
G_M000_IG14:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       test     rsi, rsi
       je       SHORT G_M000_IG17
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG17
 
G_M000_IG16:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG70
 
G_M000_IG17:
       mov      rdi, rsi
       test     rdi, rdi
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG20
 
G_M000_IG19:
       xor      rdi, rdi
 
G_M000_IG20:
       test     rdi, rdi
       je       G_M000_IG69
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x90], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG70
 
G_M000_IG22:
       mov      rax, qword ptr [rsi+0x08]
       test     r14, r14
       je       G_M000_IG51
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG41
 
G_M000_IG24:
       mov      r14, qword ptr [r14+0x08]
       lea      rcx, [rax+r14]
       mov      qword ptr [rbp-0xB0], rcx
       mov      rdi, rax
       xor      rdi, rcx
       mov      rdx, r14
       xor      rdx, rcx
       and      rdi, rdx
       jl       G_M000_IG86
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG40
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG26:
       mov      r14, rax
       jmp      G_M000_IG52
 
G_M000_IG27:
       test     rdx, rdx
       jl       G_M000_IG88
       mov      rdi, rdx
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG87
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rdx, rdi
       mov      rax, rdx
       jmp      G_M000_IG56
 
G_M000_IG28:
       mov      gword ptr [rbp-0xE8], rax
       mov      rsi, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x30], rsi
       test     rdx, rdx
       mov      rax, gword ptr [rbp-0xE8]
       je       G_M000_IG62
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG62
 
G_M000_IG30:
       mov      r8, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x38], r8
       test     r8, r8
       jle      G_M000_IG62
 
G_M000_IG31:
       test     rsi, rsi
       jl       G_M000_IG62
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x30]
       cqo      
       idiv     rdx:rax, qword ptr [rbp-0x38]
       mov      qword ptr [rsi+0x08], rdx
       jmp      G_M000_IG63
 
G_M000_IG33:
       test     rdx, rdx
       je       SHORT G_M000_IG35
 
G_M000_IG34:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       G_M000_IG62
 
G_M000_IG35:
       mov      rsi, rdx
       test     rsi, rsi
       je       SHORT G_M000_IG38
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG38
 
G_M000_IG37:
       xor      rsi, rsi
 
G_M000_IG38:
       test     rsi, rsi
       je       G_M000_IG62
 
G_M000_IG39:
       mov      gword ptr [rbp-0xE8], rax
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xE8]
       mov      rdx, gword ptr [rbp+0x50]
       jmp      G_M000_IG62
 
G_M000_IG40:
       test     r14, r14
       jl       G_M000_IG88
       mov      rax, r14
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG87
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      G_M000_IG26
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG51
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG52
 
G_M000_IG43:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       test     r14, r14
       je       SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      SHORT G_M000_IG52
 
G_M000_IG46:
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG49
 
G_M000_IG48:
       xor      rdi, rdi
 
G_M000_IG49:
       test     rdi, rdi
       je       SHORT G_M000_IG51
 
G_M000_IG50:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:
       mov      rdx, r14
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r14, rax
 
G_M000_IG52:
       add      r15, r12
       jo       G_M000_IG88
       mov      ecx, dword ptr [rbp-0xDC]
 
G_M000_IG53:
       mov      edi, ecx
       cmp      r15, r13
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       cmp      r15, r13
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG79
 
G_M000_IG54:
       lea      rdx, [r15+0x05]
       cmp      rdx, 261
       jbe      G_M000_IG27
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
 
G_M000_IG56:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG87
       cmp      dword ptr [rdx+4*rsi+0x10], 25
       je       SHORT G_M000_IG59
 
G_M000_IG58:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG87
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 25
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp+0x50], rdx
       test     rax, rax
       je       SHORT G_M000_IG62
 
G_M000_IG60:
       mov      rsi, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG28
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG33
 
G_M000_IG62:
       mov      rsi, rax
       mov      edi, 6
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG63:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG66
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       dec      edx
       mov      esi, edx
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG87
       cmp      dword ptr [rdi+4*rsi+0x10], 26
       je       SHORT G_M000_IG66
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG87
       mov      edx, edx
       mov      dword ptr [rdi+4*rdx+0x10], 26
 
G_M000_IG66:
       mov      rdx, 0xD1FFAB1E
       mov      rsi, gword ptr [rdx]
       test     rax, rax
       je       SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rcx, qword ptr [rax]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       je       G_M000_IG03
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG14
 
G_M000_IG69:
       mov      rdx, rsi
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG70:
       test     rsi, rsi
       je       G_M000_IG51
 
G_M000_IG71:
       mov      rax, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG22
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG43
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG51
 
G_M000_IG74:
       mov      rdi, rsi
       mov      rax, r14
       test     rax, rax
       je       SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG77
 
G_M000_IG76:
       xor      rax, rax
 
G_M000_IG77:
       test     rax, rax
       je       G_M000_IG51
 
G_M000_IG78:
       mov      rsi, rax
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r14, rax
       jmp      G_M000_IG52
 
G_M000_IG79:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG82
 
G_M000_IG80:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       lea      edi, [rcx-0x01]
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG87
       cmp      dword ptr [rdx+4*rsi+0x10], 27
       je       SHORT G_M000_IG82
 
G_M000_IG81:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG87
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 27
 
G_M000_IG82:
       mov      rax, qword ptr [rbp+0xB8]
       test     rax, rax
       jle      SHORT G_M000_IG84
 
G_M000_IG83:
       movsxd   rcx, ecx
       cmp      rax, rcx
       jg       SHORT G_M000_IG84
       dec      eax
       mov      dword ptr [(reloc)], eax
 
G_M000_IG84:
       mov      rax, r14
 
G_M000_IG85:
       add      rsp, 488
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG86:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0xF8], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xD8], ecx
       mov      rdi, r14
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0xF8]
       mov      esi, dword ptr [rbp-0xD8]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      G_M000_IG26
 
G_M000_IG87:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG88:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2178

