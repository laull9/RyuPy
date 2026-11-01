; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mul_add(System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 304
       lea      rbp, [rsp+0x130]
       xor      eax, eax
       mov      qword ptr [rbp-0x128], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x120], zmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqa  xmmword ptr [rbp-0x60], xmm8
       mov      qword ptr [rbp-0x50], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x130], 0x3E8
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
       mov      eax, dword ptr [rbp-0x130]
       dec      eax
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x130], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x130]
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
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xD8], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0xF8], rax
       jmp      G_M000_IG23
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       G_M000_IG26
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdi, qword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xD0]
 
G_M000_IG09:
       add      rsp, 304
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x130]
       dec      eax
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x130], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x130]
       mov      esi, 205
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x50], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x130]
       dec      eax
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x130], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x130]
       mov      esi, 213
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
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
       jmp      G_M000_IG10
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       mov      rsi, gword ptr [rbp-0xE8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       mov      rsi, gword ptr [rbp-0xF8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x100], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rbp-0x108], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x108]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rbp-0x110], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x128], rax
       mov      rax, gword ptr [rbp-0x128]
       vmovsd   xmm0, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG10
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x130]
       dec      eax
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x130], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x130]
       mov      esi, 296
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       mov      rsi, gword ptr [rbp-0xF8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x114], eax
       cmp      dword ptr [rbp-0x114], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x130]
       dec      eax
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x130], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x130]
       mov      esi, 323
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       mov      rsi, gword ptr [rbp-0xE8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jne      G_M000_IG17
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG23:
       mov      eax, dword ptr [rbp-0x130]
       dec      eax
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x130], 0
       jg       SHORT G_M000_IG25
 
G_M000_IG24:
       lea      rdi, [rbp-0x130]
       mov      esi, 350
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG25:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x11C], eax
       cmp      dword ptr [rbp-0x11C], 0
       jne      G_M000_IG20
       jmp      G_M000_IG13
 
G_M000_IG26:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1288

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_norm(System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 448
       lea      rbp, [rsp+0x1C0]
       xor      eax, eax
       mov      qword ptr [rbp-0x1B8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x1B0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x170], zmm8
       vmovdqu32 zmmword ptr [rbp-0x130], zmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu  ymmword ptr [rbp-0x70], ymm8
       mov      qword ptr [rbp-0x50], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x1C0], 0x3E8
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
       mov      eax, dword ptr [rbp-0x1C0]
       dec      eax
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1C0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x1C0]
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
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x150], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x160], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x170], rax
       jmp      G_M000_IG23
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x88]
       add      rax, qword ptr [rbp-0x98]
       jo       G_M000_IG26
       mov      qword ptr [rbp-0x88], rax
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
       add      rsp, 448
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x1C0]
       dec      eax
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1C0], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x1C0]
       mov      esi, 214
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
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
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x1C0]
       dec      eax
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1C0], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x1C0]
       mov      esi, 262
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
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
       jmp      G_M000_IG10
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x150]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x158], xmm0
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x168], xmm0
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x158]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rbp-0x180], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x168]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x188], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1B8], rax
       mov      rax, gword ptr [rbp-0x1B8]
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1B8]
       mov      gword ptr [rbp-0xE8], rax
       jmp      G_M000_IG10
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x1C0]
       dec      eax
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1C0], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x1C0]
       mov      esi, 402
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1A4], eax
       cmp      dword ptr [rbp-0x1A4], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x1C0]
       dec      eax
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1C0], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x1C0]
       mov      esi, 429
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1A8], eax
       cmp      dword ptr [rbp-0x1A8], 0
       jne      G_M000_IG17
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG23:
       mov      eax, dword ptr [rbp-0x1C0]
       dec      eax
       mov      dword ptr [rbp-0x1C0], eax
       cmp      dword ptr [rbp-0x1C0], 0
       jg       SHORT G_M000_IG25
 
G_M000_IG24:
       lea      rdi, [rbp-0x1C0]
       mov      esi, 456
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG25:
       mov      rsi, gword ptr [rbp-0x150]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1AC], eax
       cmp      dword ptr [rbp-0x1AC], 0
       jne      G_M000_IG20
       jmp      G_M000_IG13
 
G_M000_IG26:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1620

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (Instrumented Tier0)
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
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x150], 0x3E8
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
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x150]
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
       jmp      G_M000_IG08
 
G_M000_IG06:
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
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0xD8]
       mov      gword ptr [rbp-0x108], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x118], rax
       jmp      G_M000_IG23
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       G_M000_IG26
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 27
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG09:
       add      rsp, 336
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x150]
       mov      esi, 275
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x150]
       mov      esi, 283
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
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
       jmp      G_M000_IG10
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0xF8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x100], xmm0
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x110], xmm0
       mov      rsi, gword ptr [rbp-0x118]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x120], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rbp-0x128], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x128]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x120]
       vmovsd   qword ptr [rbp-0x130], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x148]
       vmovsd   xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0xE8], rax
       jmp      G_M000_IG10
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x150]
       mov      esi, 368
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       mov      rsi, gword ptr [rbp-0x118]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x134], eax
       cmp      dword ptr [rbp-0x134], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x150]
       mov      esi, 395
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jne      G_M000_IG17
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG23:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG25
 
G_M000_IG24:
       lea      rdi, [rbp-0x150]
       mov      esi, 422
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG25:
       mov      rsi, gword ptr [rbp-0xF8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x13C], eax
       cmp      dword ptr [rbp-0x13C], 0
       jne      G_M000_IG20
       jmp      G_M000_IG13
 
G_M000_IG26:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1450

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mul_add(System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xcd
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 7
; 16 inlinees with PGO data; 22 single block inlinees; 37 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 288
       mov      qword ptr [rsp+0x258], r15
       mov      qword ptr [rsp+0x250], r14
       mov      qword ptr [rsp+0x248], r13
       mov      qword ptr [rsp+0x240], r12
       mov      qword ptr [rsp+0x238], rbx
       lea      rbp, [rsp+0x120]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      qword ptr [rbp-0x50], rax
       mov      rbx, gword ptr [rbp+0x108]
       mov      r15, gword ptr [rbp+0x100]
       mov      r14, qword ptr [rbp+0xC0]
       mov      r13, qword ptr [rbp+0xB8]
       mov      rax, qword ptr [rbp+0xB0]
       mov      r12, gword ptr [rbp+0x78]
 
G_M000_IG02:
       test     rax, rax
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0xF8], edx
       jmp      G_M000_IG37
 
