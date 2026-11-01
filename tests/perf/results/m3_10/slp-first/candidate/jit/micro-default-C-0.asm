; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 864
       lea      rbp, [rsp+0x360]
       xor      eax, eax
       mov      qword ptr [rbp-0x338], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x330], xmm8
       mov      rax, -720
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
       mov      dword ptr [rbp-0x340], 0x3E8
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
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x340]
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
       add      rsp, 864
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x340]
       mov      esi, 222
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x338], rax
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x340]
       mov      esi, 261
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x250], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x260], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x270], rax
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
       vmovsd   xmm1, qword ptr [rbp-0x188]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x168]
       vmovsd   xmm1, qword ptr [rbp-0x168]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vmovsd   xmm1, qword ptr [rbp-0x178]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1C0], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1A0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1D0]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1D0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1E0]
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1E0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1F0]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1F0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x200]
 
G_M000_IG17:
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x200]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x210]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x210]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x220]
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovsd   xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x228], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0xF8], rax
       vmovsd   xmm0, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rbp-0x230], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x340]
       mov      esi, 453
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x340]
       mov      esi, 480
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x238], eax
       cmp      dword ptr [rbp-0x238], 0
       jne      G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x340]
       mov      esi, 507
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x23C], eax
       cmp      dword ptr [rbp-0x23C], 0
       jne      G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x340]
       mov      esi, 534
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0x150]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x240], eax
       cmp      dword ptr [rbp-0x240], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x340]
       mov      esi, 561
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      dword ptr [rbp-0x244], 1
       cmp      dword ptr [rbp-0x244], 0
       jne      G_M000_IG27
       jmp      G_M000_IG13
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x340]
       mov      esi, 580
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x2E0], rax
       jmp      G_M000_IG62
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x340]
       mov      esi, 599
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
       mov      rsi, gword ptr [rbp-0x250]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x258], xmm0
       mov      rsi, gword ptr [rbp-0x260]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x268], xmm0
       mov      rsi, gword ptr [rbp-0x270]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x278], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x280], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x290]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x298], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x298]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x340]
       mov      esi, 758
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x270]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2AC], eax
       cmp      dword ptr [rbp-0x2AC], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x340]
       mov      esi, 785
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x260]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2B0], eax
       cmp      dword ptr [rbp-0x2B0], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x340]
       mov      esi, 812
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rsi, gword ptr [rbp-0x250]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2B4], eax
       cmp      dword ptr [rbp-0x2B4], 0
       jne      G_M000_IG43
       jmp      G_M000_IG36
 
G_M000_IG49:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG51
 
G_M000_IG50:
       lea      rdi, [rbp-0x340]
       mov      esi, 839
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG51:
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x340]
       mov      esi, 848
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
       mov      rsi, gword ptr [rbp-0x2C0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       mov      rsi, gword ptr [rbp-0x2D0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       mov      rsi, gword ptr [rbp-0x2E0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2E8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x2F0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x300], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x308]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x318], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG49
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x3F3
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rsi, gword ptr [rbp-0x2E0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x31C], eax
       cmp      dword ptr [rbp-0x31C], 0
       jne      G_M000_IG55
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x40E
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG61:
       mov      rsi, gword ptr [rbp-0x2D0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x320], eax
       cmp      dword ptr [rbp-0x320], 0
       jne      G_M000_IG56
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG62:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x429
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp-0x2C0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x324], eax
       cmp      dword ptr [rbp-0x324], 0
       jne      G_M000_IG59
       jmp      G_M000_IG52
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3864

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1c5
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 32 inlinees with PGO data; 69 single block inlinees; 120 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x480
       mov      qword ptr [rsp+0x7E8], r15
       mov      qword ptr [rsp+0x7E0], r14
       mov      qword ptr [rsp+0x7D8], r13
       mov      qword ptr [rsp+0x7D0], r12
       mov      qword ptr [rsp+0x7C8], rbx
       lea      rbp, [rsp+0x480]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      r8, gword ptr [rbp+0x220]
       mov      rdx, gword ptr [rbp+0x1F0]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG229
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x1F0], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG229
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x220], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x220]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x218], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rbp+0x210]
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x210], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x210]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x208], xmm1
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x200]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x200], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x200]
 
G_M000_IG10:
       vmovsd   xmm2, qword ptr [r10+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x218]
       mov      rdx, gword ptr [rbp+0x1F0]
       vmovsd   xmm3, qword ptr [rdx+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovsd   xmm1, qword ptr [rbp+0x208]
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp+0x278], rax
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x248], rax
       jmp      G_M000_IG219
 
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
       jmp      G_M000_IG271
 
G_M000_IG14:
       test     rax, rax
       jl       G_M000_IG291
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG290
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
       mov      gword ptr [rbp-0x448], rax
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
       js       G_M000_IG289
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x450], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x448]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x450]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x450]
 
G_M000_IG29:
       jmp      SHORT G_M000_IG34
 
G_M000_IG30:
       mov      rsi, rax
       jmp      SHORT G_M000_IG29
 
G_M000_IG31:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG287
 
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
       mov      gword ptr [rbp-0x478], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x41C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x478]
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
       jl       G_M000_IG291
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
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
       jl       G_M000_IG291
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG290
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
       mov      gword ptr [rbp-0x458], rax
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
       js       G_M000_IG289
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x460], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x458]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x1F4]
       mov      rsi, gword ptr [rbp-0x460]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x460]
 
G_M000_IG91:
       jmp      SHORT G_M000_IG96
 
G_M000_IG92:
       mov      rsi, rax
       jmp      SHORT G_M000_IG91
 
G_M000_IG93:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG288
 
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
       jl       G_M000_IG291
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rdx, rdx
       jl       G_M000_IG291
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
       jl       G_M000_IG291
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rcx, rcx
       jl       G_M000_IG291
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
       mov      gword ptr [rbp+0x278], rax
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
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG218
 
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
       mov      qword ptr [rbp-0x428], rcx
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
       mov      rdi, qword ptr [rbp-0x428]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG162:
       jmp      SHORT G_M000_IG159
 
G_M000_IG163:
       test     rcx, rcx
       jl       G_M000_IG291
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rcx, rcx
       jl       G_M000_IG291
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
       jl       G_M000_IG291
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rcx, rcx
       jl       G_M000_IG291
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
       mov      gword ptr [rbp+0x268], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x410], rdx
 
G_M000_IG200:
       mov      gword ptr [rbp-0x408], rdx
 
G_M000_IG201:
       mov      gword ptr [rbp-0x400], rdx
 
G_M000_IG202:
       mov      gword ptr [rbp-0x3F8], rdx
       test     rax, rax
       je       G_M000_IG213
 
G_M000_IG203:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG272
 
G_M000_IG204:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG212
 
G_M000_IG205:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG208
 
G_M000_IG206:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x410]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG213
 
G_M000_IG207:
       mov      rdx, gword ptr [rbp-0x410]
       jmp      SHORT G_M000_IG214
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG211
 
G_M000_IG209:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG213
 
G_M000_IG210:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG214
 
G_M000_IG211:
       mov      rax, gword ptr [rbp+0x268]
       jmp      SHORT G_M000_IG213
 
G_M000_IG212:
       mov      rax, gword ptr [rbp+0x268]
 
G_M000_IG213:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG214:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x410], rcx
 
G_M000_IG215:
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG216:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG217:
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
 
G_M000_IG218:
       mov      gword ptr [rbp+0x248], rax
 
G_M000_IG219:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x430], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x430]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x438], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x440], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x430]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x438]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x440]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x438]
       mov      rcx, qword ptr [rbp+0x2D0]
       add      r12, rcx
       jo       G_M000_IG291
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x2D8]
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
       je       G_M000_IG280
 
G_M000_IG220:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG223
 
G_M000_IG221:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      eax
       mov      r8d, eax
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG290
       cmp      dword ptr [rsi+4*r8+0x10], 10
       je       SHORT G_M000_IG223
 
G_M000_IG222:
       cmp      eax, dword ptr [rdx+0x08]
       jae      G_M000_IG290
       mov      eax, eax
       mov      dword ptr [rdx+4*rax+0x10], 10
 
G_M000_IG223:
       mov      r8, r14
       mov      r9, rbx
       mov      r10, r15
       mov      rdx, r13
       test     r14, r14
       mov      qword ptr [rbp+0x2D8], rdi
       mov      qword ptr [rbp+0x2D0], rcx
       mov      gword ptr [rbp+0x210], r9
       mov      gword ptr [rbp+0x200], r10
       je       SHORT G_M000_IG229
 
G_M000_IG224:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG229
 
G_M000_IG225:
       test     rbx, rbx
       je       SHORT G_M000_IG229
 
G_M000_IG226:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG229
 
G_M000_IG227:
       test     r15, r15
       je       SHORT G_M000_IG229
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       je       G_M000_IG02
 
G_M000_IG229:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG236
 
G_M000_IG230:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rdx
       jne      SHORT G_M000_IG236
 
G_M000_IG231:
       test     rbx, rbx
       je       SHORT G_M000_IG236
 
G_M000_IG232:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG236
 
G_M000_IG233:
       test     r15, r15
       je       SHORT G_M000_IG236
 
G_M000_IG234:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG236
 
G_M000_IG235:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG144
 
G_M000_IG236:
       test     r14, r14
       je       G_M000_IG264
 
G_M000_IG237:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG258
 
G_M000_IG238:
       mov      rsi, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG264
 
G_M000_IG239:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG256
 
G_M000_IG240:
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
       je       G_M000_IG252
 
G_M000_IG241:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x468], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x468]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG245
 
G_M000_IG242:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG243:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG244:
       mov      rsi, rax
       jmp      G_M000_IG265
 
G_M000_IG245:
       test     rcx, rcx
       je       SHORT G_M000_IG250
 
G_M000_IG246:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG247:
       test     rax, rax
       je       SHORT G_M000_IG251
 
G_M000_IG248:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG249:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG243
 
G_M000_IG250:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG247
 
G_M000_IG251:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG249
 
G_M000_IG252:
       mov      rdx, r10
       mov      qword ptr [rbp-0x88], rdx
       lea      rsi, [rdx+0x05]
       cmp      rsi, 261
       jbe      SHORT G_M000_IG255
 
G_M000_IG253:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG254:
       jmp      SHORT G_M000_IG244
 
G_M000_IG255:
       test     rsi, rsi
       jl       G_M000_IG291
       mov      rdi, rsi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG290
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG254
 
G_M000_IG256:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG264
 
G_M000_IG257:
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
       jmp      G_M000_IG265
 
G_M000_IG258:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG264
 
G_M000_IG259:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG264
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG262
 
G_M000_IG261:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG265
 
G_M000_IG262:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG264
 
G_M000_IG263:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG265
 
G_M000_IG264:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG265:
       test     rsi, rsi
       je       G_M000_IG33
 
G_M000_IG266:
       mov      rax, qword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      rax, rcx
       jne      G_M000_IG17
 
G_M000_IG267:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG268:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG15
 
G_M000_IG269:
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
 
G_M000_IG270:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x418], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x470]
       mov      esi, dword ptr [rbp-0x418]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG271:
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG272:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG213
 
G_M000_IG273:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG213
 
G_M000_IG274:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG277
 
G_M000_IG275:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x400]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG213
 
G_M000_IG276:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG214
 
G_M000_IG277:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG213
 
G_M000_IG278:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG213
 
G_M000_IG279:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG214
 
G_M000_IG280:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG283
 
G_M000_IG281:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG290
       cmp      dword ptr [rdi+4*r8+0x10], 11
       je       SHORT G_M000_IG283
 
G_M000_IG282:
       mov      edi, esi
       cmp      edi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG290
       mov      dword ptr [rdx+4*rdi+0x10], 11
 
G_M000_IG283:
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG286
 
G_M000_IG284:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG286
 
G_M000_IG285:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG286:
       add      rsp, 0x7C8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG287:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG29
 
G_M000_IG288:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG91
 
G_M000_IG289:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG290:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG291:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7583

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 864
       lea      rbp, [rsp+0x360]
       xor      eax, eax
       mov      qword ptr [rbp-0x338], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x330], xmm8
       mov      rax, -720
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
       mov      dword ptr [rbp-0x340], 0x3E8
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
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x340]
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
       add      rsp, 864
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x340]
       mov      esi, 223
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x338], rax
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x340]
       mov      esi, 262
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x250], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x260], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x270], rax
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
       vmovsd   xmm1, qword ptr [rbp-0x188]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x168]
       vmovsd   xmm1, qword ptr [rbp-0x168]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vmovsd   xmm1, qword ptr [rbp-0x178]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1C0], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1A0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1D0]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1D0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1E0]
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1E0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1F0]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1F0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x200]
 
