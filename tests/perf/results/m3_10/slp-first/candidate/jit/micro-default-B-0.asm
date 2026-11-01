; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 768
       lea      rbp, [rsp+0x300]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -672
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
       mov      gword ptr [rbp-0x50], r8
 
G_M000_IG02:
       mov      dword ptr [rbp-0x2F8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x68], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 90
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0xA0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xB0], eax
       cmp      dword ptr [rbp-0xA4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       and      eax, dword ptr [rbp-0xAC]
       movzx    rax, al
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB0]
       or       eax, dword ptr [rbp-0xB8]
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x150], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x160], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x170], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x180], rax
       jmp      G_M000_IG30
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       G_M000_IG65
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, qword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x138]
 
G_M000_IG09:
       add      rsp, 768
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 222
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2F0], rax
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F0]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 261
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x208], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x218], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x228], rax
       jmp      G_M000_IG46
 
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
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x188], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x158]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1A8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1B0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1B8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0xF8], rax
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1C0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1C8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1D0], xmm0
 
G_M000_IG17:
       vmovsd   xmm0, qword ptr [rbp-0x1D0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1D8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1E0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1E0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1E8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x1E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 418
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1EC], eax
       cmp      dword ptr [rbp-0x1EC], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 445
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jne      G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 472
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F4], eax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 499
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0x150]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F8], eax
       cmp      dword ptr [rbp-0x1F8], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 526
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      dword ptr [rbp-0x1FC], 1
       cmp      dword ptr [rbp-0x1FC], 0
       jne      G_M000_IG27
       jmp      G_M000_IG13
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 545
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x278], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x288], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x298], rax
       jmp      G_M000_IG62
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 564
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG38:
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       xor      edi, edi
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 2
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 3
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 4
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 5
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG33
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x210], xmm0
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x220], xmm0
       mov      rsi, gword ptr [rbp-0x228]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x230], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x210]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x238], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x238]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x240], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x240]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x248], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x248]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x250], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x250]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x258], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x260], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x260]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 723
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x228]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x264], eax
       cmp      dword ptr [rbp-0x264], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 750
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x268], eax
       cmp      dword ptr [rbp-0x268], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 777
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x26C], eax
       cmp      dword ptr [rbp-0x26C], 0
       jne      G_M000_IG43
       jmp      G_M000_IG36
 
G_M000_IG49:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG51
 
G_M000_IG50:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 804
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG51:
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 813
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG54:
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 6
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 7
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 8
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rdx, gword ptr [rbp-0x118]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG49
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x280], xmm0
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x290], xmm0
       mov      rsi, gword ptr [rbp-0x298]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2A8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rbp-0x2B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2B0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rbp-0x2B8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rbp-0x2C0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rbp-0x2D0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x2D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG49
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 976
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rsi, gword ptr [rbp-0x298]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2D4], eax
       cmp      dword ptr [rbp-0x2D4], 0
       jne      G_M000_IG55
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 0x3EB
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG61:
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2D8], eax
       cmp      dword ptr [rbp-0x2D8], 0
       jne      G_M000_IG56
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG62:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 0x406
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2DC], eax
       cmp      dword ptr [rbp-0x2DC], 0
       jne      G_M000_IG59
       jmp      G_M000_IG52
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3769

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1a2
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 29 inlinees with PGO data; 60 single block inlinees; 121 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x470
       mov      qword ptr [rsp+0x778], r15
       mov      qword ptr [rsp+0x770], r14
       mov      qword ptr [rsp+0x768], r13
       mov      qword ptr [rsp+0x760], r12
       mov      qword ptr [rsp+0x758], rbx
       lea      rbp, [rsp+0x470]
       xor      eax, eax
       mov      qword ptr [rbp-0x408], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x400], xmm8
       vmovdqa32 xmmword ptr [rbp-0x3F0], xmm8
       mov      rax, -912
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      r8, gword ptr [rbp+0x1C0]
       mov      rdx, gword ptr [rbp+0x190]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG226
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x190], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG226
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x1C0], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x1C0]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x1B8], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rbp+0x1B0]
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x1B0], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x1B0]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x1A8], xmm1
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x1A0]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x1A0], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x1A0]
 
G_M000_IG10:
       vmovsd   xmm2, qword ptr [r10+0x08]
       vmovsd   qword ptr [rbp+0x198], xmm2
       mov      rdx, gword ptr [rbp+0x190]
       vmovsd   xmm3, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x188], xmm3
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x1B8]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x158], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x218], rax
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x188]
       vmovsd   xmm2, qword ptr [rbp+0x198]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x128]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG216
 
G_M000_IG11:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG13:
       jmp      G_M000_IG268
 
G_M000_IG14:
       test     rax, rax
       jl       G_M000_IG288
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG287
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG13
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG33
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG23
 
G_M000_IG18:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG21:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG33
 
G_M000_IG22:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG33
 
G_M000_IG24:
       mov      rax, rsi
       mov      gword ptr [rbp-0x440], rax
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG33
 
G_M000_IG26:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG31
 
G_M000_IG27:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG30
 
G_M000_IG28:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xF4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG286
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x448], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x440]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x448]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x448]
 
G_M000_IG29:
       jmp      SHORT G_M000_IG34
 
G_M000_IG30:
       mov      rsi, rax
       jmp      SHORT G_M000_IG29
 
G_M000_IG31:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG284
 
G_M000_IG32:
       mov      rsi, r15
       jmp      SHORT G_M000_IG29
 
G_M000_IG33:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG34:
       test     rsi, rsi
       je       G_M000_IG63
 
G_M000_IG35:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG57
 
G_M000_IG36:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG63
 
G_M000_IG37:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG55
 
G_M000_IG38:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG51
 
G_M000_IG39:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x470]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG44
 
G_M000_IG40:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG41:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG42:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG43:
       mov      rsi, rax
       jmp      G_M000_IG64
 
G_M000_IG44:
       test     rcx, rcx
       je       SHORT G_M000_IG49
 
G_M000_IG45:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG46:
       test     rax, rax
       je       SHORT G_M000_IG50
 
G_M000_IG47:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG48:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG42
 
G_M000_IG49:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG46
 
G_M000_IG50:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG48
 
G_M000_IG51:
       mov      rax, r9
       mov      qword ptr [rbp-0x168], rax
       lea      rdx, [rax+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG54
 
G_M000_IG52:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG53:
       jmp      SHORT G_M000_IG43
 
G_M000_IG54:
       test     rdx, rdx
       jl       G_M000_IG288
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG287
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      SHORT G_M000_IG53
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG63
 
G_M000_IG56:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG64
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG63
 
G_M000_IG58:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rbx, rbx
       je       G_M000_IG63
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG61
 
G_M000_IG60:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG64
 
G_M000_IG61:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG63
 
G_M000_IG62:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG64
 
G_M000_IG63:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG64:
       test     rsi, rsi
       je       G_M000_IG95
 
G_M000_IG65:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG79
 
G_M000_IG66:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG95
 
G_M000_IG67:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG77
 
G_M000_IG68:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG73
 
G_M000_IG69:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG70:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG71:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG72:
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG73:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG75:
       jmp      SHORT G_M000_IG72
 
G_M000_IG76:
       test     rax, rax
       jl       G_M000_IG288
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG287
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG75
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG95
 
G_M000_IG78:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG85
 
G_M000_IG80:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     r15, r15
       je       G_M000_IG95
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG83
 
G_M000_IG82:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG83:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG95
 
G_M000_IG84:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG95
 
G_M000_IG86:
       mov      rax, rsi
       mov      gword ptr [rbp-0x450], rax
       test     r15, r15
       je       G_M000_IG95
 
G_M000_IG87:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG95
 
G_M000_IG88:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG93
 
G_M000_IG89:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG92
 
G_M000_IG90:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0x1F4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG286
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x458], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x450]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x1F4]
       mov      rsi, gword ptr [rbp-0x458]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x458]
 
G_M000_IG91:
       jmp      SHORT G_M000_IG96
 
G_M000_IG92:
       mov      rsi, rax
       jmp      SHORT G_M000_IG91
 
G_M000_IG93:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG285
 
G_M000_IG94:
       mov      rsi, r15
       jmp      SHORT G_M000_IG91
 
G_M000_IG95:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG96:
       test     rsi, rsi
       je       G_M000_IG117
 
G_M000_IG97:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG111
 
G_M000_IG98:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG117
 
G_M000_IG99:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG109
 
G_M000_IG100:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x218], rcx
       lea      rdi, [rbp-0x250]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x250]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG105
 
G_M000_IG101:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       lea      rdi, [rbp-0x228]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG102:
       vmovdqu  xmm0, xmmword ptr [rbp-0x228]
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
 
G_M000_IG103:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x260], xmm0
       lea      rdi, [rbp-0x260]
       mov      rsi, qword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x230]
       mov      rdx, gword ptr [rbp-0x260]
       mov      rcx, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG104:
       mov      rsi, rax
       jmp      G_M000_IG118
 
G_M000_IG105:
       mov      rcx, r9
       mov      qword ptr [rbp-0x268], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG108
 
G_M000_IG106:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x268]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG107:
       jmp      SHORT G_M000_IG104
 
G_M000_IG108:
       test     rdx, rdx
       jl       G_M000_IG288
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG287
       test     rdx, rdx
       jl       G_M000_IG288
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG107
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG117
 
G_M000_IG110:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x270]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG118
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG117
 
G_M000_IG112:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm0
       test     rbx, rbx
       je       G_M000_IG117
 
G_M000_IG113:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG115
 
G_M000_IG114:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG118
 
G_M000_IG115:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG117
 
G_M000_IG116:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG118
 
G_M000_IG117:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG118:
       test     rsi, rsi
       je       G_M000_IG143
 
G_M000_IG119:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG133
 
G_M000_IG120:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG143
 
G_M000_IG121:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG131
 
G_M000_IG122:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x298], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x2A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG127
 
G_M000_IG123:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2B0], xmm0
       lea      rdi, [rbp-0x2B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG124:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2B0]
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
 
G_M000_IG125:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2E0], xmm0
       lea      rdi, [rbp-0x2E0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x2B8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rcx, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D0], rax
       mov      qword ptr [rbp-0x2C8], rdx
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG126:
       jmp      G_M000_IG144
 
G_M000_IG127:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG130
 
G_M000_IG128:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG129:
       jmp      SHORT G_M000_IG126
 
G_M000_IG130:
       test     rcx, rcx
       jl       G_M000_IG288
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG287
       test     rcx, rcx
       jl       G_M000_IG288
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG129
 
G_M000_IG131:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG143
 
G_M000_IG132:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG144
 
G_M000_IG133:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG139
 
G_M000_IG134:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm0
       test     r15, r15
       je       G_M000_IG143
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG137
 
G_M000_IG136:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG144
 
G_M000_IG137:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG143
 
G_M000_IG138:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG144
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG143
 
G_M000_IG140:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG143
 
G_M000_IG141:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG143
 
G_M000_IG142:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG144
 
G_M000_IG143:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG144:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       je       G_M000_IG151
 
G_M000_IG145:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG151
 
G_M000_IG146:
       test     rbx, rbx
       je       SHORT G_M000_IG151
 
G_M000_IG147:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG151
 
G_M000_IG148:
       test     r15, r15
       je       SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG151
 
G_M000_IG150:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x40], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG215
 
G_M000_IG151:
       test     r13, r13
       je       G_M000_IG172
 
G_M000_IG152:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG166
 
G_M000_IG153:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x308], r8
       test     rbx, rbx
       je       G_M000_IG172
 
G_M000_IG154:
       mov      qword ptr [rbp-0x420], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG164
 
G_M000_IG155:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x318], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x320], rdx
       lea      rdx, [rbp-0x320]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x320]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG160
 
G_M000_IG156:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x330], xmm0
       lea      rdi, [rbp-0x330]
       mov      rsi, qword ptr [rbp-0x308]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG157:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x330]
       vmovdqu32 xmmword ptr [rbp-0x340], xmm0
 
G_M000_IG158:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x360], xmm0
       lea      rdi, [rbp-0x360]
       mov      rsi, qword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x340]
       mov      rsi, qword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x360]
       mov      rcx, qword ptr [rbp-0x358]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x350], rax
       mov      qword ptr [rbp-0x348], rdx
       mov      rdi, gword ptr [rbp-0x350]
       mov      rsi, qword ptr [rbp-0x348]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG159:
       mov      rsi, rax
       jmp      G_M000_IG173
 
G_M000_IG160:
       mov      rax, qword ptr [rbp-0x320]
       mov      qword ptr [rbp-0x368], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG163
 
G_M000_IG161:
       mov      rdi, qword ptr [rbp-0x420]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG162:
       jmp      SHORT G_M000_IG159
 
G_M000_IG163:
       test     rcx, rcx
       jl       G_M000_IG288
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG287
       test     rcx, rcx
       jl       G_M000_IG288
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG162
 
G_M000_IG164:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG172
 
G_M000_IG165:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x370], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x370]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG173
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG172
 
G_M000_IG167:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     rbx, rbx
       je       G_M000_IG172
 
G_M000_IG168:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG170
 
G_M000_IG169:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG173
 
G_M000_IG170:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG172
 
G_M000_IG171:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x378], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x378]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG173
 
G_M000_IG172:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG173:
       test     rsi, rsi
       je       G_M000_IG198
 
G_M000_IG174:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG188
 
G_M000_IG175:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG198
 
G_M000_IG176:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG186
 
G_M000_IG177:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x398], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x3A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG182
 
G_M000_IG178:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3B0], xmm0
       lea      rdi, [rbp-0x3B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG179:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3B0]
       vmovdqu32 xmmword ptr [rbp-0x3C0], xmm0
 
G_M000_IG180:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3E0], xmm0
       lea      rdi, [rbp-0x3E0]
       mov      rsi, qword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C0]
       mov      rsi, qword ptr [rbp-0x3B8]
       mov      rdx, gword ptr [rbp-0x3E0]
       mov      rcx, qword ptr [rbp-0x3D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D0], rax
       mov      qword ptr [rbp-0x3C8], rdx
       mov      rdi, gword ptr [rbp-0x3D0]
       mov      rsi, qword ptr [rbp-0x3C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG181:
       jmp      G_M000_IG199
 
G_M000_IG182:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG185
 
G_M000_IG183:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG184:
       jmp      SHORT G_M000_IG181
 
G_M000_IG185:
       test     rcx, rcx
       jl       G_M000_IG288
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG287
       test     rcx, rcx
       jl       G_M000_IG288
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG184
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG198
 
G_M000_IG187:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG199
 
G_M000_IG188:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG194
 
G_M000_IG189:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm0
       test     r15, r15
       je       G_M000_IG198
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG192
 
G_M000_IG191:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG199
 
G_M000_IG192:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG198
 
G_M000_IG193:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG199
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG198
 
G_M000_IG195:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG198
 
G_M000_IG196:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG198
 
G_M000_IG197:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG199
 
G_M000_IG198:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG199:
       mov      gword ptr [rbp+0x208], rax
       xor      rdi, rdi
       mov      gword ptr [rbp-0x408], rdi
 
G_M000_IG200:
       mov      gword ptr [rbp-0x400], rdi
 
G_M000_IG201:
       mov      gword ptr [rbp-0x3F8], rdi
       test     rax, rax
       je       G_M000_IG211
 
G_M000_IG202:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG269
 
G_M000_IG203:
       mov      rdi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG210
 
G_M000_IG204:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG206
 
G_M000_IG205:
       mov      rsi, qword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyInt:MultiplyI64(long,long):System.Object]
       mov      rdx, rax
       jmp      SHORT G_M000_IG212
 
G_M000_IG206:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG209
 
G_M000_IG207:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       SHORT G_M000_IG211
 
G_M000_IG208:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG212
 
G_M000_IG209:
       mov      rax, gword ptr [rbp+0x208]
       jmp      SHORT G_M000_IG211
 
G_M000_IG210:
       mov      rax, gword ptr [rbp+0x208]
 
G_M000_IG211:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG212:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG213:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG214:
       mov      gword ptr [rbp-0x3F8], rcx
       mov      rcx, r15
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG215:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG216:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x428], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x428]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x430], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x438], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x428]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x430]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x438]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x430]
       mov      rcx, qword ptr [rbp+0x270]
       add      r12, rcx
       jo       G_M000_IG288
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x278]
       cmp      r12, rdi
       setl     sil
       movzx    rsi, sil
       and      esi, edx
       cmp      r12, rdi
       setg     r8b
       movzx    r8, r8b
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, r8d
       or       edx, esi
       je       G_M000_IG277
 
G_M000_IG217:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG220
 
G_M000_IG218:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      eax
       mov      r8d, eax
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG287
       cmp      dword ptr [rsi+4*r8+0x10], 10
       je       SHORT G_M000_IG220
 
G_M000_IG219:
       cmp      eax, dword ptr [rdx+0x08]
       jae      G_M000_IG287
       mov      eax, eax
       mov      dword ptr [rdx+4*rax+0x10], 10
 
G_M000_IG220:
       mov      r8, r14
       mov      r9, rbx
       mov      r10, r15
       mov      rdx, r13
       test     r14, r14
       mov      qword ptr [rbp+0x278], rdi
       mov      qword ptr [rbp+0x270], rcx
       mov      gword ptr [rbp+0x1B0], r9
       mov      gword ptr [rbp+0x1A0], r10
       je       SHORT G_M000_IG226
 
G_M000_IG221:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG226
 
G_M000_IG222:
       test     rbx, rbx
       je       SHORT G_M000_IG226
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG226
 
G_M000_IG224:
       test     r15, r15
       je       SHORT G_M000_IG226
 
G_M000_IG225:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       je       G_M000_IG02
 
G_M000_IG226:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG233
 
G_M000_IG227:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rdx
       jne      SHORT G_M000_IG233
 
G_M000_IG228:
       test     rbx, rbx
       je       SHORT G_M000_IG233
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG233
 
G_M000_IG230:
       test     r15, r15
       je       SHORT G_M000_IG233
 
G_M000_IG231:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG233
 
G_M000_IG232:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG144
 
G_M000_IG233:
       test     r14, r14
       je       G_M000_IG261
 
G_M000_IG234:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG255
 
G_M000_IG235:
       mov      rsi, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG261
 
G_M000_IG236:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG253
 
G_M000_IG237:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x48], r9
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r10, r9
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x60]
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
       je       G_M000_IG249
 
G_M000_IG238:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x460], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x40C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x460]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG242
 
G_M000_IG239:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x40C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG240:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG241:
       mov      rsi, rax
       jmp      G_M000_IG262
 
G_M000_IG242:
       test     rcx, rcx
       je       SHORT G_M000_IG247
 
G_M000_IG243:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG244:
       test     rax, rax
       je       SHORT G_M000_IG248
 
G_M000_IG245:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG246:
       mov      edx, dword ptr [rbp-0x40C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG240
 
G_M000_IG247:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG244
 
G_M000_IG248:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG246
 
G_M000_IG249:
       mov      rdx, r10
       mov      qword ptr [rbp-0x88], rdx
       lea      rsi, [rdx+0x05]
       cmp      rsi, 261
       jbe      SHORT G_M000_IG252
 
G_M000_IG250:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG251:
       jmp      SHORT G_M000_IG241
 
G_M000_IG252:
       test     rsi, rsi
       jl       G_M000_IG288
       mov      rdi, rsi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG287
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG251
 
G_M000_IG253:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG261
 
G_M000_IG254:
       vmovsd   xmm0, qword ptr [rbx+0x08]
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
       jmp      G_M000_IG262
 
G_M000_IG255:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG261
 
G_M000_IG256:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG261
 
G_M000_IG257:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG259
 
G_M000_IG258:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG262
 
G_M000_IG259:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG261
 
G_M000_IG260:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG262
 
G_M000_IG261:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG262:
       test     rsi, rsi
       je       G_M000_IG33
 
G_M000_IG263:
       mov      rax, qword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      rax, rcx
       jne      G_M000_IG17
 
G_M000_IG264:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG265:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG15
 
G_M000_IG266:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG11
 
G_M000_IG267:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x468], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x410], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x468]
       mov      esi, dword ptr [rbp-0x410]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG268:
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG269:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG211
 
G_M000_IG270:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG211
 
G_M000_IG271:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG274
 
G_M000_IG272:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x400]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG211
 
G_M000_IG273:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG212
 
G_M000_IG274:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG211
 
G_M000_IG275:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG211
 
G_M000_IG276:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG212
 
G_M000_IG277:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG280
 
G_M000_IG278:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG287
       cmp      dword ptr [rdi+4*r8+0x10], 11
       je       SHORT G_M000_IG280
 
G_M000_IG279:
       mov      edi, esi
       cmp      edi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG287
       mov      dword ptr [rdx+4*rdi+0x10], 11
 
G_M000_IG280:
       mov      rbx, qword ptr [rbp+0x2A8]
       test     rbx, rbx
       jle      SHORT G_M000_IG283
 
G_M000_IG281:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG283
 
G_M000_IG282:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG283:
       add      rsp, 0x758
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG284:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG29
 
G_M000_IG285:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG91
 
G_M000_IG286:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG287:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG288:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7574

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 768
       lea      rbp, [rsp+0x300]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -672
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
       mov      gword ptr [rbp-0x50], r8
 
G_M000_IG02:
       mov      dword ptr [rbp-0x2F8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x68], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 91
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0xA0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xB0], eax
       cmp      dword ptr [rbp-0xA4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       and      eax, dword ptr [rbp-0xAC]
       movzx    rax, al
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB0]
       or       eax, dword ptr [rbp-0xB8]
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
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
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x180], rax
       jmp      G_M000_IG30
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       G_M000_IG65
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, qword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x138]
 
G_M000_IG09:
       add      rsp, 768
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 223
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2F0], rax
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F0]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 262
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x208], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x218], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x228], rax
       jmp      G_M000_IG46
 
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
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x188], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x158]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1A8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1B0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1B8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0xF8], rax
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1C0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1C8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1D0], xmm0
 