G_M000_IG03:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp+0x30], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       vmovsd   xmm0, qword ptr [rbp+0x30]
       vmovsd   qword ptr [r12+0x08], xmm0
       mov      rax, qword ptr [rbp+0xB0]
       mov      edx, dword ptr [rbp-0xF8]
       jmp      G_M000_IG37
 
G_M000_IG04:
       test     rbx, rbx
       jne      G_M000_IG47
 
G_M000_IG05:
       mov      rdx, r15
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG06:
       test     rsi, rsi
       je       G_M000_IG35
 
G_M000_IG07:
       mov      rax, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG19
 
G_M000_IG08:
       mov      rax, qword ptr [rsi+0x08]
       test     r12, r12
       je       G_M000_IG35
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG17
 
G_M000_IG10:
       mov      r12, qword ptr [r12+0x08]
       lea      rcx, [rax+r12]
       mov      qword ptr [rbp-0xB8], rcx
       mov      rdi, rax
       xor      rdi, rcx
       mov      rsi, r12
       xor      rsi, rcx
       and      rdi, rsi
       jge      G_M000_IG13
 
G_M000_IG11:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x118], rax
       mov      ecx, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xF4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x118]
       mov      esi, dword ptr [rbp-0xF4]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG12:
       mov      r12, rax
       jmp      G_M000_IG36
 
G_M000_IG13:
       lea      r12, [rcx+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG16
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG15:
       jmp      SHORT G_M000_IG12
 
G_M000_IG16:
       test     r12, r12
       jl       G_M000_IG84
       mov      rax, r12
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG83
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      SHORT G_M000_IG15
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG35
 
G_M000_IG18:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG36
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG25
 
G_M000_IG20:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r12, r12
       je       G_M000_IG35
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG23
 
G_M000_IG22:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG36
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG35
 
G_M000_IG24:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG36
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG35
 
G_M000_IG26:
       mov      rax, rsi
       mov      gword ptr [rbp-0x100], rax
       test     r12, r12
       je       G_M000_IG35
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG35
 
G_M000_IG28:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG33
 
G_M000_IG29:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG32
 
G_M000_IG30:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xEC], ecx
       mov      esi, ecx
       add      esi, dword ptr [r12+0x08]
       js       G_M000_IG82
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x108], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x100]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xEC]
       mov      rax, gword ptr [rbp-0x108]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r12, gword ptr [rbp-0x108]
 
G_M000_IG31:
       jmp      SHORT G_M000_IG36
 
G_M000_IG32:
       mov      r12, rax
       jmp      SHORT G_M000_IG31
 
G_M000_IG33:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG81
 
G_M000_IG34:
       jmp      SHORT G_M000_IG31
 
G_M000_IG35:
       mov      rdx, r12
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r12, rax
 
G_M000_IG36:
       mov      rax, qword ptr [rbp+0xB0]
       mov      edx, dword ptr [rbp-0xF8]
 
G_M000_IG37:
       mov      qword ptr [rbp+0xB0], rax
       add      r14, rax
       jo       G_M000_IG84
       mov      edi, edx
       cmp      r14, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r14, r13
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG72
 
G_M000_IG38:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG41
 
G_M000_IG39:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG83
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG41
 
G_M000_IG40:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG83
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 10
 
G_M000_IG41:
       test     rbx, rbx
       je       G_M000_IG04
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG04
 
G_M000_IG43:
       test     r15, r15
       je       SHORT G_M000_IG47
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG47
 
G_M000_IG45:
       test     r12, r12
       je       SHORT G_M000_IG47
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       je       G_M000_IG03
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG66
 
G_M000_IG48:
       mov      rsi, qword ptr [rbx+0x08]
       test     r15, r15
       je       G_M000_IG05
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG64
 
G_M000_IG50:
       mov      r8, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x48], r8
       lea      rdi, [rbp-0x68]
       mov      rdx, rsi
       mulx     rdx, r9, r8
       mov      qword ptr [rdi], r9
       mov      r9, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x60], r9
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r8
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG60
 
G_M000_IG51:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x110], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0xF0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      r9d, dword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x110]
       test     rcx, rcx
       jne      SHORT G_M000_IG55
 
G_M000_IG52:
       test     rax, rax
       jne      G_M000_IG80
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       movsxd   rsi, dword ptr [rbp-0xF0]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x88]
       mov      esi, dword ptr [rbp-0x80]
 
G_M000_IG53:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG54:
       jmp      G_M000_IG06
 
G_M000_IG55:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG56:
       test     rax, rax
       je       SHORT G_M000_IG59
 
G_M000_IG57:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG58:
       mov      edx, dword ptr [rbp-0xF0]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG53
 
G_M000_IG59:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG58
 
G_M000_IG60:
       lea      rdx, [r9+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG63
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x60]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG62:
       jmp      SHORT G_M000_IG54
 
G_M000_IG63:
       test     rdx, rdx
       jl       G_M000_IG84
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG83
       mov      rsi, gword ptr [rdx+8*rsi+0x10]
       jmp      SHORT G_M000_IG62
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG05
 
G_M000_IG65:
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
       jmp      G_M000_IG06
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG05
 
G_M000_IG67:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r15, r15
       je       G_M000_IG05
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG70
 
G_M000_IG69:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG06
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG05
 
G_M000_IG71:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG06
 
G_M000_IG72:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG75
 
G_M000_IG73:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       cmp      r8d, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG83
       cmp      dword ptr [rax+4*r8+0x10], 11
       je       SHORT G_M000_IG75
 
G_M000_IG74:
       mov      rax, rsi
       cmp      edx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG83
       mov      edx, edx
       mov      dword ptr [rax+4*rdx+0x10], 11
 
G_M000_IG75:
       mov      rcx, qword ptr [rbp+0xE0]
       test     rcx, rcx
       jle      SHORT G_M000_IG78
 