G_M000_IG17:
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x200]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x210]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x210]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x220]
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovsd   xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x228], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0xF8], rax
       vmovsd   xmm0, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rbp-0x230], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x340]
       mov      esi, 454
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x340]
       mov      esi, 481
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x238], eax
       cmp      dword ptr [rbp-0x238], 0
       jne      G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x340]
       mov      esi, 508
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x23C], eax
       cmp      dword ptr [rbp-0x23C], 0
       jne      G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x340]
       mov      esi, 535
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0x150]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x240], eax
       cmp      dword ptr [rbp-0x240], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x340]
       mov      esi, 562
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      dword ptr [rbp-0x244], 1
       cmp      dword ptr [rbp-0x244], 0
       jne      G_M000_IG27
       jmp      G_M000_IG13
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x340]
       mov      esi, 581
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x2E0], rax
       jmp      G_M000_IG62
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x340]
       mov      esi, 600
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
       mov      rsi, gword ptr [rbp-0x250]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x258], xmm0
       mov      rsi, gword ptr [rbp-0x260]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x268], xmm0
       mov      rsi, gword ptr [rbp-0x270]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x278], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x280], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x290]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x298], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x298]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x340]
       mov      esi, 765
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x270]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2AC], eax
       cmp      dword ptr [rbp-0x2AC], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x340]
       mov      esi, 792
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x260]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2B0], eax
       cmp      dword ptr [rbp-0x2B0], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x340]
       mov      esi, 819
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rsi, gword ptr [rbp-0x250]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2B4], eax
       cmp      dword ptr [rbp-0x2B4], 0
       jne      G_M000_IG43
       jmp      G_M000_IG36
 
G_M000_IG49:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG51
 
G_M000_IG50:
       lea      rdi, [rbp-0x340]
       mov      esi, 846
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG51:
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x340]
       mov      esi, 855
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
       mov      rsi, gword ptr [rbp-0x2C0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       mov      rsi, gword ptr [rbp-0x2D0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       mov      rsi, gword ptr [rbp-0x2E0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2E8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x2F0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x300], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x308]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x318], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG49
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x3FD
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rsi, gword ptr [rbp-0x2E0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x31C], eax
       cmp      dword ptr [rbp-0x31C], 0
       jne      G_M000_IG55
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x418
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG61:
       mov      rsi, gword ptr [rbp-0x2D0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x320], eax
       cmp      dword ptr [rbp-0x320], 0
       jne      G_M000_IG56
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG62:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x433
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp-0x2C0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x324], eax
       cmp      dword ptr [rbp-0x324], 0
       jne      G_M000_IG59
       jmp      G_M000_IG52
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3867

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1c6
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 32 inlinees with PGO data; 69 single block inlinees; 120 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x470
       mov      qword ptr [rsp+0x7D8], r15
       mov      qword ptr [rsp+0x7D0], r14
       mov      qword ptr [rsp+0x7C8], r13
       mov      qword ptr [rsp+0x7C0], r12
       mov      qword ptr [rsp+0x7B8], rbx
       lea      rbp, [rsp+0x470]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      r8, gword ptr [rbp+0x220]
       mov      rdx, gword ptr [rbp+0x1F0]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG229
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x1F0], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG229
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x220], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x220]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x218], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rbp+0x210]
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x210], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x210]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x208], xmm1
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x200]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x200], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x200]
 
G_M000_IG10:
       vmovsd   xmm2, qword ptr [r10+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x218]
       mov      rdx, gword ptr [rbp+0x1F0]
       vmovsd   xmm3, qword ptr [rdx+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovsd   xmm1, qword ptr [rbp+0x208]
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp+0x278], rax
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x248], rax
       jmp      G_M000_IG219
 
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
       jmp      G_M000_IG271
 
G_M000_IG14:
       test     rax, rax
       jl       G_M000_IG291
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG290
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
       js       G_M000_IG289
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
       je       G_M000_IG287
 
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
       jl       G_M000_IG291
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
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
       jl       G_M000_IG291
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG290
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
       js       G_M000_IG289
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
       je       G_M000_IG288
 
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
       jl       G_M000_IG291
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rdx, rdx
       jl       G_M000_IG291
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
       jl       G_M000_IG291
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rcx, rcx
       jl       G_M000_IG291
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
       mov      gword ptr [rbp+0x278], rax
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
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG218
 
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
       mov      qword ptr [rbp-0x428], rcx
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
       mov      rdi, qword ptr [rbp-0x428]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG162:
       jmp      SHORT G_M000_IG159
 
G_M000_IG163:
       test     rcx, rcx
       jl       G_M000_IG291
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rcx, rcx
       jl       G_M000_IG291
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
       jl       G_M000_IG291
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG290
       test     rcx, rcx
       jl       G_M000_IG291
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
       mov      gword ptr [rbp+0x268], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x410], rdx
 
G_M000_IG200:
       mov      gword ptr [rbp-0x408], rdx
 
G_M000_IG201:
       mov      gword ptr [rbp-0x400], rdx
 
G_M000_IG202:
       mov      gword ptr [rbp-0x3F8], rdx
       test     rax, rax
       je       G_M000_IG213
 
G_M000_IG203:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG272
 
G_M000_IG204:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG212
 
G_M000_IG205:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG208
 
G_M000_IG206:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x410]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG213
 
G_M000_IG207:
       mov      rdx, gword ptr [rbp-0x410]
       jmp      SHORT G_M000_IG214
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG211
 
G_M000_IG209:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG213
 
G_M000_IG210:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG214
 
G_M000_IG211:
       mov      rax, gword ptr [rbp+0x268]
       jmp      SHORT G_M000_IG213
 
G_M000_IG212:
       mov      rax, gword ptr [rbp+0x268]
 
G_M000_IG213:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG214:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x410], rcx
 
G_M000_IG215:
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG216:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG217:
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
 
G_M000_IG218:
       mov      gword ptr [rbp+0x248], rax
 
G_M000_IG219:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x430], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x430]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x438], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x430]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x438]
       mov      rcx, qword ptr [rbp+0x2D0]
       add      r12, rcx
       jo       G_M000_IG291
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x2D8]
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
       je       G_M000_IG280
 
G_M000_IG220:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG223
 
G_M000_IG221:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      eax
       mov      r8d, eax
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG290
       cmp      dword ptr [rsi+4*r8+0x10], 17
       je       SHORT G_M000_IG223
 
G_M000_IG222:
       cmp      eax, dword ptr [rdx+0x08]
       jae      G_M000_IG290
       mov      eax, eax
       mov      dword ptr [rdx+4*rax+0x10], 17
 
G_M000_IG223:
       mov      r8, r14
       mov      r9, rbx
       mov      r10, r15
       mov      rdx, r13
       test     r14, r14
       mov      qword ptr [rbp+0x2D8], rdi
       mov      qword ptr [rbp+0x2D0], rcx
       mov      gword ptr [rbp+0x210], r9
       mov      gword ptr [rbp+0x200], r10
       je       SHORT G_M000_IG229
 
G_M000_IG224:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG229
 
G_M000_IG225:
       test     rbx, rbx
       je       SHORT G_M000_IG229
 
G_M000_IG226:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG229
 
G_M000_IG227:
       test     r15, r15
       je       SHORT G_M000_IG229
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       je       G_M000_IG02
 
G_M000_IG229:
       mov      rdi, r14
       test     rdi, rdi
       je       G_M000_IG236
 
G_M000_IG230:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rdx
       jne      SHORT G_M000_IG236
 
G_M000_IG231:
       test     rbx, rbx
       je       SHORT G_M000_IG236
 
G_M000_IG232:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG236
 
G_M000_IG233:
       test     r15, r15
       je       SHORT G_M000_IG236
 
G_M000_IG234:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG236
 
G_M000_IG235:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG144
 
G_M000_IG236:
       test     r14, r14
       je       G_M000_IG264
 
G_M000_IG237:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG258
 
G_M000_IG238:
       mov      rsi, qword ptr [r14+0x08]
       test     rbx, rbx
       je       G_M000_IG264
 
G_M000_IG239:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG256
 
G_M000_IG240:
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
       je       G_M000_IG252
 
G_M000_IG241:
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
       jne      SHORT G_M000_IG245
 
G_M000_IG242:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x80]
       mov      esi, dword ptr [rbp-0x78]
 
G_M000_IG243:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG244:
       mov      rsi, rax
       jmp      G_M000_IG265
 
G_M000_IG245:
       test     rcx, rcx
       je       SHORT G_M000_IG250
 
G_M000_IG246:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG247:
       test     rax, rax
       je       SHORT G_M000_IG251
 
G_M000_IG248:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG249:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG243
 
G_M000_IG250:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG247
 
G_M000_IG251:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG249
 
G_M000_IG252:
       mov      rdx, r10
       mov      qword ptr [rbp-0x88], rdx
       lea      rsi, [rdx+0x05]
       cmp      rsi, 261
       jbe      SHORT G_M000_IG255
 
G_M000_IG253:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG254:
       jmp      SHORT G_M000_IG244
 
G_M000_IG255:
       test     rsi, rsi
       jl       G_M000_IG291
       mov      rdi, rsi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG290
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG254
 
G_M000_IG256:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG264
 
G_M000_IG257:
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
       jmp      G_M000_IG265
 
G_M000_IG258:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG264
 
G_M000_IG259:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG264
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG262
 
G_M000_IG261:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG265
 
G_M000_IG262:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG264
 
G_M000_IG263:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG265
 
G_M000_IG264:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG265:
       test     rsi, rsi
       je       G_M000_IG33
 
G_M000_IG266:
       mov      rax, qword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      rax, rcx
       jne      G_M000_IG17
 
G_M000_IG267:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG33
 
G_M000_IG268:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG15
 
G_M000_IG269:
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
 
G_M000_IG270:
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
 
G_M000_IG271:
       mov      rsi, rax
       jmp      G_M000_IG34
 
G_M000_IG272:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG213
 
G_M000_IG273:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG213
 
G_M000_IG274:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG277
 
G_M000_IG275:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x400]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG213
 
G_M000_IG276:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG214
 
G_M000_IG277:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG213
 
G_M000_IG278:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG213
 
G_M000_IG279:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG214
 
G_M000_IG280:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG283
 
G_M000_IG281:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG290
       cmp      dword ptr [rdi+4*r8+0x10], 18
       je       SHORT G_M000_IG283
 
G_M000_IG282:
       mov      edi, esi
       cmp      edi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG290
       mov      dword ptr [rdx+4*rdi+0x10], 18
 
G_M000_IG283:
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG286
 
G_M000_IG284:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG286
 
G_M000_IG285:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG286:
       add      rsp, 0x7B8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG287:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG29
 
G_M000_IG288:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG91
 
G_M000_IG289:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG290:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG291:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7538

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 864
       lea      rbp, [rsp+0x360]
       xor      eax, eax
       mov      qword ptr [rbp-0x338], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x330], xmm8
       mov      rax, -720
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
       mov      dword ptr [rbp-0x340], 0x3E8
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
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x340]
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
       add      rsp, 864
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x340]
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
       mov      gword ptr [rbp-0x338], rax
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rdx, gword ptr [rbp-0x130]
       mov      esi, 2
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x338]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x138], rax
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x58], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x340]
       mov      esi, 284
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x250], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x260], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x270], rax
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
       vmovsd   xmm1, qword ptr [rbp-0x190]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x170]
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vmovsd   xmm1, qword ptr [rbp-0x180]
       vunpcklpd xmm0, xmm0, xmm1
       vmovaps  xmmword ptr [rbp-0x1C0], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1A0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1D0]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1D0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1E0]
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1E0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x1F0]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x1F0]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x200]
 