G_M000_IG17:
       vmovsd   xmm0, qword ptr [rbp-0x1D0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1D8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rbp-0x1E0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1E0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x1E8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x1E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 419
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1EC], eax
       cmp      dword ptr [rbp-0x1EC], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 446
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jne      G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 473
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F4], eax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 500
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0x150]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F8], eax
       cmp      dword ptr [rbp-0x1F8], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 527
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      dword ptr [rbp-0x1FC], 1
       cmp      dword ptr [rbp-0x1FC], 0
       jne      G_M000_IG27
       jmp      G_M000_IG13
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 546
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x278], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x288], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x298], rax
       jmp      G_M000_IG62
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 565
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG38:
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 12
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 13
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 14
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 15
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 16
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 17
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG33
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x210], xmm0
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x220], xmm0
       mov      rsi, gword ptr [rbp-0x228]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x230], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x210]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x238], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x238]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x240], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x240]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x248], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x248]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x250], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x250]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x258], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x260], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x260]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 730
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x228]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x264], eax
       cmp      dword ptr [rbp-0x264], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 757
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x268], eax
       cmp      dword ptr [rbp-0x268], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 784
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x26C], eax
       cmp      dword ptr [rbp-0x26C], 0
       jne      G_M000_IG43
       jmp      G_M000_IG36
 
G_M000_IG49:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG51
 
G_M000_IG50:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 811
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG51:
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 820
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG54:
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 18
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 19
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 20
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 21
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rdx, gword ptr [rbp-0x118]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 22
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 23
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG49
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x280], xmm0
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x290], xmm0
       mov      rsi, gword ptr [rbp-0x298]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2A8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rbp-0x2B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2B0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rbp-0x2B8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rbp-0x2C0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rbp-0x2D0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2E8], rax
       mov      rax, gword ptr [rbp-0x2E8]
       vmovsd   xmm0, qword ptr [rbp-0x2D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2E8]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG49
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 986
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rsi, gword ptr [rbp-0x298]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2D4], eax
       cmp      dword ptr [rbp-0x2D4], 0
       jne      G_M000_IG55
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 0x3F5
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG61:
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2D8], eax
       cmp      dword ptr [rbp-0x2D8], 0
       jne      G_M000_IG56
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG62:
       mov      eax, dword ptr [rbp-0x2F8]
       dec      eax
       mov      dword ptr [rbp-0x2F8], eax
       cmp      dword ptr [rbp-0x2F8], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:
       lea      rdi, [rbp-0x2F8]
       mov      esi, 0x410
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2DC], eax
       cmp      dword ptr [rbp-0x2DC], 0
       jne      G_M000_IG59
       jmp      G_M000_IG52
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3772

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1a3
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 35 inlinees with PGO data; 66 single block inlinees; 120 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x480
       mov      qword ptr [rsp+0x788], r15
       mov      qword ptr [rsp+0x780], r14
       mov      qword ptr [rsp+0x778], r13
       mov      qword ptr [rsp+0x770], r12
       mov      qword ptr [rsp+0x768], rbx
       lea      rbp, [rsp+0x480]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      r8, gword ptr [rbp+0x1C0]
       mov      rdx, gword ptr [rbp+0x190]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG237
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x190], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG237
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x1C0], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x1C0]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x1B8], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rbp+0x1B0]
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x1B0], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x1B0]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x1A8], xmm1
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x1A0]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x1A0], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x1A0]
 
G_M000_IG10:
       vmovsd   xmm2, qword ptr [r10+0x08]
       vmovsd   qword ptr [rbp+0x198], xmm2
       mov      rdx, gword ptr [rbp+0x190]
       vmovsd   xmm3, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x188], xmm3
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x1B8]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x158], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x218], rax
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x188]
       vmovsd   xmm2, qword ptr [rbp+0x198]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x128]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG227
 
G_M000_IG11:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG13:
       jmp      G_M000_IG279
 
G_M000_IG14:
       test     rax, rax
       jl       G_M000_IG299
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG298
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG13
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG33
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG23
 
G_M000_IG18:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG21:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG33
 
G_M000_IG22:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG33
 
G_M000_IG24:
       mov      rax, rsi
       mov      gword ptr [rbp-0x440], rax
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG33
 
G_M000_IG26:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG31
 
G_M000_IG27:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG30
 
G_M000_IG28:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xF4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG297
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x448], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x440]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x448]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x448]
 
G_M000_IG29:
       jmp      SHORT G_M000_IG34
 
G_M000_IG30:
       mov      rsi, rax
       jmp      SHORT G_M000_IG29
 
G_M000_IG31:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG295
 
G_M000_IG32:
       mov      rsi, r15
       jmp      SHORT G_M000_IG29
 
G_M000_IG33:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG34:
       test     rsi, rsi
       je       G_M000_IG63
 
G_M000_IG35:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG57
 
G_M000_IG36:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG63
 
G_M000_IG37:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG55
 
G_M000_IG38:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG51
 
G_M000_IG39:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x41C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x470]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG44
 
G_M000_IG40:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x41C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG41:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG42:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG43:
       mov      rsi, rax
       jmp      G_M000_IG64
 
G_M000_IG44:
       test     rcx, rcx
       je       SHORT G_M000_IG49
 
G_M000_IG45:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG46:
       test     rax, rax
       je       SHORT G_M000_IG50
 
G_M000_IG47:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG48:
       mov      edx, dword ptr [rbp-0x41C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG42
 
G_M000_IG49:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG46
 
G_M000_IG50:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG48
 
G_M000_IG51:
       mov      rax, r9
       mov      qword ptr [rbp-0x168], rax
       lea      rdx, [rax+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG54
 
G_M000_IG52:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG53:
       jmp      SHORT G_M000_IG43
 
G_M000_IG54:
       test     rdx, rdx
       jl       G_M000_IG299
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG298
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      SHORT G_M000_IG53
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG63
 
G_M000_IG56:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG64
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG63
 
G_M000_IG58:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rbx, rbx
       je       G_M000_IG63
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG61
 
G_M000_IG60:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG64
 
G_M000_IG61:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG63
 
G_M000_IG62:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG64
 
G_M000_IG63:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG64:
       test     rsi, rsi
       je       G_M000_IG95
 
G_M000_IG65:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG79
 
G_M000_IG66:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG95
 
G_M000_IG67:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG77
 
G_M000_IG68:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG73
 
G_M000_IG69:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG70:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG71:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG72:
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG73:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG75:
       jmp      SHORT G_M000_IG72
 
G_M000_IG76:
       test     rax, rax
       jl       G_M000_IG299
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG298
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG75
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG95
 
G_M000_IG78:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG85
 
G_M000_IG80:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     r15, r15
       je       G_M000_IG95
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG83
 
G_M000_IG82:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG83:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG95
 
G_M000_IG84:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG96
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG95
 
G_M000_IG86:
       mov      rax, rsi
       mov      gword ptr [rbp-0x450], rax
       test     r15, r15
       je       G_M000_IG95
 
G_M000_IG87:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG95
 
G_M000_IG88:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG93
 
G_M000_IG89:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG92
 
G_M000_IG90:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0x1F4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG297
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x458], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x450]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x1F4]
       mov      rsi, gword ptr [rbp-0x458]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x458]
 
G_M000_IG91:
       jmp      SHORT G_M000_IG96
 
G_M000_IG92:
       mov      rsi, rax
       jmp      SHORT G_M000_IG91
 
G_M000_IG93:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG296
 
G_M000_IG94:
       mov      rsi, r15
       jmp      SHORT G_M000_IG91
 
G_M000_IG95:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG96:
       test     rsi, rsi
       je       G_M000_IG125
 
G_M000_IG97:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG119
 
G_M000_IG98:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG125
 
G_M000_IG99:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG117
 
G_M000_IG100:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x218], rcx
       lea      rdi, [rbp-0x230]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x230]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG113
 
G_M000_IG101:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       lea      rdi, [rbp-0x228]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x228]
       mov      gword ptr [rbp-0x478], rax
       mov      ecx, dword ptr [rbp-0x220]
       mov      dword ptr [rbp-0x420], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       lea      rdi, [rbp-0x240]
       mov      rsi, qword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x240]
       mov      r9d, dword ptr [rbp-0x238]
       mov      rcx, gword ptr [rbp-0x478]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG106
 
G_M000_IG102:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x260], xmm0
       movsxd   rsi, dword ptr [rbp-0x420]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x260]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG103:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x260]
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
 
G_M000_IG104:
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG105:
       mov      rsi, rax
       jmp      G_M000_IG126
 
G_M000_IG106:
       test     rcx, rcx
       je       SHORT G_M000_IG111
 
G_M000_IG107:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG108:
       test     rax, rax
       je       SHORT G_M000_IG112
 
G_M000_IG109:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG110:
       mov      edx, dword ptr [rbp-0x420]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x250], rax
       mov      qword ptr [rbp-0x248], rdx
       jmp      SHORT G_M000_IG104
 
G_M000_IG111:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG108
 
G_M000_IG112:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG110
 
G_M000_IG113:
       mov      rcx, r9
       mov      qword ptr [rbp-0x268], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG116
 
G_M000_IG114:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x268]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG115:
       jmp      SHORT G_M000_IG105
 
G_M000_IG116:
       test     rdx, rdx
       jl       G_M000_IG299
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG298
       test     rdx, rdx
       jl       G_M000_IG299
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG115
 
G_M000_IG117:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG125
 
G_M000_IG118:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x270]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG126
 
G_M000_IG119:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG125
 
G_M000_IG120:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm0
       test     rbx, rbx
       je       G_M000_IG125
 
G_M000_IG121:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG123
 
G_M000_IG122:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG126
 
G_M000_IG123:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG125
 
G_M000_IG124:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG126
 
G_M000_IG125:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG126:
       test     rsi, rsi
       je       G_M000_IG151
 
G_M000_IG127:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG141
 
G_M000_IG128:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG151
 
G_M000_IG129:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG139
 
G_M000_IG130:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x298], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x2A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG135
 
G_M000_IG131:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2B0], xmm0
       lea      rdi, [rbp-0x2B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG132:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2B0]
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
 
G_M000_IG133:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2E0], xmm0
       lea      rdi, [rbp-0x2E0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x2B8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rcx, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D0], rax
       mov      qword ptr [rbp-0x2C8], rdx
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG134:
       jmp      G_M000_IG152
 
G_M000_IG135:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG138
 
G_M000_IG136:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG137:
       jmp      SHORT G_M000_IG134
 
G_M000_IG138:
       test     rcx, rcx
       jl       G_M000_IG299
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG298
       test     rcx, rcx
       jl       G_M000_IG299
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG137
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG151
 
G_M000_IG140:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG152
 
G_M000_IG141:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG147
 
G_M000_IG142:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm0
       test     r15, r15
       je       G_M000_IG151
 
G_M000_IG143:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG145
 
G_M000_IG144:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG152
 
G_M000_IG145:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG151
 
G_M000_IG146:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG152
 
G_M000_IG147:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG151
 
G_M000_IG148:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG151
 
G_M000_IG150:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG152
 
G_M000_IG151:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG152:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       je       G_M000_IG159
 
G_M000_IG153:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG159
 
G_M000_IG154:
       test     rbx, rbx
       je       SHORT G_M000_IG159
 
G_M000_IG155:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG159
 
G_M000_IG156:
       test     r15, r15
       je       SHORT G_M000_IG159
 
G_M000_IG157:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG159
 
G_M000_IG158:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x40], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG226
 
G_M000_IG159:
       test     r13, r13
       je       G_M000_IG180
 
G_M000_IG160:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG174
 
G_M000_IG161:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x308], r8
       test     rbx, rbx
       je       G_M000_IG180
 
G_M000_IG162:
       mov      qword ptr [rbp-0x428], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG172
 
G_M000_IG163:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x318], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x320], rdx
       lea      rdx, [rbp-0x320]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x320]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG168
 
G_M000_IG164:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x330], xmm0
       lea      rdi, [rbp-0x330]
       mov      rsi, qword ptr [rbp-0x308]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG165:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x330]
       vmovdqu32 xmmword ptr [rbp-0x340], xmm0
 
G_M000_IG166:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x360], xmm0
       lea      rdi, [rbp-0x360]
       mov      rsi, qword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x340]
       mov      rsi, qword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x360]
       mov      rcx, qword ptr [rbp-0x358]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x350], rax
       mov      qword ptr [rbp-0x348], rdx
       mov      rdi, gword ptr [rbp-0x350]
       mov      rsi, qword ptr [rbp-0x348]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG167:
       mov      rsi, rax
       jmp      G_M000_IG181
 
G_M000_IG168:
       mov      rax, qword ptr [rbp-0x320]
       mov      qword ptr [rbp-0x368], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG171
 
G_M000_IG169:
       mov      rdi, qword ptr [rbp-0x428]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG170:
       jmp      SHORT G_M000_IG167
 
G_M000_IG171:
       test     rcx, rcx
       jl       G_M000_IG299
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG298
       test     rcx, rcx
       jl       G_M000_IG299
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG170
 
G_M000_IG172:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG180
 
G_M000_IG173:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x370], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x370]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG181
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG180
 
G_M000_IG175:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     rbx, rbx
       je       G_M000_IG180
 
G_M000_IG176:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG178
 
G_M000_IG177:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG181
 
G_M000_IG178:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG180
 
G_M000_IG179:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x378], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x378]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG181
 
G_M000_IG180:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG181:
       test     rsi, rsi
       je       G_M000_IG206
 
G_M000_IG182:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG196
 
G_M000_IG183:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG206
 
G_M000_IG184:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG194
 
G_M000_IG185:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x398], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x3A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG190
 
G_M000_IG186:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3B0], xmm0
       lea      rdi, [rbp-0x3B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG187:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3B0]
       vmovdqu32 xmmword ptr [rbp-0x3C0], xmm0
 
G_M000_IG188:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3E0], xmm0
       lea      rdi, [rbp-0x3E0]
       mov      rsi, qword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C0]
       mov      rsi, qword ptr [rbp-0x3B8]
       mov      rdx, gword ptr [rbp-0x3E0]
       mov      rcx, qword ptr [rbp-0x3D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D0], rax
       mov      qword ptr [rbp-0x3C8], rdx
       mov      rdi, gword ptr [rbp-0x3D0]
       mov      rsi, qword ptr [rbp-0x3C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG189:
       jmp      G_M000_IG207
 
G_M000_IG190:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG193
 
G_M000_IG191:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG192:
       jmp      SHORT G_M000_IG189
 
G_M000_IG193:
       test     rcx, rcx
       jl       G_M000_IG299
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG298
       test     rcx, rcx
       jl       G_M000_IG299
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG192
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG206
 
G_M000_IG195:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG207
 
G_M000_IG196:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG202
 
G_M000_IG197:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm0
       test     r15, r15
       je       G_M000_IG206
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG200
 
G_M000_IG199:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG207
 
G_M000_IG200:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG206
 
G_M000_IG201:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG207
 
G_M000_IG202:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG206
 
G_M000_IG203:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG206
 
G_M000_IG204:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG206
 
G_M000_IG205:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG207
 
G_M000_IG206:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG207:
       mov      gword ptr [rbp+0x208], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x410], rdx
 
G_M000_IG208:
       mov      gword ptr [rbp-0x408], rdx
 
G_M000_IG209:
       mov      gword ptr [rbp-0x400], rdx
 
G_M000_IG210:
       mov      gword ptr [rbp-0x3F8], rdx
       test     rax, rax
       je       G_M000_IG221
 
G_M000_IG211:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG280
 
G_M000_IG212:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG220
 
G_M000_IG213:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG216
 
G_M000_IG214:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x410]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       SHORT G_M000_IG221
 
G_M000_IG215:
       mov      rdx, gword ptr [rbp-0x410]
       jmp      SHORT G_M000_IG222
 
G_M000_IG216:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG219
 
G_M000_IG217:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       SHORT G_M000_IG221
 
G_M000_IG218:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG222
 
G_M000_IG219:
       mov      rax, gword ptr [rbp+0x208]
       jmp      SHORT G_M000_IG221
 
G_M000_IG220:
       mov      rax, gword ptr [rbp+0x208]
 
G_M000_IG221:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG222:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x410], rcx
 
G_M000_IG223:
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG224:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG225:
       mov      gword ptr [rbp-0x3F8], rcx
       mov      rcx, r15
       mov      edi, 21
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 22
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 23
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG226:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG227:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x430], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x430]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x438], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x430]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x438]
       mov      rcx, qword ptr [rbp+0x270]
       add      r12, rcx
       jo       G_M000_IG299
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x278]
       cmp      r12, rdi
       setl     sil
       movzx    rsi, sil
       and      esi, edx
       cmp      r12, rdi
       setg     r8b
       movzx    r8, r8b
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, r8d
       or       edx, esi
       je       G_M000_IG288
 
G_M000_IG228:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG231
 
G_M000_IG229:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      eax
       mov      r8d, eax
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG298
       cmp      dword ptr [rsi+4*r8+0x10], 17
       je       SHORT G_M000_IG231
 
G_M000_IG230:
       cmp      eax, dword ptr [rdx+0x08]
       jae      G_M000_IG298
       mov      eax, eax
       mov      dword ptr [rdx+4*rax+0x10], 17
 
G_M000_IG231:
       mov      r8, r14
       mov      r9, rbx
       mov      r10, r15
       mov      rdx, r13
       test     r14, r14
       mov      qword ptr [rbp+0x278], rdi
       mov      qword ptr [rbp+0x270], rcx
       mov      gword ptr [rbp+0x1B0], r9
       mov      gword ptr [rbp+0x1A0], r10
       je       SHORT G_M000_IG237
 
G_M000_IG232:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG233:
       test     rbx, rbx
       je       SHORT G_M000_IG237
 
G_M000_IG234:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG235:
       test     r15, r15
       je       SHORT G_M000_IG237
 
G_M000_IG236:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       je       G_M000_IG02
 
G_M000_IG237:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG244
 
G_M000_IG238:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rdx
       jne      SHORT G_M000_IG244
 
G_M000_IG239:
       test     rbx, rbx
       je       SHORT G_M000_IG244
 
G_M000_IG240:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG244
 
G_M000_IG241:
       test     r15, r15
       je       SHORT G_M000_IG244
 
G_M000_IG242:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG244
 
G_M000_IG243:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG152
 
G_M000_IG244:
       test     r14, r14
       je       G_M000_IG272
 
G_M000_IG245:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG266
 
G_M000_IG246:
       mov      rsi, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG272
 
G_M000_IG247:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG264
 
G_M000_IG248:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x48], r9
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r10, r9
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x60]
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
       je       G_M000_IG260
 
G_M000_IG249:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x460], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x460]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG253
 
G_M000_IG250:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG251:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG252:
       mov      rsi, rax
       jmp      G_M000_IG273
 
G_M000_IG253:
       test     rcx, rcx
       je       SHORT G_M000_IG258
 
G_M000_IG254:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG255:
       test     rax, rax
       je       SHORT G_M000_IG259
 
G_M000_IG256:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG257:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG251
 
G_M000_IG258:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG255
 
G_M000_IG259:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG257
 
G_M000_IG260:
       mov      rdx, r10
       mov      qword ptr [rbp-0x88], rdx
       lea      rsi, [rdx+0x05]
       cmp      rsi, 261
       jbe      SHORT G_M000_IG263
 
G_M000_IG261:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG262:
       jmp      SHORT G_M000_IG252
 
G_M000_IG263:
       test     rsi, rsi
       jl       G_M000_IG299
       mov      rdi, rsi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG298
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG262
 
G_M000_IG264:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG272
 
G_M000_IG265:
       vmovsd   xmm0, qword ptr [rbx+0x08]
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
       jmp      G_M000_IG273
 
G_M000_IG266:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG272
 
G_M000_IG267:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG272
 
G_M000_IG268:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG270
 
G_M000_IG269:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG273
 
G_M000_IG270:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG272
 
G_M000_IG271:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG273
 
G_M000_IG272:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG273:
       test     rsi, rsi
       je       G_M000_IG33
 
G_M000_IG274:
       mov      rax, qword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      rax, rcx
       jne      G_M000_IG17
 
G_M000_IG275:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG276:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG15
 
G_M000_IG277:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG11
 
G_M000_IG278:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x468], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x418], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x468]
       mov      esi, dword ptr [rbp-0x418]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG279:
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG280:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG221
 
G_M000_IG281:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG221
 
G_M000_IG282:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG285
 
G_M000_IG283:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x400]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG221
 
G_M000_IG284:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG222
 
G_M000_IG285:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG221
 
G_M000_IG286:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG221
 
G_M000_IG287:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG222
 
G_M000_IG288:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG291
 
G_M000_IG289:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG298
       cmp      dword ptr [rdi+4*r8+0x10], 18
       je       SHORT G_M000_IG291
 
G_M000_IG290:
       mov      edi, esi
       cmp      edi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG298
       mov      dword ptr [rdx+4*rdi+0x10], 18
 
G_M000_IG291:
       mov      rbx, qword ptr [rbp+0x2A8]
       test     rbx, rbx
       jle      SHORT G_M000_IG294
 
G_M000_IG292:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG294
 
G_M000_IG293:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG294:
       add      rsp, 0x768
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG295:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG29
 
G_M000_IG296:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG91
 
G_M000_IG297:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG298:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG299:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7656

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 768
       lea      rbp, [rsp+0x300]
       xor      eax, eax
       mov      qword ptr [rbp-0x2F8], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -672
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
       mov      gword ptr [rbp-0x50], r8
 
G_M000_IG02:
       mov      dword ptr [rbp-0x300], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x68], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x300]
       mov      esi, 91
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0xA0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xB0], eax
       cmp      dword ptr [rbp-0xA4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       and      eax, dword ptr [rbp-0xAC]
       movzx    rax, al
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB0]
       or       eax, dword ptr [rbp-0xB8]
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x158], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x168], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x178], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x188], rax
       jmp      G_M000_IG30
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       G_M000_IG65
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, qword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x140]
 
G_M000_IG09:
       add      rsp, 768
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x300]
       mov      esi, 223
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x50]
       mov      edi, 24
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 3
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x2F8], rax
       mov      rdi, gword ptr [rbp-0x2F8]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F8]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F8]
       mov      rdx, gword ptr [rbp-0x130]
       mov      esi, 2
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x2F8]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x138], rax
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x300]
       mov      esi, 284
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x210], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x220], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x230], rax
       jmp      G_M000_IG46
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x160], xmm0
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x170], xmm0
       mov      rsi, gword ptr [rbp-0x178]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x180], xmm0
       mov      rsi, gword ptr [rbp-0x188]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x160]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1A8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1B0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rbp-0x1B8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1B8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x1C0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2F0], rax
       mov      rax, gword ptr [rbp-0x2F0]
       vmovsd   xmm0, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2F0]
       mov      gword ptr [rbp-0xF8], rax
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rbp-0x1C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1C8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x1D0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1D0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rbp-0x1D8], xmm0
 