G_M000_IG76:
       movsxd   rax, edi
       cmp      rcx, rax
       jg       SHORT G_M000_IG78
 
G_M000_IG77:
       dec      ecx
       mov      dword ptr [(reloc)], ecx
 
G_M000_IG78:
       mov      rax, r12
 
G_M000_IG79:
       add      rsp, 568
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG80:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG56
 
G_M000_IG81:
       mov      r12, 0xD1FFAB1E
       jmp      G_M000_IG31
 
G_M000_IG82:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG83:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG84:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2150

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_norm(System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xd6
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 3
; 29 inlinees with PGO data; 45 single block inlinees; 82 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 576
       mov      qword ptr [rsp+0x408], r15
       mov      qword ptr [rsp+0x400], r14
       mov      qword ptr [rsp+0x3F8], r13
       mov      qword ptr [rsp+0x3F0], r12
       mov      qword ptr [rsp+0x3E8], rbx
       lea      rbp, [rsp+0x240]
       xor      eax, eax
       mov      qword ptr [rbp-0x1C8], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      rbx, gword ptr [rbp+0x198]
       mov      r15, gword ptr [rbp+0x190]
       mov      r14, gword ptr [rbp+0x188]
       mov      r13, qword ptr [rbp+0x148]
       mov      r12, qword ptr [rbp+0x140]
       mov      rax, qword ptr [rbp+0x138]
       mov      rcx, gword ptr [rbp+0xE8]
 
G_M000_IG02:
       jmp      G_M000_IG103
 
G_M000_IG03:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmulsd   xmm0, xmm0, xmm0
       vmulsd   xmm1, xmm1, xmm1
       vaddsd   xmm0, xmm0, xmm1
       vmulsd   xmm1, xmm2, xmm2
       vaddsd   xmm0, xmm0, xmm1
       vmovsd   qword ptr [rbp+0x30], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       vmovsd   xmm0, qword ptr [rbp+0x30]
       vmovsd   qword ptr [rcx+0x08], xmm0
       mov      rdx, rbx
       mov      rbx, r15
       mov      r15, r14
       mov      r14, rdx
       mov      rax, qword ptr [rbp+0x138]
       jmp      G_M000_IG103
 
G_M000_IG04:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG05:
       test     rax, rax
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG07:
       mov      edx, dword ptr [rbp-0x1DC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      G_M000_IG120
 
G_M000_IG08:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG07
 
G_M000_IG09:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x58]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG11:
       jmp      G_M000_IG121
 
G_M000_IG12:
       test     rcx, rcx
       jl       G_M000_IG135
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG134
       mov      rdi, gword ptr [rcx+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG11
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG16
 
G_M000_IG14:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x1F8], rax
       jmp      SHORT G_M000_IG17
 
G_M000_IG15:
       test     r15, r15
       jne      G_M000_IG116
 
G_M000_IG16:
       mov      rdx, r15
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x1F8], rax
 
G_M000_IG17:
       test     r14, r14
       je       G_M000_IG35
 
G_M000_IG18:
       mov      rcx, qword ptr [r14]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       jne      G_M000_IG33
 
G_M000_IG19:
       mov      rsi, qword ptr [r14+0x08]
       mov      rcx, qword ptr [r14+0x08]
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
       je       G_M000_IG29
 
G_M000_IG20:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x228], rax
       mov      ecx, dword ptr [rbp-0xA0]
       mov      dword ptr [rbp-0x1E0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, qword ptr [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      r9d, dword ptr [rbp-0xC0]
       mov      rcx, gword ptr [rbp-0x228]
       test     rcx, rcx
       jne      SHORT G_M000_IG24
 
G_M000_IG21:
       test     rax, rax
       jne      G_M000_IG131
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       movsxd   rsi, dword ptr [rbp-0x1E0]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xD8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xD8]
       mov      esi, dword ptr [rbp-0xD0]
 
G_M000_IG22:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG23:
       mov      rax, rdx
       jmp      G_M000_IG36
 
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
       mov      edx, dword ptr [rbp-0x1E0]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG22
 
G_M000_IG28:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG27
 
G_M000_IG29:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG31:
       jmp      SHORT G_M000_IG23
 
G_M000_IG32:
       test     rcx, rcx
       jl       G_M000_IG135
       mov      rdx, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG134
       mov      rdx, gword ptr [rcx+8*rdx+0x10]
       jmp      SHORT G_M000_IG31
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      rax, rdx
       jmp      SHORT G_M000_IG36
 
G_M000_IG35:
       mov      rdx, r14
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG36:
       mov      rcx, gword ptr [rbp-0x1F8]
       test     rcx, rcx
       je       G_M000_IG65
 
G_M000_IG37:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG49
 
G_M000_IG38:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG65
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG47
 
G_M000_IG40:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0xF8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x100], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG43
 
G_M000_IG41:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x230], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x1E4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x120], xmm0
       lea      rdi, [rbp-0x120]
       mov      rsi, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x230]
       mov      esi, dword ptr [rbp-0x1E4]
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG42:
       mov      gword ptr [rbp-0x200], rax
       jmp      G_M000_IG66
 
G_M000_IG43:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x100]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG45:
       jmp      SHORT G_M000_IG42
 
G_M000_IG46:
       test     rax, rax
       jl       G_M000_IG135
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG134
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG45
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG65
 
G_M000_IG48:
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
       mov      gword ptr [rbp-0x200], rax
       jmp      G_M000_IG66
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG55
 
G_M000_IG50:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       test     rax, rax
       je       G_M000_IG65
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG53
 
G_M000_IG52:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x200], rax
       jmp      G_M000_IG66
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG65
 
G_M000_IG54:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x200], rax
       jmp      G_M000_IG66
 
G_M000_IG55:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG65
 
G_M000_IG56:
       mov      rsi, rcx
       mov      gword ptr [rbp-0x208], rsi
       test     rax, rax
       je       G_M000_IG65
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG65
 
G_M000_IG58:
       mov      gword ptr [rbp-0x210], rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG63
 
G_M000_IG59:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG62
 