G_M000_IG17:
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x200]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1B0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x210]
       call     [System.Runtime.Intrinsics.Vector128:Multiply[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovaps  xmm0, xmmword ptr [rbp-0x210]
       vmovups  xmmword ptr [rsp], xmm0
       vmovaps  xmm0, xmmword ptr [rbp-0x1C0]
       vmovups  xmmword ptr [rsp+0x10], xmm0
       lea      rdi, [rbp-0x220]
       call     [System.Runtime.Intrinsics.Vector128:Add[double](System.Runtime.Intrinsics.Vector128`1[double],System.Runtime.Intrinsics.Vector128`1[double]):System.Runtime.Intrinsics.Vector128`1[double]]
       vmovsd   xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x228], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0xF8], rax
       vmovsd   xmm0, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rbp-0x230], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x340]
       mov      esi, 476
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rsi, gword ptr [rbp-0x188]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x340]
       mov      esi, 503
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rsi, gword ptr [rbp-0x178]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x238], eax
       cmp      dword ptr [rbp-0x238], 0
       jne      G_M000_IG18
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x340]
       mov      esi, 530
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x23C], eax
       cmp      dword ptr [rbp-0x23C], 0
       jne      G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x340]
       mov      esi, 557
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x240], eax
       cmp      dword ptr [rbp-0x240], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x340]
       mov      esi, 584
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      dword ptr [rbp-0x244], 1
       cmp      dword ptr [rbp-0x244], 0
       jne      G_M000_IG27
       jmp      G_M000_IG13
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x340]
       mov      esi, 603
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0xF8], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x2E0], rax
       jmp      G_M000_IG62
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x340]
       mov      esi, 622
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
       mov      rsi, gword ptr [rbp-0x250]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x258], xmm0
       mov      rsi, gword ptr [rbp-0x260]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x268], xmm0
       mov      rsi, gword ptr [rbp-0x270]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x278], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x280], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x290], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x290]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x298], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x298]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x2A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rbp-0x2A8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x148], rax
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x340]
       mov      esi, 787
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x270]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2AC], eax
       cmp      dword ptr [rbp-0x2AC], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x340]
       mov      esi, 814
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x260]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2B0], eax
       cmp      dword ptr [rbp-0x2B0], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG36
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x340]
       mov      esi, 841
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rsi, gword ptr [rbp-0x250]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x2B4], eax
       cmp      dword ptr [rbp-0x2B4], 0
       jne      G_M000_IG43
       jmp      G_M000_IG36
 
G_M000_IG49:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG51
 
G_M000_IG50:
       lea      rdi, [rbp-0x340]
       mov      esi, 868
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG51:
       mov      rax, gword ptr [rbp-0x150]
       mov      gword ptr [rbp-0x128], rax
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG54
 
G_M000_IG53:
       lea      rdi, [rbp-0x340]
       mov      esi, 877
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
       mov      rsi, gword ptr [rbp-0x2C0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2C8], xmm0
       mov      rsi, gword ptr [rbp-0x2D0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       mov      rsi, gword ptr [rbp-0x2E0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2E8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2C8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x2F0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x300], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x308]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rbp-0x310], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rbp-0x318], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x330]
       vmovsd   xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x150], rax
       jmp      G_M000_IG49
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x413
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rsi, gword ptr [rbp-0x2E0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x31C], eax
       cmp      dword ptr [rbp-0x31C], 0
       jne      G_M000_IG55
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG59:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x42E
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG61:
       mov      rsi, gword ptr [rbp-0x2D0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x320], eax
       cmp      dword ptr [rbp-0x320], 0
       jne      G_M000_IG56
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG52
 
G_M000_IG62:
       mov      eax, dword ptr [rbp-0x340]
       dec      eax
       mov      dword ptr [rbp-0x340], eax
       cmp      dword ptr [rbp-0x340], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:
       lea      rdi, [rbp-0x340]
       mov      esi, 0x449
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:
       mov      rsi, gword ptr [rbp-0x2C0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x324], eax
       cmp      dword ptr [rbp-0x324], 0
       jne      G_M000_IG59
       jmp      G_M000_IG52
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3923

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1dc
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 44 inlinees with PGO data; 71 single block inlinees; 110 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x3F0
       mov      qword ptr [rsp+0x758], r15
       mov      qword ptr [rsp+0x750], r14
       mov      qword ptr [rsp+0x748], r13
       mov      qword ptr [rsp+0x740], r12
       mov      qword ptr [rsp+0x738], rbx
       lea      rbp, [rsp+0x3F0]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -816
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      r8, gword ptr [rbp+0x218]
       mov      rdx, gword ptr [rbp+0x1E8]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG218
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x1E8], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG218
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x218], r8
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x218]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x210], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r9, gword ptr [rbp+0x208]
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x208], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x208]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x200], xmm1
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x1F8]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x1F8], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x1F8]
 
G_M000_IG10:
       vmovsd   xmm2, qword ptr [r10+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x210]
       mov      rdx, gword ptr [rbp+0x1E8]
       vmovsd   xmm3, qword ptr [rdx+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovsd   xmm1, qword ptr [rbp+0x200]
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp+0x278], rax
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x248], rax
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
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
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
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
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
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG149:
       mov      gword ptr [rbp+0x278], rax
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
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
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
       mov      gword ptr [rbp+0x248], rax
 
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
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x3A0]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
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
       mov      rcx, qword ptr [rbp+0x2D0]
       add      r12, rcx
       jo       G_M000_IG271
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x2D8]
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
       mov      qword ptr [rbp+0x2D8], rdi
       mov      qword ptr [rbp+0x2D0], rcx
       mov      gword ptr [rbp+0x208], r9
       mov      gword ptr [rbp+0x1F8], r10
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
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
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
       mov      rbx, qword ptr [rbp+0x308]
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
       add      rsp, 0x738
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
 
; Total bytes of code 7106

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xde
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 32 inlinees with PGO data; 69 single block inlinees; 120 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x480
       mov      qword ptr [rsp+0x7E8], r15
       mov      qword ptr [rsp+0x7E0], r14
       mov      qword ptr [rsp+0x7D8], r13
       mov      qword ptr [rsp+0x7D0], r12
       mov      qword ptr [rsp+0x7C8], rbx
       lea      rbp, [rsp+0x480]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      r8, qword ptr [rbp+0x2D0]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x2D0], r8
       test     r8, r8
       setg     r10b
       movzx    r10, r10b
       mov      dword ptr [rbp-0x420], r10d
       jmp      G_M000_IG254
 
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
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG179
 
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
       mov      gword ptr [rbp-0x468], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x468]
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
       jl       G_M000_IG286
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
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
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x418], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x470]
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
       jl       G_M000_IG286
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG285
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
       mov      gword ptr [rbp-0x448], rsi
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
       js       G_M000_IG284
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x450], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x448]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x450]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x450]
 
G_M000_IG64:
       jmp      SHORT G_M000_IG69
 
G_M000_IG65:
       jmp      SHORT G_M000_IG64
 
G_M000_IG66:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG282
 
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
       mov      gword ptr [rbp-0x478], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x41C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x478]
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
       jl       G_M000_IG286
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG285
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
       jl       G_M000_IG286
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG285
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
       mov      gword ptr [rbp-0x458], rax
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
       js       G_M000_IG284
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x460], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x458]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x1F4]
       mov      rsi, gword ptr [rbp-0x460]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x460]
 
G_M000_IG126:
       jmp      SHORT G_M000_IG131
 
G_M000_IG127:
       mov      rsi, rax
       jmp      SHORT G_M000_IG126
 
G_M000_IG128:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG283
 
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
       je       G_M000_IG152
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG146
 
G_M000_IG133:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG152
 
G_M000_IG134:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG144
 
G_M000_IG135:
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
       je       G_M000_IG140
 
G_M000_IG136:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
       lea      rdi, [rbp-0x228]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG137:
       vmovdqu  xmm0, xmmword ptr [rbp-0x228]
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
 
G_M000_IG138:
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
 
G_M000_IG139:
       mov      rsi, rax
       jmp      G_M000_IG153
 
G_M000_IG140:
       mov      rcx, r9
       mov      qword ptr [rbp-0x268], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG143
 
G_M000_IG141:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x268]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG142:
       jmp      SHORT G_M000_IG139
 
G_M000_IG143:
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
       jmp      SHORT G_M000_IG142
 
G_M000_IG144:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG152
 
G_M000_IG145:
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
       jmp      G_M000_IG153
 
G_M000_IG146:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG152
 
G_M000_IG147:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm0
       test     rbx, rbx
       je       G_M000_IG152
 
G_M000_IG148:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG150
 
G_M000_IG149:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG153
 
G_M000_IG150:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG152
 
G_M000_IG151:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x278]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG153
 
G_M000_IG152:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG153:
       test     rsi, rsi
       je       G_M000_IG178
 
G_M000_IG154:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG168
 
G_M000_IG155:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG178
 
G_M000_IG156:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG166
 
G_M000_IG157:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x298], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x2A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG162
 
G_M000_IG158:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2B0], xmm0
       lea      rdi, [rbp-0x2B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG159:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2B0]
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
 
G_M000_IG160:
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
 
G_M000_IG161:
       jmp      G_M000_IG179
 
G_M000_IG162:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG165
 
G_M000_IG163:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG164:
       jmp      SHORT G_M000_IG161
 
G_M000_IG165:
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
       jmp      SHORT G_M000_IG164
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG178
 
G_M000_IG167:
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
       jmp      G_M000_IG179
 
G_M000_IG168:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG174
 
G_M000_IG169:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm0
       test     r15, r15
       je       G_M000_IG178
 
G_M000_IG170:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG172
 
G_M000_IG171:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG179
 
G_M000_IG172:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG178
 
G_M000_IG173:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x280]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG179
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG178
 
G_M000_IG175:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG178
 
G_M000_IG176:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG178
 
G_M000_IG177:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG179
 
G_M000_IG178:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG179:
       mov      gword ptr [rbp+0x278], rax
       mov      rdi, r13
       test     rdi, rdi
       je       G_M000_IG186
 
G_M000_IG180:
       mov      rdi, qword ptr [rdi]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG186
 
G_M000_IG181:
       test     rbx, rbx
       je       SHORT G_M000_IG186
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG186
 
G_M000_IG183:
       test     r15, r15
       je       SHORT G_M000_IG186
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG186
 
G_M000_IG185:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG253
 
G_M000_IG186:
       test     r13, r13
       je       G_M000_IG207
 
G_M000_IG187:
       mov      rdi, qword ptr [r13]
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      rdi, rcx
       jne      G_M000_IG201
 
G_M000_IG188:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x308], r8
       test     rbx, rbx
       je       G_M000_IG207
 
G_M000_IG189:
       mov      qword ptr [rbp-0x428], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG199
 
G_M000_IG190:
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
       je       G_M000_IG195
 
G_M000_IG191:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x330], xmm0
       lea      rdi, [rbp-0x330]
       mov      rsi, qword ptr [rbp-0x308]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG192:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x330]
       vmovdqu32 xmmword ptr [rbp-0x340], xmm0
 
G_M000_IG193:
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
 
G_M000_IG194:
       mov      rsi, rax
       jmp      G_M000_IG208
 
G_M000_IG195:
       mov      rax, qword ptr [rbp-0x320]
       mov      qword ptr [rbp-0x368], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG198
 
G_M000_IG196:
       mov      rdi, qword ptr [rbp-0x428]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x368]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG197:
       jmp      SHORT G_M000_IG194
 
G_M000_IG198:
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
       jmp      SHORT G_M000_IG197
 
G_M000_IG199:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG207
 
G_M000_IG200:
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
       jmp      G_M000_IG208
 
G_M000_IG201:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      G_M000_IG207
 
G_M000_IG202:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     rbx, rbx
       je       G_M000_IG207
 
G_M000_IG203:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG205
 
G_M000_IG204:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG208
 
G_M000_IG205:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG207
 
G_M000_IG206:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x378], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x378]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG208
 
G_M000_IG207:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG208:
       test     rsi, rsi
       je       G_M000_IG233
 
G_M000_IG209:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG223
 
G_M000_IG210:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG233
 
G_M000_IG211:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG221
 
G_M000_IG212:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x398], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x3A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG217
 
G_M000_IG213:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3B0], xmm0
       lea      rdi, [rbp-0x3B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG214:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3B0]
       vmovdqu32 xmmword ptr [rbp-0x3C0], xmm0
 
G_M000_IG215:
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
 
G_M000_IG216:
       jmp      G_M000_IG234
 
G_M000_IG217:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG220
 
G_M000_IG218:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x3A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG219:
       jmp      SHORT G_M000_IG216
 
G_M000_IG220:
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
       jmp      SHORT G_M000_IG219
 
G_M000_IG221:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG233
 
G_M000_IG222:
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
       jmp      G_M000_IG234
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG229
 
G_M000_IG224:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm0
       test     r15, r15
       je       G_M000_IG233
 
G_M000_IG225:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG227
 
G_M000_IG226:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG234
 
G_M000_IG227:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG233
 
G_M000_IG228:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x380]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG234
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG233
 
G_M000_IG230:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG233
 
G_M000_IG231:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG233
 
G_M000_IG232:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG234
 
G_M000_IG233:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG234:
       mov      gword ptr [rbp+0x268], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x410], rdx
 
G_M000_IG235:
       mov      gword ptr [rbp-0x408], rdx
 
G_M000_IG236:
       mov      gword ptr [rbp-0x400], rdx
 
G_M000_IG237:
       mov      gword ptr [rbp-0x3F8], rdx
       test     rax, rax
       je       G_M000_IG248
 
G_M000_IG238:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG267
 
G_M000_IG239:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG247
 
G_M000_IG240:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG243
 
G_M000_IG241:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x410]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG248
 
G_M000_IG242:
       mov      rdx, gword ptr [rbp-0x410]
       jmp      SHORT G_M000_IG249
 
G_M000_IG243:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG246
 
G_M000_IG244:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x408]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG248
 
G_M000_IG245:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG249
 
G_M000_IG246:
       mov      rax, gword ptr [rbp+0x268]
       jmp      SHORT G_M000_IG248
 
G_M000_IG247:
       mov      rax, gword ptr [rbp+0x268]
 
G_M000_IG248:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG249:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x410], rcx
 
G_M000_IG250:
       mov      gword ptr [rbp-0x408], rcx
 
G_M000_IG251:
       mov      gword ptr [rbp-0x400], rcx
 
G_M000_IG252:
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
 
G_M000_IG253:
       mov      gword ptr [rbp+0x248], rax
 
G_M000_IG254:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x430], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x430]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x438], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x440], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x430]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x438]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x440]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x438]
       add      r12, qword ptr [rbp+0x2D0]
       jo       G_M000_IG286
       mov      edx, dword ptr [rbp-0x420]
       mov      edi, edx
       mov      rsi, qword ptr [rbp+0x2D8]
       cmp      r12, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       mov      qword ptr [rbp+0x2D8], rsi
       cmp      r12, rsi
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG275
 
G_M000_IG255:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG258
 
G_M000_IG256:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      r8, rax
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG285
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG258
 
G_M000_IG257:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG285
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 10
 
G_M000_IG258:
       test     r14, r14
       je       G_M000_IG03
 
G_M000_IG259:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG03
 
G_M000_IG260:
       test     rbx, rbx
       je       G_M000_IG03
 
G_M000_IG261:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG03
 
G_M000_IG262:
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG263:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG264:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG265:
       mov      rax, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG03
 
G_M000_IG266:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   xmm3, qword ptr [r13+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rsi+0x08], xmm1
       mov      gword ptr [rbp+0x278], rsi
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      rax, rsi
       mov      gword ptr [rbp+0x248], rax
       jmp      G_M000_IG254
 
G_M000_IG267:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG248
 
G_M000_IG268:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG248
 
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
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG248
 
G_M000_IG271:
       mov      rdx, gword ptr [rbp-0x400]
       jmp      G_M000_IG249
 
G_M000_IG272:
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG248
 
G_M000_IG273:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x3F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x268]
       je       G_M000_IG248
 
G_M000_IG274:
       mov      rdx, gword ptr [rbp-0x3F8]
       jmp      G_M000_IG249
 
G_M000_IG275:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG278
 
G_M000_IG276:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG285
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG278
 
G_M000_IG277:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG285
       mov      dword ptr [rcx+4*rdx+0x10], 11
 
G_M000_IG278:
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG281
 
G_M000_IG279:
       movsxd   rcx, edi
       cmp      rbx, rcx
       jg       SHORT G_M000_IG281
 
G_M000_IG280:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG281:
       add      rsp, 0x7C8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG282:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG64
 
G_M000_IG283:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG126
 
G_M000_IG284:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG285:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG286:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7392

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1e0
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 29 inlinees with PGO data; 66 single block inlinees; 121 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x470
       mov      qword ptr [rsp+0x7D8], r15
       mov      qword ptr [rsp+0x7D0], r14
       mov      qword ptr [rsp+0x7C8], r13
       mov      qword ptr [rsp+0x7C0], r12
       mov      qword ptr [rsp+0x7B8], rbx
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
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      r9, gword ptr [rbp+0x220]
       mov      rdx, gword ptr [rbp+0x200]
       mov      r8, gword ptr [rbp+0x1F0]
 
G_M000_IG02:
       test     rdx, rdx
       je       G_M000_IG251
 
G_M000_IG03:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x200], rdx
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG251
 
G_M000_IG04:
       test     r8, r8
       je       G_M000_IG251
 
G_M000_IG05:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x1F0], r8
       cmp      qword ptr [r8], rsi
       jne      G_M000_IG251
 
G_M000_IG06:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r9], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x220], r9
       mov      rsi, r9
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r9, gword ptr [rbp+0x220]
 
G_M000_IG08:
       vmovsd   xmm0, qword ptr [r9+0x08]
       vmovsd   qword ptr [rbp+0x218], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rbp+0x210]
       cmp      qword ptr [r10], rsi
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       mov      gword ptr [rbp+0x210], r10
       mov      rsi, r10
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r10, gword ptr [rbp+0x210]
 
G_M000_IG10:
       vmovsd   xmm1, qword ptr [r10+0x08]
       mov      rdx, gword ptr [rbp+0x200]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x218]
       mov      r8, gword ptr [rbp+0x1F0]
       vmovsd   xmm3, qword ptr [r8+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp+0x278], rax
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x248], rax
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
       mov      gword ptr [rbp+0x278], rax
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
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
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
       mov      gword ptr [rbp+0x268], rax
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
       mov      rax, gword ptr [rbp+0x268]
       je       SHORT G_M000_IG238
 
G_M000_IG235:
       mov      rdx, gword ptr [rbp-0x408]
       jmp      SHORT G_M000_IG239
 
G_M000_IG236:
       mov      rax, gword ptr [rbp+0x268]
       jmp      SHORT G_M000_IG238
 
G_M000_IG237:
       mov      rax, gword ptr [rbp+0x268]
 
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
       mov      gword ptr [rbp+0x248], rax
 
G_M000_IG243:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x428], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x428]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
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
       mov      rcx, qword ptr [rbp+0x2D0]
       add      r12, rcx
       jo       G_M000_IG286
       test     rcx, rcx
       setg     dl
       movzx    rdx, dl
       mov      rdi, qword ptr [rbp+0x2D8]
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
       mov      qword ptr [rbp+0x2D8], rdi
       mov      qword ptr [rbp+0x2D0], rcx
       mov      gword ptr [rbp+0x210], r10
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
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
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
       mov      rax, gword ptr [rbp+0x268]
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
       mov      rax, gword ptr [rbp+0x268]
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
       mov      rax, gword ptr [rbp+0x268]
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
       mov      rbx, qword ptr [rbp+0x308]
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
       add      rsp, 0x7B8
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
 
; Total bytes of code 7525

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x105
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 75 inlinees with PGO data; 67 single block inlinees; 68 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x490
       mov      qword ptr [rsp+0x7F8], r15
       mov      qword ptr [rsp+0x7F0], r14
       mov      qword ptr [rsp+0x7E8], r13
       mov      qword ptr [rsp+0x7E0], r12
       mov      qword ptr [rsp+0x7D8], rbx
       lea      rbp, [rsp+0x490]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      rcx, qword ptr [rbp+0x2D0]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x2D0], rcx
       test     rcx, rcx
       setg     sil
       movzx    rsi, sil
       mov      dword ptr [rbp-0x428], esi
 
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
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG283
 
G_M000_IG10:
       test     r14, r14
       je       G_M000_IG55
 
G_M000_IG11:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG42
 
G_M000_IG12:
       mov      r9, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x40], r9
       test     rbx, rbx
       je       G_M000_IG36
 
G_M000_IG13:
       mov      qword ptr [rbp-0x430], r8
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG36
 
G_M000_IG14:
       mov      r10, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x50], r10
       lea      rdi, [rbp-0x68]
       mov      rdx, r9
       mulx     rdx, r11, r10
       mov      qword ptr [rdi], r11
       mov      r11, qword ptr [rbp-0x68]
       mov      rdi, r9
       sar      rdi, 63
       and      rdi, r10
       sub      rdx, rdi
       mov      rdi, r10
       sar      rdi, 63
       and      rdi, r9
       sub      rdx, rdi
       mov      rdi, r11
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG32
 
G_M000_IG15:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, r9
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x470], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x414], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      r9d, dword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x470]
       mov      rsi, rcx
       or       rsi, rax
       jne      G_M000_IG23
 
G_M000_IG16:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       movsxd   rsi, dword ptr [rbp-0x414]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r10d, dword ptr [rbp-0x80]
 
G_M000_IG17:
       mov      gword ptr [rbp-0x478], rax
       mov      ecx, r10d
       mov      dword ptr [rbp-0x418], ecx
       test     rax, rax
       jne      G_M000_IG366
 
G_M000_IG18:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG375
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      G_M000_IG30
 
G_M000_IG19:
       mov      edi, -1
 
G_M000_IG20:
       test     edi, edi
       jg       G_M000_IG31
 
G_M000_IG21:
       mov      rdi, qword ptr [rbp-0x430]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x458], rax
       mov      rdi, gword ptr [rbp-0x478]
       mov      esi, dword ptr [rbp-0x418]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rcx, gword ptr [rbp-0x458]
       mov      qword ptr [rcx+0x08], rax
 
G_M000_IG22:
       mov      rax, rcx
       jmp      G_M000_IG56
 
G_M000_IG23:
       test     rcx, rcx
       je       SHORT G_M000_IG28
 
G_M000_IG24:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG25:
       test     rax, rax
       je       SHORT G_M000_IG29
 
G_M000_IG26:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG27:
       mov      edx, dword ptr [rbp-0x414]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG17
 
G_M000_IG28:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG25
 
G_M000_IG29:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG27
 
G_M000_IG30:
       xor      edi, edi
       jmp      G_M000_IG20
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      gword ptr [rbp-0x450], rcx
       lea      rdi, bword ptr [rcx+0x08]
       mov      rsi, gword ptr [rbp-0x478]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x450]
       mov      edi, dword ptr [rbp-0x418]
       mov      dword ptr [rcx+0x10], edi
       jmp      G_M000_IG22
 
G_M000_IG32:
       mov      r8, qword ptr [rbp-0x430]
       mov      rdx, r11
       mov      qword ptr [rbp-0x90], rdx
       lea      r9, [rdx+0x05]
       cmp      r9, 261
       jbe      SHORT G_M000_IG35
 
G_M000_IG33:
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x90]
       mov      qword ptr [rax+0x08], rsi
       mov      rcx, rax
 
G_M000_IG34:
       jmp      G_M000_IG22
 
G_M000_IG35:
       test     r9, r9
       jl       G_M000_IG401
       mov      rdi, r9
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG400
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       mov      rcx, rdi
       jmp      SHORT G_M000_IG34
 
G_M000_IG36:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG39
 
G_M000_IG37:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       je       SHORT G_M000_IG39
 
G_M000_IG38:
       xor      rdi, rdi
 
G_M000_IG39:
       test     rdi, rdi
       je       G_M000_IG55
 
G_M000_IG40:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      edi, 1
       test     edi, edi
       je       G_M000_IG55
 
G_M000_IG41:
       mov      rax, rcx
       jmp      G_M000_IG56
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG52
 
G_M000_IG43:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG56
 
G_M000_IG46:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG49
 
G_M000_IG48:
       xor      rdi, rdi
 
G_M000_IG49:
       test     rdi, rdi
       je       SHORT G_M000_IG55
 
G_M000_IG50:
       mov      rsi, rbx
       mov      rdi, r8
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG55
 
G_M000_IG51:
       jmp      SHORT G_M000_IG56
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG55
 
G_M000_IG54:
       mov      rdi, qword ptr [rdi]
 
G_M000_IG55:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG56:
       mov      gword ptr [rbp+0x2A0], rax
       test     rax, rax
       je       G_M000_IG80
 
G_M000_IG57:
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG311
 
G_M000_IG58:
       mov      rdx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       test     r15, r15
       je       G_M000_IG73
 
G_M000_IG59:
       mov      qword ptr [rbp-0x430], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG73
 
G_M000_IG60:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xC8], rax
       lea      r8, [rdx+rax]
       mov      qword ptr [rbp-0xD0], r8
       mov      rdi, rdx
       xor      rdi, r8
       mov      rsi, rax
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG69
 
G_M000_IG61:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0x480], rax
       mov      ecx, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0x41C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       mov      rsi, qword ptr [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x480]
       mov      esi, dword ptr [rbp-0x41C]
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x488], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x420], edx
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG383
 
G_M000_IG62:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG392
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG67
 
G_M000_IG63:
       mov      edi, -1
 
G_M000_IG64:
       test     edi, edi
       jg       SHORT G_M000_IG68
 
G_M000_IG65:
       mov      rdi, qword ptr [rbp-0x430]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x468], rax
       mov      rdi, gword ptr [rbp-0x488]
       mov      esi, dword ptr [rbp-0x420]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rcx, gword ptr [rbp-0x468]
       mov      qword ptr [rcx+0x08], rax
 
G_M000_IG66:
       mov      rax, rcx
       jmp      G_M000_IG81
 
G_M000_IG67:
       xor      edi, edi
       jmp      SHORT G_M000_IG64
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      gword ptr [rbp-0x460], rcx
       lea      rdi, bword ptr [rcx+0x08]
       mov      rsi, gword ptr [rbp-0x488]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x460]
       mov      edi, dword ptr [rbp-0x420]
       mov      dword ptr [rcx+0x10], edi
       jmp      SHORT G_M000_IG66
 
G_M000_IG69:
       mov      rcx, qword ptr [rbp-0x430]
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0xD0]
       mov      qword ptr [rax+0x08], rsi
       mov      rcx, rax
 
G_M000_IG71:
       jmp      SHORT G_M000_IG66
 
G_M000_IG72:
       test     rax, rax
       jl       G_M000_IG401
       mov      rcx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rcx, 262
       jae      G_M000_IG400
       mov      rcx, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG71
 
G_M000_IG73:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       xor      rsi, rsi
 
G_M000_IG76:
       test     rsi, rsi
       je       G_M000_IG310
 
G_M000_IG77:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG79
 
G_M000_IG78:
       jmp      SHORT G_M000_IG81
 
G_M000_IG79:
       mov      rax, gword ptr [rbp+0x2A0]
 
G_M000_IG80:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG81:
       mov      gword ptr [rbp+0x298], rax
       test     rax, rax
       je       G_M000_IG117
 
G_M000_IG82:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG329
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG355
 
G_M000_IG84:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG117
 
G_M000_IG85:
       mov      rdx, rbx
       test     rdx, rdx
       je       G_M000_IG117
 
G_M000_IG86:
       mov      rdx, qword ptr [rdx]
       jmp      G_M000_IG117
 
G_M000_IG87:
       mov      rdx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1A0], rdx
       test     r15, r15
       je       G_M000_IG98
 
G_M000_IG88:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG98
 
G_M000_IG89:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x1B0], rax
       lea      r8, [rdx+rax]
       mov      qword ptr [rbp-0x1B8], r8
       mov      rdi, rdx
       xor      rdi, r8
       mov      rsi, rax
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG94
 
G_M000_IG90:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
       lea      rdi, [rbp-0x1C8]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG91:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1C8]
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
 
G_M000_IG92:
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
 
G_M000_IG93:
       jmp      G_M000_IG158
 
G_M000_IG94:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG97
 
G_M000_IG95:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG96:
       jmp      SHORT G_M000_IG93
 
G_M000_IG97:
       test     rax, rax
       jl       G_M000_IG401
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG400
       test     rax, rax
       jl       G_M000_IG401
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG96
 
G_M000_IG98:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG101
 
G_M000_IG99:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       xor      rsi, rsi
 
G_M000_IG101:
       test     rsi, rsi
       je       SHORT G_M000_IG104
 
G_M000_IG102:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       G_M000_IG156
 
G_M000_IG103:
       jmp      G_M000_IG158
 
G_M000_IG104:
       mov      rax, gword ptr [rbp+0x290]
       jmp      G_M000_IG157
 
G_M000_IG105:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       test     r15, r15
       je       SHORT G_M000_IG108
 
G_M000_IG106:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG108
 
G_M000_IG107:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG158
 
G_M000_IG108:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       xor      rsi, rsi
 
G_M000_IG111:
       test     rsi, rsi
       je       SHORT G_M000_IG115
 
G_M000_IG112:
       mov      rsi, r15
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x208], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       jmp      G_M000_IG158
 
G_M000_IG114:
       mov      rax, gword ptr [rbp+0x290]
       jmp      G_M000_IG157
 
G_M000_IG115:
       mov      rax, gword ptr [rbp+0x290]
       jmp      G_M000_IG157
 
G_M000_IG116:
       mov      rax, gword ptr [rbp+0x298]
 
G_M000_IG117:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG118:
       mov      gword ptr [rbp+0x290], rax
       test     rax, rax
       je       G_M000_IG157
 
G_M000_IG119:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG87
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG105
 
G_M000_IG121:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG157
 
G_M000_IG122:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG125
 
G_M000_IG123:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG125
 
G_M000_IG124:
       xor      rsi, rsi
 
G_M000_IG125:
       test     rsi, rsi
       je       G_M000_IG157
 
G_M000_IG126:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG158
 
G_M000_IG127:
       mov      r8, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x220], r8
       test     rbx, rbx
       je       G_M000_IG138
 
G_M000_IG128:
       mov      qword ptr [rbp-0x430], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG138
 
G_M000_IG129:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x230], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x238], rdx
       lea      rdx, [rbp-0x238]
       mov      rdi, r8
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x238]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG134
 
G_M000_IG130:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x248], xmm0
       lea      rdi, [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG131:
       vmovdqu  xmm0, xmmword ptr [rbp-0x248]
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
 
G_M000_IG132:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x278], xmm0
       lea      rdi, [rbp-0x278]
       mov      rsi, qword ptr [rbp-0x230]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x258]
       mov      rsi, qword ptr [rbp-0x250]
       mov      rdx, gword ptr [rbp-0x278]
       mov      rcx, qword ptr [rbp-0x270]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x268], rax
       mov      qword ptr [rbp-0x260], rdx
       mov      rdi, gword ptr [rbp-0x268]
       mov      rsi, qword ptr [rbp-0x260]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG133:
       jmp      G_M000_IG195
 
G_M000_IG134:
       mov      rax, qword ptr [rbp-0x238]
       mov      qword ptr [rbp-0x280], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG137
 
G_M000_IG135:
       mov      rdi, qword ptr [rbp-0x430]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x280]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG136:
       jmp      SHORT G_M000_IG133
 
G_M000_IG137:
       test     rcx, rcx
       jl       G_M000_IG401
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG400
       test     rcx, rcx
       jl       G_M000_IG401
       mov      rsi, rcx
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG136
 
G_M000_IG138:
       mov      rsi, rbx
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
       je       SHORT G_M000_IG144
 
G_M000_IG142:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x228], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       G_M000_IG193
 
G_M000_IG143:
       jmp      G_M000_IG195
 
G_M000_IG144:
       mov      rax, gword ptr [rbp+0x288]
       jmp      G_M000_IG194
 
G_M000_IG145:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG148
 
G_M000_IG146:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG148
 
G_M000_IG147:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x210]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG195
 
G_M000_IG148:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG151
 
G_M000_IG150:
       xor      rsi, rsi
 
G_M000_IG151:
       test     rsi, rsi
       je       SHORT G_M000_IG155
 
G_M000_IG152:
       mov      rsi, rbx
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x290], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x290]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG154
 
G_M000_IG153:
       jmp      G_M000_IG195
 
G_M000_IG154:
       mov      rax, gword ptr [rbp+0x288]
       jmp      G_M000_IG194
 
G_M000_IG155:
       mov      rax, gword ptr [rbp+0x288]
       jmp      G_M000_IG194
 
G_M000_IG156:
       mov      rax, gword ptr [rbp+0x290]
 
G_M000_IG157:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG158:
       mov      gword ptr [rbp+0x288], rax
       test     rax, rax
       je       G_M000_IG194
 
G_M000_IG159:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG127
 
G_M000_IG160:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG145
 
G_M000_IG161:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG194
 
G_M000_IG162:
       mov      rdx, rbx
       test     rdx, rdx
       je       G_M000_IG194
 
G_M000_IG163:
       mov      rdx, qword ptr [rdx]
       jmp      G_M000_IG194
 
G_M000_IG164:
       mov      rdx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x2A8], rdx
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG165:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG175
 
G_M000_IG166:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x2B8], rax
       lea      r8, [rdx+rax]
       mov      qword ptr [rbp-0x2C0], r8
       mov      rdi, rdx
       xor      rdi, r8
       mov      rsi, rax
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG171
 
G_M000_IG167:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2D0], xmm0
       lea      rdi, [rbp-0x2D0]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG168:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2D0]
       vmovdqu32 xmmword ptr [rbp-0x2E0], xmm0
 
G_M000_IG169:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x300], xmm0
       lea      rdi, [rbp-0x300]
       mov      rsi, qword ptr [rbp-0x2B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2E0]
       mov      rsi, qword ptr [rbp-0x2D8]
       mov      rdx, gword ptr [rbp-0x300]
       mov      rcx, qword ptr [rbp-0x2F8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2F0], rax
       mov      qword ptr [rbp-0x2E8], rdx
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rsi, qword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG170:
       jmp      G_M000_IG283
 
G_M000_IG171:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG174
 
G_M000_IG172:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x2C0]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG173:
       jmp      SHORT G_M000_IG170
 
G_M000_IG174:
       test     rax, rax
       jl       G_M000_IG401
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG400
       test     rax, rax
       jl       G_M000_IG401
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG173
 
G_M000_IG175:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG178
 
G_M000_IG176:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG178
 
G_M000_IG177:
       xor      rsi, rsi
 
G_M000_IG178:
       test     rsi, rsi
       je       SHORT G_M000_IG181
 
G_M000_IG179:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2B0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x2A8]
       vmovsd   qword ptr [rbp-0x308], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x308]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       G_M000_IG281
 
G_M000_IG180:
       jmp      G_M000_IG283
 
G_M000_IG181:
       mov      rax, gword ptr [rbp+0x280]
       jmp      G_M000_IG282
 
G_M000_IG182:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x298], xmm0
       test     r15, r15
       je       SHORT G_M000_IG185
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG185
 
G_M000_IG184:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2A0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x298]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG283
 
G_M000_IG185:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG188
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG188
 
G_M000_IG187:
       xor      rsi, rsi
 
G_M000_IG188:
       test     rsi, rsi
       je       SHORT G_M000_IG192
 
G_M000_IG189:
       mov      rsi, r15
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x310], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG191
 
G_M000_IG190:
       jmp      G_M000_IG283
 
G_M000_IG191:
       mov      rax, gword ptr [rbp+0x280]
       jmp      G_M000_IG282
 
G_M000_IG192:
       mov      rax, gword ptr [rbp+0x280]
       jmp      G_M000_IG282
 
G_M000_IG193:
       mov      rax, gword ptr [rbp+0x288]
 
G_M000_IG194:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG195:
       mov      gword ptr [rbp+0x280], rax
       test     rax, rax
       je       G_M000_IG282
 
G_M000_IG196:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG164
 
G_M000_IG197:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG182
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG282
 
G_M000_IG199:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG202
 
G_M000_IG200:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG202
 
G_M000_IG201:
       xor      rsi, rsi
 
G_M000_IG202:
       test     rsi, rsi
       je       G_M000_IG282
 
G_M000_IG203:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG283
 
G_M000_IG204:
       mov      rcx, qword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG284
 
G_M000_IG205:
       test     rbx, rbx
       je       G_M000_IG284
 
G_M000_IG206:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG284
 
G_M000_IG207:
       test     r15, r15
       je       G_M000_IG284
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG284
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG296
 
G_M000_IG210:
       mov      rcx, qword ptr [r13]
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      rcx, r8
       jne      G_M000_IG228
 
G_M000_IG211:
       mov      rcx, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x328], rcx
       test     rbx, rbx
       je       G_M000_IG222
 
G_M000_IG212:
       mov      qword ptr [rbp-0x430], r8
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG222
 
G_M000_IG213:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x338], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x340], rdx
       lea      rdx, [rbp-0x340]
       mov      rdi, rcx
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x340]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG218
 
G_M000_IG214:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x350], xmm0
       lea      rdi, [rbp-0x350]
       mov      rsi, qword ptr [rbp-0x328]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG215:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x350]
       vmovdqu32 xmmword ptr [rbp-0x360], xmm0
 
G_M000_IG216:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x380], xmm0
       lea      rdi, [rbp-0x380]
       mov      rsi, qword ptr [rbp-0x338]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x360]
       mov      rsi, qword ptr [rbp-0x358]
       mov      rdx, gword ptr [rbp-0x380]
       mov      rcx, qword ptr [rbp-0x378]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x370], rax
       mov      qword ptr [rbp-0x368], rdx
       mov      rdi, gword ptr [rbp-0x370]
       mov      rsi, qword ptr [rbp-0x368]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG217:
       jmp      G_M000_IG286
 
G_M000_IG218:
       mov      rax, qword ptr [rbp-0x340]
       mov      qword ptr [rbp-0x388], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG221
 
G_M000_IG219:
       mov      rdi, qword ptr [rbp-0x430]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x388]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG220:
       jmp      SHORT G_M000_IG217
 
G_M000_IG221:
       test     rcx, rcx
       jl       G_M000_IG401
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rdi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG400
       test     rcx, rcx
       jl       G_M000_IG401
       mov      rsi, rcx
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG220
 
G_M000_IG222:
       mov      rsi, rbx
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
       je       G_M000_IG285
 
G_M000_IG226:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x330], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x328]
       vmovsd   qword ptr [rbp-0x390], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x390]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x330]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       G_M000_IG285
 
G_M000_IG227:
       jmp      G_M000_IG286
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG238
 
G_M000_IG229:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG232
 
G_M000_IG230:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG232
 
G_M000_IG231:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x320], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x318]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x320]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG286
 
G_M000_IG232:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG235
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG235
 
G_M000_IG234:
       xor      rsi, rsi
 
G_M000_IG235:
       test     rsi, rsi
       je       G_M000_IG285
 
G_M000_IG236:
       mov      rsi, rbx
       mov      rdi, r8
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x398], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x398]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       G_M000_IG285
 
G_M000_IG237:
       jmp      G_M000_IG286
 
G_M000_IG238:
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       jne      G_M000_IG285
 
G_M000_IG239:
       mov      rdx, rbx
       test     rdx, rdx
       je       G_M000_IG285
 
G_M000_IG240:
       mov      rdx, qword ptr [rdx]
       jmp      G_M000_IG285
 
G_M000_IG241:
       mov      rdx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x3B8], rdx
       test     r15, r15
       je       G_M000_IG252
 
G_M000_IG242:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG252
 
G_M000_IG243:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x3C8], rax
       lea      r8, [rdx+rax]
       mov      qword ptr [rbp-0x3D0], r8
       mov      rdi, rdx
       xor      rdi, r8
       mov      rsi, rax
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG248
 
G_M000_IG244:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3E0], xmm0
       lea      rdi, [rbp-0x3E0]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG245:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3E0]
       vmovdqu32 xmmword ptr [rbp-0x3F0], xmm0
 
G_M000_IG246:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x410], xmm0
       lea      rdi, [rbp-0x410]
       mov      rsi, qword ptr [rbp-0x3C8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3F0]
       mov      rsi, qword ptr [rbp-0x3E8]
       mov      rdx, gword ptr [rbp-0x410]
       mov      rcx, qword ptr [rbp-0x408]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x400], rax
       mov      qword ptr [rbp-0x3F8], rdx
       mov      rdi, gword ptr [rbp-0x400]
       mov      rsi, qword ptr [rbp-0x3F8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG247:
       mov      rdx, rax
       jmp      G_M000_IG293
 
G_M000_IG248:
       lea      rax, [r8+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG251
 
G_M000_IG249:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x3D0]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG250:
       jmp      SHORT G_M000_IG247
 
G_M000_IG251:
       test     rax, rax
       jl       G_M000_IG401
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG400
       test     rax, rax
       jl       G_M000_IG401
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG250
 
G_M000_IG252:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG255
 
G_M000_IG253:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG255
 
G_M000_IG254:
       xor      rsi, rsi
 
G_M000_IG255:
       test     rsi, rsi
       je       SHORT G_M000_IG258
 
G_M000_IG256:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm1, qword ptr [rax]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rbp-0x3B8]
       lea      rsi, [rbp-0x3C0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x270]
       je       G_M000_IG292
 
G_M000_IG257:
       mov      rdx, gword ptr [rbp-0x3C0]
       jmp      G_M000_IG293
 
G_M000_IG258:
       mov      rax, gword ptr [rbp+0x270]
       jmp      G_M000_IG292
 
G_M000_IG259:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x3A0], xmm0
       test     r15, r15
       je       SHORT G_M000_IG262
 
G_M000_IG260:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r15], rsi
       jne      SHORT G_M000_IG262
 
G_M000_IG261:
       vmovsd   xmm1, qword ptr [r15+0x08]
       lea      rsi, [rbp-0x3B0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x270]
       je       G_M000_IG292
       mov      rdx, gword ptr [rbp-0x3B0]
       jmp      G_M000_IG293
 
G_M000_IG262:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG265
 
G_M000_IG263:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG265
 
G_M000_IG264:
       xor      rsi, rsi
 
G_M000_IG265:
       test     rsi, rsi
       je       SHORT G_M000_IG268
 
G_M000_IG266:
       mov      rsi, r15
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax]
       lea      rsi, [rbp-0x3A8]
       vmovsd   xmm0, qword ptr [rbp-0x3A0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x270]
       je       G_M000_IG292
 
G_M000_IG267:
       mov      rdx, gword ptr [rbp-0x3A8]
       jmp      G_M000_IG293
 
G_M000_IG268:
       mov      rax, gword ptr [rbp+0x270]
       jmp      G_M000_IG292
 
G_M000_IG269:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG272
 
G_M000_IG270:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG272
 
G_M000_IG271:
       xor      rsi, rsi
 
G_M000_IG272:
       test     rsi, rsi
       je       G_M000_IG292
 
G_M000_IG273:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
       jmp      G_M000_IG293
 
G_M000_IG274:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG277
 
G_M000_IG275:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG400
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG277
 
G_M000_IG276:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG400
       mov      dword ptr [rcx+4*rdx+0x10], 11
 
G_M000_IG277:
       mov      rbx, qword ptr [rbp+0x308]
       test     rbx, rbx
       jle      SHORT G_M000_IG280
 
G_M000_IG278:
       movsxd   rcx, edi
       cmp      rbx, rcx
       jg       SHORT G_M000_IG280
 
G_M000_IG279:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG280:
       add      rsp, 0x7D8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG281:
       mov      rax, gword ptr [rbp+0x280]
 
G_M000_IG282:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG283:
       mov      gword ptr [rbp+0x278], rax
       mov      rdi, r13
       test     rdi, rdi
       jne      G_M000_IG204
 
G_M000_IG284:
       test     r13, r13
       jne      G_M000_IG210
 
G_M000_IG285:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG286:
       mov      gword ptr [rbp+0x270], rax
       xor      rdi, rdi
       mov      gword ptr [rbp-0x3C0], rdi
 
G_M000_IG287:
       mov      gword ptr [rbp-0x3B0], rdi
 
G_M000_IG288:
       mov      gword ptr [rbp-0x3A8], rdi
       test     rax, rax
       je       SHORT G_M000_IG292
 
G_M000_IG289:
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      qword ptr [rax], rcx
       je       G_M000_IG241
 
G_M000_IG290:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       G_M000_IG259
 
G_M000_IG291:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG269
 
G_M000_IG292:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG293:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x3C0], rcx
 
G_M000_IG294:
       mov      gword ptr [rbp-0x3B0], rcx
 
G_M000_IG295:
       mov      gword ptr [rbp-0x3A8], rcx
       mov      rcx, rbx
       mov      edi, 8
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
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
 
G_M000_IG296:
       mov      gword ptr [rbp+0x248], rax
 
G_M000_IG297:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x438], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x438]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
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
       add      r12, qword ptr [rbp+0x2D0]
       jo       G_M000_IG401
       mov      edx, dword ptr [rbp-0x428]
       mov      edi, edx
       mov      rsi, qword ptr [rbp+0x2D8]
       cmp      r12, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       mov      qword ptr [rbp+0x2D8], rsi
       cmp      r12, rsi
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG274
 
G_M000_IG298:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG301
 
G_M000_IG299:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      r8, rax
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG400
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG301
 
G_M000_IG300:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG400
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 10
 
G_M000_IG301:
       test     r14, r14
       je       G_M000_IG03
 
G_M000_IG302:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG03
 
G_M000_IG303:
       test     rbx, rbx
       je       G_M000_IG03
 
G_M000_IG304:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG03
 
G_M000_IG305:
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG306:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG307:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG308:
       mov      rdi, qword ptr [r13]
       mov      r8, 0xD1FFAB1E
       cmp      rdi, r8
       jne      G_M000_IG03
 
G_M000_IG309:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   xmm3, qword ptr [r13+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rsi+0x08], xmm1
       mov      gword ptr [rbp+0x278], rsi
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      rax, rsi
       mov      gword ptr [rbp+0x248], rax
       jmp      G_M000_IG297
 
G_M000_IG310:
       mov      rax, gword ptr [rbp+0x2A0]
       jmp      G_M000_IG80
 
G_M000_IG311:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG323
 
G_M000_IG312:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       test     r15, r15
       je       SHORT G_M000_IG315
 
G_M000_IG313:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG315
 
G_M000_IG314:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG81
 
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
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x100], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG321
 
G_M000_IG320:
       jmp      G_M000_IG81
 
G_M000_IG321:
       mov      rax, gword ptr [rbp+0x2A0]
       jmp      G_M000_IG80
 
G_M000_IG322:
       mov      rax, gword ptr [rbp+0x2A0]
       jmp      G_M000_IG80
 
G_M000_IG323:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG80
 
G_M000_IG324:
       mov      rdi, rax
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG327
 
G_M000_IG325:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG327
 
G_M000_IG326:
       xor      rsi, rsi
 
G_M000_IG327:
       test     rsi, rsi
       je       G_M000_IG80
 
G_M000_IG328:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      G_M000_IG81
 
G_M000_IG329:
       mov      r8, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x118], r8
       test     rbx, rbx
       je       G_M000_IG348
 
G_M000_IG330:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG348
 
G_M000_IG331:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x128], rax
       lea      rdi, [rbp-0x140]
       mov      rdx, r8
       mulx     rdx, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x140]
       mov      rdi, r8
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, r8
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG344
 
G_M000_IG332:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, r8
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x490], rax
       mov      ecx, dword ptr [rbp-0x130]
       mov      dword ptr [rbp-0x424], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
       lea      rdi, [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x128]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x150]
       mov      r9d, dword ptr [rbp-0x148]
       mov      rcx, gword ptr [rbp-0x490]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG337
 
G_M000_IG333:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x170], xmm0
       movsxd   rsi, dword ptr [rbp-0x424]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG334:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x170]
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
 
G_M000_IG335:
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, qword ptr [rbp-0x158]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG336:
       jmp      G_M000_IG118
 
G_M000_IG337:
       test     rcx, rcx
       je       SHORT G_M000_IG342
 
G_M000_IG338:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG339:
       test     rax, rax
       je       SHORT G_M000_IG343
 
G_M000_IG340:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG341:
       mov      edx, dword ptr [rbp-0x424]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x160], rax
       mov      qword ptr [rbp-0x158], rdx
       jmp      SHORT G_M000_IG335
 
G_M000_IG342:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG339
 
G_M000_IG343:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG341
 
G_M000_IG344:
       mov      rax, r9
       mov      qword ptr [rbp-0x178], rax
       lea      rdx, [rax+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG347
 
G_M000_IG345:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x178]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG346:
       jmp      SHORT G_M000_IG336
 
G_M000_IG347:
       test     rdx, rdx
       jl       G_M000_IG401
       mov      rax, rdx
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG400
       test     rdx, rdx
       jl       G_M000_IG401
       mov      rdi, rdx
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG346
 
G_M000_IG348:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG351
 
G_M000_IG349:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG351
 
G_M000_IG350:
       xor      rsi, rsi
 
G_M000_IG351:
       test     rsi, rsi
       je       SHORT G_M000_IG354
 
G_M000_IG352:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x120], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rbp-0x180], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x120]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       G_M000_IG116
 
G_M000_IG353:
       jmp      G_M000_IG118
 
G_M000_IG354:
       mov      rax, gword ptr [rbp+0x298]
       jmp      G_M000_IG117
 
G_M000_IG355:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG358
 
G_M000_IG356:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG358
 
G_M000_IG357:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x108]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG118
 
G_M000_IG358:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG361
 
G_M000_IG359:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG361
 
G_M000_IG360:
       xor      rsi, rsi
 
G_M000_IG361:
       test     rsi, rsi
       je       SHORT G_M000_IG365
 
G_M000_IG362:
       mov      rsi, rbx
       mov      rdi, rcx
       call     [CORINFO_HELP_UNBOX]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x188], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG364
 
G_M000_IG363:
       jmp      G_M000_IG118
 
G_M000_IG364:
       mov      rax, gword ptr [rbp+0x298]
       jmp      G_M000_IG117
 
G_M000_IG365:
       mov      rax, gword ptr [rbp+0x298]
       jmp      G_M000_IG117
 
G_M000_IG366:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG367
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG368
 
G_M000_IG367:
       mov      edx, ecx
       jmp      SHORT G_M000_IG374
 
G_M000_IG368:
       cmp      edi, 2
       je       SHORT G_M000_IG369
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG400
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG370
 
G_M000_IG369:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG370:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG371
       mov      edx, -1
       jmp      SHORT G_M000_IG373
 
G_M000_IG371:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG372
       mov      edx, 1
       jmp      SHORT G_M000_IG373
 
G_M000_IG372:
       xor      edx, edx
 
G_M000_IG373:
       imul     edx, ecx
 
G_M000_IG374:
       test     edx, edx
       jge      G_M000_IG18
       jmp      G_M000_IG31
 
G_M000_IG375:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG376
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG377
 
G_M000_IG376:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x418]
       jmp      G_M000_IG20
 
G_M000_IG377:
       cmp      edi, 2
       je       SHORT G_M000_IG378
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG400
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG379
 
G_M000_IG378:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x478]
 
G_M000_IG379:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG380
       mov      edx, -1
       jmp      SHORT G_M000_IG382
 
G_M000_IG380:
       test     rdi, rdi
       jge      SHORT G_M000_IG381
       mov      edx, 1
       jmp      SHORT G_M000_IG382
 
G_M000_IG381:
       xor      edx, edx
 
G_M000_IG382:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG20
 
G_M000_IG383:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG384
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG385
 
G_M000_IG384:
       mov      edx, ecx
       jmp      SHORT G_M000_IG391
 
G_M000_IG385:
       cmp      edi, 2
       je       SHORT G_M000_IG386
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG400
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG387
 
G_M000_IG386:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG387:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG388
       mov      edx, -1
       jmp      SHORT G_M000_IG390
 
G_M000_IG388:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG389
       mov      edx, 1
       jmp      SHORT G_M000_IG390
 
G_M000_IG389:
       xor      edx, edx
 
G_M000_IG390:
       imul     edx, ecx
 
G_M000_IG391:
       test     edx, edx
       jge      G_M000_IG62
       jmp      G_M000_IG68
 
G_M000_IG392:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG393
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG394
 
G_M000_IG393:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x420]
       jmp      G_M000_IG64
 
G_M000_IG394:
       cmp      edi, 2
       je       SHORT G_M000_IG395
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG400
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG396
 
G_M000_IG395:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x488]
 
G_M000_IG396:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG397
       mov      edx, -1
       jmp      SHORT G_M000_IG399
 
G_M000_IG397:
       test     rdi, rdi
       jge      SHORT G_M000_IG398
       mov      edx, 1
       jmp      SHORT G_M000_IG399
 
G_M000_IG398:
       xor      edx, edx
 
G_M000_IG399:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG64
 
G_M000_IG400:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG401:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8785

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2f6
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 105 inlinees with PGO data; 13 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x570
       mov      qword ptr [rsp+0x8D8], r15
       mov      qword ptr [rsp+0x8D0], r14
       mov      qword ptr [rsp+0x8C8], r13
       mov      qword ptr [rsp+0x8C0], r12
       mov      qword ptr [rsp+0x8B8], rbx
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
       mov      r14, gword ptr [rbp+0x338]
       mov      rbx, gword ptr [rbp+0x330]
       mov      r15, gword ptr [rbp+0x328]
       mov      r13, gword ptr [rbp+0x320]
       mov      r12, qword ptr [rbp+0x2E0]
       mov      r9, qword ptr [rbp+0x2D0]
       mov      rax, gword ptr [rbp+0x120]
       mov      r8, gword ptr [rbp+0x100]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x2D0], r9
       test     r9, r9
       setg     r11b
       movzx    r11, r11b
       mov      dword ptr [rbp-0x524], r11d
 
G_M000_IG03:
       test     r8, r8
       je       G_M000_IG308
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x100], r8
       cmp      qword ptr [r8], rsi
       jne      G_M000_IG308
 
G_M000_IG05:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       mov      gword ptr [rbp+0x120], rax
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       mov      rax, gword ptr [rbp+0x120]
 
G_M000_IG07:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp+0x118], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rbp+0x110]
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       mov      gword ptr [rbp+0x110], rdx
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       mov      rdx, gword ptr [rbp+0x110]
 
G_M000_IG09:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       mov      r8, gword ptr [rbp+0x100]
       vmovsd   xmm2, qword ptr [r8+0x08]
       vmulsd   xmm0, xmm1, qword ptr [rbp+0x118]
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0xC8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG160
 
G_M000_IG10:
       mov      rax, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x70], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG339
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x70]
       mov      qword ptr [rax+0x08], rsi
       mov      rsi, rax
 
G_M000_IG11:
       jmp      G_M000_IG314
 
G_M000_IG12:
       mov      rsi, rbx
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
       je       G_M000_IG26
 
G_M000_IG16:
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
       mov      rsi, rax
       jmp      G_M000_IG27
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG26
 
G_M000_IG18:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG21:
       mov      rsi, rbx
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
       mov      rsi, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG27:
       test     rsi, rsi
       je       G_M000_IG54
 
G_M000_IG28:
       mov      rax, qword ptr [rsi]
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      rax, rcx
       jne      G_M000_IG39
 
G_M000_IG29:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x98], rax
       test     r15, r15
       je       SHORT G_M000_IG34
 
G_M000_IG30:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xA8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0xB0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       G_M000_IG340
       lea      rax, [rsi+0x05]
       cmp      rax, 261
       jbe      G_M000_IG341
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG32:
       mov      rsi, rax
 
G_M000_IG33:
       jmp      G_M000_IG55
 
G_M000_IG34:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       xor      rdi, rdi
 
G_M000_IG37:
       test     rdi, rdi
       je       G_M000_IG54
 
G_M000_IG38:
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
       mov      rsi, rax
       jmp      G_M000_IG55
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG48
 
G_M000_IG40:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     r15, r15
       je       SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG43
 
G_M000_IG42:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG55
 
G_M000_IG43:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       xor      rdi, rdi
 
G_M000_IG46:
       test     rdi, rdi
       je       G_M000_IG54
 
G_M000_IG47:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG55
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG54
 
G_M000_IG49:
       mov      rdi, rsi
       mov      rax, r15
       test     rax, rax
       je       SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       xor      rax, rax
 
G_M000_IG52:
       test     rax, rax
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       mov      rsi, rax
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG55
 
G_M000_IG54:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG55:
       test     rsi, rsi
       je       G_M000_IG78
 
G_M000_IG56:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG69
 
G_M000_IG57:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0xE8], rax
       test     rbx, rbx
       je       G_M000_IG64
 
G_M000_IG58:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG64
 
G_M000_IG59:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0xF8], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x100], rdx
       lea      rdx, [rbp-0x100]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x100]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG62
 
G_M000_IG60:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x568], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x51C], ecx
       mov      rdi, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x568]
       mov      esi, dword ptr [rbp-0x51C]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG61:
       jmp      G_M000_IG79
 
G_M000_IG62:
       mov      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x118], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG342
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rsi
       mov      rsi, rax
 
G_M000_IG63:
       jmp      SHORT G_M000_IG61
 
G_M000_IG64:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG67
 
G_M000_IG65:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG67
 
G_M000_IG66:
       xor      rdi, rdi
 
G_M000_IG67:
       test     rdi, rdi
       je       G_M000_IG78
 
G_M000_IG68:
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
       mov      rsi, rax
       jmp      G_M000_IG79
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG78
 
G_M000_IG70:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG73
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG73
 
G_M000_IG72:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG79
 
G_M000_IG73:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       xor      rdi, rdi
 
G_M000_IG76:
       test     rdi, rdi
       je       SHORT G_M000_IG78
 
G_M000_IG77:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x128], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x128]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG79
 
G_M000_IG78:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG79:
       test     rsi, rsi
       je       G_M000_IG106
 
G_M000_IG80:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG91
 
G_M000_IG81:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x140], rax
       test     r15, r15
       je       SHORT G_M000_IG86
 
G_M000_IG82:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG83:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x150], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x158], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       G_M000_IG343
       lea      rax, [rsi+0x05]
       cmp      rax, 261
       jbe      G_M000_IG344
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x158]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG84:
       mov      rsi, rax
 
G_M000_IG85:
       jmp      G_M000_IG107
 
G_M000_IG86:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG89
 
G_M000_IG87:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG89
 
G_M000_IG88:
       xor      rdi, rdi
 
G_M000_IG89:
       test     rdi, rdi
       je       G_M000_IG106
 
G_M000_IG90:
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
       mov      rsi, rax
       jmp      G_M000_IG107
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG100
 
G_M000_IG92:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm0
       test     r15, r15
       je       SHORT G_M000_IG95
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG95
 
G_M000_IG94:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x130]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x138]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG107
 
G_M000_IG95:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG98
 
G_M000_IG96:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       xor      rdi, rdi
 
G_M000_IG98:
       test     rdi, rdi
       je       G_M000_IG106
 
G_M000_IG99:
       mov      rsi, r15
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG100:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG106
 
G_M000_IG101:
       mov      rdi, rsi
       mov      rax, r15
       test     rax, rax
       je       SHORT G_M000_IG104
 
G_M000_IG102:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG104
 
G_M000_IG103:
       xor      rax, rax
 
G_M000_IG104:
       test     rax, rax
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       mov      rsi, rax
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG106:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG107:
       test     rsi, rsi
       je       G_M000_IG132
 
G_M000_IG108:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG123
 
G_M000_IG109:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x190], rax
       test     rbx, rbx
       je       G_M000_IG118
 
G_M000_IG110:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG118
 
G_M000_IG111:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x1A0], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x1A8], rdx
       lea      rdx, [rbp-0x1A8]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x1A8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG116
 
G_M000_IG112:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       mov      rsi, qword ptr [rbp-0x190]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG113:
       vmovdqu  xmm0, xmmword ptr [rbp-0x1B8]
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
 
G_M000_IG114:
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
       mov      rsi, rax
 
G_M000_IG115:
       jmp      G_M000_IG133
 
G_M000_IG116:
       mov      rax, qword ptr [rbp-0x1A8]
       mov      qword ptr [rbp-0x1F0], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG345
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x1F0]
       mov      qword ptr [rax+0x08], rsi
       mov      rsi, rax
 
G_M000_IG117:
       jmp      SHORT G_M000_IG115
 
G_M000_IG118:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG121
 
G_M000_IG119:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG121
 
G_M000_IG120:
       xor      rdi, rdi
 
G_M000_IG121:
       test     rdi, rdi
       je       G_M000_IG132
 
G_M000_IG122:
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
       mov      rsi, rax
       jmp      G_M000_IG133
 
G_M000_IG123:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG132
 
G_M000_IG124:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG127
 
G_M000_IG125:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG127
 
G_M000_IG126:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG133
 
G_M000_IG127:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG130
 
G_M000_IG128:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG130
 
G_M000_IG129:
       xor      rdi, rdi
 
G_M000_IG130:
       test     rdi, rdi
       je       SHORT G_M000_IG132
 
G_M000_IG131:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x200], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x200]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG133
 
G_M000_IG132:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG133:
       test     rsi, rsi
       je       G_M000_IG159
 
G_M000_IG134:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG144
 
G_M000_IG135:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x218], rax
       test     r15, r15
       je       SHORT G_M000_IG139
 
G_M000_IG136:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG139
 
G_M000_IG137:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x228], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x230], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       G_M000_IG346
       lea      rax, [rsi+0x05]
       cmp      rax, 261
       jbe      G_M000_IG349
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x230]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG138:
       jmp      G_M000_IG160
 
G_M000_IG139:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG142
 
G_M000_IG140:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG142
 
G_M000_IG141:
       xor      rdi, rdi
 
G_M000_IG142:
       test     rdi, rdi
       je       G_M000_IG159
 
G_M000_IG143:
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
       jmp      G_M000_IG160
 
G_M000_IG144:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG153
 
G_M000_IG145:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       test     r15, r15
       je       SHORT G_M000_IG148
 
G_M000_IG146:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG148
 
G_M000_IG147:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG160
 
G_M000_IG148:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG151
 
G_M000_IG150:
       xor      rdi, rdi
 
G_M000_IG151:
       test     rdi, rdi
       je       SHORT G_M000_IG159
 
G_M000_IG152:
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
       jmp      SHORT G_M000_IG160
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG159
 
G_M000_IG154:
       mov      rdi, rsi
       mov      rax, r15
       test     rax, rax
       je       SHORT G_M000_IG157
 
G_M000_IG155:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG157
 
G_M000_IG156:
       xor      rax, rax
 
G_M000_IG157:
       test     rax, rax
       je       SHORT G_M000_IG159
 
G_M000_IG158:
       mov      rsi, rax
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG160
 
G_M000_IG159:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG160:
       mov      gword ptr [rbp+0x278], rax
       test     r13, r13
       je       G_M000_IG167
 
G_M000_IG161:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG167
 
G_M000_IG162:
       test     rbx, rbx
       je       SHORT G_M000_IG168
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG168
 
G_M000_IG164:
       test     r15, r15
       je       SHORT G_M000_IG168
 
G_M000_IG165:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG168
 
G_M000_IG166:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp+0x58], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG298
 
G_M000_IG167:
       test     r13, r13
       je       G_M000_IG192
 
G_M000_IG168:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG183
 
G_M000_IG169:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x298], r8
       test     rbx, rbx
       je       G_M000_IG178
 
G_M000_IG170:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG178
 
G_M000_IG171:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x2A8], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x2B0], rdx
       lea      rdx, [rbp-0x2B0]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x2B0]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG176
 
G_M000_IG172:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
       lea      rdi, [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG173:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2C0]
       vmovdqu32 xmmword ptr [rbp-0x2D0], xmm0
 
G_M000_IG174:
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
       mov      rsi, rax
 
G_M000_IG175:
       jmp      G_M000_IG193
 
G_M000_IG176:
       mov      rax, qword ptr [rbp-0x2B0]
       mov      qword ptr [rbp-0x2F8], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG350
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x2F8]
       mov      qword ptr [rax+0x08], rsi
       mov      rsi, rax
 
G_M000_IG177:
       jmp      SHORT G_M000_IG175
 
G_M000_IG178:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG181
 
G_M000_IG179:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG181
 
G_M000_IG180:
       xor      rsi, rsi
 
G_M000_IG181:
       test     rsi, rsi
       je       G_M000_IG192
 
G_M000_IG182:
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
       mov      rsi, rax
       jmp      G_M000_IG193
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG192
 
G_M000_IG184:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG187
 
G_M000_IG185:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG187
 
G_M000_IG186:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG193
 
G_M000_IG187:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG190
 
G_M000_IG188:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG190
 
G_M000_IG189:
       xor      rsi, rsi
 
G_M000_IG190:
       test     rsi, rsi
       je       SHORT G_M000_IG192
 
G_M000_IG191:
       mov      rsi, rbx
       mov      rdi, rcx
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x308], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x308]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG193
 
G_M000_IG192:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG193:
       test     rsi, rsi
       je       G_M000_IG220
 
G_M000_IG194:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG205
 
G_M000_IG195:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x320], rax
       test     r15, r15
       je       SHORT G_M000_IG200
 
G_M000_IG196:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG200
 
G_M000_IG197:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x330], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x338], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       G_M000_IG351
       lea      rax, [rsi+0x05]
       cmp      rax, 261
       jbe      G_M000_IG354
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x338]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG198:
       mov      rsi, rax
 
G_M000_IG199:
       jmp      G_M000_IG221
 
G_M000_IG200:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG203
 
G_M000_IG201:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG203
 
G_M000_IG202:
       xor      rdi, rdi
 
G_M000_IG203:
       test     rdi, rdi
       je       G_M000_IG220
 
G_M000_IG204:
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
       mov      rsi, rax
       jmp      G_M000_IG221
 
G_M000_IG205:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG214
 
G_M000_IG206:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x310], xmm0
       test     r15, r15
       je       SHORT G_M000_IG209
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG209
 
G_M000_IG208:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x310]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG221
 
G_M000_IG209:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG212
 
G_M000_IG210:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG212
 
G_M000_IG211:
       xor      rdi, rdi
 
G_M000_IG212:
       test     rdi, rdi
       je       G_M000_IG220
 
G_M000_IG213:
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
       mov      rsi, rax
       jmp      SHORT G_M000_IG221
 
G_M000_IG214:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG220
 
G_M000_IG215:
       mov      rdi, rsi
       mov      rax, r15
       test     rax, rax
       je       SHORT G_M000_IG218
 
G_M000_IG216:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG218
 
G_M000_IG217:
       xor      rax, rax
 
G_M000_IG218:
       test     rax, rax
       je       SHORT G_M000_IG220
 
G_M000_IG219:
       mov      rsi, rax
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG221
 
G_M000_IG220:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG221:
       test     rsi, rsi
       je       G_M000_IG246
 
G_M000_IG222:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG237
 
G_M000_IG223:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x3A0], rax
       test     rbx, rbx
       je       G_M000_IG232
 
G_M000_IG224:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG232
 
G_M000_IG225:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x3B0], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x3B8], rdx
       lea      rdx, [rbp-0x3B8]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x3B8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG230
 
G_M000_IG226:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3C8], xmm0
       lea      rdi, [rbp-0x3C8]
       mov      rsi, qword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG227:
       vmovdqu  xmm0, xmmword ptr [rbp-0x3C8]
       vmovdqu  xmmword ptr [rbp-0x3D8], xmm0
 
G_M000_IG228:
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
       mov      rsi, rax
 
G_M000_IG229:
       jmp      G_M000_IG247
 
G_M000_IG230:
       mov      rax, qword ptr [rbp-0x3B8]
       mov      qword ptr [rbp-0x400], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG355
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x400]
       mov      qword ptr [rax+0x08], rsi
       mov      rsi, rax
 
G_M000_IG231:
       jmp      SHORT G_M000_IG229
 
G_M000_IG232:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG235
 
G_M000_IG233:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG235
 
G_M000_IG234:
       xor      rdi, rdi
 
G_M000_IG235:
       test     rdi, rdi
       je       G_M000_IG246
 
G_M000_IG236:
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
       mov      rsi, rax
       jmp      G_M000_IG247
 
G_M000_IG237:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG246
 
G_M000_IG238:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG241
 
G_M000_IG239:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG241
 
G_M000_IG240:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x390]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG247
 
G_M000_IG241:
       mov      rdi, rbx
       test     rdi, rdi
       je       SHORT G_M000_IG244
 
G_M000_IG242:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG244
 
G_M000_IG243:
       xor      rdi, rdi
 
G_M000_IG244:
       test     rdi, rdi
       je       SHORT G_M000_IG246
 
G_M000_IG245:
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
       mov      rsi, rax
       jmp      SHORT G_M000_IG247
 
G_M000_IG246:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG247:
       test     rsi, rsi
       je       G_M000_IG273
 
G_M000_IG248:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG258
 
G_M000_IG249:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x428], rax
       test     r15, r15
       je       SHORT G_M000_IG253
 
G_M000_IG250:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG253
 
G_M000_IG251:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x438], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x440], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       G_M000_IG356
       lea      rax, [rsi+0x05]
       cmp      rax, 261
       jbe      G_M000_IG359
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x440]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG252:
       jmp      G_M000_IG274
 
G_M000_IG253:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG256
 
G_M000_IG254:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       je       SHORT G_M000_IG256
 
G_M000_IG255:
       xor      rdi, rdi
 
G_M000_IG256:
       test     rdi, rdi
       je       G_M000_IG273
 
G_M000_IG257:
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
       jmp      G_M000_IG274
 
G_M000_IG258:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG267
 
G_M000_IG259:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm0
       test     r15, r15
       je       SHORT G_M000_IG262
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG262
 
G_M000_IG261:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x420], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x418]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x420]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG274
 
G_M000_IG262:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG265
 
G_M000_IG263:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG265
 
G_M000_IG264:
       xor      rdi, rdi
 
G_M000_IG265:
       test     rdi, rdi
       je       G_M000_IG273
 
G_M000_IG266:
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
       jmp      SHORT G_M000_IG274
 
G_M000_IG267:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG273
 
G_M000_IG268:
       mov      rdi, rsi
       mov      rax, r15
       test     rax, rax
       je       SHORT G_M000_IG271
 
G_M000_IG269:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG271
 
G_M000_IG270:
       xor      rax, rax
 
G_M000_IG271:
       test     rax, rax
       je       SHORT G_M000_IG273
 
G_M000_IG272:
       mov      rsi, rax
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG274
 
G_M000_IG273:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG274:
       xor      rdx, rdx
       mov      gword ptr [rbp-0x4B8], rdx
 
G_M000_IG275:
       mov      gword ptr [rbp-0x4A8], rdx
 
G_M000_IG276:
       mov      gword ptr [rbp-0x4A0], rdx
       test     rax, rax
       je       G_M000_IG294
 
G_M000_IG277:
       mov      rcx, qword ptr [rax]
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      rcx, r8
       jne      G_M000_IG315
 
G_M000_IG278:
       mov      gword ptr [rbp-0x550], rax
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x4B0], rcx
       test     rbx, rbx
       je       G_M000_IG287
 
G_M000_IG279:
       mov      qword ptr [rbp-0x530], r8
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG287
 
G_M000_IG280:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x4C0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x4C8], rdx
       lea      rdx, [rbp-0x4C8]
       mov      rdi, rcx
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x4C8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG285
 
G_M000_IG281:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4D8], xmm0
       lea      rdi, [rbp-0x4D8]
       mov      rsi, qword ptr [rbp-0x4B0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG282:
       vmovdqu  xmm0, xmmword ptr [rbp-0x4D8]
       vmovdqu  xmmword ptr [rbp-0x4E8], xmm0
 
G_M000_IG283:
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
       mov      rdx, rax
 
G_M000_IG284:
       jmp      G_M000_IG295
 
G_M000_IG285:
       mov      rax, qword ptr [rbp-0x4C8]
       mov      qword ptr [rbp-0x510], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG360
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x510]
       mov      qword ptr [rax+0x08], rsi
       mov      rdx, rax
 
G_M000_IG286:
       jmp      SHORT G_M000_IG284
 
G_M000_IG287:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG290
 
G_M000_IG288:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG290
 
G_M000_IG289:
       xor      rsi, rsi
 
G_M000_IG290:
       test     rsi, rsi
       je       SHORT G_M000_IG293
 
G_M000_IG291:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm1, qword ptr [rax]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rbp-0x4B0]
       lea      rsi, [rbp-0x4B8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x550]
       je       SHORT G_M000_IG294
 
G_M000_IG292:
       mov      rdx, gword ptr [rbp-0x4B8]
       jmp      SHORT G_M000_IG295
 
G_M000_IG293:
       mov      rax, gword ptr [rbp-0x550]
 
G_M000_IG294:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG295:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x4B8], rcx
 
G_M000_IG296:
       mov      gword ptr [rbp-0x4A8], rcx
 
G_M000_IG297:
       mov      gword ptr [rbp-0x4A0], rcx
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG298:
       mov      gword ptr [rbp+0x248], rax
 
G_M000_IG299:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x538], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x278]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x538]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x248]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x540], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x548], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x538]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x540]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x548]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x540]
       add      r12, qword ptr [rbp+0x2D0]
       jo       G_M000_IG362
       mov      edx, dword ptr [rbp-0x524]
       mov      edi, edx
       mov      rsi, qword ptr [rbp+0x2D8]
       cmp      r12, rsi
       setl     r8b
       movzx    r8, r8b
       and      r8d, edi
       mov      qword ptr [rbp+0x2D8], rsi
       cmp      r12, rsi
       setg     r9b
       movzx    r9, r9b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r9d
       or       edi, r8d
       je       G_M000_IG333
 
G_M000_IG300:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG303
 
G_M000_IG301:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      r8, rax
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG361
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG303
 
G_M000_IG302:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG361
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 10
 
G_M000_IG303:
       test     r14, r14
       jne      G_M000_IG325
 
G_M000_IG304:
       mov      rax, r14
       mov      rdi, rbx
       mov      r8, r15
       test     rax, rax
       mov      gword ptr [rbp+0x110], rdi
       je       SHORT G_M000_IG308
 
G_M000_IG305:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG308
 
G_M000_IG306:
       mov      rdx, gword ptr [rbp+0x110]
       test     rdx, rdx
       mov      gword ptr [rbp+0x110], rdx
       je       SHORT G_M000_IG308
 
G_M000_IG307:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rbp+0x110]
       cmp      qword ptr [rdx], rdi
       mov      gword ptr [rbp+0x110], rdx
       je       G_M000_IG03
 
G_M000_IG308:
       test     r14, r14
       je       G_M000_IG26
 
G_M000_IG309:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r14], r8
       jne      G_M000_IG17
 
G_M000_IG310:
       mov      rax, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x40], rax
       test     rbx, rbx
       je       G_M000_IG12
 
G_M000_IG311:
       mov      qword ptr [rbp-0x530], r8
       cmp      qword ptr [rbx], r8
       jne      G_M000_IG12
 
G_M000_IG312:
       mov      rsi, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x50], rsi
       xor      edx, edx
       mov      qword ptr [rbp-0x58], rdx
       lea      rdx, [rbp-0x58]
       mov      rdi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x58]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG10
 
G_M000_IG313:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, qword ptr [rbp-0x40]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x558], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x514], ecx
       mov      rdi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x558]
       mov      esi, dword ptr [rbp-0x514]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG314:
       jmp      G_M000_IG27
 
G_M000_IG315:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG294
 
G_M000_IG316:
       mov      gword ptr [rbp-0x550], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x498], xmm0
       test     rbx, rbx
       mov      rax, gword ptr [rbp-0x550]
       je       SHORT G_M000_IG319
 
G_M000_IG317:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG319
 
G_M000_IG318:
       mov      gword ptr [rbp-0x550], rax
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4A8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x550]
       je       G_M000_IG294
       mov      rdx, gword ptr [rbp-0x4A8]
       jmp      G_M000_IG295
 
G_M000_IG319:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG322
 
G_M000_IG320:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG322
 
G_M000_IG321:
       xor      rsi, rsi
 
G_M000_IG322:
       test     rsi, rsi
       je       G_M000_IG294
 
G_M000_IG323:
       mov      gword ptr [rbp-0x550], rax
       mov      rsi, rbx
       mov      rdi, r8
       call     CORINFO_HELP_UNBOX
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax]
       lea      rsi, [rbp-0x4A0]
       vmovsd   xmm0, qword ptr [rbp-0x498]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x550]
       je       G_M000_IG294
 
G_M000_IG324:
       mov      rdx, gword ptr [rbp-0x4A0]
       jmp      G_M000_IG295
 
G_M000_IG325:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG304
 
G_M000_IG326:
       test     rbx, rbx
       je       G_M000_IG304
 
G_M000_IG327:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG304
 
G_M000_IG328:
       test     r15, r15
       je       G_M000_IG304
 
G_M000_IG329:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG304
 
G_M000_IG330:
       test     r13, r13
       je       G_M000_IG304
 
G_M000_IG331:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG304
 
G_M000_IG332:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   xmm3, qword ptr [r13+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp+0x150], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp+0x148], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm1, qword ptr [rbp+0x148]
       vmovsd   qword ptr [rsi+0x08], xmm1
       mov      gword ptr [rbp+0x278], rsi
       vmovaps  xmm0, xmmword ptr [rbp+0x150]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      rax, rsi
       mov      gword ptr [rbp+0x248], rax
       jmp      G_M000_IG299
 
G_M000_IG333:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG336
 
G_M000_IG334:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG361
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG336
 
G_M000_IG335:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG361
       mov      dword ptr [rcx+4*rdx+0x10], 11
 
G_M000_IG336:
       mov      r10, qword ptr [rbp+0x308]
       test     r10, r10
       jle      SHORT G_M000_IG338
 
G_M000_IG337:
       movsxd   rcx, edi
       cmp      r10, rcx
       jg       SHORT G_M000_IG338
       dec      r10d
       mov      dword ptr [(reloc)], r10d
 
G_M000_IG338:
       add      rsp, 0x8B8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG339:
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rsi, rcx
       cmp      rsi, 262
       jae      G_M000_IG361
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rdi, rcx
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG11
 
G_M000_IG340:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x560], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x518], ecx
       mov      rdi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x560]
       mov      esi, dword ptr [rbp-0x518]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
       jmp      G_M000_IG33
 
G_M000_IG341:
       test     rax, rax
       jl       G_M000_IG362
       mov      rdi, rax
       cmp      rdi, 262
       jae      G_M000_IG361
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rax, rax
       jl       G_M000_IG362
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG32
 
G_M000_IG342:
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rsi, rcx
       cmp      rsi, 262
       jae      G_M000_IG361
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rdi, rcx
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG63
 
G_M000_IG343:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x570], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x520], ecx
       mov      rdi, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x570]
       mov      esi, dword ptr [rbp-0x520]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
       jmp      G_M000_IG85
 
G_M000_IG344:
       test     rax, rax
       jl       G_M000_IG362
       mov      rdi, rax
       cmp      rdi, 262
       jae      G_M000_IG361
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rax, rax
       jl       G_M000_IG362
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG84
 
G_M000_IG345:
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rsi, rcx
       cmp      rsi, 262
       jae      G_M000_IG361
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rdi, rcx
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG117
 
G_M000_IG346:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
       lea      rdi, [rbp-0x240]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG347:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x240]
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
 
G_M000_IG348:
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
       jmp      G_M000_IG138
 
G_M000_IG349:
       test     rax, rax
       jl       G_M000_IG362
       mov      rdi, rax
       cmp      rdi, 262
       jae      G_M000_IG361
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rax, rax
       jl       G_M000_IG362
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG138
 
G_M000_IG350:
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rsi, rcx
       cmp      rsi, 262
       jae      G_M000_IG361
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rdi, rcx
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG177
 
G_M000_IG351:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x348], xmm0
       lea      rdi, [rbp-0x348]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG352:
       vmovdqu  xmm0, xmmword ptr [rbp-0x348]
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
 
G_M000_IG353:
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
       mov      rsi, rax
       jmp      G_M000_IG199
 
G_M000_IG354:
       test     rax, rax
       jl       G_M000_IG362
       mov      rdi, rax
       cmp      rdi, 262
       jae      G_M000_IG361
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rax, rax
       jl       G_M000_IG362
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG198
 
G_M000_IG355:
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rsi, rcx
       cmp      rsi, 262
       jae      G_M000_IG361
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rcx, rcx
       jl       G_M000_IG362
       mov      rdi, rcx
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG231
 
G_M000_IG356:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x450], xmm0
       lea      rdi, [rbp-0x450]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG357:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x450]
       vmovdqu32 xmmword ptr [rbp-0x460], xmm0
 
G_M000_IG358:
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
       jmp      G_M000_IG252
 
G_M000_IG359:
       test     rax, rax
       jl       SHORT G_M000_IG362
       mov      rdi, rax
       cmp      rdi, 262
       jae      SHORT G_M000_IG361
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       test     rax, rax
       jl       SHORT G_M000_IG362
       mov      rsi, rax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG252
 
G_M000_IG360:
       test     rcx, rcx
       jl       SHORT G_M000_IG362
       mov      rdx, rcx
       cmp      rdx, 262
       jae      SHORT G_M000_IG361
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rcx, rcx
       jl       SHORT G_M000_IG362
       mov      rdi, rcx
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG286
 
G_M000_IG361:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG362:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 9404