G_M000_IG17:
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x1E0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1E0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rbp-0x1E8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x1F0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2F0], rax
       mov      rax, gword ptr [rbp-0x2F0]
       vmovsd   xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2F0]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x300]
       mov      esi, 441
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rsi, gword ptr [rbp-0x188]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F4], eax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x300]
       mov      esi, 468
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rsi, gword ptr [rbp-0x178]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F8], eax
       cmp      dword ptr [rbp-0x1F8], 0
       jne      G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x300]
       mov      esi, 495
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1FC], eax
       cmp      dword ptr [rbp-0x1FC], 0
       jne      G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x300]
       mov      esi, 522
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x200], eax
       cmp      dword ptr [rbp-0x200], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x300]
       mov      esi, 549
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      dword ptr [rbp-0x204], 1
       cmp      dword ptr [rbp-0x204], 0
       jne      G_M000_IG27
       jmp      G_M000_IG13
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x300]
       mov      esi, 568
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x280], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x290], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x2A0], rax
       jmp      G_M000_IG62
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x300]
       mov      esi, 587
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG38:
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 25
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 26
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 27
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 28
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 29
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 30
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG33
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x210]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x218], xmm0
       mov      rsi, gword ptr [rbp-0x220]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x228], xmm0
       mov      rsi, gword ptr [rbp-0x230]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x238], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x218]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rbp-0x240], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x240]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x238]
       vmovsd   qword ptr [rbp-0x248], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x248]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rbp-0x250], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x250]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x238]
       vmovsd   qword ptr [rbp-0x258], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rbp-0x260], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x260]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x238]
       vmovsd   qword ptr [rbp-0x268], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2F0], rax
       mov      rax, gword ptr [rbp-0x2F0]
       vmovsd   xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2F0]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x300]
       mov      esi, 752
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x230]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x26C], eax
       cmp      dword ptr [rbp-0x26C], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x300]
       mov      esi, 779
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x220]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x270], eax
       cmp      dword ptr [rbp-0x270], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x300]
       mov      esi, 806
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rsi, gword ptr [rbp-0x210]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x274], eax
       cmp      dword ptr [rbp-0x274], 0
       jne      G_M000_IG43
       jmp      G_M000_IG36
 
G_M000_IG49:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG51
 
G_M000_IG50:
       lea      rdi, [rbp-0x300]
       mov      esi, 833
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG51:
       mov      rax, gword ptr [rbp-0x150]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x300]
       mov      esi, 842
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG54:
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 31
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 32
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 33
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 34
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rdx, gword ptr [rbp-0x118]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 35
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 36
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x150], rax
       jmp      G_M000_IG49
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x280]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x288], xmm0
       mov      rsi, gword ptr [rbp-0x290]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x298], xmm0
       mov      rsi, gword ptr [rbp-0x2A0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rbp-0x2B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2B0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rbp-0x2B8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2B8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rbp-0x2C0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rbp-0x2D0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2D0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x2F0], rax
       mov      rax, gword ptr [rbp-0x2F0]
       vmovsd   xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x2F0]
       mov      gword ptr [rbp-0x150], rax
       jmp      G_M000_IG49
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x300]
       mov      esi, 0x3F0
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rsi, gword ptr [rbp-0x2A0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2DC], eax
       cmp      dword ptr [rbp-0x2DC], 0
       jne      G_M000_IG55
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       lea      rdi, [rbp-0x300]
       mov      esi, 0x40B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG61:
       mov      rsi, gword ptr [rbp-0x290]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E0], eax
       cmp      dword ptr [rbp-0x2E0], 0
       jne      G_M000_IG56
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG62:
       mov      eax, dword ptr [rbp-0x300]
       dec      eax
       mov      dword ptr [rbp-0x300], eax
       cmp      dword ptr [rbp-0x300], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:
       lea      rdi, [rbp-0x300]
       mov      esi, 0x426
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp-0x280]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2E4], eax
       cmp      dword ptr [rbp-0x2E4], 0
       jne      G_M000_IG59
       jmp      G_M000_IG52
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3837

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1b9
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 44 inlinees with PGO data; 66 single block inlinees; 108 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x3F0
       mov      qword ptr [rsp+0x6F8], r15
       mov      qword ptr [rsp+0x6F0], r14
       mov      qword ptr [rsp+0x6E8], r13
       mov      qword ptr [rsp+0x6E0], r12
       mov      qword ptr [rsp+0x6D8], rbx
       lea      rbp, [rsp+0x3F0]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -816
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      r8, gword ptr [rbp+0x1B8]
       mov      rdx, gword ptr [rbp+0x188]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG218
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x188], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG218
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x1B8], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x1B8]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x1B0], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rbp+0x1A8]
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x1A8], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x1A8]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x1A0], xmm1
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x198]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x198], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x198]
 
G_M000_IG10:
       vmovsd   xmm2, qword ptr [r10+0x08]
       vmovsd   qword ptr [rbp+0x190], xmm2
       mov      rdx, gword ptr [rbp+0x188]
       vmovsd   xmm3, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x180], xmm3
       vmovsd   xmm1, qword ptr [rbp+0x1A0]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x1B0]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x150], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x218], rax
       vmovsd   xmm1, qword ptr [rbp+0x1A0]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x180]
       vmovsd   xmm2, qword ptr [rbp+0x190]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x120], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x120]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG179
 
G_M000_IG11:
       test     rsi, rsi
       jl       G_M000_IG271
       mov      rdi, rsi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG270
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rax, rdi
       jmp      G_M000_IG243
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG20
 
G_M000_IG13:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG21
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG20
 
G_M000_IG15:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     rbx, rbx
       je       G_M000_IG20
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG18
 
G_M000_IG17:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG21
 
G_M000_IG18:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG21:
       test     rsi, rsi
       je       G_M000_IG52
 
G_M000_IG22:
       mov      r8, 0xD1FFAB1E
       mov      rax, r8
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG36
 
G_M000_IG23:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG52
 
G_M000_IG24:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG34
 
G_M000_IG25:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x110], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x118], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG30
 
G_M000_IG26:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG27:
       vmovdqu  xmm0, xmmword ptr [rbp-0x128]
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
 
G_M000_IG28:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x158], xmm0
       lea      rdi, [rbp-0x158]
       mov      rsi, qword ptr [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x138]
       mov      rsi, qword ptr [rbp-0x130]
       mov      rdx, gword ptr [rbp-0x158]
       mov      rcx, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x148], rax
       mov      qword ptr [rbp-0x140], rdx
       mov      rdi, gword ptr [rbp-0x148]
       mov      rsi, qword ptr [rbp-0x140]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG29:
       mov      rsi, rax
       jmp      G_M000_IG53
 
G_M000_IG30:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG33
 
G_M000_IG31:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG32:
       jmp      SHORT G_M000_IG29
 
G_M000_IG33:
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG270
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rdi, rcx
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG32
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG52
 
G_M000_IG35:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x160], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x160]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG53
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG42
 
G_M000_IG37:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm0
       test     r15, r15
       je       G_M000_IG52
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG40
 
G_M000_IG39:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG53
 
G_M000_IG40:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG52
 
G_M000_IG41:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x168], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG53
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG52
 
G_M000_IG43:
       mov      rax, rsi
       mov      gword ptr [rbp-0x3D0], rax
       test     r15, r15
       je       G_M000_IG52
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG52
 
G_M000_IG45:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG50
 
G_M000_IG46:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0x16C], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG269
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x3D8], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x3D0]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x16C]
       mov      rsi, gword ptr [rbp-0x3D8]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x3D8]
 
G_M000_IG48:
       jmp      SHORT G_M000_IG53
 
G_M000_IG49:
       mov      rsi, rax
       jmp      SHORT G_M000_IG48
 
G_M000_IG50:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG268
 
G_M000_IG51:
       mov      rsi, r15
       jmp      SHORT G_M000_IG48
 
G_M000_IG52:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG53:
       test     rsi, rsi
       je       G_M000_IG74
 
G_M000_IG54:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG68
 
G_M000_IG55:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG74
 
G_M000_IG56:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG66
 
G_M000_IG57:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x190], rcx
       lea      rdi, [rbp-0x1C8]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x1C8]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG62
 
G_M000_IG58:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
       lea      rdi, [rbp-0x1A0]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG59:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1A0]
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
 
G_M000_IG60:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
       lea      rdi, [rbp-0x1D8]
       mov      rsi, qword ptr [rbp-0x190]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1B0]
       mov      rsi, qword ptr [rbp-0x1A8]
       mov      rdx, gword ptr [rbp-0x1D8]
       mov      rcx, qword ptr [rbp-0x1D0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1C0], rax
       mov      qword ptr [rbp-0x1B8], rdx
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG61:
       mov      rsi, rax
       jmp      G_M000_IG75
 
G_M000_IG62:
       mov      rcx, r9
       mov      qword ptr [rbp-0x1E0], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1E0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG64:
       jmp      SHORT G_M000_IG61
 
G_M000_IG65:
       test     rdx, rdx
       jl       G_M000_IG271
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG270
       test     rdx, rdx
       jl       G_M000_IG271
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG64
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG74
 
G_M000_IG67:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG75
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG74
 
G_M000_IG69:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm0
       test     rbx, rbx
       je       G_M000_IG74
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG72
 
G_M000_IG71:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG75
 
G_M000_IG72:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG74
 
G_M000_IG73:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG75
 
G_M000_IG74:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG75:
       test     rsi, rsi
       je       G_M000_IG100
 
G_M000_IG76:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG90
 
G_M000_IG77:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG100
 
G_M000_IG78:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG88
 
G_M000_IG79:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x210], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x218], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG84
 
G_M000_IG80:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       lea      rdi, [rbp-0x228]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG81:
       vmovdqu  xmm0, xmmword ptr [rbp-0x228]
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
 
G_M000_IG82:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
       lea      rdi, [rbp-0x258]
       mov      rsi, qword ptr [rbp-0x210]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x230]
       mov      rdx, gword ptr [rbp-0x258]
       mov      rcx, qword ptr [rbp-0x250]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG83:
       mov      rsi, rax
       jmp      G_M000_IG101
 
G_M000_IG84:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG87
 
G_M000_IG85:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x218]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG86:
       jmp      SHORT G_M000_IG83
 
G_M000_IG87:
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG270
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG86
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG100
 
G_M000_IG89:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x260], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x260]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG101
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG96
 
G_M000_IG91:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1F8], xmm0
       test     r15, r15
       je       G_M000_IG100
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG94
 
G_M000_IG93:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x200]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG101
 
G_M000_IG94:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG100
 
G_M000_IG95:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x268], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG101
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG100
 
G_M000_IG97:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG100
 
G_M000_IG98:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG100
 
G_M000_IG99:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG101
 
G_M000_IG100:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG101:
       test     rsi, rsi
       je       G_M000_IG122
 
G_M000_IG102:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG116
 
G_M000_IG103:
       mov      rcx, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x280], rcx
       test     rbx, rbx
       je       G_M000_IG122
 
G_M000_IG104:
       mov      qword ptr [rbp-0x398], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG114
 
G_M000_IG105:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x290], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x298], rdx
       lea      rdx, [rbp-0x298]
       mov      rdi, rcx
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x298]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG110
 
G_M000_IG106:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2A8], xmm0
       lea      rdi, [rbp-0x2A8]
       mov      rsi, qword ptr [rbp-0x280]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG107:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2A8]
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
 
G_M000_IG108:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
       lea      rdi, [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x290]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2B8]
       mov      rsi, qword ptr [rbp-0x2B0]
       mov      rdx, gword ptr [rbp-0x2D8]
       mov      rcx, qword ptr [rbp-0x2D0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2C8], rax
       mov      qword ptr [rbp-0x2C0], rdx
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG109:
       mov      rsi, rax
       jmp      G_M000_IG123
 
G_M000_IG110:
       mov      rax, qword ptr [rbp-0x298]
       mov      qword ptr [rbp-0x2E0], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG113
 
G_M000_IG111:
       mov      rdi, qword ptr [rbp-0x398]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2E0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG112:
       jmp      SHORT G_M000_IG109
 
G_M000_IG113:
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG270
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG112
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG122
 
G_M000_IG115:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG123
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG122
 
G_M000_IG117:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x270], xmm0
       test     rbx, rbx
       je       G_M000_IG122
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG120
 
G_M000_IG119:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x270]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG123
 
G_M000_IG120:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG122
 
G_M000_IG121:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x270]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG123:
       test     rsi, rsi
       je       G_M000_IG148
 
G_M000_IG124:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG138
 
G_M000_IG125:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG148
 
G_M000_IG126:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG136
 
G_M000_IG127:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x310], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x318], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG132
 
G_M000_IG128:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x328], xmm0
       lea      rdi, [rbp-0x328]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG129:
       vmovdqu  xmm0, xmmword ptr [rbp-0x328]
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
 
G_M000_IG130:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
       lea      rdi, [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x330]
       mov      rdx, gword ptr [rbp-0x358]
       mov      rcx, qword ptr [rbp-0x350]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x348], rax
       mov      qword ptr [rbp-0x340], rdx
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG131:
       jmp      G_M000_IG149
 
G_M000_IG132:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG135
 
G_M000_IG133:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x318]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG134:
       jmp      SHORT G_M000_IG131
 
G_M000_IG135:
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG270
       test     rcx, rcx
       jl       G_M000_IG271
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG134
 
G_M000_IG136:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG148
 
G_M000_IG137:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x360], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x360]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG149
 
G_M000_IG138:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG144
 
G_M000_IG139:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     r15, r15
       je       G_M000_IG148
 
G_M000_IG140:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG142
 
G_M000_IG141:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG149
 
G_M000_IG142:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG148
 
G_M000_IG143:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x368], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x368]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG149
 
G_M000_IG144:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG148
 
G_M000_IG145:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG148
 
G_M000_IG146:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG149
 
G_M000_IG148:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG149:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       je       G_M000_IG156
 
G_M000_IG150:
       mov      rcx, qword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG156
 
G_M000_IG151:
       test     rbx, rbx
       je       SHORT G_M000_IG156
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG156
 
G_M000_IG153:
       test     r15, r15
       je       SHORT G_M000_IG156
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG156
 
G_M000_IG155:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x38], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG178
 
G_M000_IG156:
       xor      rdi, rdi
       mov      gword ptr [rbp-0x380], rdi
 
G_M000_IG157:
       mov      gword ptr [rbp-0x378], rdi
 
G_M000_IG158:
       mov      gword ptr [rbp-0x370], rdi
       test     r13, r13
       je       G_M000_IG174
 
G_M000_IG159:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      SHORT G_M000_IG166
 
G_M000_IG160:
       mov      rdi, qword ptr [r13+0x08]
       test     rbx, rbx
       je       G_M000_IG174
 
G_M000_IG161:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG163
 
G_M000_IG162:
       mov      rsi, qword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyInt:MultiplyI64(long,long):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG175
 
G_M000_IG163:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG174
 
G_M000_IG164:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x380]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       je       G_M000_IG174
 
G_M000_IG165:
       mov      rdx, gword ptr [rbp-0x380]
       jmp      G_M000_IG175
 
G_M000_IG166:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG174
 
G_M000_IG167:
       vmovsd   xmm0, qword ptr [r13+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG174
 
G_M000_IG168:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG171
 
G_M000_IG169:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x378]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG174
 
G_M000_IG170:
       mov      rdx, gword ptr [rbp-0x378]
       jmp      SHORT G_M000_IG175
 
G_M000_IG171:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG174
 
G_M000_IG172:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x370]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG174
 
G_M000_IG173:
       mov      rdx, gword ptr [rbp-0x370]
       jmp      SHORT G_M000_IG175
 
G_M000_IG174:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG175:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x380], rcx
 
G_M000_IG176:
       mov      gword ptr [rbp-0x378], rcx
 
G_M000_IG177:
       mov      gword ptr [rbp-0x370], rcx
       mov      rcx, r15
       mov      edi, 32
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 33
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 34
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 35
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 36
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG178:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG179:
       test     r14, r14
       je       G_M000_IG206
 
G_M000_IG180:
       mov      rdx, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      rdx, rcx
       jne      G_M000_IG198
 
G_M000_IG181:
       mov      rsi, qword ptr [r14+0x08]
       test     r13, r13
       je       G_M000_IG206
 
G_M000_IG182:
       mov      rdi, qword ptr [r13]
       mov      qword ptr [rbp-0x398], rcx
       cmp      rdi, rcx
       jne      G_M000_IG196
 
G_M000_IG183:
       mov      rdx, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x48], rdx
       mov      r8, rsi
       sub      r8, rdx
       mov      qword ptr [rbp-0x50], r8
       mov      rdi, rsi
       xor      rdi, rdx
       mov      r9, rsi
       xor      r9, r8
       and      rdi, r9
       jge      G_M000_IG192
 
G_M000_IG184:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x3E0], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x384], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3E0]
       mov      esi, dword ptr [rbp-0x384]
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3E8], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x388], edx
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG251
       movsxd   rdi, ecx
 
G_M000_IG185:
       mov      rdx, rax
       test     rdx, rdx
       jne      G_M000_IG260
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG190
 
G_M000_IG186:
       mov      edi, -1
 
G_M000_IG187:
       test     edi, edi
       jg       SHORT G_M000_IG191
 
G_M000_IG188:
       mov      rdi, qword ptr [rbp-0x398]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3B8], rax
       mov      rdi, gword ptr [rbp-0x3E8]
       mov      esi, dword ptr [rbp-0x388]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rcx, gword ptr [rbp-0x3B8]
       mov      qword ptr [rcx+0x08], rax
 
G_M000_IG189:
       mov      gword ptr [rbp-0x3A8], rcx
       jmp      G_M000_IG207
 
G_M000_IG190:
       xor      edi, edi
       jmp      SHORT G_M000_IG187
 
G_M000_IG191:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      gword ptr [rbp-0x3B0], rcx
       lea      rdi, bword ptr [rcx+0x08]
       mov      rsi, gword ptr [rbp-0x3E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x3B0]
       mov      edi, dword ptr [rbp-0x388]
       mov      dword ptr [rcx+0x10], edi
       jmp      SHORT G_M000_IG189
 
G_M000_IG192:
       mov      rcx, qword ptr [rbp-0x398]
       lea      rdx, [r8+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG195
 
G_M000_IG193:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x50]
       mov      qword ptr [rax+0x08], rdi
       mov      rcx, rax
 
G_M000_IG194:
       jmp      SHORT G_M000_IG189
 
G_M000_IG195:
       test     rdx, rdx
       jl       G_M000_IG271
       mov      rcx, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG270
       mov      rcx, gword ptr [rdi+8*rcx+0x10]
       jmp      SHORT G_M000_IG194
 
G_M000_IG196:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      G_M000_IG206
 
G_M000_IG197:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      gword ptr [rbp-0x3A8], rcx
       jmp      G_M000_IG207
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG204
 
G_M000_IG199:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r13, r13
       je       G_M000_IG206
 
G_M000_IG200:
       mov      rdi, qword ptr [r13]
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      SHORT G_M000_IG202
 
G_M000_IG201:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      rax, rcx
       mov      gword ptr [rbp-0x3A8], rax
       jmp      SHORT G_M000_IG207
 
G_M000_IG202:
       cmp      rdi, rcx
       jne      SHORT G_M000_IG206
 
G_M000_IG203:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x3A8], rax
       jmp      SHORT G_M000_IG207
 
G_M000_IG204:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      SHORT G_M000_IG206
 
G_M000_IG205:
       test     r13, r13
       je       SHORT G_M000_IG206
 
G_M000_IG206:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x3A8], rax
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 3
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x3A0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x3A0]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x3A0]
       lea      rdi, bword ptr [rax+0x20]
       mov      rsi, gword ptr [rbp-0x3A8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3C0], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3C8], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x3A0]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x3C0]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x3C8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x3C0]
       mov      rcx, qword ptr [rbp+0x270]
       add      r12, rcx
       jo       G_M000_IG271
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x278]
       cmp      r12, rdi
       setl     sil
       movzx    rsi, sil
       and      esi, edx
       cmp      r12, rdi
       setg     r8b
 
G_M000_IG208:
       movzx    r8, r8b
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, r8d
       or       edx, esi
       je       G_M000_IG244
 
G_M000_IG209:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG212
 
G_M000_IG210:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      eax
       mov      r8d, eax
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG270
       cmp      dword ptr [rsi+4*r8+0x10], 24
       je       SHORT G_M000_IG212
 
G_M000_IG211:
       cmp      eax, dword ptr [rdx+0x08]
       jae      G_M000_IG270
       mov      eax, eax
       mov      dword ptr [rdx+4*rax+0x10], 24
 
G_M000_IG212:
       mov      r8, r14
       mov      r9, rbx
       mov      r10, r15
       mov      rdx, r13
       test     r14, r14
       mov      qword ptr [rbp+0x278], rdi
       mov      qword ptr [rbp+0x270], rcx
       mov      gword ptr [rbp+0x1A8], r9
       mov      gword ptr [rbp+0x198], r10
       je       SHORT G_M000_IG218
 
G_M000_IG213:
       mov      rdi, qword ptr [r14]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      SHORT G_M000_IG218
 
G_M000_IG214:
       test     rbx, rbx
       je       SHORT G_M000_IG218
 
G_M000_IG215:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG218
 
G_M000_IG216:
       test     r15, r15
       je       SHORT G_M000_IG218
 
G_M000_IG217:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       je       G_M000_IG02
 
G_M000_IG218:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG225
 
G_M000_IG219:
       mov      rdx, qword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG225
 
G_M000_IG220:
       test     rbx, rbx
       je       SHORT G_M000_IG225
 
G_M000_IG221:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG225
 
G_M000_IG222:
       test     r15, r15
       je       SHORT G_M000_IG225
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG225
 
G_M000_IG224:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xA8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG149
 
G_M000_IG225:
       test     r14, r14
       je       G_M000_IG20
 
G_M000_IG226:
       mov      rdx, qword ptr [r14]
       mov      r8, 0xD1FFAB1E
       cmp      rdx, r8
       jne      G_M000_IG14
 
G_M000_IG227:
       mov      rsi, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG20
 
G_M000_IG228:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG12
 
G_M000_IG229:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0xA0], r9
       lea      rdi, [rbp-0xB8]
       mov      rdx, rsi
       mulx     rdx, r10, r9
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0xB8]
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
       je       G_M000_IG241
 