G_M000_IG60:
       mov      ecx, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x134], ecx
       mov      edi, ecx
       add      edi, dword ptr [rax+0x08]
       js       G_M000_IG133
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x218], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x208]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x134]
       mov      rax, gword ptr [rbp-0x218]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rsi, gword ptr [rbp-0x210]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rax, gword ptr [rbp-0x218]
 
G_M000_IG61:
       mov      gword ptr [rbp-0x200], rax
       jmp      SHORT G_M000_IG66
 
G_M000_IG62:
       mov      rax, rsi
       jmp      SHORT G_M000_IG61
 
G_M000_IG63:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG132
 
G_M000_IG64:
       jmp      SHORT G_M000_IG61
 
G_M000_IG65:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x200], rax
 
G_M000_IG66:
       test     rbx, rbx
       je       G_M000_IG77
 
G_M000_IG67:
       mov      rsi, qword ptr [rbx]
       mov      rdx, 0xD1FFAB1E
       cmp      rsi, rdx
       jne      G_M000_IG75
 
G_M000_IG68:
       mov      rsi, qword ptr [rbx+0x08]
       mov      rcx, qword ptr [rbx+0x08]
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
       je       G_M000_IG71
 
G_M000_IG69:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x160]
       mov      gword ptr [rbp-0x238], rax
       mov      ecx, dword ptr [rbp-0x158]
       mov      dword ptr [rbp-0x1E8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x180], xmm0
       lea      rdi, [rbp-0x180]
       mov      rsi, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x238]
       mov      esi, dword ptr [rbp-0x1E8]
       mov      rdx, gword ptr [rbp-0x180]
       mov      rcx, qword ptr [rbp-0x178]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG70:
       jmp      G_M000_IG78
 
G_M000_IG71:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG74
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG73:
       jmp      SHORT G_M000_IG70
 
G_M000_IG74:
       test     rcx, rcx
       jl       G_M000_IG135
       mov      rdx, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG134
       mov      rdx, gword ptr [rcx+8*rdx+0x10]
       jmp      SHORT G_M000_IG73
 
G_M000_IG75:
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       jne      SHORT G_M000_IG77
 
G_M000_IG76:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm0
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x140]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG78
 
G_M000_IG77:
       mov      rdx, rbx
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG78:
       mov      rax, gword ptr [rbp-0x200]
       test     rax, rax
       je       G_M000_IG101
 
G_M000_IG79:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG91
 
G_M000_IG80:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG101
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG89
 
G_M000_IG82:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1A0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1A8], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG85
 
G_M000_IG83:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1B8]
       mov      gword ptr [rbp-0x240], rax
       mov      ecx, dword ptr [rbp-0x1B0]
       mov      dword ptr [rbp-0x1EC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
       lea      rdi, [rbp-0x1C8]
       mov      rsi, qword ptr [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x240]
       mov      esi, dword ptr [rbp-0x1EC]
       mov      rdx, gword ptr [rbp-0x1C8]
       mov      rcx, qword ptr [rbp-0x1C0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG84:
       mov      rcx, rax
       jmp      G_M000_IG102
 
G_M000_IG85:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG88
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG87:
       jmp      SHORT G_M000_IG84
 
G_M000_IG88:
       test     rax, rax
       jl       G_M000_IG135
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG134
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG87
 
G_M000_IG89:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG101
 
G_M000_IG90:
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
       mov      rcx, rax
       jmp      G_M000_IG102
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG97
 
G_M000_IG92:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm0
       test     rdx, rdx
       je       G_M000_IG101
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG95
 
G_M000_IG94:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       jmp      G_M000_IG102
 
G_M000_IG95:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG101
 
G_M000_IG96:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1D8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       jmp      SHORT G_M000_IG102
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG101
 
G_M000_IG98:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG101
 
G_M000_IG99:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG101
 
G_M000_IG100:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rcx, rax
       jmp      SHORT G_M000_IG102
 
G_M000_IG101:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rcx, rax
 
G_M000_IG102:
       mov      rdx, rbx
       mov      rbx, r15
       mov      r15, r14
       mov      r14, rdx
       mov      rax, qword ptr [rbp+0x138]
 
G_M000_IG103:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG106
 
G_M000_IG104:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       cmp      r10d, dword ptr [r8+0x08]
       jae      G_M000_IG134
       cmp      dword ptr [r8+4*r10+0x10], 18
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       cmp      r9d, dword ptr [rsi+0x08]
       jae      G_M000_IG134
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 18
 
G_M000_IG106:
       add      r13, rax
       jo       G_M000_IG135
       mov      qword ptr [rbp+0x138], rax
       test     rax, rax
       setg     sil
       movzx    rsi, sil
       cmp      r13, r12
       setl     r8b
       movzx    r8, r8b
       and      r8d, esi
       cmp      r13, r12
       setg     r9b
       movzx    r9, r9b
       test     esi, esi
       sete     sil
       movzx    rsi, sil
       and      esi, r9d
       or       esi, r8d
       je       G_M000_IG122
 
G_M000_IG107:
       test     edi, edi
       jle      SHORT G_M000_IG110
 
G_M000_IG108:
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       mov      rcx, rsi
       lea      r9d, [rdi-0x01]
       mov      edi, r9d
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG134
       cmp      dword ptr [rcx+4*rdi+0x10], 17
       je       SHORT G_M000_IG110
 
G_M000_IG109:
       mov      rdi, rsi
       mov      ecx, r9d
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG134
       mov      dword ptr [rdi+4*rcx+0x10], 17
 
G_M000_IG110:
       test     r15, r15
       je       G_M000_IG15
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG15
 
G_M000_IG112:
       test     r14, r14
       je       SHORT G_M000_IG116
 
G_M000_IG113:
       mov      rcx, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG116
 
G_M000_IG114:
       test     rbx, rbx
       je       SHORT G_M000_IG116
 
G_M000_IG115:
       mov      rsi, qword ptr [rbx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG03
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG13
 
G_M000_IG117:
       mov      rsi, qword ptr [r15+0x08]
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x40], rcx
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r8, rcx
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x58], r8
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG09
 
G_M000_IG118:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x220], rax
       mov      ecx, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x1DC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x40]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x220]
       test     rcx, rcx
       jne      G_M000_IG04
 
