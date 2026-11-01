; Assembly listing for method RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object (Instrumented Tier0)
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
       vmovdqu32 zmmword ptr [rbp-0x130], zmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
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
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 19
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
       xor      eax, eax
       mov      qword ptr [rbp-0xE0], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xE8], eax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0xE4], 0
       jne      G_M000_IG14
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x138]
       mov      esi, 121
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
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, gword ptr [rbp-0x38]
       mov      edi, 3
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:GetAttr(int,System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rsi, gword ptr [rbp-0x38]
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:GetAttr(int,System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdx, gword ptr [rbp-0xB8]
       mov      rcx, gword ptr [rbp-0xC0]
       mov      edi, 5
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0xF0], rax
       cmp      dword ptr [rbp-0xE4], 0
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
       cmp      dword ptr [rbp-0xE8], 0
       jne      G_M000_IG33
       jmp      G_M000_IG30
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xD8]
 
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
       mov      esi, 289
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
       mov      qword ptr [rbp-0xE0], rax
       jmp      G_M000_IG03
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE0], rax
       jmp      G_M000_IG11
 
G_M000_IG16:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG18
 
G_M000_IG17:
       lea      rdi, [rbp-0x138]
       mov      esi, 320
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG18:
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rsi, gword ptr [rbp-0x40]
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xE8], eax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0xE4], 0
       jne      G_M000_IG15
       jmp      G_M000_IG11
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xE0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xE8], eax
       jmp      G_M000_IG16
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x138]
       mov      esi, 388
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       cmp      dword ptr [rbp-0xE8], 0
       jne      SHORT G_M000_IG19
       jmp      G_M000_IG16
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x108]
       mov      qword ptr [rbp-0xE0], rax
       mov      dword ptr [rbp-0xE8], 1
       jmp      G_M000_IG11
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x138]
       dec      eax
       mov      dword ptr [rbp-0x138], eax
       cmp      dword ptr [rbp-0x138], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x138]
       mov      esi, 412
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rsi, gword ptr [rbp-0xF0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x108], rax
       mov      rax, qword ptr [rbp-0xE0]
       xor      rax, qword ptr [rbp-0x108]
       mov      qword ptr [rbp-0x110], rax
       mov      rax, qword ptr [rbp-0x100]
       xor      rax, qword ptr [rbp-0x108]
       mov      qword ptr [rbp-0x118], rax
       mov      rax, qword ptr [rbp-0x110]
       and      rax, qword ptr [rbp-0x118]
       mov      qword ptr [rbp-0x120], rax
       cmp      qword ptr [rbp-0x120], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x124], eax
       cmp      dword ptr [rbp-0x124], 0
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
       mov      esi, 479
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rsi, gword ptr [rbp-0xF0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0xF4], eax
       cmp      dword ptr [rbp-0xF4], 0
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
       mov      esi, 506
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xD8], rax
       jmp      G_M000_IG09
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xE0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xE8], eax
       jmp      SHORT G_M000_IG30
 
G_M000_IG34:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1601

; Assembly listing for method RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x121
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 28 inlinees with PGO data; 33 single block inlinees; 31 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 256
       mov      qword ptr [rsp+0x248], r15
       mov      qword ptr [rsp+0x240], r14
       mov      qword ptr [rsp+0x238], r13
       mov      qword ptr [rsp+0x230], r12
       mov      qword ptr [rsp+0x228], rbx
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       mov      rbx, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      r15, qword ptr [rbp+0xD8]
       mov      r13, qword ptr [rbp+0xD0]
       mov      r9, qword ptr [rbp+0xC8]
       mov      r12d, dword ptr [rbp+0x68]
 
G_M000_IG02:
       test     r9, r9
       setg     r11b
       movzx    r11, r11b
       mov      dword ptr [rbp-0xA0], r11d
       jmp      G_M000_IG74
 
G_M000_IG03:
       mov      rdi, gword ptr [rbp-0xA8]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rcx, qword ptr [rbp-0x30]
       mov      rsi, rbx
       mov      edi, 3
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:FillGetAttr(int,RyuPy.Runtime.PyInstance,System.String,long)]
       mov      rax, gword ptr [rbp-0xB0]
       jmp      SHORT G_M000_IG05
       align    [0 bytes for IG08]
 
G_M000_IG04:
       mov      rdi, rbx
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
 