G_M000_IG230:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0x3F0], rax
       mov      ecx, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0x38C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, qword ptr [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      r9d, dword ptr [rbp-0xC0]
       mov      rcx, gword ptr [rbp-0x3F0]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG234
 
G_M000_IG231:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       movsxd   rsi, dword ptr [rbp-0x38C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xD8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xD8]
       mov      esi, dword ptr [rbp-0xD0]
 
G_M000_IG232:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG233:
       mov      rsi, rax
       jmp      G_M000_IG21
 
G_M000_IG234:
       test     rcx, rcx
       je       SHORT G_M000_IG239
 
G_M000_IG235:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG236:
       test     rax, rax
       je       SHORT G_M000_IG240
 
G_M000_IG237:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG238:
       mov      edx, dword ptr [rbp-0x38C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG232
 
G_M000_IG239:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG236
 
G_M000_IG240:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG238
 
G_M000_IG241:
       mov      rdx, r10
       mov      qword ptr [rbp-0xE0], rdx
       lea      rsi, [rdx+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG11
 
G_M000_IG242:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xE0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG243:
       jmp      SHORT G_M000_IG233
 
G_M000_IG244:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG247
 
G_M000_IG245:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG270
       cmp      dword ptr [rdi+4*r8+0x10], 25
       je       SHORT G_M000_IG247
 
G_M000_IG246:
       mov      edi, esi
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG270
       mov      dword ptr [rdx+4*rdi+0x10], 25
 
G_M000_IG247:
       mov      rbx, qword ptr [rbp+0x2A8]
       test     rbx, rbx
       jle      SHORT G_M000_IG250
 
G_M000_IG248:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG250
 
G_M000_IG249:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG250:
       add      rsp, 0x6D8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG251:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdx, rdi
       jl       SHORT G_M000_IG252
       mov      edx, dword ptr [rax+0x08]
       cmp      edx, 2
       jle      SHORT G_M000_IG253
 
G_M000_IG252:
       mov      esi, ecx
       jmp      SHORT G_M000_IG259
 
G_M000_IG253:
       cmp      edx, 2
       je       SHORT G_M000_IG254
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG270
       mov      edx, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG255
 
G_M000_IG254:
       mov      edx, dword ptr [rax+0x14]
       shl      rdx, 32
       mov      esi, dword ptr [rax+0x10]
       or       rdx, rsi
 
G_M000_IG255:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jae      SHORT G_M000_IG256
       mov      esi, -1
       jmp      SHORT G_M000_IG258
 
G_M000_IG256:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jbe      SHORT G_M000_IG257
       mov      esi, 1
       jmp      SHORT G_M000_IG258
 
G_M000_IG257:
       xor      esi, esi
 
G_M000_IG258:
       imul     esi, ecx
 
G_M000_IG259:
       test     esi, esi
       jge      G_M000_IG185
       jmp      G_M000_IG191
 
G_M000_IG260:
       mov      rdx, 0xD1FFAB1E
       xor      rdx, rdi
       jl       SHORT G_M000_IG261
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG262
 
G_M000_IG261:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x388]
       jmp      G_M000_IG187
 
G_M000_IG262:
       cmp      edi, 2
       je       SHORT G_M000_IG263
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG270
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG264
 
G_M000_IG263:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x3E8]
 
G_M000_IG264:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG265
       mov      edx, -1
       jmp      SHORT G_M000_IG267
 
G_M000_IG265:
       test     rdi, rdi
       jge      SHORT G_M000_IG266
       mov      edx, 1
       jmp      SHORT G_M000_IG267
 
G_M000_IG266:
       xor      edx, edx
 
G_M000_IG267:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG187
 
G_M000_IG268:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG48
 
G_M000_IG269:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG270:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG271:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7109

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xde
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 35 inlinees with PGO data; 66 single block inlinees; 120 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x490
       mov      qword ptr [rsp+0x798], r15
       mov      qword ptr [rsp+0x790], r14
       mov      qword ptr [rsp+0x788], r13
       mov      qword ptr [rsp+0x780], r12
       mov      qword ptr [rsp+0x778], rbx
       lea      rbp, [rsp+0x490]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      r8, qword ptr [rbp+0x270]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x270], r8
       test     r8, r8
       setg     r10b
       movzx    r10, r10b
       mov      dword ptr [rbp-0x424], r10d
       jmp      G_M000_IG262
 
G_M000_IG03:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG10
 
G_M000_IG04:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG10
 
G_M000_IG05:
       test     rbx, rbx
       je       SHORT G_M000_IG10
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG07:
       test     r15, r15
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG187
 
G_M000_IG10:
       test     r14, r14
       je       G_M000_IG38
 
G_M000_IG11:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG32
 
G_M000_IG12:
       mov      rax, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG38
 
G_M000_IG13:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG30
 
G_M000_IG14:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x48], r9
       lea      rdi, [rbp-0x60]
       mov      rdx, rax
       mulx     rdx, r10, r9
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x60]
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, r9
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, r10
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG26
 
G_M000_IG15:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x470]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG19
 
G_M000_IG16:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG17:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG18:
       jmp      G_M000_IG39
 
G_M000_IG19:
       test     rcx, rcx
       je       SHORT G_M000_IG24
 
G_M000_IG20:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG21:
       test     rax, rax
       je       SHORT G_M000_IG25
 
G_M000_IG22:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG23:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG17
 
G_M000_IG24:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG21
 
G_M000_IG25:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG23
 
G_M000_IG26:
       mov      rax, r10
       mov      qword ptr [rbp-0x88], rax
       lea      rdx, [rax+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG28:
       jmp      SHORT G_M000_IG18
 
G_M000_IG29:
       test     rdx, rdx
       jl       G_M000_IG294
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG293
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      SHORT G_M000_IG28
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG38
 
G_M000_IG31:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG39
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG38
 
G_M000_IG33:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG38
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG36
 
G_M000_IG35:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG39
 
G_M000_IG36:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG38
 
G_M000_IG37:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG39
 
G_M000_IG38:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG39:
       test     rax, rax
       je       G_M000_IG68
 
G_M000_IG40:
       mov      rcx, qword ptr [rax]
       mov      r8, 0xD1FFAB1E
       mov      rdx, r8
       cmp      rcx, rdx
       jne      G_M000_IG52
 
G_M000_IG41:
       mov      rsi, qword ptr [rax+0x08]
       test     r15, r15
       je       G_M000_IG68
 
G_M000_IG42:
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG50
 
G_M000_IG43:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xB8], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0xC0], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdi, r8
       jge      G_M000_IG46
 
G_M000_IG44:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x478], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x418], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x478]
       mov      esi, dword ptr [rbp-0x418]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG45:
       mov      rsi, rax
       jmp      G_M000_IG69
 
G_M000_IG46:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG49
 
G_M000_IG47:
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG48:
       jmp      SHORT G_M000_IG45
 
G_M000_IG49:
       test     rax, rax
       jl       G_M000_IG294
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG293
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG48
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG68
 
G_M000_IG51:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG69
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG58
 
G_M000_IG53:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r15, r15
       je       G_M000_IG68
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG56
 
G_M000_IG55:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG69
 
G_M000_IG56:
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG68
 
G_M000_IG57:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG69
 
G_M000_IG58:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG68
 
G_M000_IG59:
       mov      rsi, rax
       mov      gword ptr [rbp-0x450], rsi
       test     r15, r15
       je       G_M000_IG68
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG68
 
G_M000_IG61:
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG66
 
G_M000_IG62:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG65
 
G_M000_IG63:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0xF4], eax
       mov      edi, eax
       add      edi, dword ptr [r15+0x08]
       js       G_M000_IG292
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x458], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x450]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x458]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x458]
 
G_M000_IG64:
       jmp      SHORT G_M000_IG69
 
G_M000_IG65:
       jmp      SHORT G_M000_IG64
 
G_M000_IG66:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG290
 
G_M000_IG67:
       mov      rsi, r15
       jmp      SHORT G_M000_IG64
 
G_M000_IG68:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG69:
       test     rsi, rsi
       je       G_M000_IG98
 
G_M000_IG70:
       mov      rax, qword ptr [rsi]
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      rax, rcx
       jne      G_M000_IG92
 
G_M000_IG71:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG98
 
G_M000_IG72:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG90
 
G_M000_IG73:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG86
 
G_M000_IG74:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x480], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x41C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x480]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG79
 
G_M000_IG75:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x41C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG76:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG77:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG78:
       mov      rsi, rax
       jmp      G_M000_IG99
 
G_M000_IG79:
       test     rcx, rcx
       je       SHORT G_M000_IG84
 
G_M000_IG80:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG81:
       test     rax, rax
       je       SHORT G_M000_IG85
 
G_M000_IG82:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG83:
       mov      edx, dword ptr [rbp-0x41C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG77
 
G_M000_IG84:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG81
 
G_M000_IG85:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG83
 
G_M000_IG86:
       mov      rax, r9
       mov      qword ptr [rbp-0x168], rax
       lea      rdx, [rax+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG89
 
G_M000_IG87:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG88:
       jmp      SHORT G_M000_IG78
 
G_M000_IG89:
       test     rdx, rdx
       jl       G_M000_IG294
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG293
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      SHORT G_M000_IG88
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG98
 
G_M000_IG91:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG99
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG98
 
G_M000_IG93:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rbx, rbx
       je       G_M000_IG98
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG96
 
G_M000_IG95:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG99
 
G_M000_IG96:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG98
 
G_M000_IG97:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG99
 
G_M000_IG98:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG99:
       test     rsi, rsi
       je       G_M000_IG130
 
G_M000_IG100:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG114
 
G_M000_IG101:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG102:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG112
 
G_M000_IG103:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG108
 
G_M000_IG104:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG105:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG106:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG107:
       mov      rsi, rax
       jmp      G_M000_IG131
 
G_M000_IG108:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG110:
       jmp      SHORT G_M000_IG107
 
G_M000_IG111:
       test     rax, rax
       jl       G_M000_IG294
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG293
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG110
 
G_M000_IG112:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG130
 
G_M000_IG113:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG131
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG120
 
G_M000_IG115:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG118
 
G_M000_IG117:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG131
 
G_M000_IG118:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG130
 
G_M000_IG119:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG131
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG130
 
G_M000_IG121:
       mov      rax, rsi
       mov      gword ptr [rbp-0x460], rax
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG122:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG130
 
G_M000_IG123:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG128
 
G_M000_IG124:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG127
 
G_M000_IG125:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0x1F4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG292
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x468], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x460]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x1F4]
       mov      rsi, gword ptr [rbp-0x468]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x468]
 
G_M000_IG126:
       jmp      SHORT G_M000_IG131
 
G_M000_IG127:
       mov      rsi, rax
       jmp      SHORT G_M000_IG126
 
G_M000_IG128:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG291
 
G_M000_IG129:
       mov      rsi, r15
       jmp      SHORT G_M000_IG126
 
G_M000_IG130:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG131:
       test     rsi, rsi
       je       G_M000_IG160
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG154
 
G_M000_IG133:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG160
 
G_M000_IG134:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG152
 
G_M000_IG135:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x218], rcx
       lea      rdi, [rbp-0x230]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x230]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG148
 
G_M000_IG136:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       lea      rdi, [rbp-0x228]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x228]
       mov      gword ptr [rbp-0x488], rax
       mov      ecx, dword ptr [rbp-0x220]
       mov      dword ptr [rbp-0x420], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       lea      rdi, [rbp-0x240]
       mov      rsi, qword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x240]
       mov      r9d, dword ptr [rbp-0x238]
       mov      rcx, gword ptr [rbp-0x488]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG141
 
G_M000_IG137:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x260], xmm0
       movsxd   rsi, dword ptr [rbp-0x420]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x260]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG138:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x260]
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
 
G_M000_IG139:
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG140:
       mov      rsi, rax
       jmp      G_M000_IG161
 
G_M000_IG141:
       test     rcx, rcx
       je       SHORT G_M000_IG146
 
G_M000_IG142:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG143:
       test     rax, rax
       je       SHORT G_M000_IG147
 
G_M000_IG144:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG145:
       mov      edx, dword ptr [rbp-0x420]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x250], rax
       mov      qword ptr [rbp-0x248], rdx
       jmp      SHORT G_M000_IG139
 
G_M000_IG146:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG143
 
G_M000_IG147:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG145
 
G_M000_IG148:
       mov      rcx, r9
       mov      qword ptr [rbp-0x268], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x268]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG150:
       jmp      SHORT G_M000_IG140
 
G_M000_IG151:
       test     rdx, rdx
       jl       G_M000_IG294
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG293
       test     rdx, rdx
       jl       G_M000_IG294
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG150
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG160
 
G_M000_IG153:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x270]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG161
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG160
 
G_M000_IG155:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm0
       test     rbx, rbx
       je       G_M000_IG160
 
G_M000_IG156:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG158
 
G_M000_IG157:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG161
 
G_M000_IG158:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG160
 
G_M000_IG159:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG161
 
G_M000_IG160:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG161:
       test     rsi, rsi
       je       G_M000_IG186
 
G_M000_IG162:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG176
 
G_M000_IG163:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG186
 
G_M000_IG164:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG174
 
G_M000_IG165:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x298], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x2A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG170
 
G_M000_IG166:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2B0], xmm0
       lea      rdi, [rbp-0x2B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG167:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2B0]
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
 
G_M000_IG168:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2E0], xmm0
       lea      rdi, [rbp-0x2E0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x2B8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rcx, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D0], rax
       mov      qword ptr [rbp-0x2C8], rdx
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG169:
       jmp      G_M000_IG187
 
G_M000_IG170:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG173
 
G_M000_IG171:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG172:
       jmp      SHORT G_M000_IG169
 
G_M000_IG173:
       test     rcx, rcx
       jl       G_M000_IG294
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG293
       test     rcx, rcx
       jl       G_M000_IG294
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG172
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG186
 
G_M000_IG175:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG187
 
G_M000_IG176:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG182
 
G_M000_IG177:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm0
       test     r15, r15
       je       G_M000_IG186
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG180
 
G_M000_IG179:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG187
 
G_M000_IG180:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG186
 
G_M000_IG181:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG187
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG186
 
G_M000_IG183:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG186
 
G_M000_IG184:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG186
 
G_M000_IG185:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG187
 
G_M000_IG186:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG187:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       je       G_M000_IG194
 
G_M000_IG188:
       mov      rdi, qword ptr [rdi]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG194
 
G_M000_IG189:
       test     rbx, rbx
       je       SHORT G_M000_IG194
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG194
 
G_M000_IG191:
       test     r15, r15
       je       SHORT G_M000_IG194
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG194
 
G_M000_IG193:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x40], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG261
 
G_M000_IG194:
       test     r13, r13
       je       G_M000_IG215
 
G_M000_IG195:
       mov      rdi, qword ptr [r13]
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      rdi, rcx
       jne      G_M000_IG209
 
G_M000_IG196:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x308], r8
       test     rbx, rbx
       je       G_M000_IG215
 
G_M000_IG197:
       mov      qword ptr [rbp-0x430], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG207
 
G_M000_IG198:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x318], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x320], rdx
       lea      rdx, [rbp-0x320]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x320]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG203
 
G_M000_IG199:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x330], xmm0
       lea      rdi, [rbp-0x330]
       mov      rsi, qword ptr [rbp-0x308]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG200:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x330]
       vmovdqu32 xmmword ptr [rbp-0x340], xmm0
 
G_M000_IG201:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x360], xmm0
       lea      rdi, [rbp-0x360]
       mov      rsi, qword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x340]
       mov      rsi, qword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x360]
       mov      rcx, qword ptr [rbp-0x358]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x350], rax
       mov      qword ptr [rbp-0x348], rdx
       mov      rdi, gword ptr [rbp-0x350]
       mov      rsi, qword ptr [rbp-0x348]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG202:
       mov      rsi, rax
       jmp      G_M000_IG216
 
G_M000_IG203:
       mov      rax, qword ptr [rbp-0x320]
       mov      qword ptr [rbp-0x368], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG206
 
G_M000_IG204:
       mov      rdi, qword ptr [rbp-0x430]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG205:
       jmp      SHORT G_M000_IG202
 
G_M000_IG206:
       test     rcx, rcx
       jl       G_M000_IG294
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG293
       test     rcx, rcx
       jl       G_M000_IG294
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG205
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG215
 
G_M000_IG208:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x370], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x370]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG216
 
G_M000_IG209:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      G_M000_IG215
 
G_M000_IG210:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     rbx, rbx
       je       G_M000_IG215
 
G_M000_IG211:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG213
 
G_M000_IG212:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG216
 
G_M000_IG213:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG215
 
G_M000_IG214:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x378], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x378]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG216
 
G_M000_IG215:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG216:
       test     rsi, rsi
       je       G_M000_IG241
 
G_M000_IG217:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG231
 
G_M000_IG218:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG241
 
G_M000_IG219:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG229
 
G_M000_IG220:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x398], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x3A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG225
 
G_M000_IG221:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3B0], xmm0
       lea      rdi, [rbp-0x3B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG222:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3B0]
       vmovdqu32 xmmword ptr [rbp-0x3C0], xmm0
 
G_M000_IG223:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3E0], xmm0
       lea      rdi, [rbp-0x3E0]
       mov      rsi, qword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C0]
       mov      rsi, qword ptr [rbp-0x3B8]
       mov      rdx, gword ptr [rbp-0x3E0]
       mov      rcx, qword ptr [rbp-0x3D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D0], rax
       mov      qword ptr [rbp-0x3C8], rdx
       mov      rdi, gword ptr [rbp-0x3D0]
       mov      rsi, qword ptr [rbp-0x3C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG224:
       jmp      G_M000_IG242
 
G_M000_IG225:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG228
 
G_M000_IG226:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG227:
       jmp      SHORT G_M000_IG224
 
G_M000_IG228:
       test     rcx, rcx
       jl       G_M000_IG294
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG293
       test     rcx, rcx
       jl       G_M000_IG294
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG227
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG241
 
G_M000_IG230:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG242
 
G_M000_IG231:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG237
 
G_M000_IG232:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm0
       test     r15, r15
       je       G_M000_IG241
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG235
 
G_M000_IG234:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG242
 
G_M000_IG235:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG241
 
G_M000_IG236:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG242
 
G_M000_IG237:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG241
 
G_M000_IG238:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG241
 
G_M000_IG239:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG241
 
G_M000_IG240:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG242:
       mov      gword ptr [rbp+0x208], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x410], rdx
 
G_M000_IG243:
       mov      gword ptr [rbp-0x408], rdx
 
G_M000_IG244:
       mov      gword ptr [rbp-0x400], rdx
 
G_M000_IG245:
       mov      gword ptr [rbp-0x3F8], rdx
       test     rax, rax
       je       G_M000_IG256
 
G_M000_IG246:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG275
 
G_M000_IG247:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG255
 
G_M000_IG248:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG251
 
G_M000_IG249:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x410]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       SHORT G_M000_IG256
 
G_M000_IG250:
       mov      rdx, gword ptr [rbp-0x410]
       jmp      SHORT G_M000_IG257
 
G_M000_IG251:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG254
 
G_M000_IG252:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       SHORT G_M000_IG256
 
G_M000_IG253:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG257
 
G_M000_IG254:
       mov      rax, gword ptr [rbp+0x208]
       jmp      SHORT G_M000_IG256
 
G_M000_IG255:
       mov      rax, gword ptr [rbp+0x208]
 
G_M000_IG256:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG257:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x410], rcx
 
G_M000_IG258:
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG259:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG260:
       mov      gword ptr [rbp-0x3F8], rcx
       mov      rcx, r15
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG261:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG262:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x438], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x438]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x440], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x448], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x438]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x440]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x448]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x440]
       add      r12, qword ptr [rbp+0x270]
       jo       G_M000_IG294
       mov      edx, dword ptr [rbp-0x424]
       mov      edi, edx
       mov      rsi, qword ptr [rbp+0x278]
       cmp      r12, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       mov      qword ptr [rbp+0x278], rsi
       cmp      r12, rsi
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG283
 
G_M000_IG263:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG266
 
G_M000_IG264:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      r8, rax
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG293
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG266
 
G_M000_IG265:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG293
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 10
 
G_M000_IG266:
       test     r14, r14
       je       G_M000_IG03
 
G_M000_IG267:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG03
 
G_M000_IG268:
       test     rbx, rbx
       je       G_M000_IG03
 
G_M000_IG269:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG03
 
G_M000_IG270:
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG271:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG272:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG273:
       mov      rax, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG03
 
G_M000_IG274:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp+0x1A8], xmm1
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x198], xmm2
       vmovsd   xmm3, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x188], xmm3
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x158], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x158]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x218], rsi
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x188]
       vmovsd   xmm2, qword ptr [rbp+0x198]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x128]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      rax, rsi
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG262
 
G_M000_IG275:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG256
 
G_M000_IG276:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG256
 
G_M000_IG277:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG280
 
G_M000_IG278:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x400]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG256
 
G_M000_IG279:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG257
 
G_M000_IG280:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG256
 
G_M000_IG281:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG256
 
G_M000_IG282:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG257
 
G_M000_IG283:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG286
 
G_M000_IG284:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG293
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG286
 
G_M000_IG285:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG293
       mov      dword ptr [rcx+4*rdx+0x10], 11
 
G_M000_IG286:
       mov      rbx, qword ptr [rbp+0x2A8]
       test     rbx, rbx
       jle      SHORT G_M000_IG289
 
G_M000_IG287:
       movsxd   rcx, edi
       cmp      rbx, rcx
       jg       SHORT G_M000_IG289
 
G_M000_IG288:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG289:
       add      rsp, 0x778
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG290:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG64
 
G_M000_IG291:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG126
 
G_M000_IG292:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG293:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG294:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7521

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1bd
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 29 inlinees with PGO data; 60 single block inlinees; 121 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x470
       mov      qword ptr [rsp+0x778], r15
       mov      qword ptr [rsp+0x770], r14
       mov      qword ptr [rsp+0x768], r13
       mov      qword ptr [rsp+0x760], r12
       mov      qword ptr [rsp+0x758], rbx
       lea      rbp, [rsp+0x470]
       xor      eax, eax
       mov      qword ptr [rbp-0x408], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x400], xmm8
       vmovdqa32 xmmword ptr [rbp-0x3F0], xmm8
       mov      rax, -912
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      r9, gword ptr [rbp+0x1C0]
       mov      rdx, gword ptr [rbp+0x1A0]
       mov      r8, gword ptr [rbp+0x190]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG251
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x1A0], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG251
 
G_M000_IG04:
       test     r8, r8
       je       G_M000_IG251
 
G_M000_IG05:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x190], r8
       cmp      qword ptr [r8], rsi
       jne      G_M000_IG251
 
G_M000_IG06:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x1C0], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x1C0]
 
G_M000_IG08:
       vmovsd   xmm0, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x1B8], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x1B0]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x1B0], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x1B0]
 