G_M000_IG119:
       test     rax, rax
       jne      G_M000_IG130
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x1DC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG120:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG121:
       mov      gword ptr [rbp-0x1F8], rax
       jmp      G_M000_IG17
 
G_M000_IG122:
       test     edi, edi
       jle      SHORT G_M000_IG125
 
G_M000_IG123:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      r9d, [rdi-0x01]
       mov      r8d, r9d
       cmp      r8d, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG134
       cmp      dword ptr [rax+4*r8+0x10], 19
       je       SHORT G_M000_IG125
 
G_M000_IG124:
       mov      rax, rsi
       mov      esi, r9d
       cmp      esi, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG134
       mov      dword ptr [rax+4*rsi+0x10], 19
 
G_M000_IG125:
       mov      rdx, qword ptr [rbp+0x170]
       test     rdx, rdx
       jle      SHORT G_M000_IG128
 
G_M000_IG126:
       movsxd   rax, edi
       cmp      rdx, rax
       jg       SHORT G_M000_IG128
 
G_M000_IG127:
       dec      edx
       mov      dword ptr [(reloc)], edx
 
G_M000_IG128:
       mov      rax, rcx
 
G_M000_IG129:
       add      rsp, 0x3E8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG130:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG05
 
G_M000_IG131:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG25
 
G_M000_IG132:
       mov      rax, 0xD1FFAB1E
       jmp      G_M000_IG61
 
G_M000_IG133:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG134:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG135:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3735

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1a6
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 34 inlinees with PGO data; 1 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 208
       mov      qword ptr [rsp+0x228], r15
       mov      qword ptr [rsp+0x220], r14
       mov      qword ptr [rsp+0x218], r13
       mov      qword ptr [rsp+0x210], r12
       mov      qword ptr [rsp+0x208], rbx
       lea      rbp, [rsp+0xD0]
       xor      eax, eax
       mov      qword ptr [rbp-0xA8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       mov      qword ptr [rbp-0x60], rax
       mov      rbx, gword ptr [rbp+0x128]
       mov      r13, gword ptr [rbp+0x120]
       mov      r14, gword ptr [rbp+0x110]
       mov      r15, qword ptr [rbp+0xE0]
       mov      r8, qword ptr [rbp+0xD0]
       mov      r12, gword ptr [rbp+0x88]
       mov      rax, gword ptr [rbp+0x68]
       mov      rdx, gword ptr [rbp+0x58]
       mov      rsi, gword ptr [rbp+0x48]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xD0], r8
       test     r8, r8
       setg     r10b
       movzx    r10, r10b
       mov      dword ptr [rbp-0xC0], r10d
       jmp      G_M000_IG89
 
G_M000_IG03:
       test     r14, r14
       je       G_M000_IG17
 
G_M000_IG04:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r14], rdx
       jne      G_M000_IG17
 
G_M000_IG05:
       mov      rax, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x50], rax
       test     r12, r12
       je       G_M000_IG12
 
G_M000_IG06:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r12], rdx
       jne      G_M000_IG12
 
G_M000_IG07:
       mov      r12, qword ptr [r12+0x08]
       xor      edx, edx
       mov      qword ptr [rbp-0x60], rdx
       lea      rdx, [rbp-0x60]
       mov      rdi, rax
       mov      rsi, r12
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x60]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x70]
       mov      eax, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0xBC], eax
       mov      rdi, r12
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0xBC]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r14, rax
 
G_M000_IG09:
       jmp      G_M000_IG34
 
G_M000_IG10:
       mov      r14, qword ptr [rbp-0x60]
       lea      r12, [r14+0x05]
       cmp      r12, 261
       jbe      G_M000_IG125
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG11:
       jmp      SHORT G_M000_IG09
 
G_M000_IG12:
       mov      rsi, r12
       test     rsi, rsi
       je       SHORT G_M000_IG15
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       xor      rsi, rsi
 
G_M000_IG15:
       test     rsi, rsi
       je       G_M000_IG33
 
G_M000_IG16:
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG34
 
G_M000_IG17:
       mov      rsi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG20
 
G_M000_IG19:
       xor      rsi, rsi
 
G_M000_IG20:
       test     rsi, rsi
       je       G_M000_IG31
 
G_M000_IG21:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x40], xmm0
       mov      rsi, r12
       test     rsi, rsi
       je       SHORT G_M000_IG24
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       xor      rsi, rsi
 
G_M000_IG24:
       test     rsi, rsi
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG34
 
G_M000_IG26:
       mov      rsi, r12
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
       je       SHORT G_M000_IG33
 
G_M000_IG30:
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x80], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdx, r14
       test     rdx, rdx
       je       SHORT G_M000_IG33
 
G_M000_IG32:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG33:
       mov      rdx, r12
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG34:
       test     r14, r14
       je       G_M000_IG45
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG45
 
G_M000_IG36:
       mov      r12, qword ptr [r14+0x08]
       test     r13, r13
       je       SHORT G_M000_IG40
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG40
 
G_M000_IG38:
       mov      r13, qword ptr [r13+0x08]
       lea      r14, [r12+r13]
       mov      rdi, r12
       xor      rdi, r14
       mov      rax, r13
       xor      rax, r14
       and      rdi, rax
       jl       G_M000_IG126
       lea      r12, [r14+0x05]
       cmp      r12, 261
       jbe      G_M000_IG127
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG39:
       jmp      G_M000_IG68
 
G_M000_IG40:
       mov      rsi, r13
       test     rsi, rsi
       je       SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG43
 
G_M000_IG42:
       xor      rsi, rsi
 
G_M000_IG43:
       test     rsi, rsi
       je       G_M000_IG96
 
G_M000_IG44:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x98], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r12
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG68
 
G_M000_IG45:
       mov      rsi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG48
 
G_M000_IG47:
       xor      rsi, rsi
 
G_M000_IG48:
       test     rsi, rsi
       je       G_M000_IG59
 
G_M000_IG49:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rsi, r13
       test     rsi, rsi
       je       SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       xor      rsi, rsi
 
G_M000_IG52:
       test     rsi, rsi
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x90], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG68
 