G_M000_IG05:
       mov      gword ptr [rbp+0x98], rax
       test     rbx, rbx
       je       G_M000_IG17
 
G_M000_IG06:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      G_M000_IG17
 
G_M000_IG07:
       mov      rdi, rbx
       mov      rdx, gword ptr [rbx+0x08]
       cmp      byte  ptr [rdx+0x74], 0
       jne      G_M000_IG17
       mov      r8, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x38], r8
       xor      edx, edx
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       jmp      SHORT G_M000_IG10
 
G_M000_IG08:
       mov      r8, qword ptr [rbp-0x38]
 
G_M000_IG09:
       inc      edx
       cmp      edx, 4
       jge      G_M000_IG16
 
G_M000_IG10:
       lea      ecx, [rdx+0x10]
       imul     rcx, rcx, 48
       lea      rcx, bword ptr [rsi+rcx+0x10]
       cmp      byte  ptr [rcx+0x2C], 0
       je       SHORT G_M000_IG09
 
G_M000_IG11:
       mov      r9, gword ptr [rcx]
       cmp      r9, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG09
       cmp      qword ptr [rcx+0x20], r8
       jne      G_M000_IG105
       cmp      gword ptr [rcx+0x08], 0
       jne      G_M000_IG106
       cmp      byte  ptr [rcx+0x2D], 0
       jne      G_M000_IG107
       mov      ecx, dword ptr [rcx+0x28]
       mov      r9, gword ptr [rbx+0x10]
       cmp      dword ptr [r9+0x08], ecx
       jbe      SHORT G_M000_IG08
 
G_M000_IG12:
       cmp      ecx, dword ptr [r9+0x08]
       jae      G_M000_IG131
       mov      ecx, ecx
       mov      rcx, gword ptr [r9+8*rcx+0x10]
       test     rcx, rcx
       je       SHORT G_M000_IG15
 
G_M000_IG13:
       mov      r9, 0xD1FFAB1E
       cmp      rcx, gword ptr [r9]
       mov      r8, qword ptr [rbp-0x38]
       je       SHORT G_M000_IG09
 
G_M000_IG14:
       mov      rdx, rcx
       mov      r9, rdx
       mov      rax, r9
       jmp      SHORT G_M000_IG18
 
G_M000_IG15:
       mov      r8, qword ptr [rbp-0x38]
       jmp      G_M000_IG09
 
G_M000_IG16:
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rcx, qword ptr [rbp-0x38]
       mov      rsi, rbx
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:FillGetAttr(int,RyuPy.Runtime.PyInstance,System.String,long)]
       mov      rax, gword ptr [rbp-0xB8]
       jmp      SHORT G_M000_IG18
       align    [0 bytes for IG60]
 
G_M000_IG17:
       mov      rdi, rbx
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
 
G_M000_IG18:
       mov      rcx, gword ptr [rbp+0x98]
       test     rcx, rcx
       je       G_M000_IG54
 
G_M000_IG19:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG38
 
G_M000_IG20:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG54
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG36
 
G_M000_IG22:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x58], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x60], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG32
 
G_M000_IG23:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0xF0], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x98], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xF0]
       mov      esi, dword ptr [rbp-0x98]
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0xF8], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x9C], edx
       test     rax, rax
       jne      G_M000_IG112
 
G_M000_IG24:
       test     rax, rax
       jne      G_M000_IG121
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG30
 
G_M000_IG25:
       mov      edi, -1
 
G_M000_IG26:
       test     edi, edi
       jg       SHORT G_M000_IG31
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, gword ptr [rbp-0xF8]
       mov      esi, dword ptr [rbp-0x9C]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rcx, gword ptr [rbp-0xE0]
       mov      qword ptr [rcx+0x08], rax
 
G_M000_IG28:
       mov      rax, rcx
 
G_M000_IG29:
       jmp      G_M000_IG55
 
G_M000_IG30:
       xor      edi, edi
       jmp      SHORT G_M000_IG26
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      gword ptr [rbp-0xD8], rcx
       lea      rdi, bword ptr [rcx+0x08]
       mov      rsi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0xD8]
       mov      edi, dword ptr [rbp-0x9C]
       mov      dword ptr [rcx+0x10], edi
       jmp      SHORT G_M000_IG28
 
G_M000_IG32:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG35
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x60]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG34:
       jmp      SHORT G_M000_IG29
 