G_M000_IG10:
       vmovsd   xmm1, qword ptr [r10+0x08]
       vmovsd   qword ptr [rbp+0x1A8], xmm1
       mov      rdx, gword ptr [rbp+0x1A0]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x198], xmm2
       mov      r8, gword ptr [rbp+0x190]
       vmovsd   xmm3, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x188], xmm3
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x1B8]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x158], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x218], rax
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x188]
       vmovsd   xmm2, qword ptr [rbp+0x198]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x128]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG243
 
G_M000_IG11:
       test     rcx, rcx
       je       SHORT G_M000_IG16
 
G_M000_IG12:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG13:
       test     rax, rax
       je       SHORT G_M000_IG17
 
G_M000_IG14:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG15:
       mov      edx, dword ptr [rbp-0x40C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      G_M000_IG265
 
G_M000_IG16:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG13
 
G_M000_IG17:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG15
 
G_M000_IG18:
       mov      rdx, r10
       mov      qword ptr [rbp-0x88], rdx
       lea      rsi, [rdx+0x05]
       cmp      rsi, 261
       jbe      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG20:
       jmp      G_M000_IG266
 
G_M000_IG21:
       test     rsi, rsi
       jl       G_M000_IG286
       mov      rdi, rsi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG285
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG20
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG30
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [rbx+0x08]
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
       jmp      G_M000_IG31
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG30
 
G_M000_IG25:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG30
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG27:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG31
 
G_M000_IG28:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG30
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG31:
       test     rsi, rsi
       je       G_M000_IG60
 
G_M000_IG32:
       mov      rax, qword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      rax, rcx
       jne      G_M000_IG44
 
G_M000_IG33:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG60
 
G_M000_IG34:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG42
 
G_M000_IG35:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG38
 
G_M000_IG36:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x468], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x410], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x468]
       mov      esi, dword ptr [rbp-0x410]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG37:
       mov      rsi, rax
       jmp      G_M000_IG61
 
G_M000_IG38:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG41
 
G_M000_IG39:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG40:
       jmp      SHORT G_M000_IG37
 
G_M000_IG41:
       test     rax, rax
       jl       G_M000_IG286
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG285
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG40
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG60
 
G_M000_IG43:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG61
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG50
 
G_M000_IG45:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r15, r15
       je       G_M000_IG60
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG48
 
G_M000_IG47:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG61
 
G_M000_IG48:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG60
 
G_M000_IG49:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG61
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG60
 
G_M000_IG51:
       mov      rax, rsi
       mov      gword ptr [rbp-0x440], rax
       test     r15, r15
       je       G_M000_IG60
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG60
 
G_M000_IG53:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG58
 
G_M000_IG54:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG57
 
G_M000_IG55:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xF4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG284
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x448], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x440]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x448]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x448]
 
G_M000_IG56:
       jmp      SHORT G_M000_IG61
 
G_M000_IG57:
       mov      rsi, rax
       jmp      SHORT G_M000_IG56
 
G_M000_IG58:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG282
 
G_M000_IG59:
       mov      rsi, r15
       jmp      SHORT G_M000_IG56
 
G_M000_IG60:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG61:
       test     rsi, rsi
       je       G_M000_IG90
 
G_M000_IG62:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG84
 
G_M000_IG63:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG90
 
G_M000_IG64:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG82
 
G_M000_IG65:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG78
 
G_M000_IG66:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x470]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG71
 
G_M000_IG67:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG68:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG69:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG70:
       mov      rsi, rax
       jmp      G_M000_IG91
 
G_M000_IG71:
       test     rcx, rcx
       je       SHORT G_M000_IG76
 
G_M000_IG72:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG73:
       test     rax, rax
       je       SHORT G_M000_IG77
 
G_M000_IG74:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG75:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG69
 
G_M000_IG76:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG73
 
G_M000_IG77:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG75
 
G_M000_IG78:
       mov      rax, r9
       mov      qword ptr [rbp-0x168], rax
       lea      rdx, [rax+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG81
 
G_M000_IG79:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG80:
       jmp      SHORT G_M000_IG70
 
G_M000_IG81:
       test     rdx, rdx
       jl       G_M000_IG286
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
       mov      rax, gword ptr [rdx+8*rax+0x10]
       jmp      SHORT G_M000_IG80
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG90
 
G_M000_IG83:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG91
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG90
 
G_M000_IG85:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rbx, rbx
       je       G_M000_IG90
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG88
 
G_M000_IG87:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG91
 
G_M000_IG88:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG90
 
G_M000_IG89:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG91
 
G_M000_IG90:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG91:
       test     rsi, rsi
       je       G_M000_IG122
 
G_M000_IG92:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG106
 
G_M000_IG93:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG122
 
G_M000_IG94:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG104
 
G_M000_IG95:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rax+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rax
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG100
 
G_M000_IG96:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG97:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG98:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG99:
       mov      rsi, rax
       jmp      G_M000_IG123
 
G_M000_IG100:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG102:
       jmp      SHORT G_M000_IG99
 
G_M000_IG103:
       test     rax, rax
       jl       G_M000_IG286
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG285
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG102
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG122
 
G_M000_IG105:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG123
 
G_M000_IG106:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG112
 
G_M000_IG107:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     r15, r15
       je       G_M000_IG122
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG110
 
G_M000_IG109:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG123
 
G_M000_IG110:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG122
 
G_M000_IG111:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG123
 
G_M000_IG112:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG122
 
G_M000_IG113:
       mov      rax, rsi
       mov      gword ptr [rbp-0x450], rax
       test     r15, r15
       je       G_M000_IG122
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG122
 
G_M000_IG115:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG120
 
G_M000_IG116:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG119
 
G_M000_IG117:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0x1F4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG284
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x458], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x450]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x1F4]
       mov      rsi, gword ptr [rbp-0x458]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x458]
 
G_M000_IG118:
       jmp      SHORT G_M000_IG123
 
G_M000_IG119:
       mov      rsi, rax
       jmp      SHORT G_M000_IG118
 
G_M000_IG120:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG283
 
G_M000_IG121:
       mov      rsi, r15
       jmp      SHORT G_M000_IG118
 
G_M000_IG122:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG123:
       test     rsi, rsi
       je       G_M000_IG144
 
G_M000_IG124:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG138
 
G_M000_IG125:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG144
 
G_M000_IG126:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG136
 
G_M000_IG127:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x218], rcx
       lea      rdi, [rbp-0x250]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x250]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG132
 
G_M000_IG128:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       lea      rdi, [rbp-0x228]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG129:
       vmovdqu  xmm0, xmmword ptr [rbp-0x228]
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
 
G_M000_IG130:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x260], xmm0
       lea      rdi, [rbp-0x260]
       mov      rsi, qword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x230]
       mov      rdx, gword ptr [rbp-0x260]
       mov      rcx, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG131:
       mov      rsi, rax
       jmp      G_M000_IG145
 
G_M000_IG132:
       mov      rcx, r9
       mov      qword ptr [rbp-0x268], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG135
 
G_M000_IG133:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x268]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG134:
       jmp      SHORT G_M000_IG131
 
G_M000_IG135:
       test     rdx, rdx
       jl       G_M000_IG286
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
       test     rdx, rdx
       jl       G_M000_IG286
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG134
 
G_M000_IG136:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG144
 
G_M000_IG137:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x270]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG145
 
G_M000_IG138:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG144
 
G_M000_IG139:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm0
       test     rbx, rbx
       je       G_M000_IG144
 
G_M000_IG140:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG142
 
G_M000_IG141:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG145
 
G_M000_IG142:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG144
 
G_M000_IG143:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG145
 
G_M000_IG144:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG145:
       test     rsi, rsi
       je       G_M000_IG170
 
G_M000_IG146:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG160
 
G_M000_IG147:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG170
 
G_M000_IG148:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG158
 
G_M000_IG149:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x298], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x2A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG154
 
G_M000_IG150:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2B0], xmm0
       lea      rdi, [rbp-0x2B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG151:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2B0]
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
 
G_M000_IG152:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2E0], xmm0
       lea      rdi, [rbp-0x2E0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x2B8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rcx, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D0], rax
       mov      qword ptr [rbp-0x2C8], rdx
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG153:
       jmp      G_M000_IG171
 
G_M000_IG154:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG157
 
G_M000_IG155:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG156:
       jmp      SHORT G_M000_IG153
 
G_M000_IG157:
       test     rcx, rcx
       jl       G_M000_IG286
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
       test     rcx, rcx
       jl       G_M000_IG286
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG156
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG170
 
G_M000_IG159:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG171
 
G_M000_IG160:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG166
 
G_M000_IG161:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm0
       test     r15, r15
       je       G_M000_IG170
 
G_M000_IG162:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG164
 
G_M000_IG163:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG171
 
G_M000_IG164:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG170
 
G_M000_IG165:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG171
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG170
 
G_M000_IG167:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG170
 
G_M000_IG168:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG170
 
G_M000_IG169:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG171
 
G_M000_IG170:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG171:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       je       G_M000_IG178
 
G_M000_IG172:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG178
 
G_M000_IG173:
       test     rbx, rbx
       je       SHORT G_M000_IG178
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG178
 
G_M000_IG175:
       test     r15, r15
       je       SHORT G_M000_IG178
 
G_M000_IG176:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG178
 
G_M000_IG177:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x40], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG242
 
G_M000_IG178:
       test     r13, r13
       je       G_M000_IG199
 
G_M000_IG179:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG193
 
G_M000_IG180:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x308], r8
       test     rbx, rbx
       je       G_M000_IG199
 
G_M000_IG181:
       mov      qword ptr [rbp-0x420], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG191
 
G_M000_IG182:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x318], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x320], rdx
       lea      rdx, [rbp-0x320]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x320]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG187
 
G_M000_IG183:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x330], xmm0
       lea      rdi, [rbp-0x330]
       mov      rsi, qword ptr [rbp-0x308]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG184:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x330]
       vmovdqu32 xmmword ptr [rbp-0x340], xmm0
 
G_M000_IG185:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x360], xmm0
       lea      rdi, [rbp-0x360]
       mov      rsi, qword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x340]
       mov      rsi, qword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x360]
       mov      rcx, qword ptr [rbp-0x358]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x350], rax
       mov      qword ptr [rbp-0x348], rdx
       mov      rdi, gword ptr [rbp-0x350]
       mov      rsi, qword ptr [rbp-0x348]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG186:
       mov      rsi, rax
       jmp      G_M000_IG200
 
G_M000_IG187:
       mov      rax, qword ptr [rbp-0x320]
       mov      qword ptr [rbp-0x368], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG190
 
G_M000_IG188:
       mov      rdi, qword ptr [rbp-0x420]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG189:
       jmp      SHORT G_M000_IG186
 
G_M000_IG190:
       test     rcx, rcx
       jl       G_M000_IG286
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
       test     rcx, rcx
       jl       G_M000_IG286
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG189
 
G_M000_IG191:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG199
 
G_M000_IG192:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x370], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x370]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG200
 
G_M000_IG193:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG199
 
G_M000_IG194:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     rbx, rbx
       je       G_M000_IG199
 
G_M000_IG195:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG197
 
G_M000_IG196:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG200
 
G_M000_IG197:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG199
 
G_M000_IG198:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x378], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x378]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG200
 
G_M000_IG199:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG200:
       test     rsi, rsi
       je       G_M000_IG225
 
G_M000_IG201:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG215
 
G_M000_IG202:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG225
 
G_M000_IG203:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG213
 
G_M000_IG204:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x398], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x3A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG209
 
G_M000_IG205:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3B0], xmm0
       lea      rdi, [rbp-0x3B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG206:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3B0]
       vmovdqu32 xmmword ptr [rbp-0x3C0], xmm0
 
G_M000_IG207:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3E0], xmm0
       lea      rdi, [rbp-0x3E0]
       mov      rsi, qword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C0]
       mov      rsi, qword ptr [rbp-0x3B8]
       mov      rdx, gword ptr [rbp-0x3E0]
       mov      rcx, qword ptr [rbp-0x3D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D0], rax
       mov      qword ptr [rbp-0x3C8], rdx
       mov      rdi, gword ptr [rbp-0x3D0]
       mov      rsi, qword ptr [rbp-0x3C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG208:
       jmp      G_M000_IG226
 
G_M000_IG209:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG212
 
G_M000_IG210:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG211:
       jmp      SHORT G_M000_IG208
 
G_M000_IG212:
       test     rcx, rcx
       jl       G_M000_IG286
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
       test     rcx, rcx
       jl       G_M000_IG286
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG211
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG225
 
G_M000_IG214:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG226
 
G_M000_IG215:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG221
 
G_M000_IG216:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm0
       test     r15, r15
       je       G_M000_IG225
 
G_M000_IG217:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG219
 
G_M000_IG218:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG226
 
G_M000_IG219:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG225
 
G_M000_IG220:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG226
 
G_M000_IG221:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG225
 
G_M000_IG222:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG225
 
G_M000_IG223:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG225
 
G_M000_IG224:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG226
 
G_M000_IG225:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG226:
       mov      gword ptr [rbp+0x208], rax
       xor      rdi, rdi
       mov      gword ptr [rbp-0x408], rdi
 
G_M000_IG227:
       mov      gword ptr [rbp-0x400], rdi
 
G_M000_IG228:
       mov      gword ptr [rbp-0x3F8], rdi
       test     rax, rax
       je       G_M000_IG238
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG267
 
G_M000_IG230:
       mov      rdi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG237
 
G_M000_IG231:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG233
 
G_M000_IG232:
       mov      rsi, qword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyInt:MultiplyI64(long,long):System.Object]
       mov      rdx, rax
       jmp      SHORT G_M000_IG239
 
G_M000_IG233:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG236
 
G_M000_IG234:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       SHORT G_M000_IG238
 
G_M000_IG235:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG239
 
G_M000_IG236:
       mov      rax, gword ptr [rbp+0x208]
       jmp      SHORT G_M000_IG238
 
G_M000_IG237:
       mov      rax, gword ptr [rbp+0x208]
 
G_M000_IG238:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG239:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG240:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG241:
       mov      gword ptr [rbp-0x3F8], rcx
       mov      rcx, r15
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG242:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG243:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x428], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x428]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x430], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x438], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x428]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x430]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x438]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x430]
       mov      rcx, qword ptr [rbp+0x270]
       add      r12, rcx
       jo       G_M000_IG286
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x278]
       cmp      r12, rdi
       setl     sil
       movzx    rsi, sil
       and      esi, edx
       cmp      r12, rdi
       setg     r8b
       movzx    r8, r8b
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, r8d
       or       edx, esi
       je       G_M000_IG275
 
G_M000_IG244:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG247
 
G_M000_IG245:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      eax
       mov      r8d, eax
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG285
       cmp      dword ptr [rsi+4*r8+0x10], 10
       je       SHORT G_M000_IG247
 
G_M000_IG246:
       cmp      eax, dword ptr [rdx+0x08]
       jae      G_M000_IG285
       mov      eax, eax
       mov      dword ptr [rdx+4*rax+0x10], 10
 
G_M000_IG247:
       mov      r9, r14
       mov      r10, rbx
       mov      rdx, r15
       mov      r8, r13
       test     r14, r14
       mov      qword ptr [rbp+0x278], rdi
       mov      qword ptr [rbp+0x270], rcx
       mov      gword ptr [rbp+0x1B0], r10
       je       SHORT G_M000_IG251
 
G_M000_IG248:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG251
 
G_M000_IG249:
       test     rbx, rbx
       je       SHORT G_M000_IG251
 
G_M000_IG250:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       je       G_M000_IG02
 
G_M000_IG251:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG258
 
G_M000_IG252:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rdx
       jne      SHORT G_M000_IG258
 
G_M000_IG253:
       test     rbx, rbx
       je       SHORT G_M000_IG258
 
G_M000_IG254:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG258
 
G_M000_IG255:
       test     r15, r15
       je       SHORT G_M000_IG258
 
G_M000_IG256:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG258
 
G_M000_IG257:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG171
 
G_M000_IG258:
       test     r14, r14
       je       G_M000_IG30
 
G_M000_IG259:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG24
 
G_M000_IG260:
       mov      rsi, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG30
 
G_M000_IG261:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG22
 
G_M000_IG262:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x48], r9
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r10, r9
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rbp-0x60]
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
       je       G_M000_IG18
 
G_M000_IG263:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x460], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x40C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x460]
       mov      rsi, rcx
       or       rsi, rax
       jne      G_M000_IG11
 
G_M000_IG264:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x40C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG265:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG266:
       mov      rsi, rax
       jmp      G_M000_IG31
 
G_M000_IG267:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG238
 
G_M000_IG268:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG238
 
G_M000_IG269:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG272
 
G_M000_IG270:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x400]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG238
 
G_M000_IG271:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG239
 
G_M000_IG272:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG238
 
G_M000_IG273:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x208]
       je       G_M000_IG238
 
G_M000_IG274:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG239
 
G_M000_IG275:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG278
 
G_M000_IG276:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG285
       cmp      dword ptr [rdi+4*r8+0x10], 11
       je       SHORT G_M000_IG278
 
G_M000_IG277:
       mov      edi, esi
       cmp      edi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG285
       mov      dword ptr [rdx+4*rdi+0x10], 11
 
G_M000_IG278:
       mov      rbx, qword ptr [rbp+0x2A8]
       test     rbx, rbx
       jle      SHORT G_M000_IG281
 
G_M000_IG279:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG281
 
G_M000_IG280:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG281:
       add      rsp, 0x758
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG282:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG56
 
G_M000_IG283:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG118
 
G_M000_IG284:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG285:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG286:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7541

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x105
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 42 inlinees with PGO data; 7 single block inlinees; 88 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x570
       mov      qword ptr [rsp+0x878], r15
       mov      qword ptr [rsp+0x870], r14
       mov      qword ptr [rsp+0x868], r13
       mov      qword ptr [rsp+0x860], r12
       mov      qword ptr [rsp+0x858], rbx
       lea      rbp, [rsp+0x570]
       xor      eax, eax
       mov      qword ptr [rbp-0x508], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -0x4B0
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      rcx, qword ptr [rbp+0x270]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x270], rcx
       test     rcx, rcx
       setg     sil
       movzx    rsi, sil
       mov      dword ptr [rbp-0x52C], esi
 
G_M000_IG03:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG10
 
G_M000_IG04:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       jne      SHORT G_M000_IG10
 
G_M000_IG05:
       test     rbx, rbx
       je       SHORT G_M000_IG10
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG07:
       test     r15, r15
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG231
 
G_M000_IG10:
       test     r14, r14
       je       G_M000_IG25
 
G_M000_IG11:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       je       G_M000_IG36
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG22
 
G_M000_IG13:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG16
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG16
 
G_M000_IG15:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG26
 
G_M000_IG16:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG19
 
G_M000_IG18:
       xor      rdi, rdi
 
G_M000_IG19:
       test     rdi, rdi
       je       SHORT G_M000_IG25
 
G_M000_IG20:
       mov      rsi, rbx
       mov      rdi, r8
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x80], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG25
 
G_M000_IG21:
       jmp      SHORT G_M000_IG26
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       mov      rdi, qword ptr [rdi]
 
G_M000_IG25:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG26:
       mov      gword ptr [rbp+0x240], rax
       test     rax, rax
       je       G_M000_IG66
 
G_M000_IG27:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG54
 
G_M000_IG28:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x98], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       xor      rsi, rsi
 
G_M000_IG31:
       test     rsi, rsi
       je       G_M000_IG88
 
G_M000_IG32:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xA8], rax
       mov      rcx, qword ptr [rbp-0x98]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0xB0], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG84
 
G_M000_IG33:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x560], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x520], ecx
       mov      rdi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x560]
       mov      esi, dword ptr [rbp-0x520]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG34:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG65
 
G_M000_IG35:
       jmp      G_M000_IG67
 
G_M000_IG36:
       mov      rsi, r14
       mov      qword ptr [rbp-0x538], r8
       mov      rdi, r8
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x40], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG39
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG39
 
G_M000_IG38:
       xor      rsi, rsi
 
G_M000_IG39:
       test     rsi, rsi
       je       G_M000_IG48
 
G_M000_IG40:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x50], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x58], rdx
       lea      rdx, [rbp-0x58]
       mov      rdi, qword ptr [rbp-0x40]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x58]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG44
 
G_M000_IG41:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, qword ptr [rbp-0x40]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x558], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x51C], ecx
       mov      rdi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x558]
       mov      esi, dword ptr [rbp-0x51C]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG42:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG25
 
G_M000_IG43:
       jmp      G_M000_IG26
 
G_M000_IG44:
       mov      rax, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x70], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG47
 
G_M000_IG45:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x70]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG46:
       jmp      SHORT G_M000_IG42
 
G_M000_IG47:
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG484
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG46
 
G_M000_IG48:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG51
 
G_M000_IG50:
       xor      rsi, rsi
 
G_M000_IG51:
       test     rsi, rsi
       je       G_M000_IG25
 
G_M000_IG52:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       G_M000_IG25
 
G_M000_IG53:
       jmp      G_M000_IG26
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG78
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     r15, r15
       je       SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG57:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG67
 
G_M000_IG58:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG61
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG61
 
G_M000_IG60:
       xor      rsi, rsi
 
G_M000_IG61:
       test     rsi, rsi
       je       G_M000_IG77
 
G_M000_IG62:
       mov      rsi, r15
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG64
 
G_M000_IG63:
       jmp      SHORT G_M000_IG67
 
G_M000_IG64:
       mov      rax, gword ptr [rbp+0x240]
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:
       mov      rax, gword ptr [rbp+0x240]
 
G_M000_IG66:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG67:
       mov      gword ptr [rbp+0x238], rax
       test     rax, rax
       je       G_M000_IG108
 
G_M000_IG68:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG96
 
G_M000_IG69:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE8], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG72
 
G_M000_IG71:
       xor      rsi, rsi
 
G_M000_IG72:
       test     rsi, rsi
       je       G_M000_IG126
 
G_M000_IG73:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xF8], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x100], rdx
       lea      rdx, [rbp-0x100]
       mov      rdi, qword ptr [rbp-0xE8]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x100]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG122
 
G_M000_IG74:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x568], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x524], ecx
       mov      rdi, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x568]
       mov      esi, dword ptr [rbp-0x524]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG75:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG107
 
G_M000_IG76:
       jmp      G_M000_IG109
 
G_M000_IG77:
       mov      rax, gword ptr [rbp+0x240]
       jmp      G_M000_IG66
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG66
 
G_M000_IG79:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG82
 
G_M000_IG80:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG82
 
G_M000_IG81:
       xor      rsi, rsi
 