G_M000_IG54:
       mov      rsi, r13
       test     rsi, rsi
       je       SHORT G_M000_IG57
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG57
 
G_M000_IG56:
       xor      rsi, rsi
 
G_M000_IG57:
       test     rsi, rsi
       je       G_M000_IG96
 
G_M000_IG58:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG68
 
G_M000_IG59:
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG62
 
G_M000_IG60:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG62
 
G_M000_IG61:
       xor      rdi, rdi
 
G_M000_IG62:
       test     rdi, rdi
       je       G_M000_IG96
 
G_M000_IG63:
       mov      rsi, r13
       test     rsi, rsi
       je       SHORT G_M000_IG66
 
G_M000_IG64:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG66
 
G_M000_IG65:
       xor      rsi, rsi
 
G_M000_IG66:
       test     rsi, rsi
       je       G_M000_IG96
 
G_M000_IG67:
       call     [System.String:Concat(System.String,System.String):System.String]
 
G_M000_IG68:
       mov      r13, rax
       mov      r14, qword ptr [rbp+0xD0]
       add      r15, r14
       jo       G_M000_IG129
       mov      r12d, dword ptr [rbp-0xC0]
       mov      edi, r12d
       mov      rax, qword ptr [rbp+0xD8]
       cmp      r15, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       mov      qword ptr [rbp+0xD8], rax
       cmp      r15, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG117
 
G_M000_IG69:
       lea      rcx, [r15+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG124
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
 
G_M000_IG70:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG73
 
G_M000_IG71:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG128
       cmp      dword ptr [rdx+4*rsi+0x10], 25
       je       SHORT G_M000_IG73
 
G_M000_IG72:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 25
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0xA0], rcx
       test     rax, rax
       je       SHORT G_M000_IG75
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG97
 
G_M000_IG75:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG78
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG78
 
G_M000_IG77:
       xor      rsi, rsi
 
G_M000_IG78:
       test     rsi, rsi
       je       G_M000_IG115
 
G_M000_IG79:
       mov      gword ptr [rbp-0xC8], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp+0xA0]
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG82
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG82
 
G_M000_IG81:
       xor      rsi, rsi
 
G_M000_IG82:
       test     rsi, rsi
       je       G_M000_IG109
 
G_M000_IG83:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp+0xA0]
 
G_M000_IG84:
       mov      rdx, rcx
       mov      rsi, rax
       mov      edi, 6
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG85:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG88
 
G_M000_IG86:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       dec      ecx
       mov      esi, ecx
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG128
       cmp      dword ptr [rdi+4*rsi+0x10], 26
       je       SHORT G_M000_IG88
 
G_M000_IG87:
       cmp      ecx, dword ptr [rdx+0x08]
       jae      G_M000_IG128
       mov      ecx, ecx
       mov      dword ptr [rdx+4*rcx+0x10], 26
 
G_M000_IG88:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rax
       mov      rdi, rcx
       mov      rsi, r13
       mov      qword ptr [rbp+0xD0], r14
       mov      r12, rcx
       mov      r14, rax
       mov      rax, rdx
       mov      rdx, rdi
 
G_M000_IG89:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG03
 
G_M000_IG91:
       test     rdx, rdx
       je       G_M000_IG03
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG03
 
G_M000_IG93:
       test     rsi, rsi
       je       G_M000_IG03
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG03
 
G_M000_IG95:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x30], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG68
 
G_M000_IG96:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       jmp      G_M000_IG68
 
G_M000_IG97:
       mov      gword ptr [rbp-0xC8], rax
       mov      rdx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x30], rdx
       test     rcx, rcx
       je       SHORT G_M000_IG103
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG103
 
G_M000_IG99:
       mov      rsi, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x38], rsi
       test     rsi, rsi
       jle      SHORT G_M000_IG102
 
G_M000_IG100:
       test     rdx, rdx
       mov      rax, gword ptr [rbp-0xC8]
       jl       G_M000_IG84
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x30]
       cqo      
       idiv     rdx:rax, qword ptr [rbp-0x38]
       mov      qword ptr [rsi+0x08], rdx
       jmp      G_M000_IG85
 
G_M000_IG102:
       mov      rax, gword ptr [rbp-0xC8]
       jmp      G_M000_IG84
 
G_M000_IG103:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG106
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       xor      rsi, rsi
 
G_M000_IG106:
       test     rsi, rsi
       je       SHORT G_M000_IG108
 
G_M000_IG107:
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp+0xA0]
       jmp      G_M000_IG84
 
G_M000_IG108:
       mov      rax, gword ptr [rbp-0xC8]
       jmp      G_M000_IG84
 
G_M000_IG109:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG112
 
G_M000_IG110:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG112
 
G_M000_IG111:
       xor      rsi, rsi
 
G_M000_IG112:
       test     rsi, rsi
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp+0xA0]
       jmp      G_M000_IG84
 
G_M000_IG114:
       mov      rcx, rax
       mov      rax, gword ptr [rbp-0xC8]
       jmp      G_M000_IG84
 
G_M000_IG115:
       mov      rdx, rax
       test     rdx, rdx
       je       G_M000_IG84
 
G_M000_IG116:
       mov      rdx, qword ptr [rdx]
       jmp      G_M000_IG84
 
G_M000_IG117:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG120
 
G_M000_IG118:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       lea      edx, [rax-0x01]
       mov      edi, edx
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       cmp      dword ptr [rcx+4*rdi+0x10], 27
       je       SHORT G_M000_IG120
 
G_M000_IG119:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       mov      edx, edx
       mov      dword ptr [rcx+4*rdx+0x10], 27
 
G_M000_IG120:
       mov      rbx, qword ptr [rbp+0x108]
       test     rbx, rbx
       jle      SHORT G_M000_IG122
 
G_M000_IG121:
       cdqe     
       cmp      rbx, rax
       jg       SHORT G_M000_IG122
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG122:
       mov      rax, r13
 
G_M000_IG123:
       add      rsp, 520
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG124:
       test     rcx, rcx
       jl       G_M000_IG129
       mov      rdi, rcx
       cmp      rdi, 262
       jae      G_M000_IG128
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rcx, rcx
       jl       G_M000_IG129
       mov      rdi, gword ptr [rdi+8*rcx+0x10]
       mov      rcx, rdi
       mov      rax, rcx
       jmp      G_M000_IG70
 