G_M000_IG35:
       test     rax, rax
       jl       G_M000_IG132
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG131
       mov      rax, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG34
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG54
 
G_M000_IG37:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x88], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG55
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG44
 
G_M000_IG39:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       test     rax, rax
       je       G_M000_IG54
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG42
 
G_M000_IG41:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG55
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG54
 
G_M000_IG43:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG55
 
G_M000_IG44:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG54
 
G_M000_IG45:
       mov      rsi, rcx
       mov      gword ptr [rbp-0xC8], rsi
       test     rax, rax
       je       G_M000_IG54
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG54
 
G_M000_IG47:
       mov      gword ptr [rbp-0xD0], rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG52
 
G_M000_IG48:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG51
 
G_M000_IG49:
       mov      ecx, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x94], ecx
       mov      edi, ecx
       add      edi, dword ptr [rax+0x08]
       js       G_M000_IG130
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0xE8], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0xC8]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x94]
       mov      rax, gword ptr [rbp-0xE8]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rsi, gword ptr [rbp-0xD0]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rax, gword ptr [rbp-0xE8]
 
G_M000_IG50:
       jmp      SHORT G_M000_IG55
 
G_M000_IG51:
       mov      rax, rsi
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG129
 
G_M000_IG53:
       jmp      SHORT G_M000_IG50
 
G_M000_IG54:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG55:
       mov      ecx, dword ptr [rbp+0x6C]
       test     ecx, ecx
       je       G_M000_IG68
 
G_M000_IG56:
       test     rax, rax
       je       G_M000_IG68
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG68
 
G_M000_IG58:
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rdx, qword ptr [rbp+0x70]
       lea      rsi, [rdx+rdi]
       mov      r8, rdx
       xor      r8, rsi
       xor      rdi, rsi
       and      rdi, r8
       mov      qword ptr [rbp+0x70], rdx
       mov      rax, gword ptr [rbp-0xC0]
       jl       G_M000_IG68
 
G_M000_IG59:
       mov      rdx, rsi
       mov      r12d, 1
       mov      dword ptr [rbp+0x6C], ecx
       mov      qword ptr [rbp+0x70], rdx
       mov      r9, qword ptr [rbp+0xC8]
       mov      r11d, dword ptr [rbp-0xA0]
       jmp      G_M000_IG74
 
G_M000_IG60:
       mov      rcx, qword ptr [rbp-0x30]
 
G_M000_IG61:
       inc      edx
       cmp      edx, 4
       jge      G_M000_IG03
 
G_M000_IG62:
       lea      edi, [rdx+0x0C]
       imul     rdi, rdi, 48
       lea      rdi, bword ptr [rsi+rdi+0x10]
       cmp      byte  ptr [rdi+0x2C], 0
       je       SHORT G_M000_IG61
 
G_M000_IG63:
       mov      r10, gword ptr [rdi]
       cmp      r10, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG61
       cmp      qword ptr [rdi+0x20], rcx
       jne      G_M000_IG98
       cmp      gword ptr [rdi+0x08], 0
       jne      G_M000_IG99
       cmp      byte  ptr [rdi+0x2D], 0
       jne      G_M000_IG100
       mov      edi, dword ptr [rdi+0x28]
       mov      r10, gword ptr [rbx+0x10]
       cmp      dword ptr [r10+0x08], edi
       jbe      SHORT G_M000_IG60
 
G_M000_IG64:
       cmp      edi, dword ptr [r10+0x08]
       jae      G_M000_IG131
       mov      edi, edi
       mov      r10, gword ptr [r10+8*rdi+0x10]
       test     r10, r10
       je       SHORT G_M000_IG67
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       cmp      r10, gword ptr [rdi]
       mov      rcx, qword ptr [rbp-0x30]
       je       SHORT G_M000_IG61
 
G_M000_IG66:
       mov      rdi, r10
       mov      rax, rdi
       jmp      G_M000_IG05
 
G_M000_IG67:
       mov      rcx, qword ptr [rbp-0x30]
       jmp      G_M000_IG61
 
G_M000_IG68:
       test     r12d, r12d
       je       SHORT G_M000_IG72
 
G_M000_IG69:
       mov      rdx, qword ptr [rbp+0x70]
       lea      r14, [rdx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG81
 
G_M000_IG70:
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x70]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG71:
       mov      r14, rax
       mov      rax, gword ptr [rbp-0xC0]
 