G_M000_IG82:
       test     rsi, rsi
       je       G_M000_IG66
 
G_M000_IG83:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG67
 
G_M000_IG84:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG87
 
G_M000_IG85:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG86:
       jmp      G_M000_IG34
 
G_M000_IG87:
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG484
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG86
 
G_M000_IG88:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG91
 
G_M000_IG89:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG91
 
G_M000_IG90:
       xor      rsi, rsi
 
G_M000_IG91:
       test     rsi, rsi
       je       SHORT G_M000_IG95
 
G_M000_IG92:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG94
 
G_M000_IG93:
       jmp      G_M000_IG67
 
G_M000_IG94:
       mov      rax, gword ptr [rbp+0x240]
       jmp      G_M000_IG66
 
G_M000_IG95:
       mov      rax, gword ptr [rbp+0x240]
       jmp      G_M000_IG66
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG119
 
G_M000_IG97:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm0
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x238]
       je       SHORT G_M000_IG100
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG100
 
G_M000_IG99:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG109
 
G_M000_IG100:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG103
 
G_M000_IG102:
       xor      rsi, rsi
 
G_M000_IG103:
       test     rsi, rsi
       je       SHORT G_M000_IG108
 
G_M000_IG104:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x128]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       jmp      SHORT G_M000_IG109
 
G_M000_IG106:
       mov      rax, gword ptr [rbp+0x238]
       jmp      SHORT G_M000_IG108
 
G_M000_IG107:
       mov      rax, gword ptr [rbp+0x238]
 
G_M000_IG108:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG109:
       mov      gword ptr [rbp+0x230], rax
       test     rax, rax
       je       G_M000_IG146
 
G_M000_IG110:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG134
 
G_M000_IG111:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x140], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG114
 
G_M000_IG112:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       xor      rsi, rsi
 
G_M000_IG114:
       test     rsi, rsi
       je       G_M000_IG170
 
G_M000_IG115:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x150], rax
       mov      rcx, qword ptr [rbp-0x140]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x158], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG166
 
G_M000_IG116:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x570], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x528], ecx
       mov      rdi, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x570]
       mov      esi, dword ptr [rbp-0x528]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG117:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG145
 
G_M000_IG118:
       jmp      G_M000_IG147
 
G_M000_IG119:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG108
 
G_M000_IG120:
       mov      rsi, rbx
       test     rsi, rsi
       je       G_M000_IG108
 
G_M000_IG121:
       mov      rsi, qword ptr [rsi]
       jmp      G_M000_IG108
 
G_M000_IG122:
       mov      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x118], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG125
 
G_M000_IG123:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG124:
       jmp      G_M000_IG75
 
G_M000_IG125:
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG484
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG124
 
G_M000_IG126:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG129
 
G_M000_IG127:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG129
 
G_M000_IG128:
       xor      rsi, rsi
 
G_M000_IG129:
       test     rsi, rsi
       je       SHORT G_M000_IG133
 
G_M000_IG130:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rbp-0x120], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG132
 
G_M000_IG131:
       jmp      G_M000_IG109
 
G_M000_IG132:
       mov      rax, gword ptr [rbp+0x238]
       jmp      G_M000_IG108
 
G_M000_IG133:
       mov      rax, gword ptr [rbp+0x238]
       jmp      G_M000_IG108
 
G_M000_IG134:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG160
 
G_M000_IG135:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm0
       test     r15, r15
       je       SHORT G_M000_IG138
 
G_M000_IG136:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG138
 
G_M000_IG137:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x130]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x138]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG147
 
G_M000_IG138:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG141
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG141
 
G_M000_IG140:
       xor      rsi, rsi
 
G_M000_IG141:
       test     rsi, rsi
       je       G_M000_IG159
 
G_M000_IG142:
       mov      rsi, r15
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG144
 
G_M000_IG143:
       jmp      SHORT G_M000_IG147
 
G_M000_IG144:
       mov      rax, gword ptr [rbp+0x230]
       jmp      SHORT G_M000_IG146
 
G_M000_IG145:
       mov      rax, gword ptr [rbp+0x230]
 
G_M000_IG146:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG147:
       mov      gword ptr [rbp+0x228], rax
       test     rax, rax
       je       G_M000_IG190
 
G_M000_IG148:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG178
 
G_M000_IG149:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x190], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG152
 
G_M000_IG150:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG152
 
G_M000_IG151:
       xor      rsi, rsi
 
G_M000_IG152:
       test     rsi, rsi
       je       G_M000_IG210
 
G_M000_IG153:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x1A0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x1A8], rdx
       lea      rdx, [rbp-0x1A8]
       mov      rdi, qword ptr [rbp-0x190]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x1A8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG206
 
G_M000_IG154:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       mov      rsi, qword ptr [rbp-0x190]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG155:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1B8]
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
 
G_M000_IG156:
       mov      rdi, qword ptr [rbp-0x1A0]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D8], rax
       mov      qword ptr [rbp-0x1D0], rdx
       mov      rdi, gword ptr [rbp-0x1C8]
       mov      rsi, qword ptr [rbp-0x1C0]
       mov      rdx, gword ptr [rbp-0x1D8]
       mov      rcx, qword ptr [rbp-0x1D0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1E8], rax
       mov      qword ptr [rbp-0x1E0], rdx
       mov      rdi, gword ptr [rbp-0x1E8]
       mov      rsi, qword ptr [rbp-0x1E0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG157:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG189
 
G_M000_IG158:
       jmp      G_M000_IG191
 
G_M000_IG159:
       mov      rax, gword ptr [rbp+0x230]
       jmp      G_M000_IG146
 
G_M000_IG160:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG146
 
G_M000_IG161:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG164
 
G_M000_IG162:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG164
 
G_M000_IG163:
       xor      rsi, rsi
 
G_M000_IG164:
       test     rsi, rsi
       je       G_M000_IG146
 
G_M000_IG165:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG147
 
G_M000_IG166:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG169
 
G_M000_IG167:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x158]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG168:
       jmp      G_M000_IG117
 
G_M000_IG169:
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG484
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG168
 
G_M000_IG170:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG173
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG173
 
G_M000_IG172:
       xor      rsi, rsi
 
G_M000_IG173:
       test     rsi, rsi
       je       SHORT G_M000_IG177
 
G_M000_IG174:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x140]
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG176
 
G_M000_IG175:
       jmp      G_M000_IG147
 
G_M000_IG176:
       mov      rax, gword ptr [rbp+0x230]
       jmp      G_M000_IG146
 
G_M000_IG177:
       mov      rax, gword ptr [rbp+0x230]
       jmp      G_M000_IG146
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG203
 
G_M000_IG179:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x228]
       je       SHORT G_M000_IG182
 
G_M000_IG180:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG182
 
G_M000_IG181:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG191
 
G_M000_IG182:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG185
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG185
 
G_M000_IG184:
       xor      rsi, rsi
 
G_M000_IG185:
       test     rsi, rsi
       je       SHORT G_M000_IG190
 
G_M000_IG186:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x200], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG188
 
G_M000_IG187:
       jmp      SHORT G_M000_IG191
 
G_M000_IG188:
       mov      rax, gword ptr [rbp+0x228]
       jmp      SHORT G_M000_IG190
 
G_M000_IG189:
       mov      rax, gword ptr [rbp+0x228]
 
G_M000_IG190:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG191:
       mov      gword ptr [rbp+0x220], rax
       test     rax, rax
       je       G_M000_IG230
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG218
 
G_M000_IG193:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x218], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG196
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG196
 
G_M000_IG195:
       xor      rsi, rsi
 
G_M000_IG196:
       test     rsi, rsi
       je       G_M000_IG271
 
G_M000_IG197:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x228], rax
       mov      rcx, qword ptr [rbp-0x218]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x230], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG267
 
G_M000_IG198:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       lea      rdi, [rbp-0x240]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG199:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x240]
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
 
G_M000_IG200:
       mov      rdi, qword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x260], rax
       mov      qword ptr [rbp-0x258], rdx
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       mov      rdx, gword ptr [rbp-0x260]
       mov      rcx, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x270], rax
       mov      qword ptr [rbp-0x268], rdx
       mov      rdi, gword ptr [rbp-0x270]
       mov      rsi, qword ptr [rbp-0x268]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG201:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG229
 
G_M000_IG202:
       jmp      G_M000_IG231
 
G_M000_IG203:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG190
 
G_M000_IG204:
       mov      rsi, rbx
       test     rsi, rsi
       je       G_M000_IG190
 
G_M000_IG205:
       mov      rsi, qword ptr [rsi]
       jmp      G_M000_IG190
 
G_M000_IG206:
       mov      rax, qword ptr [rbp-0x1A8]
       mov      qword ptr [rbp-0x1F0], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG209
 
G_M000_IG207:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x1F0]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG208:
       jmp      G_M000_IG157
 
G_M000_IG209:
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG484
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG208
 
G_M000_IG210:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG213
 
G_M000_IG211:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG213
 
G_M000_IG212:
       xor      rsi, rsi
 
G_M000_IG213:
       test     rsi, rsi
       je       SHORT G_M000_IG217
 
G_M000_IG214:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG216
 
G_M000_IG215:
       jmp      G_M000_IG191
 
G_M000_IG216:
       mov      rax, gword ptr [rbp+0x228]
       jmp      G_M000_IG190
 
G_M000_IG217:
       mov      rax, gword ptr [rbp+0x228]
       jmp      G_M000_IG190
 
G_M000_IG218:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG261
 
G_M000_IG219:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       test     r15, r15
       je       SHORT G_M000_IG222
 
G_M000_IG220:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG222
 
G_M000_IG221:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG231
 
G_M000_IG222:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG225
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG225
 
G_M000_IG224:
       xor      rsi, rsi
 
G_M000_IG225:
       test     rsi, rsi
       je       G_M000_IG260
 
G_M000_IG226:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x280], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG228
 
G_M000_IG227:
       jmp      SHORT G_M000_IG231
 
G_M000_IG228:
       mov      rax, gword ptr [rbp+0x220]
       jmp      SHORT G_M000_IG230
 
G_M000_IG229:
       mov      rax, gword ptr [rbp+0x220]
 
G_M000_IG230:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG231:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       jne      G_M000_IG279
 
G_M000_IG232:
       test     r13, r13
       je       G_M000_IG247
 
G_M000_IG233:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       je       G_M000_IG285
 
G_M000_IG234:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG244
 
G_M000_IG235:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG238
 
G_M000_IG236:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG238
 
G_M000_IG237:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG248
 
G_M000_IG238:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG241
 
G_M000_IG239:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG241
 
G_M000_IG240:
       xor      rsi, rsi
 
G_M000_IG241:
       test     rsi, rsi
       je       SHORT G_M000_IG247
 
G_M000_IG242:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x308], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x308]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG247
 
G_M000_IG243:
       jmp      SHORT G_M000_IG248
 
G_M000_IG244:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG247
 
G_M000_IG245:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG247
 
G_M000_IG246:
       mov      rsi, qword ptr [rsi]
 
G_M000_IG247:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG248:
       mov      gword ptr [rbp+0x210], rax
       test     rax, rax
       je       G_M000_IG317
 
G_M000_IG249:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG305
 
G_M000_IG250:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x320], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG253
 
G_M000_IG251:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG253
 
G_M000_IG252:
       xor      rsi, rsi
 
G_M000_IG253:
       test     rsi, rsi
       je       G_M000_IG341
 
G_M000_IG254:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x330], rax
       mov      rcx, qword ptr [rbp-0x320]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x338], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG337
 
G_M000_IG255:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x348], xmm0
       lea      rdi, [rbp-0x348]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG256:
       vmovdqu  xmm0, xmmword ptr [rbp-0x348]
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
 
G_M000_IG257:
       mov      rdi, qword ptr [rbp-0x330]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x368], rax
       mov      qword ptr [rbp-0x360], rdx
       mov      rdi, gword ptr [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x350]
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, qword ptr [rbp-0x360]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x378], rax
       mov      qword ptr [rbp-0x370], rdx
       mov      rdi, gword ptr [rbp-0x378]
       mov      rsi, qword ptr [rbp-0x370]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG258:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG316
 
G_M000_IG259:
       jmp      G_M000_IG318
 
G_M000_IG260:
       mov      rax, gword ptr [rbp+0x220]
       jmp      G_M000_IG230
 
G_M000_IG261:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG230
 
G_M000_IG262:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG265
 
G_M000_IG263:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG265
 
G_M000_IG264:
       xor      rsi, rsi
 
G_M000_IG265:
       test     rsi, rsi
       je       G_M000_IG230
 
G_M000_IG266:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG231
 
G_M000_IG267:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG270
 
G_M000_IG268:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x230]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG269:
       jmp      G_M000_IG201
 
G_M000_IG270:
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG484
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG269
 
G_M000_IG271:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG274
 
G_M000_IG272:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG274
 
G_M000_IG273:
       xor      rsi, rsi
 
G_M000_IG274:
       test     rsi, rsi
       je       SHORT G_M000_IG278
 
G_M000_IG275:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x220], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG277
 
G_M000_IG276:
       jmp      G_M000_IG231
 
G_M000_IG277:
       mov      rax, gword ptr [rbp+0x220]
       jmp      G_M000_IG230
 
G_M000_IG278:
       mov      rax, gword ptr [rbp+0x220]
       jmp      G_M000_IG230
 
G_M000_IG279:
       mov      rcx, qword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG232
 
G_M000_IG280:
       test     rbx, rbx
       je       G_M000_IG232
 
G_M000_IG281:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG232
 
G_M000_IG282:
       test     r15, r15
       je       G_M000_IG232
 
G_M000_IG283:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG232
 
G_M000_IG284:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x40], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG452
 
G_M000_IG285:
       mov      rsi, r13
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x298], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG288
 
G_M000_IG286:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG288
 
G_M000_IG287:
       xor      rsi, rsi
 
G_M000_IG288:
       test     rsi, rsi
       je       G_M000_IG299
 
G_M000_IG289:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x2A8], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x2B0], rdx
       lea      rdx, [rbp-0x2B0]
       mov      rdi, qword ptr [rbp-0x298]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x2B0]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG295
 
G_M000_IG290:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
       lea      rdi, [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG291:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2C0]
       vmovdqu32 xmmword ptr [rbp-0x2D0], xmm0
 
G_M000_IG292:
       mov      rdi, qword ptr [rbp-0x2A8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2E0], rax
       mov      qword ptr [rbp-0x2D8], rdx
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2C8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rcx, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2F0], rax
       mov      qword ptr [rbp-0x2E8], rdx
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rsi, qword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG293:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG247
 
G_M000_IG294:
       jmp      G_M000_IG248
 
G_M000_IG295:
       mov      rax, qword ptr [rbp-0x2B0]
       mov      qword ptr [rbp-0x2F8], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG298
 
G_M000_IG296:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x2F8]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG297:
       jmp      SHORT G_M000_IG293
 
G_M000_IG298:
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG484
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rdi, rcx
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG297
 
G_M000_IG299:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG302
 
G_M000_IG300:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG302
 
G_M000_IG301:
       xor      rsi, rsi
 
G_M000_IG302:
       test     rsi, rsi
       je       G_M000_IG247
 
G_M000_IG303:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x300]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       G_M000_IG247
 
G_M000_IG304:
       jmp      G_M000_IG248
 
G_M000_IG305:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG331
 
G_M000_IG306:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       test     r15, r15
       je       SHORT G_M000_IG309
 
G_M000_IG307:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG309
 
G_M000_IG308:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG318
 
G_M000_IG309:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG312
 
G_M000_IG310:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG312
 
G_M000_IG311:
       xor      rsi, rsi
 
G_M000_IG312:
       test     rsi, rsi
       je       G_M000_IG330
 
G_M000_IG313:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x388], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x388]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG315
 
G_M000_IG314:
       jmp      SHORT G_M000_IG318
 
G_M000_IG315:
       mov      rax, gword ptr [rbp+0x210]
       jmp      SHORT G_M000_IG317
 
G_M000_IG316:
       mov      rax, gword ptr [rbp+0x210]
 
G_M000_IG317:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG318:
       mov      gword ptr [rbp+0x208], rax
       test     rax, rax
       je       G_M000_IG361
 
G_M000_IG319:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG349
 
G_M000_IG320:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x3A0], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG323
 
G_M000_IG321:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG323
 
G_M000_IG322:
       xor      rsi, rsi
 
G_M000_IG323:
       test     rsi, rsi
       je       G_M000_IG382
 
G_M000_IG324:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x3B0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x3B8], rdx
       lea      rdx, [rbp-0x3B8]
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x3B8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG378
 
G_M000_IG325:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3C8], xmm0
       lea      rdi, [rbp-0x3C8]
       mov      rsi, qword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG326:
       vmovdqu  xmm0, xmmword ptr [rbp-0x3C8]
       vmovdqu  xmmword ptr [rbp-0x3D8], xmm0
 
G_M000_IG327:
       mov      rdi, qword ptr [rbp-0x3B0]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3E8], rax
       mov      qword ptr [rbp-0x3E0], rdx
       mov      rdi, gword ptr [rbp-0x3D8]
       mov      rsi, qword ptr [rbp-0x3D0]
       mov      rdx, gword ptr [rbp-0x3E8]
       mov      rcx, qword ptr [rbp-0x3E0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3F8], rax
       mov      qword ptr [rbp-0x3F0], rdx
       mov      rdi, gword ptr [rbp-0x3F8]
       mov      rsi, qword ptr [rbp-0x3F0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG328:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG360
 
G_M000_IG329:
       jmp      G_M000_IG362
 
G_M000_IG330:
       mov      rax, gword ptr [rbp+0x210]
       jmp      G_M000_IG317
 
G_M000_IG331:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG317
 
G_M000_IG332:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG335
 
G_M000_IG333:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG335
 
G_M000_IG334:
       xor      rsi, rsi
 
G_M000_IG335:
       test     rsi, rsi
       je       G_M000_IG317
 
G_M000_IG336:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG318
 
G_M000_IG337:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG340
 
G_M000_IG338:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x338]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG339:
       jmp      G_M000_IG258
 
G_M000_IG340:
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG484
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG339
 
G_M000_IG341:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG344
 
G_M000_IG342:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG344
 
G_M000_IG343:
       xor      rsi, rsi
 
G_M000_IG344:
       test     rsi, rsi
       je       SHORT G_M000_IG348
 
G_M000_IG345:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x328], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x320]
       vmovsd   qword ptr [rbp-0x380], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x328]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG347
 
G_M000_IG346:
       jmp      G_M000_IG318
 
G_M000_IG347:
       mov      rax, gword ptr [rbp+0x210]
       jmp      G_M000_IG317
 
G_M000_IG348:
       mov      rax, gword ptr [rbp+0x210]
       jmp      G_M000_IG317
 
G_M000_IG349:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG375
 
G_M000_IG350:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG353
 
G_M000_IG351:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG353
 
G_M000_IG352:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x390]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG362
 
G_M000_IG353:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG356
 
G_M000_IG354:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG356
 
G_M000_IG355:
       xor      rsi, rsi
 
G_M000_IG356:
       test     rsi, rsi
       je       G_M000_IG374
 
G_M000_IG357:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x410], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x410]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG359
 
G_M000_IG358:
       jmp      SHORT G_M000_IG362
 
G_M000_IG359:
       mov      rax, gword ptr [rbp+0x208]
       jmp      SHORT G_M000_IG361
 
G_M000_IG360:
       mov      rax, gword ptr [rbp+0x208]
 
G_M000_IG361:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG362:
       mov      gword ptr [rbp+0x200], rax
       test     rax, rax
       je       G_M000_IG402
 
G_M000_IG363:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG390
 
G_M000_IG364:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x428], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG367
 
G_M000_IG365:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG367
 
G_M000_IG366:
       xor      rsi, rsi
 
G_M000_IG367:
       test     rsi, rsi
       je       G_M000_IG427
 
G_M000_IG368:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x438], rax
       mov      rcx, qword ptr [rbp-0x428]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x440], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG423
 
G_M000_IG369:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x450], xmm0
       lea      rdi, [rbp-0x450]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG370:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x450]
       vmovdqu32 xmmword ptr [rbp-0x460], xmm0
 
G_M000_IG371:
       mov      rdi, qword ptr [rbp-0x438]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x470], rax
       mov      qword ptr [rbp-0x468], rdx
       mov      rdi, gword ptr [rbp-0x460]
       mov      rsi, qword ptr [rbp-0x458]
       mov      rdx, gword ptr [rbp-0x470]
       mov      rcx, qword ptr [rbp-0x468]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x480], rax
       mov      qword ptr [rbp-0x478], rdx
       mov      rdi, gword ptr [rbp-0x480]
       mov      rsi, qword ptr [rbp-0x478]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG372:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG401
 
G_M000_IG373:
       jmp      G_M000_IG403
 
G_M000_IG374:
       mov      rax, gword ptr [rbp+0x208]
       jmp      G_M000_IG361
 
G_M000_IG375:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG361
 
G_M000_IG376:
       mov      rsi, rbx
       test     rsi, rsi
       je       G_M000_IG361
 
G_M000_IG377:
       mov      rsi, qword ptr [rsi]
       jmp      G_M000_IG361
 
G_M000_IG378:
       mov      rax, qword ptr [rbp-0x3B8]
       mov      qword ptr [rbp-0x400], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG381
 
G_M000_IG379:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x400]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG380:
       jmp      G_M000_IG328
 
G_M000_IG381:
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG484
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rsi, rcx
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG380
 
G_M000_IG382:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG385
 
G_M000_IG383:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG385
 
G_M000_IG384:
       xor      rsi, rsi
 
G_M000_IG385:
       test     rsi, rsi
       je       SHORT G_M000_IG389
 
G_M000_IG386:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x3A8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x3A0]
       vmovsd   qword ptr [rbp-0x408], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x408]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x3A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG388
 
G_M000_IG387:
       jmp      G_M000_IG362
 
G_M000_IG388:
       mov      rax, gword ptr [rbp+0x208]
       jmp      G_M000_IG361
 
G_M000_IG389:
       mov      rax, gword ptr [rbp+0x208]
       jmp      G_M000_IG361
 
G_M000_IG390:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG417
 
G_M000_IG391:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm0
       test     r15, r15
       je       SHORT G_M000_IG394
 
G_M000_IG392:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG394
 
G_M000_IG393:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x420], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x418]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x420]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG403
 
G_M000_IG394:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG397
 
G_M000_IG395:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG397
 
G_M000_IG396:
       xor      rsi, rsi
 
G_M000_IG397:
       test     rsi, rsi
       je       G_M000_IG416
 