G_M000_IG125:
       test     r12, r12
       jl       G_M000_IG129
       mov      rsi, r12
       cmp      rsi, 262
       jae      G_M000_IG128
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     r12, r12
       jl       G_M000_IG129
       mov      rdi, r12
       mov      r14, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG11
 
G_M000_IG126:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xA8]
       mov      r14d, dword ptr [rbp-0xA0]
       mov      rdi, r13
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r12
       mov      esi, r14d
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      G_M000_IG39
 
G_M000_IG127:
       test     r12, r12
       jl       SHORT G_M000_IG129
       mov      rax, r12
       cmp      rax, 262
       jae      SHORT G_M000_IG128
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     r12, r12
       jl       SHORT G_M000_IG129
       mov      rsi, r12
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG39
 
G_M000_IG128:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG129:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2731

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x11b
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 34 inlinees with PGO data; 1 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 208
       mov      qword ptr [rsp+0x228], r15
       mov      qword ptr [rsp+0x220], r14
       mov      qword ptr [rsp+0x218], r13
       mov      qword ptr [rsp+0x210], r12
       mov      qword ptr [rsp+0x208], rbx
       lea      rbp, [rsp+0xD0]
       xor      eax, eax
       mov      qword ptr [rbp-0xA8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       mov      qword ptr [rbp-0x60], rax
       mov      rbx, gword ptr [rbp+0x128]
       mov      r12, gword ptr [rbp+0x120]
       mov      r15, gword ptr [rbp+0x110]
       mov      r14, qword ptr [rbp+0xE0]
       mov      rcx, qword ptr [rbp+0xD0]
       mov      r13, gword ptr [rbp+0x88]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xD0], rcx
       test     rcx, rcx
       setg     r9b
       movzx    r9, r9b
       mov      dword ptr [rbp-0xC0], r9d
 
G_M000_IG03:
       test     r15, r15
       je       G_M000_IG17
 
G_M000_IG04:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG17
 
G_M000_IG05:
       mov      r10, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x50], r10
       test     r13, r13
       je       G_M000_IG12
 
G_M000_IG06:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r13], rdx
       jne      G_M000_IG12
 
G_M000_IG07:
       mov      r13, qword ptr [r13+0x08]
       xor      edx, edx
       mov      qword ptr [rbp-0x60], rdx
       lea      rdx, [rbp-0x60]
       mov      rdi, r10
       mov      rsi, r13
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x60]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0x70]
       mov      eax, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0xBC], eax
       mov      rdi, r13
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r15
       mov      esi, dword ptr [rbp-0xBC]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r15, rax
 
G_M000_IG09:
       jmp      G_M000_IG34
 
G_M000_IG10:
       mov      r15, qword ptr [rbp-0x60]
       lea      r13, [r15+0x05]
       cmp      r13, 261
       jbe      G_M000_IG125
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
       mov      r15, rax
 
G_M000_IG11:
       jmp      SHORT G_M000_IG09
 
G_M000_IG12:
       mov      rsi, r13
       test     rsi, rsi
       je       SHORT G_M000_IG15
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       xor      rsi, rsi
 
G_M000_IG15:
       test     rsi, rsi
       je       G_M000_IG33
 
G_M000_IG16:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG34
 
G_M000_IG17:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG20
 
G_M000_IG19:
       xor      rsi, rsi
 
G_M000_IG20:
       test     rsi, rsi
       je       G_M000_IG31
 
G_M000_IG21:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x40], xmm0
       mov      rsi, r13
       test     rsi, rsi
       je       SHORT G_M000_IG24
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       xor      rsi, rsi
 
G_M000_IG24:
       test     rsi, rsi
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG34
 
G_M000_IG26:
       mov      rsi, r13
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
       je       SHORT G_M000_IG33
 
G_M000_IG30:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x80], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdx, r15
       test     rdx, rdx
       je       SHORT G_M000_IG33
 
G_M000_IG32:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG33:
       mov      rdx, r13
       mov      rsi, r15
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG34:
       test     r15, r15
       je       G_M000_IG45
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG45
 
G_M000_IG36:
       mov      r13, qword ptr [r15+0x08]
       test     r12, r12
       je       SHORT G_M000_IG40
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG40
 
G_M000_IG38:
       mov      r12, qword ptr [r12+0x08]
       lea      r15, [r12+r13]
       mov      rdi, r13
       xor      rdi, r15
       mov      rax, r12
       xor      rax, r15
       and      rdi, rax
       jl       G_M000_IG126
       lea      r13, [r15+0x05]
       cmp      r13, 261
       jbe      G_M000_IG127
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
 
G_M000_IG39:
       jmp      G_M000_IG68
 
G_M000_IG40:
       mov      rsi, r12
       test     rsi, rsi
       je       SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG43
 
G_M000_IG42:
       xor      rsi, rsi
 
G_M000_IG43:
       test     rsi, rsi
       je       G_M000_IG96
 
G_M000_IG44:
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x98], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r13
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG68
 
G_M000_IG45:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG48
 
G_M000_IG47:
       xor      rsi, rsi
 
G_M000_IG48:
       test     rsi, rsi
       je       G_M000_IG59
 
G_M000_IG49:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rsi, r12
       test     rsi, rsi
       je       SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       xor      rsi, rsi
 
G_M000_IG52:
       test     rsi, rsi
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x90], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG68
 
G_M000_IG54:
       mov      rsi, r12
       test     rsi, rsi
       je       SHORT G_M000_IG57
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG57
 
G_M000_IG56:
       xor      rsi, rsi
 
G_M000_IG57:
       test     rsi, rsi
       je       G_M000_IG96
 
G_M000_IG58:
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG68
 
G_M000_IG59:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG62
 
G_M000_IG60:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG62
 
G_M000_IG61:
       xor      rdi, rdi
 
G_M000_IG62:
       test     rdi, rdi
       je       G_M000_IG96
 
G_M000_IG63:
       mov      rsi, r12
       test     rsi, rsi
       je       SHORT G_M000_IG66
 
G_M000_IG64:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG66
 