G_M000_IG72:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
       xor      r12d, r12d
       test     r14, r14
       jne      G_M000_IG82
 
G_M000_IG73:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x6C], ecx
       mov      r9, qword ptr [rbp+0xC8]
       mov      r11d, dword ptr [rbp-0xA0]
 
G_M000_IG74:
       mov      qword ptr [rbp+0xC8], r9
       add      r15, r9
       jo       G_M000_IG132
       mov      edx, r11d
       cmp      r15, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edx
       cmp      r15, r13
       setg     cl
       movzx    rcx, cl
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, ecx
       or       edx, esi
       je       G_M000_IG85
 
G_M000_IG75:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG78
 
G_M000_IG76:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edx
       mov      edi, edx
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG131
       cmp      dword ptr [rcx+4*rdi+0x10], 20
       je       SHORT G_M000_IG78
 
G_M000_IG77:
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG131
       mov      edx, edx
       mov      dword ptr [rsi+4*rdx+0x10], 20
 
G_M000_IG78:
       test     rbx, rbx
       je       G_M000_IG04
 
G_M000_IG79:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      G_M000_IG04
 
G_M000_IG80:
       mov      gword ptr [rbp-0xA8], rbx
       mov      rdx, gword ptr [rbx+0x08]
       cmp      byte  ptr [rdx+0x74], 0
       jne      G_M000_IG04
       mov      rcx, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x30], rcx
       xor      edx, edx
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       jmp      G_M000_IG62
 
G_M000_IG81:
       test     r14, r14
       jl       G_M000_IG132
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      r14, 262
       jae      G_M000_IG131
       mov      rsi, gword ptr [rsi+8*r14+0x10]
       mov      r14, rsi
       mov      gword ptr [rbp-0xC0], rax
       mov      rax, r14
       jmp      G_M000_IG71
 
G_M000_IG82:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r14], rax
       jne      SHORT G_M000_IG84
 
G_M000_IG83:
       mov      ecx, 1
       mov      rdx, qword ptr [r14+0x08]
       mov      rax, rdx
       mov      dword ptr [rbp+0x6C], ecx
       mov      qword ptr [rbp+0x70], rax
       mov      r9, qword ptr [rbp+0xC8]
       mov      r11d, dword ptr [rbp-0xA0]
       jmp      G_M000_IG74
 
G_M000_IG84:
       xor      ecx, ecx
       mov      dword ptr [rbp+0x6C], ecx
       mov      r9, qword ptr [rbp+0xC8]
       mov      r11d, dword ptr [rbp-0xA0]
       jmp      G_M000_IG74
 
G_M000_IG85:
       test     r12d, r12d
       je       SHORT G_M000_IG89
 
G_M000_IG86:
       mov      r8, qword ptr [rbp+0x70]
       lea      r14, [r8+0x05]
       cmp      r14, 261
       jbe      G_M000_IG97
 
G_M000_IG87:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp+0x70]
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG88:
       mov      r14, rax
 
G_M000_IG89:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG92
 
G_M000_IG90:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      edi, [rax-0x01]
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG131
       cmp      dword ptr [rdx+4*rsi+0x10], 21
       je       SHORT G_M000_IG92
 
G_M000_IG91:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG131
       mov      edx, edi
       mov      dword ptr [rcx+4*rdx+0x10], 21
 
G_M000_IG92:
       mov      r10, qword ptr [rbp+0x100]
       test     r10, r10
       jle      SHORT G_M000_IG95
 
G_M000_IG93:
       cdqe     
       cmp      r10, rax
       jg       SHORT G_M000_IG95
 
G_M000_IG94:
       dec      r10d
       mov      dword ptr [(reloc)], r10d
 
G_M000_IG95:
       mov      rax, r14
 
G_M000_IG96:
       add      rsp, 552
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG97:
       test     r14, r14
       jl       G_M000_IG132
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      r14, 262
       jae      G_M000_IG131
       mov      rax, gword ptr [rsi+8*r14+0x10]
       jmp      G_M000_IG88
 
G_M000_IG98:
       mov      rcx, qword ptr [rbp-0x30]
       jmp      G_M000_IG61
 