G_M000_IG398:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x490], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x490]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x418]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG400
 
G_M000_IG399:
       jmp      SHORT G_M000_IG403
 
G_M000_IG400:
       mov      rax, gword ptr [rbp+0x200]
       jmp      SHORT G_M000_IG402
 
G_M000_IG401:
       mov      rax, gword ptr [rbp+0x200]
 
G_M000_IG402:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG403:
       mov      gword ptr [rbp+0x1F8], rax
       xor      rsi, rsi
       mov      gword ptr [rbp-0x4A8], rsi
 
G_M000_IG404:
       mov      gword ptr [rbp-0x4A0], rsi
       test     rax, rax
       je       G_M000_IG449
 
G_M000_IG405:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG435
 
G_M000_IG406:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x4B0], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG409
 
G_M000_IG407:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG409
 
G_M000_IG408:
       xor      rsi, rsi
 
G_M000_IG409:
       test     rsi, rsi
       je       G_M000_IG470
 
G_M000_IG410:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x4C0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x4C8], rdx
       lea      rdx, [rbp-0x4C8]
       mov      rdi, qword ptr [rbp-0x4B0]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x4C8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG466
 
G_M000_IG411:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4D8], xmm0
       lea      rdi, [rbp-0x4D8]
       mov      rsi, qword ptr [rbp-0x4B0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG412:
       vmovdqu  xmm0, xmmword ptr [rbp-0x4D8]
       vmovdqu  xmmword ptr [rbp-0x4E8], xmm0
 
G_M000_IG413:
       mov      rdi, qword ptr [rbp-0x4C0]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x4F8], rax
       mov      qword ptr [rbp-0x4F0], rdx
       mov      rdi, gword ptr [rbp-0x4E8]
       mov      rsi, qword ptr [rbp-0x4E0]
       mov      rdx, gword ptr [rbp-0x4F8]
       mov      rcx, qword ptr [rbp-0x4F0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x508], rax
       mov      qword ptr [rbp-0x500], rdx
       mov      rdi, gword ptr [rbp-0x508]
       mov      rsi, qword ptr [rbp-0x500]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG414:
       mov      rdx, rax
       mov      esi, 1
       test     esi, esi
       mov      rax, gword ptr [rbp+0x1F8]
       je       G_M000_IG449
 
G_M000_IG415:
       jmp      G_M000_IG450
 
G_M000_IG416:
       mov      rax, gword ptr [rbp+0x200]
       jmp      G_M000_IG402
 
G_M000_IG417:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG402
 
G_M000_IG418:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG421
 
G_M000_IG419:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG421
 
G_M000_IG420:
       xor      rsi, rsi
 
G_M000_IG421:
       test     rsi, rsi
       je       G_M000_IG402
 
G_M000_IG422:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG403
 
G_M000_IG423:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG426
 
G_M000_IG424:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x440]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG425:
       jmp      G_M000_IG372
 
G_M000_IG426:
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG484
       test     rax, rax
       jl       G_M000_IG485
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG425
 
G_M000_IG427:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG430
 
G_M000_IG428:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG430
 
G_M000_IG429:
       xor      rsi, rsi
 
G_M000_IG430:
       test     rsi, rsi
       je       SHORT G_M000_IG434
 
G_M000_IG431:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x430], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x428]
       vmovsd   qword ptr [rbp-0x488], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x488]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x430]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG433
 
G_M000_IG432:
       jmp      G_M000_IG403
 
G_M000_IG433:
       mov      rax, gword ptr [rbp+0x200]
       jmp      G_M000_IG402
 
G_M000_IG434:
       mov      rax, gword ptr [rbp+0x200]
       jmp      G_M000_IG402
 
G_M000_IG435:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG446
 
G_M000_IG436:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x498], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG439
 
G_M000_IG437:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG439
 
G_M000_IG438:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4A8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x1F8]
       je       G_M000_IG449
       mov      rdx, gword ptr [rbp-0x4A8]
       jmp      G_M000_IG450
 
G_M000_IG439:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG442
 
G_M000_IG440:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG442
 
G_M000_IG441:
       xor      rsi, rsi
 
G_M000_IG442:
       test     rsi, rsi
       je       SHORT G_M000_IG445
 
G_M000_IG443:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax]
       lea      rsi, [rbp-0x4A0]
       vmovsd   xmm0, qword ptr [rbp-0x498]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x1F8]
       je       SHORT G_M000_IG449
 
G_M000_IG444:
       mov      rdx, gword ptr [rbp-0x4A0]
       jmp      SHORT G_M000_IG450
 
G_M000_IG445:
       mov      rax, gword ptr [rbp+0x1F8]
       jmp      SHORT G_M000_IG449
 
G_M000_IG446:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG449
 
G_M000_IG447:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG449
 
G_M000_IG448:
       mov      rsi, qword ptr [rsi]
 
G_M000_IG449:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG450:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x4A8], rcx
 
G_M000_IG451:
       mov      gword ptr [rbp-0x4A0], rcx
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG452:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG453:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x540], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x540]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x548], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x550], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x540]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x548]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x550]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x548]
       add      r12, qword ptr [rbp+0x270]
       jo       G_M000_IG485
       mov      edx, dword ptr [rbp-0x52C]
       mov      edi, edx
       mov      rsi, qword ptr [rbp+0x278]
       cmp      r12, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       mov      qword ptr [rbp+0x278], rsi
       cmp      r12, rsi
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG477
 
G_M000_IG454:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG457
 
G_M000_IG455:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      r8, rax
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG484
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG457
 
G_M000_IG456:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG484
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 10
 
G_M000_IG457:
       test     r14, r14
       je       G_M000_IG03
 
G_M000_IG458:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG03
 
G_M000_IG459:
       test     rbx, rbx
       je       G_M000_IG03
 
G_M000_IG460:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG03
 
G_M000_IG461:
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG462:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG463:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG464:
       mov      rdi, qword ptr [r13]
       mov      r8, 0xD1FFAB1E
       cmp      rdi, r8
       jne      G_M000_IG03
 
G_M000_IG465:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp+0x1A8], xmm1
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x198], xmm2
       vmovsd   xmm3, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x188], xmm3
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x158], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x158]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x218], rsi
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x188]
       vmovsd   xmm2, qword ptr [rbp+0x198]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x128]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      rax, rsi
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG453
 
G_M000_IG466:
       mov      rax, qword ptr [rbp-0x4C8]
       mov      qword ptr [rbp-0x510], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG469
 
G_M000_IG467:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x510]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG468:
       jmp      G_M000_IG414
 
G_M000_IG469:
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG484
       test     rcx, rcx
       jl       G_M000_IG485
       mov      rsi, rcx
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG468
 
G_M000_IG470:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG473
 
G_M000_IG471:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG473
 
G_M000_IG472:
       xor      rsi, rsi
 
G_M000_IG473:
       test     rsi, rsi
       je       SHORT G_M000_IG476
 
G_M000_IG474:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x4B8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x4B0]
       vmovsd   qword ptr [rbp-0x518], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x518]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x4B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      esi, 1
       test     esi, esi
       mov      rax, gword ptr [rbp+0x1F8]
       je       G_M000_IG449
 
G_M000_IG475:
       jmp      G_M000_IG450
 
G_M000_IG476:
       mov      rax, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG449
 
G_M000_IG477:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG480
 
G_M000_IG478:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG484
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG480
 
G_M000_IG479:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG484
       mov      dword ptr [rcx+4*rdx+0x10], 11
 
G_M000_IG480:
       mov      rbx, qword ptr [rbp+0x2A8]
       test     rbx, rbx
       jle      SHORT G_M000_IG483
 
G_M000_IG481:
       movsxd   rcx, edi
       cmp      rbx, rcx
       jg       SHORT G_M000_IG483
 
G_M000_IG482:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG483:
       add      rsp, 0x858
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG484:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG485:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 10650

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2d3
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 53 inlinees with PGO data; 7 single block inlinees; 77 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x570
       mov      qword ptr [rsp+0x878], r15
       mov      qword ptr [rsp+0x870], r14
       mov      qword ptr [rsp+0x868], r13
       mov      qword ptr [rsp+0x860], r12
       mov      qword ptr [rsp+0x858], rbx
       lea      rbp, [rsp+0x570]
       xor      eax, eax
       mov      qword ptr [rbp-0x508], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -0x4B0
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r14, gword ptr [rbp+0x2D8]
       mov      rbx, gword ptr [rbp+0x2D0]
       mov      r15, gword ptr [rbp+0x2C8]
       mov      r13, gword ptr [rbp+0x2C0]
       mov      r12, qword ptr [rbp+0x280]
       mov      r9, qword ptr [rbp+0x270]
       mov      rax, gword ptr [rbp+0x108]
       mov      rdx, gword ptr [rbp+0xE8]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x270], r9
       test     r9, r9
       setg     r11b
       movzx    r11, r11b
       mov      dword ptr [rbp-0x52C], r11d
 
G_M000_IG03:
       test     rdx, rdx
       je       G_M000_IG436
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0xE8], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG436
 
G_M000_IG05:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       mov      gword ptr [rbp+0x108], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       mov      rax, gword ptr [rbp+0x108]
 
G_M000_IG07:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0x100], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r8, gword ptr [rbp+0xF8]
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       mov      gword ptr [rbp+0xF8], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       mov      r8, gword ptr [rbp+0xF8]
 
G_M000_IG09:
       vmovsd   xmm1, qword ptr [r8+0x08]
       mov      rdx, gword ptr [rbp+0xE8]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x100]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG205
 
G_M000_IG10:
       mov      rsi, r14
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x40], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       xor      rsi, rsi
 
G_M000_IG13:
       test     rsi, rsi
       je       G_M000_IG22
 
G_M000_IG14:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x50], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x58], rdx
       lea      rdx, [rbp-0x58]
       mov      rdi, qword ptr [rbp-0x40]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x58]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG18
 
G_M000_IG15:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, qword ptr [rbp-0x40]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x558], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x51C], ecx
       mov      rdi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x558]
       mov      esi, dword ptr [rbp-0x51C]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG16:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG451
 
G_M000_IG17:
       jmp      G_M000_IG452
 
G_M000_IG18:
       mov      rax, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x70], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x70]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG20:
       jmp      SHORT G_M000_IG16
 
G_M000_IG21:
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG487
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG20
 
G_M000_IG22:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       xor      rsi, rsi
 
G_M000_IG25:
       test     rsi, rsi
       je       G_M000_IG451
 
G_M000_IG26:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       G_M000_IG451
 
G_M000_IG27:
       jmp      G_M000_IG452
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG52
 
G_M000_IG29:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     r15, r15
       je       SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG32
 
G_M000_IG31:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG41
 
G_M000_IG32:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG35
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG35
 
G_M000_IG34:
       xor      rsi, rsi
 
G_M000_IG35:
       test     rsi, rsi
       je       G_M000_IG51
 
G_M000_IG36:
       mov      rsi, r15
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG38
 
G_M000_IG37:
       jmp      SHORT G_M000_IG41
 
G_M000_IG38:
       mov      rax, gword ptr [rbp+0x240]
       jmp      SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rax, gword ptr [rbp+0x240]
 
G_M000_IG40:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG41:
       mov      gword ptr [rbp+0x238], rax
       test     rax, rax
       je       G_M000_IG82
 
G_M000_IG42:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG70
 
G_M000_IG43:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE8], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       xor      rsi, rsi
 
G_M000_IG46:
       test     rsi, rsi
       je       G_M000_IG100
 
G_M000_IG47:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xF8], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x100], rdx
       lea      rdx, [rbp-0x100]
       mov      rdi, qword ptr [rbp-0xE8]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x100]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG96
 
G_M000_IG48:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x568], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x524], ecx
       mov      rdi, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x568]
       mov      esi, dword ptr [rbp-0x524]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG49:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG81
 
G_M000_IG50:
       jmp      G_M000_IG83
 
G_M000_IG51:
       mov      rax, gword ptr [rbp+0x240]
       jmp      G_M000_IG40
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG40
 
G_M000_IG53:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG56
 
G_M000_IG54:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG56
 
G_M000_IG55:
       xor      rsi, rsi
 
G_M000_IG56:
       test     rsi, rsi
       je       G_M000_IG40
 
G_M000_IG57:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG41
 
G_M000_IG58:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG61
 
G_M000_IG59:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG60:
       jmp      G_M000_IG460
 
G_M000_IG61:
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG487
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG60
 
G_M000_IG62:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG65
 
G_M000_IG64:
       xor      rsi, rsi
 
G_M000_IG65:
       test     rsi, rsi
       je       SHORT G_M000_IG69
 
G_M000_IG66:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG68
 
G_M000_IG67:
       jmp      G_M000_IG41
 
G_M000_IG68:
       mov      rax, gword ptr [rbp+0x240]
       jmp      G_M000_IG40
 
G_M000_IG69:
       mov      rax, gword ptr [rbp+0x240]
       jmp      G_M000_IG40
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG93
 
G_M000_IG71:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm0
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x238]
       je       SHORT G_M000_IG74
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG74
 
G_M000_IG73:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG83
 
G_M000_IG74:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG77
 
G_M000_IG76:
       xor      rsi, rsi
 
G_M000_IG77:
       test     rsi, rsi
       je       SHORT G_M000_IG82
 
G_M000_IG78:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x128]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG80
 
G_M000_IG79:
       jmp      SHORT G_M000_IG83
 
G_M000_IG80:
       mov      rax, gword ptr [rbp+0x238]
       jmp      SHORT G_M000_IG82
 
G_M000_IG81:
       mov      rax, gword ptr [rbp+0x238]
 
G_M000_IG82:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG83:
       mov      gword ptr [rbp+0x230], rax
       test     rax, rax
       je       G_M000_IG120
 
G_M000_IG84:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG108
 
G_M000_IG85:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x140], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG88
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG88
 
G_M000_IG87:
       xor      rsi, rsi
 
G_M000_IG88:
       test     rsi, rsi
       je       G_M000_IG144
 
G_M000_IG89:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x150], rax
       mov      rcx, qword ptr [rbp-0x140]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x158], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG140
 
G_M000_IG90:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x570], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x528], ecx
       mov      rdi, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x570]
       mov      esi, dword ptr [rbp-0x528]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG91:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG119
 
G_M000_IG92:
       jmp      G_M000_IG121
 
G_M000_IG93:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG82
 
G_M000_IG94:
       mov      rsi, rbx
       test     rsi, rsi
       je       G_M000_IG82
 
G_M000_IG95:
       mov      rsi, qword ptr [rsi]
       jmp      G_M000_IG82
 
G_M000_IG96:
       mov      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x118], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG99
 
G_M000_IG97:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG98:
       jmp      G_M000_IG49
 
G_M000_IG99:
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG487
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG98
 
G_M000_IG100:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG103
 
G_M000_IG102:
       xor      rsi, rsi
 
G_M000_IG103:
       test     rsi, rsi
       je       SHORT G_M000_IG107
 
G_M000_IG104:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rbp-0x120], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       jmp      G_M000_IG83
 
G_M000_IG106:
       mov      rax, gword ptr [rbp+0x238]
       jmp      G_M000_IG82
 
G_M000_IG107:
       mov      rax, gword ptr [rbp+0x238]
       jmp      G_M000_IG82
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG134
 
G_M000_IG109:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm0
       test     r15, r15
       je       SHORT G_M000_IG112
 
G_M000_IG110:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG112
 
G_M000_IG111:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x130]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x138]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG121
 
G_M000_IG112:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG115
 
G_M000_IG113:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG115
 
G_M000_IG114:
       xor      rsi, rsi
 
G_M000_IG115:
       test     rsi, rsi
       je       G_M000_IG133
 
G_M000_IG116:
       mov      rsi, r15
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG118
 
G_M000_IG117:
       jmp      SHORT G_M000_IG121
 
G_M000_IG118:
       mov      rax, gword ptr [rbp+0x230]
       jmp      SHORT G_M000_IG120
 
G_M000_IG119:
       mov      rax, gword ptr [rbp+0x230]
 
G_M000_IG120:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG121:
       mov      gword ptr [rbp+0x228], rax
       test     rax, rax
       je       G_M000_IG164
 
G_M000_IG122:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG152
 
G_M000_IG123:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x190], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG126
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG126
 
G_M000_IG125:
       xor      rsi, rsi
 
G_M000_IG126:
       test     rsi, rsi
       je       G_M000_IG184
 
G_M000_IG127:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x1A0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x1A8], rdx
       lea      rdx, [rbp-0x1A8]
       mov      rdi, qword ptr [rbp-0x190]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x1A8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG180
 
G_M000_IG128:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       mov      rsi, qword ptr [rbp-0x190]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG129:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1B8]
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
 
G_M000_IG130:
       mov      rdi, qword ptr [rbp-0x1A0]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D8], rax
       mov      qword ptr [rbp-0x1D0], rdx
       mov      rdi, gword ptr [rbp-0x1C8]
       mov      rsi, qword ptr [rbp-0x1C0]
       mov      rdx, gword ptr [rbp-0x1D8]
       mov      rcx, qword ptr [rbp-0x1D0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1E8], rax
       mov      qword ptr [rbp-0x1E0], rdx
       mov      rdi, gword ptr [rbp-0x1E8]
       mov      rsi, qword ptr [rbp-0x1E0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG131:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG163
 
G_M000_IG132:
       jmp      G_M000_IG165
 
G_M000_IG133:
       mov      rax, gword ptr [rbp+0x230]
       jmp      G_M000_IG120
 
G_M000_IG134:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG120
 
G_M000_IG135:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG138
 
G_M000_IG136:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG138
 
G_M000_IG137:
       xor      rsi, rsi
 
G_M000_IG138:
       test     rsi, rsi
       je       G_M000_IG120
 
G_M000_IG139:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG121
 
G_M000_IG140:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG143
 
G_M000_IG141:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x158]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG142:
       jmp      G_M000_IG91
 
G_M000_IG143:
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG487
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG142
 
G_M000_IG144:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG147
 
G_M000_IG145:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG147
 
G_M000_IG146:
       xor      rsi, rsi
 
G_M000_IG147:
       test     rsi, rsi
       je       SHORT G_M000_IG151
 
G_M000_IG148:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x140]
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG150
 
G_M000_IG149:
       jmp      G_M000_IG121
 
G_M000_IG150:
       mov      rax, gword ptr [rbp+0x230]
       jmp      G_M000_IG120
 
G_M000_IG151:
       mov      rax, gword ptr [rbp+0x230]
       jmp      G_M000_IG120
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG177
 
G_M000_IG153:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x228]
       je       SHORT G_M000_IG156
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG156
 
G_M000_IG155:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG165
 
G_M000_IG156:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG159
 
G_M000_IG157:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG159
 
G_M000_IG158:
       xor      rsi, rsi
 
G_M000_IG159:
       test     rsi, rsi
       je       SHORT G_M000_IG164
 
G_M000_IG160:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x200], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG162
 
G_M000_IG161:
       jmp      SHORT G_M000_IG165
 
G_M000_IG162:
       mov      rax, gword ptr [rbp+0x228]
       jmp      SHORT G_M000_IG164
 
G_M000_IG163:
       mov      rax, gword ptr [rbp+0x228]
 
G_M000_IG164:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG165:
       mov      gword ptr [rbp+0x220], rax
       test     rax, rax
       je       G_M000_IG204
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG192
 
G_M000_IG167:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x218], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG170
 
G_M000_IG168:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG170
 
G_M000_IG169:
       xor      rsi, rsi
 
G_M000_IG170:
       test     rsi, rsi
       je       G_M000_IG245
 
G_M000_IG171:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x228], rax
       mov      rcx, qword ptr [rbp-0x218]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x230], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG241
 
G_M000_IG172:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       lea      rdi, [rbp-0x240]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG173:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x240]
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
 
G_M000_IG174:
       mov      rdi, qword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x260], rax
       mov      qword ptr [rbp-0x258], rdx
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       mov      rdx, gword ptr [rbp-0x260]
       mov      rcx, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x270], rax
       mov      qword ptr [rbp-0x268], rdx
       mov      rdi, gword ptr [rbp-0x270]
       mov      rsi, qword ptr [rbp-0x268]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG175:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG203
 
G_M000_IG176:
       jmp      G_M000_IG205
 
G_M000_IG177:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG164
 
G_M000_IG178:
       mov      rsi, rbx
       test     rsi, rsi
       je       G_M000_IG164
 
G_M000_IG179:
       mov      rsi, qword ptr [rsi]
       jmp      G_M000_IG164
 
G_M000_IG180:
       mov      rax, qword ptr [rbp-0x1A8]
       mov      qword ptr [rbp-0x1F0], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG183
 
G_M000_IG181:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x1F0]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG182:
       jmp      G_M000_IG131
 
G_M000_IG183:
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG487
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG182
 
G_M000_IG184:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG187
 
G_M000_IG185:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG187
 
G_M000_IG186:
       xor      rsi, rsi
 
G_M000_IG187:
       test     rsi, rsi
       je       SHORT G_M000_IG191
 
G_M000_IG188:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG190
 
G_M000_IG189:
       jmp      G_M000_IG165
 
G_M000_IG190:
       mov      rax, gword ptr [rbp+0x228]
       jmp      G_M000_IG164
 
G_M000_IG191:
       mov      rax, gword ptr [rbp+0x228]
       jmp      G_M000_IG164
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG235
 
G_M000_IG193:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       test     r15, r15
       je       SHORT G_M000_IG196
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG196
 
G_M000_IG195:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG205
 
G_M000_IG196:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG199
 
G_M000_IG197:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG199
 
G_M000_IG198:
       xor      rsi, rsi
 
G_M000_IG199:
       test     rsi, rsi
       je       G_M000_IG234
 
G_M000_IG200:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x280], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG202
 
G_M000_IG201:
       jmp      SHORT G_M000_IG205
 
G_M000_IG202:
       mov      rax, gword ptr [rbp+0x220]
       jmp      SHORT G_M000_IG204
 
G_M000_IG203:
       mov      rax, gword ptr [rbp+0x220]
 
G_M000_IG204:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG205:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, r13
       test     rdi, rdi
       jne      G_M000_IG253
 
G_M000_IG206:
       test     r13, r13
       je       G_M000_IG221
 
G_M000_IG207:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       je       G_M000_IG259
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG218
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG212
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG212
 
G_M000_IG211:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG222
 
G_M000_IG212:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG215
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG215
 
G_M000_IG214:
       xor      rsi, rsi
 
G_M000_IG215:
       test     rsi, rsi
       je       SHORT G_M000_IG221
 
G_M000_IG216:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x308], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x308]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG221
 