G_M000_IG65:
       xor      rsi, rsi
 
G_M000_IG66:
       test     rsi, rsi
       je       G_M000_IG96
 
G_M000_IG67:
       call     [System.String:Concat(System.String,System.String):System.String]
 
G_M000_IG68:
       mov      r12, rax
 
G_M000_IG69:
       add      r14, qword ptr [rbp+0xD0]
       jo       G_M000_IG129
       mov      r13d, dword ptr [rbp-0xC0]
       mov      edi, r13d
       mov      rax, qword ptr [rbp+0xD8]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       mov      qword ptr [rbp+0xD8], rax
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG117
 
G_M000_IG70:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG124
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG71:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG74
 
G_M000_IG72:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG128
       cmp      dword ptr [rdx+4*rsi+0x10], 25
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 25
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0xA0], rcx
       test     rax, rax
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG97
 
G_M000_IG76:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG79
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG79
 
G_M000_IG78:
       xor      rsi, rsi
 
G_M000_IG79:
       test     rsi, rsi
       je       G_M000_IG115
 
G_M000_IG80:
       mov      gword ptr [rbp-0xC8], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp+0xA0]
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG83
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG83
 
G_M000_IG82:
       xor      rsi, rsi
 
G_M000_IG83:
       test     rsi, rsi
       je       G_M000_IG109
 
G_M000_IG84:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp+0xA0]
 
G_M000_IG85:
       mov      rdx, rcx
       mov      rsi, rax
       mov      edi, 6
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG86:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG89
 
G_M000_IG87:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      eax
       mov      esi, eax
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG128
       cmp      dword ptr [rdx+4*rsi+0x10], 26
       je       SHORT G_M000_IG89
 
G_M000_IG88:
       cmp      eax, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       mov      eax, eax
       mov      dword ptr [rcx+4*rax+0x10], 26
 
G_M000_IG89:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       test     rdi, rdi
       mov      r13, rax
       mov      r15, rdi
       je       G_M000_IG03
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG91:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG03
 
G_M000_IG93:
       test     r12, r12
       je       G_M000_IG03
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG03
 
G_M000_IG95:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp+0x30], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       vmovsd   xmm0, qword ptr [rbp+0x30]
       vmovsd   qword ptr [r12+0x08], xmm0
       jmp      G_M000_IG69
 
G_M000_IG96:
       mov      rdx, r12
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       jmp      G_M000_IG68
 
G_M000_IG97:
       mov      gword ptr [rbp-0xC8], rax
       mov      rdx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x30], rdx
       test     rcx, rcx
       je       SHORT G_M000_IG103
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG103
 
G_M000_IG99:
       mov      rsi, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x38], rsi
       test     rsi, rsi
       jle      SHORT G_M000_IG102
 
G_M000_IG100:
       test     rdx, rdx
       mov      rax, gword ptr [rbp-0xC8]
       jl       G_M000_IG85
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x30]
       cqo      
       idiv     rdx:rax, qword ptr [rbp-0x38]
       mov      qword ptr [rsi+0x08], rdx
       jmp      G_M000_IG86
 
G_M000_IG102:
       mov      rax, gword ptr [rbp-0xC8]
       jmp      G_M000_IG85
 
G_M000_IG103:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG106
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       xor      rsi, rsi
 
G_M000_IG106:
       test     rsi, rsi
       je       SHORT G_M000_IG108
 
G_M000_IG107:
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp+0xA0]
       jmp      G_M000_IG85
 
G_M000_IG108:
       mov      rax, gword ptr [rbp-0xC8]
       jmp      G_M000_IG85
 
G_M000_IG109:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG112
 
G_M000_IG110:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG112
 
G_M000_IG111:
       xor      rsi, rsi
 
G_M000_IG112:
       test     rsi, rsi
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       mov      rax, gword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp+0xA0]
       jmp      G_M000_IG85
 
G_M000_IG114:
       mov      rcx, rax
       mov      rax, gword ptr [rbp-0xC8]
       jmp      G_M000_IG85
 
G_M000_IG115:
       mov      rdx, rax
       test     rdx, rdx
       je       G_M000_IG85
 
G_M000_IG116:
       mov      rdx, qword ptr [rdx]
       jmp      G_M000_IG85
 
G_M000_IG117:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG120
 
G_M000_IG118:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       lea      edx, [rax-0x01]
       mov      edi, edx
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       cmp      dword ptr [rcx+4*rdi+0x10], 27
       je       SHORT G_M000_IG120
 
G_M000_IG119:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG128
       mov      edx, edx
       mov      dword ptr [rcx+4*rdx+0x10], 27
 
G_M000_IG120:
       mov      rbx, qword ptr [rbp+0x108]
       test     rbx, rbx
       jle      SHORT G_M000_IG122
 
G_M000_IG121:
       cdqe     
       cmp      rbx, rax
       jg       SHORT G_M000_IG122
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG122:
       mov      rax, r12
 
G_M000_IG123:
       add      rsp, 520
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG124:
       test     rcx, rcx
       jl       G_M000_IG129
       mov      rdi, rcx
       cmp      rdi, 262
       jae      G_M000_IG128
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rcx, rcx
       jl       G_M000_IG129
       mov      rdi, gword ptr [rdi+8*rcx+0x10]
       mov      rcx, rdi
       mov      rax, rcx
       jmp      G_M000_IG71
 
G_M000_IG125:
       test     r13, r13
       jl       G_M000_IG129
       mov      rsi, r13
       cmp      rsi, 262
       jae      G_M000_IG128
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     r13, r13
       jl       G_M000_IG129
       mov      rdi, r13
       mov      r15, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG11
 
G_M000_IG126:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0xA8]
       mov      r15d, dword ptr [rbp-0xA0]
       mov      rdi, r12
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r13
       mov      esi, r15d
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      G_M000_IG39
 
G_M000_IG127:
       test     r13, r13
       jl       SHORT G_M000_IG129
       mov      rax, r13
       cmp      rax, 262
       jae      SHORT G_M000_IG128
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     r13, r13
       jl       SHORT G_M000_IG129
       mov      rsi, r13
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG39
 
G_M000_IG128:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG129:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2703