G_M000_IG99:
       mov      rdx, gword ptr [rdi+0x10]
       mov      rsi, gword ptr [rdi+0x08]
       mov      rcx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:CallGetter(int,RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,RyuPy.Runtime.PyInstance):System.Object]
       jmp      G_M000_IG05
 
G_M000_IG100:
       cmp      dword ptr [rdi+0x28], 0
       jl       SHORT G_M000_IG103
       mov      edx, dword ptr [rdi+0x28]
       mov      rsi, gword ptr [rbx+0x10]
       cmp      dword ptr [rsi+0x08], edx
       jbe      SHORT G_M000_IG101
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG131
       mov      rdx, gword ptr [rsi+8*rdx+0x10]
       test     rdx, rdx
       je       SHORT G_M000_IG101
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, gword ptr [rsi]
       je       SHORT G_M000_IG101
       mov      esi, 1
       jmp      SHORT G_M000_IG102
 
G_M000_IG101:
       xor      rdx, rdx
       xor      esi, esi
 
G_M000_IG102:
       test     esi, esi
       jne      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      rsi, gword ptr [rdi+0x18]
       mov      rax, rsi
       jmp      G_M000_IG05
 
G_M000_IG104:
       mov      rsi, rdx
       mov      rax, rsi
       jmp      G_M000_IG05
 
G_M000_IG105:
       mov      r8, qword ptr [rbp-0x38]
       jmp      G_M000_IG09
 
G_M000_IG106:
       mov      rdx, gword ptr [rcx+0x10]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rcx, rbx
       mov      edi, 4
       call     [RyuPy.Runtime.PyPic:CallGetter(int,RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,RyuPy.Runtime.PyInstance):System.Object]
       jmp      G_M000_IG18
 
G_M000_IG107:
       cmp      dword ptr [rcx+0x28], 0
       jl       SHORT G_M000_IG110
       mov      r9d, dword ptr [rcx+0x28]
       mov      rdx, gword ptr [rbx+0x10]
       cmp      dword ptr [rdx+0x08], r9d
       jbe      SHORT G_M000_IG108
       cmp      r9d, dword ptr [rdx+0x08]
       jae      G_M000_IG131
       mov      esi, r9d
       mov      r9, gword ptr [rdx+8*rsi+0x10]
       test     r9, r9
       je       SHORT G_M000_IG108
       mov      rdx, 0xD1FFAB1E
       cmp      r9, gword ptr [rdx]
       je       SHORT G_M000_IG108
       mov      edx, 1
       jmp      SHORT G_M000_IG109
 
G_M000_IG108:
       xor      r9, r9
       xor      edx, edx
 
G_M000_IG109:
       test     edx, edx
       jne      SHORT G_M000_IG111
 
G_M000_IG110:
       mov      r9, gword ptr [rcx+0x18]
       mov      rax, r9
       jmp      G_M000_IG18
 
G_M000_IG111:
       mov      rax, r9
       jmp      G_M000_IG18
 
G_M000_IG112:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG113
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG114
 
G_M000_IG113:
       mov      edx, ecx
       jmp      SHORT G_M000_IG120
 
G_M000_IG114:
       cmp      edi, 2
       je       SHORT G_M000_IG115
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG131
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG116
 
G_M000_IG115:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG116:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG117
       mov      edx, -1
       jmp      SHORT G_M000_IG119
 
G_M000_IG117:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG118
       mov      edx, 1
       jmp      SHORT G_M000_IG119
 
G_M000_IG118:
       xor      edx, edx
 
G_M000_IG119:
       imul     edx, ecx
 
G_M000_IG120:
       test     edx, edx
       jge      G_M000_IG24
       jmp      G_M000_IG31
 
G_M000_IG121:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG122
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x9C]
       jmp      G_M000_IG26
 
G_M000_IG123:
       cmp      edi, 2
       je       SHORT G_M000_IG124
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG131
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG125
 
G_M000_IG124:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0xF8]
 
G_M000_IG125:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG126
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG126:
       test     rdi, rdi
       jge      SHORT G_M000_IG127
       mov      edx, 1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:
       xor      edx, edx
 
G_M000_IG128:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG26
 
G_M000_IG129:
       mov      rax, 0xD1FFAB1E
       jmp      G_M000_IG50
 
G_M000_IG130:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG131:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG132:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2956

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_field_reads(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rsi+0x08]
       call     [RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 35