G_M000_IG217:
       jmp      SHORT G_M000_IG222
 
G_M000_IG218:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG221
 
G_M000_IG219:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG221
 
G_M000_IG220:
       mov      rsi, qword ptr [rsi]
 
G_M000_IG221:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG222:
       mov      gword ptr [rbp+0x210], rax
       test     rax, rax
       je       G_M000_IG291
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG279
 
G_M000_IG224:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x320], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG227
 
G_M000_IG225:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG227
 
G_M000_IG226:
       xor      rsi, rsi
 
G_M000_IG227:
       test     rsi, rsi
       je       G_M000_IG315
 
G_M000_IG228:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x330], rax
       mov      rcx, qword ptr [rbp-0x320]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x338], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG311
 
G_M000_IG229:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x348], xmm0
       lea      rdi, [rbp-0x348]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG230:
       vmovdqu  xmm0, xmmword ptr [rbp-0x348]
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
 
G_M000_IG231:
       mov      rdi, qword ptr [rbp-0x330]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x368], rax
       mov      qword ptr [rbp-0x360], rdx
       mov      rdi, gword ptr [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x350]
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, qword ptr [rbp-0x360]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x378], rax
       mov      qword ptr [rbp-0x370], rdx
       mov      rdi, gword ptr [rbp-0x378]
       mov      rsi, qword ptr [rbp-0x370]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG232:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG290
 
G_M000_IG233:
       jmp      G_M000_IG292
 
G_M000_IG234:
       mov      rax, gword ptr [rbp+0x220]
       jmp      G_M000_IG204
 
G_M000_IG235:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG204
 
G_M000_IG236:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG239
 
G_M000_IG237:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG239
 
G_M000_IG238:
       xor      rsi, rsi
 
G_M000_IG239:
       test     rsi, rsi
       je       G_M000_IG204
 
G_M000_IG240:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG205
 
G_M000_IG241:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG244
 
G_M000_IG242:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x230]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG243:
       jmp      G_M000_IG175
 
G_M000_IG244:
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG487
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG243
 
G_M000_IG245:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG248
 
G_M000_IG246:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG248
 
G_M000_IG247:
       xor      rsi, rsi
 
G_M000_IG248:
       test     rsi, rsi
       je       SHORT G_M000_IG252
 
G_M000_IG249:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x220], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG251
 
G_M000_IG250:
       jmp      G_M000_IG205
 
G_M000_IG251:
       mov      rax, gword ptr [rbp+0x220]
       jmp      G_M000_IG204
 
G_M000_IG252:
       mov      rax, gword ptr [rbp+0x220]
       jmp      G_M000_IG204
 
G_M000_IG253:
       mov      rcx, qword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG206
 
G_M000_IG254:
       test     rbx, rbx
       je       G_M000_IG206
 
G_M000_IG255:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG206
 
G_M000_IG256:
       test     r15, r15
       je       G_M000_IG206
 
G_M000_IG257:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG206
 
G_M000_IG258:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x40], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG426
 
G_M000_IG259:
       mov      rsi, r13
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x298], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG262
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG262
 
G_M000_IG261:
       xor      rsi, rsi
 
G_M000_IG262:
       test     rsi, rsi
       je       G_M000_IG273
 
G_M000_IG263:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x2A8], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x2B0], rdx
       lea      rdx, [rbp-0x2B0]
       mov      rdi, qword ptr [rbp-0x298]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x2B0]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG269
 
G_M000_IG264:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
       lea      rdi, [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG265:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2C0]
       vmovdqu32 xmmword ptr [rbp-0x2D0], xmm0
 
G_M000_IG266:
       mov      rdi, qword ptr [rbp-0x2A8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2E0], rax
       mov      qword ptr [rbp-0x2D8], rdx
       mov      rdi, gword ptr [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2C8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rcx, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2F0], rax
       mov      qword ptr [rbp-0x2E8], rdx
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rsi, qword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG267:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG221
 
G_M000_IG268:
       jmp      G_M000_IG222
 
G_M000_IG269:
       mov      rax, qword ptr [rbp-0x2B0]
       mov      qword ptr [rbp-0x2F8], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG272
 
G_M000_IG270:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x2F8]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG271:
       jmp      SHORT G_M000_IG267
 
G_M000_IG272:
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG487
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rdi, rcx
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG271
 
G_M000_IG273:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG276
 
G_M000_IG274:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG276
 
G_M000_IG275:
       xor      rsi, rsi
 
G_M000_IG276:
       test     rsi, rsi
       je       G_M000_IG221
 
G_M000_IG277:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x300]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       G_M000_IG221
 
G_M000_IG278:
       jmp      G_M000_IG222
 
G_M000_IG279:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG305
 
G_M000_IG280:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       test     r15, r15
       je       SHORT G_M000_IG283
 
G_M000_IG281:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG283
 
G_M000_IG282:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG292
 
G_M000_IG283:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG286
 
G_M000_IG284:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG286
 
G_M000_IG285:
       xor      rsi, rsi
 
G_M000_IG286:
       test     rsi, rsi
       je       G_M000_IG304
 
G_M000_IG287:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x388], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x388]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x310]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG289
 
G_M000_IG288:
       jmp      SHORT G_M000_IG292
 
G_M000_IG289:
       mov      rax, gword ptr [rbp+0x210]
       jmp      SHORT G_M000_IG291
 
G_M000_IG290:
       mov      rax, gword ptr [rbp+0x210]
 
G_M000_IG291:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG292:
       mov      gword ptr [rbp+0x208], rax
       test     rax, rax
       je       G_M000_IG335
 
G_M000_IG293:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG323
 
G_M000_IG294:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x3A0], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG297
 
G_M000_IG295:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG297
 
G_M000_IG296:
       xor      rsi, rsi
 
G_M000_IG297:
       test     rsi, rsi
       je       G_M000_IG356
 
G_M000_IG298:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x3B0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x3B8], rdx
       lea      rdx, [rbp-0x3B8]
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x3B8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG352
 
G_M000_IG299:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3C8], xmm0
       lea      rdi, [rbp-0x3C8]
       mov      rsi, qword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG300:
       vmovdqu  xmm0, xmmword ptr [rbp-0x3C8]
       vmovdqu  xmmword ptr [rbp-0x3D8], xmm0
 
G_M000_IG301:
       mov      rdi, qword ptr [rbp-0x3B0]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3E8], rax
       mov      qword ptr [rbp-0x3E0], rdx
       mov      rdi, gword ptr [rbp-0x3D8]
       mov      rsi, qword ptr [rbp-0x3D0]
       mov      rdx, gword ptr [rbp-0x3E8]
       mov      rcx, qword ptr [rbp-0x3E0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3F8], rax
       mov      qword ptr [rbp-0x3F0], rdx
       mov      rdi, gword ptr [rbp-0x3F8]
       mov      rsi, qword ptr [rbp-0x3F0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG302:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG334
 
G_M000_IG303:
       jmp      G_M000_IG336
 
G_M000_IG304:
       mov      rax, gword ptr [rbp+0x210]
       jmp      G_M000_IG291
 
G_M000_IG305:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG291
 
G_M000_IG306:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG309
 
G_M000_IG307:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG309
 
G_M000_IG308:
       xor      rsi, rsi
 
G_M000_IG309:
       test     rsi, rsi
       je       G_M000_IG291
 
G_M000_IG310:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG292
 
G_M000_IG311:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG314
 
G_M000_IG312:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x338]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG313:
       jmp      G_M000_IG232
 
G_M000_IG314:
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG487
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG313
 
G_M000_IG315:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG318
 
G_M000_IG316:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG318
 
G_M000_IG317:
       xor      rsi, rsi
 
G_M000_IG318:
       test     rsi, rsi
       je       SHORT G_M000_IG322
 
G_M000_IG319:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x328], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x320]
       vmovsd   qword ptr [rbp-0x380], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x328]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG321
 
G_M000_IG320:
       jmp      G_M000_IG292
 
G_M000_IG321:
       mov      rax, gword ptr [rbp+0x210]
       jmp      G_M000_IG291
 
G_M000_IG322:
       mov      rax, gword ptr [rbp+0x210]
       jmp      G_M000_IG291
 
G_M000_IG323:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG349
 
G_M000_IG324:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG327
 
G_M000_IG325:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG327
 
G_M000_IG326:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x390]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG336
 
G_M000_IG327:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG330
 
G_M000_IG328:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG330
 
G_M000_IG329:
       xor      rsi, rsi
 
G_M000_IG330:
       test     rsi, rsi
       je       G_M000_IG348
 
G_M000_IG331:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x410], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x410]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG333
 
G_M000_IG332:
       jmp      SHORT G_M000_IG336
 
G_M000_IG333:
       mov      rax, gword ptr [rbp+0x208]
       jmp      SHORT G_M000_IG335
 
G_M000_IG334:
       mov      rax, gword ptr [rbp+0x208]
 
G_M000_IG335:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG336:
       mov      gword ptr [rbp+0x200], rax
       test     rax, rax
       je       G_M000_IG376
 
G_M000_IG337:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG364
 
G_M000_IG338:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x428], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG341
 
G_M000_IG339:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG341
 
G_M000_IG340:
       xor      rsi, rsi
 
G_M000_IG341:
       test     rsi, rsi
       je       G_M000_IG401
 
G_M000_IG342:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x438], rax
       mov      rcx, qword ptr [rbp-0x428]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x440], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG397
 
G_M000_IG343:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x450], xmm0
       lea      rdi, [rbp-0x450]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG344:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x450]
       vmovdqu32 xmmword ptr [rbp-0x460], xmm0
 
G_M000_IG345:
       mov      rdi, qword ptr [rbp-0x438]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x470], rax
       mov      qword ptr [rbp-0x468], rdx
       mov      rdi, gword ptr [rbp-0x460]
       mov      rsi, qword ptr [rbp-0x458]
       mov      rdx, gword ptr [rbp-0x470]
       mov      rcx, qword ptr [rbp-0x468]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x480], rax
       mov      qword ptr [rbp-0x478], rdx
       mov      rdi, gword ptr [rbp-0x480]
       mov      rsi, qword ptr [rbp-0x478]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG346:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG375
 
G_M000_IG347:
       jmp      G_M000_IG377
 
G_M000_IG348:
       mov      rax, gword ptr [rbp+0x208]
       jmp      G_M000_IG335
 
G_M000_IG349:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG335
 
G_M000_IG350:
       mov      rsi, rbx
       test     rsi, rsi
       je       G_M000_IG335
 
G_M000_IG351:
       mov      rsi, qword ptr [rsi]
       jmp      G_M000_IG335
 
G_M000_IG352:
       mov      rax, qword ptr [rbp-0x3B8]
       mov      qword ptr [rbp-0x400], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG355
 
G_M000_IG353:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x400]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG354:
       jmp      G_M000_IG302
 
G_M000_IG355:
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG487
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rsi, rcx
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG354
 
G_M000_IG356:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG359
 
G_M000_IG357:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG359
 
G_M000_IG358:
       xor      rsi, rsi
 
G_M000_IG359:
       test     rsi, rsi
       je       SHORT G_M000_IG363
 
G_M000_IG360:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x3A8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x3A0]
       vmovsd   qword ptr [rbp-0x408], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x408]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x3A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG362
 
G_M000_IG361:
       jmp      G_M000_IG336
 
G_M000_IG362:
       mov      rax, gword ptr [rbp+0x208]
       jmp      G_M000_IG335
 
G_M000_IG363:
       mov      rax, gword ptr [rbp+0x208]
       jmp      G_M000_IG335
 
G_M000_IG364:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG391
 
G_M000_IG365:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm0
       test     r15, r15
       je       SHORT G_M000_IG368
 
G_M000_IG366:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG368
 
G_M000_IG367:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x420], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x418]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x420]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG377
 
G_M000_IG368:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG371
 
G_M000_IG369:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG371
 
G_M000_IG370:
       xor      rsi, rsi
 
G_M000_IG371:
       test     rsi, rsi
       je       G_M000_IG390
 
G_M000_IG372:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x490], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x490]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x418]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG374
 
G_M000_IG373:
       jmp      SHORT G_M000_IG377
 
G_M000_IG374:
       mov      rax, gword ptr [rbp+0x200]
       jmp      SHORT G_M000_IG376
 
G_M000_IG375:
       mov      rax, gword ptr [rbp+0x200]
 
G_M000_IG376:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG377:
       mov      gword ptr [rbp+0x1F8], rax
       xor      rsi, rsi
       mov      gword ptr [rbp-0x4A8], rsi
 
G_M000_IG378:
       mov      gword ptr [rbp-0x4A0], rsi
       test     rax, rax
       je       G_M000_IG423
 
G_M000_IG379:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG409
 
G_M000_IG380:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rcx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x4B0], rax
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG383
 
G_M000_IG381:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG383
 
G_M000_IG382:
       xor      rsi, rsi
 
G_M000_IG383:
       test     rsi, rsi
       je       G_M000_IG466
 
G_M000_IG384:
       mov      rsi, rbx
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x4C0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x4C8], rdx
       lea      rdx, [rbp-0x4C8]
       mov      rdi, qword ptr [rbp-0x4B0]
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x4C8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG462
 
G_M000_IG385:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4D8], xmm0
       lea      rdi, [rbp-0x4D8]
       mov      rsi, qword ptr [rbp-0x4B0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG386:
       vmovdqu  xmm0, xmmword ptr [rbp-0x4D8]
       vmovdqu  xmmword ptr [rbp-0x4E8], xmm0
 
G_M000_IG387:
       mov      rdi, qword ptr [rbp-0x4C0]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x4F8], rax
       mov      qword ptr [rbp-0x4F0], rdx
       mov      rdi, gword ptr [rbp-0x4E8]
       mov      rsi, qword ptr [rbp-0x4E0]
       mov      rdx, gword ptr [rbp-0x4F8]
       mov      rcx, qword ptr [rbp-0x4F0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x508], rax
       mov      qword ptr [rbp-0x500], rdx
       mov      rdi, gword ptr [rbp-0x508]
       mov      rsi, qword ptr [rbp-0x500]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG388:
       mov      rdx, rax
       mov      esi, 1
       test     esi, esi
       mov      rax, gword ptr [rbp+0x1F8]
       je       G_M000_IG423
 
G_M000_IG389:
       jmp      G_M000_IG424
 
G_M000_IG390:
       mov      rax, gword ptr [rbp+0x200]
       jmp      G_M000_IG376
 
G_M000_IG391:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG376
 
G_M000_IG392:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG395
 
G_M000_IG393:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG395
 
G_M000_IG394:
       xor      rsi, rsi
 
G_M000_IG395:
       test     rsi, rsi
       je       G_M000_IG376
 
G_M000_IG396:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG377
 
G_M000_IG397:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG400
 
G_M000_IG398:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x440]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG399:
       jmp      G_M000_IG346
 
G_M000_IG400:
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG487
       test     rax, rax
       jl       G_M000_IG488
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG399
 
G_M000_IG401:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG404
 
G_M000_IG402:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG404
 
G_M000_IG403:
       xor      rsi, rsi
 
G_M000_IG404:
       test     rsi, rsi
       je       SHORT G_M000_IG408
 
G_M000_IG405:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x430], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x428]
       vmovsd   qword ptr [rbp-0x488], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x488]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x430]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG407
 
G_M000_IG406:
       jmp      G_M000_IG377
 
G_M000_IG407:
       mov      rax, gword ptr [rbp+0x200]
       jmp      G_M000_IG376
 
G_M000_IG408:
       mov      rax, gword ptr [rbp+0x200]
       jmp      G_M000_IG376
 
G_M000_IG409:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG420
 
G_M000_IG410:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x498], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG413
 
G_M000_IG411:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG413
 
G_M000_IG412:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4A8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x1F8]
       je       G_M000_IG423
       mov      rdx, gword ptr [rbp-0x4A8]
       jmp      G_M000_IG424
 
G_M000_IG413:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG416
 
G_M000_IG414:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG416
 
G_M000_IG415:
       xor      rsi, rsi
 
G_M000_IG416:
       test     rsi, rsi
       je       SHORT G_M000_IG419
 
G_M000_IG417:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax]
       lea      rsi, [rbp-0x4A0]
       vmovsd   xmm0, qword ptr [rbp-0x498]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x1F8]
       je       SHORT G_M000_IG423
 
G_M000_IG418:
       mov      rdx, gword ptr [rbp-0x4A0]
       jmp      SHORT G_M000_IG424
 
G_M000_IG419:
       mov      rax, gword ptr [rbp+0x1F8]
       jmp      SHORT G_M000_IG423
 
G_M000_IG420:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG423
 
G_M000_IG421:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG423
 
G_M000_IG422:
       mov      rsi, qword ptr [rsi]
 
G_M000_IG423:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG424:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x4A8], rcx
 
G_M000_IG425:
       mov      gword ptr [rbp-0x4A0], rcx
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG426:
       mov      gword ptr [rbp+0x1E8], rax
 
G_M000_IG427:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x540], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x218]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x540]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x1E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x548], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x550], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x540]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x548]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x550]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x548]
       add      r12, qword ptr [rbp+0x270]
       jo       G_M000_IG488
       mov      edx, dword ptr [rbp-0x52C]
       mov      edi, edx
       mov      rsi, qword ptr [rbp+0x278]
       cmp      r12, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       mov      qword ptr [rbp+0x278], rsi
       cmp      r12, rsi
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG481
 
G_M000_IG428:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG431
 
G_M000_IG429:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      r8, rax
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG487
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG431
 
G_M000_IG430:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG487
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 10
 
G_M000_IG431:
       test     r14, r14
       jne      G_M000_IG473
 
G_M000_IG432:
       mov      rax, r14
       mov      r8, rbx
       mov      rdi, r15
       test     rax, rax
       mov      gword ptr [rbp+0xF8], r8
       je       SHORT G_M000_IG436
 
G_M000_IG433:
       mov      rdx, rdi
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG436
 
G_M000_IG434:
       test     rbx, rbx
       je       SHORT G_M000_IG436
 
G_M000_IG435:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       je       G_M000_IG03
 
G_M000_IG436:
       test     r14, r14
       je       G_M000_IG451
 
G_M000_IG437:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r14], rdx
       je       G_M000_IG10
 
G_M000_IG438:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG448
 
G_M000_IG439:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG442
 
G_M000_IG440:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG442
 
G_M000_IG441:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG452
 
G_M000_IG442:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG445
 
G_M000_IG443:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG445
 
G_M000_IG444:
       xor      rsi, rsi
 
G_M000_IG445:
       test     rsi, rsi
       je       SHORT G_M000_IG451
 
G_M000_IG446:
       mov      rsi, rbx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x80], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      esi, 1
       test     esi, esi
       je       SHORT G_M000_IG451
 
G_M000_IG447:
       jmp      SHORT G_M000_IG452
 
G_M000_IG448:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r14], rsi
       jne      SHORT G_M000_IG451
 
G_M000_IG449:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG451
 
G_M000_IG450:
       mov      rsi, qword ptr [rsi]
 
G_M000_IG451:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG452:
       mov      gword ptr [rbp+0x240], rax
       test     rax, rax
       je       G_M000_IG40
 
G_M000_IG453:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rcx, rdx
       jne      G_M000_IG28
 
G_M000_IG454:
       mov      rsi, rax
       mov      qword ptr [rbp-0x538], rdx
       mov      rdi, rdx
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x98], rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG457
 
G_M000_IG455:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG457
 
G_M000_IG456:
       xor      rsi, rsi
 
G_M000_IG457:
       test     rsi, rsi
       je       G_M000_IG62
 
G_M000_IG458:
       mov      rsi, r15
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xA8], rax
       mov      rcx, qword ptr [rbp-0x98]
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0xB0], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG58
 
G_M000_IG459:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x560], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x520], ecx
       mov      rdi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x560]
       mov      esi, dword ptr [rbp-0x520]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG460:
       mov      esi, 1
       test     esi, esi
       je       G_M000_IG39
 
G_M000_IG461:
       jmp      G_M000_IG41
 
G_M000_IG462:
       mov      rax, qword ptr [rbp-0x4C8]
       mov      qword ptr [rbp-0x510], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG465
 
G_M000_IG463:
       mov      rdi, qword ptr [rbp-0x538]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x510]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG464:
       jmp      G_M000_IG388
 
G_M000_IG465:
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG487
       test     rcx, rcx
       jl       G_M000_IG488
       mov      rsi, rcx
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG464
 
G_M000_IG466:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG469
 
G_M000_IG467:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG469
 
G_M000_IG468:
       xor      rsi, rsi
 
G_M000_IG469:
       test     rsi, rsi
       je       SHORT G_M000_IG472
 
G_M000_IG470:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x4B8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x4B0]
       vmovsd   qword ptr [rbp-0x518], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x518]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x4B8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      esi, 1
       test     esi, esi
       mov      rax, gword ptr [rbp+0x1F8]
       je       G_M000_IG423
 
G_M000_IG471:
       jmp      G_M000_IG424
 
G_M000_IG472:
       mov      rax, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG423
 
G_M000_IG473:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG432
 
G_M000_IG474:
       test     rbx, rbx
       je       G_M000_IG432
 
G_M000_IG475:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG432
 
G_M000_IG476:
       test     r15, r15
       je       G_M000_IG432
 
G_M000_IG477:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG432
 
G_M000_IG478:
       test     r13, r13
       je       G_M000_IG432
 
G_M000_IG479:
       mov      rdi, qword ptr [r13]
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jne      G_M000_IG432
 
G_M000_IG480:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp+0x1A8], xmm1
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x198], xmm2
       vmovsd   xmm3, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x188], xmm3
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x158], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x158]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp+0x218], rsi
       vmovsd   xmm1, qword ptr [rbp+0x1A8]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x188]
       vmovsd   xmm2, qword ptr [rbp+0x198]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x128]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      rax, rsi
       mov      gword ptr [rbp+0x1E8], rax
       jmp      G_M000_IG427
 
G_M000_IG481:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG484
 
G_M000_IG482:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG487
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG484
 
G_M000_IG483:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG487
       mov      dword ptr [rcx+4*rdx+0x10], 11
 
G_M000_IG484:
       mov      r10, qword ptr [rbp+0x2A8]
       test     r10, r10
       jle      SHORT G_M000_IG486
 
G_M000_IG485:
       movsxd   rcx, edi
       cmp      r10, rcx
       jg       SHORT G_M000_IG486
       dec      r10d
       mov      dword ptr [(reloc)], r10d
 
G_M000_IG486:
       add      rsp, 0x858
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG487:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG488:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 10815

