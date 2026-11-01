; Assembly listing for method RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 256
       lea      rbp, [rsp+0x100]
       xor      eax, eax
       mov      qword ptr [rbp-0xF8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x100], 0x3E8
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
       mov      eax, dword ptr [rbp-0x100]
       dec      eax
       mov      dword ptr [rbp-0x100], eax
       cmp      dword ptr [rbp-0x100], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x100]
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
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF0], rax
       mov      rdi, gword ptr [rbp-0xF0]
       mov      rdx, gword ptr [rbp-0x40]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xF0]
       mov      rdx, gword ptr [rbp-0x38]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rdi, gword ptr [rbp-0xB0]
       call     [RyuPy.Runtime.PyOps:Len(System.Object):long]
       mov      qword ptr [rbp-0xB8], rax
       mov      rdi, qword ptr [rbp-0xB8]
       mov      esi, 2
       xor      edx, edx
       call     [RyuPy.Runtime.PyOps:UnpackCheck(long,long,bool)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, gword ptr [rbp-0xD0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
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
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, gword ptr [rbp-0xF8]
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xF8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xF8]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xE0]
 
G_M000_IG08:
       add      rsp, 256
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 835

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 224
       lea      rbp, [rsp+0xE0]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0xE0], 0x3E8
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
       mov      eax, dword ptr [rbp-0xE0]
       dec      eax
       mov      dword ptr [rbp-0xE0], eax
       cmp      dword ptr [rbp-0xE0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xE0]
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
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xD8], rax
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0xD8]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
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
       add      rsp, 224
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 668

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x42
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 3
; 47 inlinees with PGO data; 47 single block inlinees; 16 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 320
       mov      qword ptr [rsp+0x248], r15
       mov      qword ptr [rsp+0x240], r14
       mov      qword ptr [rsp+0x238], r13
       mov      qword ptr [rsp+0x230], r12
       mov      qword ptr [rsp+0x228], rbx
       lea      rbp, [rsp+0x140]
       xor      eax, eax
       mov      qword ptr [rbp-0x78], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0xD8]
       mov      r15, gword ptr [rbp+0xD0]
       mov      r14, qword ptr [rbp+0xA0]
       mov      r13, qword ptr [rbp+0x98]
       mov      r12, qword ptr [rbp+0x90]
 
G_M000_IG02:
       test     r12, r12
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0xAC], ecx
 
G_M000_IG03:
       mov      edi, ecx
       cmp      r14, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r14, r13
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG84
 
G_M000_IG04:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdx, rsi
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG129
       cmp      dword ptr [rdx+4*r8+0x10], 9
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG129
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 9
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xC8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      r15, gword ptr [rbp-0xC8]
       lea      rdi, bword ptr [r15+0x18]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Len(System.Object):long]
       mov      r15, rax
       vxorps   ymm0, ymm0, ymm0
       vmovdqu  ymmword ptr [rbp-0x78], ymm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       vxorps   ymm0, ymm0, ymm0
       vmovdqu  ymmword ptr [rbp-0x50], ymm0
       vmovdqu  xmmword ptr [rbp-0x38], xmm0
       cmp      r15, 2
       jge      G_M000_IG18
 
G_M000_IG08:
       xor      rdi, rdi
       mov      gword ptr [rbp-0x78], rdi
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG88
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       mov      gword ptr [rbp-0xD0], rbx
       cmp      byte  ptr [rbx], bl
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG89
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      gword ptr [rbp-0xD8], r14
       mov      rdi, 0xD1FFAB1E
       mov      rax, 0xD1FFAB1E
       call     rax
       cmp      dword ptr [rax], 2
       jle      G_M000_IG90
       mov      rdi, gword ptr [rax+0x08]
       mov      r13, bword ptr [rdi+0x10]
       test     r13, r13
       je       G_M000_IG90
 
G_M000_IG11:
       mov      bword ptr [rbp-0x140], r13
       mov      r12, gword ptr [r13+0x10]
       test     r12, r12
       je       G_M000_IG19
 
G_M000_IG12:
       cmp      dword ptr [r12+0x08], 4
       jle      G_M000_IG19
 
G_M000_IG13:
       mov      rax, gword ptr [r12+0x50]
       test     rax, rax
       je       G_M000_IG19
 
G_M000_IG14:
       xor      rdi, rdi
       mov      gword ptr [r12+0x50], rdi
       mov      r14, gword ptr [rbp-0xD8]
       cmp      byte  ptr [r14+0x9D], 0
       je       G_M000_IG67
 
G_M000_IG15:
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, rax
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       mov      r12d, eax
       test     r12d, r12d
       je       G_M000_IG91
 
G_M000_IG16:
       mov      rax, gword ptr [rbp-0xE0]
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0x80], ecx
       mov      rdi, rbx
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       mov      ecx, eax
       test     ecx, ecx
       je       G_M000_IG92
 
G_M000_IG17:
       mov      rdi, r14
       mov      esi, r12d
       mov      edx, dword ptr [rbp-0x80]
       mov      r8d, 4
       call     [System.Buffers.ArrayPoolEventSource:BufferRented(int,int,int,int):this]
       mov      r12, gword ptr [rbp-0xE0]
       jmp      G_M000_IG50
 
G_M000_IG18:
       cmp      r15, 2
       jg       G_M000_IG118
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r15, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, r15
       mov      r15, rax
       add      r14, r12
       jo       G_M000_IG130
       mov      ecx, dword ptr [rbp-0xAC]
       jmp      G_M000_IG03
 
G_M000_IG19:
       mov      rdi, gword ptr [rbx+0x10]
       cmp      dword ptr [rdi+0x08], 4
       mov      rbx, gword ptr [rbp-0xD0]
       jle      G_M000_IG44
 
G_M000_IG20:
       mov      rdi, gword ptr [rdi+0x30]
       test     rdi, rdi
       je       G_M000_IG44
 
G_M000_IG21:
       mov      r12, gword ptr [rdi+0x08]
       mov      gword ptr [rbp-0xE8], r12
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG93
 
G_M000_IG22:
       cmp      byte  ptr [(reloc)], 0
       je       G_M000_IG95
       mov      rax, 0xD1FFAB1E
 
G_M000_IG23:
       vzeroupper 
       call     rax ; Interop+Sys:SchedGetCpu():int
       mov      ebx, eax
       cmp      dword ptr [(reloc)], 0
       jne      G_M000_IG94
 
G_M000_IG24:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG97
 
G_M000_IG25:
       mov      eax, ebx
       xor      edx, edx
       div      edx:eax, dword ptr [(reloc)]
       mov      ebx, edx
       mov      r12, gword ptr [rbp-0xE8]
       mov      eax, dword ptr [r12+0x08]
       mov      dword ptr [rbp-0xB0], eax
       test     eax, eax
       jle      G_M000_IG98
 
G_M000_IG26:
       mov      dword ptr [rbp-0xB8], eax
 
G_M000_IG27:
       cmp      ebx, eax
       jae      G_M000_IG129
       mov      edi, ebx
       mov      rdx, gword ptr [r12+8*rdi+0x10]
       mov      gword ptr [rbp-0xF8], rdx
       cmp      byte  ptr [rdx], dl
       xor      rsi, rsi
       mov      gword ptr [rbp-0xF0], rsi
       mov      rdi, rdx
       call     System.Threading.Monitor:TryEnter_FastPath(System.Object):bool
       test     eax, eax
       jne      SHORT G_M000_IG29
 
G_M000_IG28:
       mov      rdi, gword ptr [rbp-0xF8]
       call     [System.Threading.Monitor:Enter_Slowpath(System.Object)]
 
G_M000_IG29:
       mov      rax, gword ptr [rbp-0xF8]
       mov      rdi, gword ptr [rax+0x08]
       mov      ecx, dword ptr [rax+0x10]
       dec      ecx
       mov      edx, dword ptr [rdi+0x08]
       cmp      edx, ecx
       jbe      SHORT G_M000_IG31
 
G_M000_IG30:
       mov      edx, ecx
       mov      rsi, gword ptr [rdi+8*rdx+0x10]
       mov      gword ptr [rbp-0xF0], rsi
       xor      r8, r8
       mov      gword ptr [rdi+8*rdx+0x10], r8
       mov      dword ptr [rax+0x10], ecx
 
G_M000_IG31:
       mov      rdi, rax
       call     System.Threading.Monitor:Exit_FastPath(System.Object):int
       test     eax, eax
       je       SHORT G_M000_IG33
 
G_M000_IG32:
       mov      edi, eax
       mov      rsi, gword ptr [rbp-0xF8]
       call     [System.Threading.Monitor:Exit_Slowpath(int,System.Object)]
 
G_M000_IG33:
       mov      rax, gword ptr [rbp-0xF0]
       test     rax, rax
       je       SHORT G_M000_IG40
 
G_M000_IG34:
       mov      rbx, rax
       test     rbx, rbx
       je       G_M000_IG43
 
G_M000_IG35:
       mov      r12, rbx
       mov      rbx, gword ptr [rbp-0xD0]
       mov      r14, gword ptr [rbp-0xD8]
       cmp      byte  ptr [r14+0x9D], 0
       je       G_M000_IG50
 
G_M000_IG36:
       mov      rdi, r12
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       test     eax, eax
       je       G_M000_IG99
 
G_M000_IG37:
       mov      dword ptr [rbp-0x88], eax
 
G_M000_IG38:
       mov      ecx, dword ptr [r12+0x08]
       mov      dword ptr [rbp-0x7C], ecx
       mov      rdi, rbx
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       mov      ecx, eax
       test     ecx, ecx
       je       G_M000_IG100
 
G_M000_IG39:
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x88]
       mov      edx, dword ptr [rbp-0x7C]
       mov      r8d, 4
       call     [System.Buffers.ArrayPoolEventSource:BufferRented(int,int,int,int):this]
       jmp      G_M000_IG50
 
G_M000_IG40:
       inc      ebx
       mov      eax, dword ptr [rbp-0xB0]
       cmp      eax, ebx
       je       SHORT G_M000_IG42
 
G_M000_IG41:
       mov      ecx, dword ptr [rbp-0xB8]
       dec      ecx
       mov      dword ptr [rbp-0xB8], ecx
       jne      G_M000_IG27
       jmp      G_M000_IG98
 
G_M000_IG42:
       xor      ebx, ebx
       jmp      SHORT G_M000_IG41
 
G_M000_IG43:
       mov      rbx, gword ptr [rbp-0xD0]
 
G_M000_IG44:
       mov      esi, 256
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWARR_1_VC
       mov      r12, rax
       mov      r14, gword ptr [rbp-0xD8]
       cmp      byte  ptr [r14+0x9D], 0
       je       G_M000_IG50
 
G_M000_IG45:
       mov      rdi, r12
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       test     eax, eax
       je       G_M000_IG64
 
G_M000_IG46:
       mov      dword ptr [rbp-0x8C], eax
 
G_M000_IG47:
       mov      rdi, rbx
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       mov      ecx, eax
       test     ecx, ecx
       je       G_M000_IG65
 
G_M000_IG48:
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x8C]
       mov      edx, 256
       mov      r8d, -1
       call     [System.Buffers.ArrayPoolEventSource:BufferRented(int,int,int,int):this]
       mov      rdi, rbx
       call     System.Runtime.CompilerServices.RuntimeHelpers:TryGetHashCode(System.Object):int
       mov      ecx, eax
       test     ecx, ecx
       je       G_M000_IG66
 
G_M000_IG49:
       mov      rdi, gword ptr [rbx+0x10]
       mov      r9d, 1
       mov      esi, 2
       cmp      dword ptr [rdi+0x08], 4
       cmovg    r9d, esi
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x8C]
       mov      edx, 256
       mov      r8d, -1
       call     [System.Buffers.ArrayPoolEventSource:BufferAllocated(int,int,int,int,int):this]
 
G_M000_IG50:
       mov      gword ptr [rbp-0x70], r12
       lea      rdi, bword ptr [r12+0x10]
       mov      esi, dword ptr [r12+0x08]
       mov      bword ptr [rbp-0x60], rdi
       mov      dword ptr [rbp-0x58], esi
       xor      edi, edi
       mov      dword ptr [rbp-0x68], edi
       mov      byte  ptr [rbp-0x64], 0
       mov      edi, dword ptr [rbp-0x68]
       cmp      edi, dword ptr [rbp-0x58]
       ja       G_M000_IG121
       mov      rsi, bword ptr [rbp-0x60]
       mov      eax, edi
       lea      rsi, bword ptr [rsi+2*rax]
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, edi
       cmp      eax, 38
       jb       G_M000_IG101
 
G_M000_IG51:
       vmovups  zmm0, zmmword ptr [reloc @RWD00]
       vmovups  zmmword ptr [rsi], zmm0
       mov      rdi, 0xD1FFAB1E
       mov      qword ptr [rsi+0x40], rdi
       mov      dword ptr [rsi+0x48], 0xD1FFAB1E
       mov      edi, dword ptr [rbp-0x68]
       add      edi, 38
       mov      dword ptr [rbp-0x68], edi
 
G_M000_IG52:
       lea      rdi, [rbp-0x78]
       mov      esi, 2
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:AppendFormatted[long](long):this]
       mov      edi, dword ptr [rbp-0x68]
       cmp      edi, dword ptr [rbp-0x58]
       ja       G_M000_IG121
       mov      rsi, bword ptr [rbp-0x60]
       mov      eax, edi
       lea      rsi, bword ptr [rsi+2*rax]
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, edi
       cmp      eax, 6
       jb       G_M000_IG102
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       mov      qword ptr [rsi], rdi
       mov      dword ptr [rsi+0x08], 0xD1FFAB1E
       mov      edi, dword ptr [rbp-0x68]
       add      edi, 6
       mov      dword ptr [rbp-0x68], edi
 
G_M000_IG54:
       lea      rdi, [rbp-0x78]
       mov      rsi, r15
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:AppendFormatted[long](long):this]
       mov      edi, dword ptr [rbp-0x68]
       cmp      edi, dword ptr [rbp-0x58]
       ja       G_M000_IG121
       mov      rsi, bword ptr [rbp-0x60]
       mov      eax, edi
       lea      rsi, bword ptr [rsi+2*rax]
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, edi
       je       G_M000_IG103
 
G_M000_IG55:
       mov      word  ptr [rsi], 41
       mov      edi, dword ptr [rbp-0x68]
       inc      edi
       mov      dword ptr [rbp-0x68], edi
 
G_M000_IG56:
       mov      esi, dword ptr [rbp-0x68]
       cmp      esi, dword ptr [rbp-0x58]
       ja       G_M000_IG121
       mov      rdi, bword ptr [rbp-0x60]
       call     System.String:.ctor(System.ReadOnlySpan`1[char]):this
       mov      gword ptr [rbp-0x100], rax
       mov      r15, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x108], r15
       xor      rdi, rdi
       mov      gword ptr [rbp-0x70], rdi
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
 
G_M000_IG57:
       mov      dword ptr [rbp-0x68], edi
       test     r15, r15
       mov      rbx, gword ptr [rbp-0x100]
       je       G_M000_IG63
 
G_M000_IG58:
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      gword ptr [rbp-0x110], r14
       cmp      byte  ptr [r14], r14b
       mov      edi, dword ptr [r15+0x08]
       dec      edi
       or       edi, 15
       xor      r12d, r12d
       lzcnt    r12d, edi
       xor      r12d, 31
       add      r12d, -3
       mov      r13, bword ptr [rbp-0x140]
       mov      rax, gword ptr [r13+0x10]
       test     rax, rax
       je       G_M000_IG104
 
G_M000_IG59:
       xor      r13d, r13d
       mov      dword ptr [rbp-0x90], 1
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, r12d
       jbe      SHORT G_M000_IG62
 
G_M000_IG60:
       mov      r13d, 1
       mov      esi, 16
       shlx     esi, esi, r12d
       cmp      dword ptr [r15+0x08], esi
       jne      G_M000_IG105
       cmp      r12d, edi
       jae      G_M000_IG129
       mov      edi, r12d
       shl      rdi, 4
       lea      rax, bword ptr [rax+rdi+0x10]
       mov      bword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rax]
       mov      gword ptr [rbp-0x128], rdx
       mov      rdi, rax
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       xor      edi, edi
       mov      rsi, bword ptr [rbp-0x120]
       mov      dword ptr [rsi+0x08], edi
       cmp      gword ptr [rbp-0x128], 0
       jne      G_M000_IG68
 
G_M000_IG61:
       mov      r15, gword ptr [rbp-0x108]
 
G_M000_IG62:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x118], rax
       cmp      byte  ptr [rax+0x9D], 0
       jne      G_M000_IG115
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, r15
       mov      rsi, rbx
       call     [RyuPy.Runtime.PyValueError:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG64:
       mov      rdi, r12
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      dword ptr [rbp-0x8C], eax
       jmp      G_M000_IG47
 
G_M000_IG65:
       mov      rdi, rbx
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      ecx, eax
       jmp      G_M000_IG48
 
G_M000_IG66:
       mov      rdi, rbx
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      ecx, eax
       jmp      G_M000_IG49
 
G_M000_IG67:
       mov      r12, rax
       jmp      G_M000_IG50
 
G_M000_IG68:
       mov      rdi, gword ptr [r14+0x10]
       cmp      r12d, dword ptr [rdi+0x08]
       jae      G_M000_IG129
       mov      esi, r12d
       mov      rcx, gword ptr [rdi+8*rsi+0x10]
       test     rcx, rcx
       je       G_M000_IG106
 
G_M000_IG69:
       mov      rcx, gword ptr [rcx+0x08]
       mov      gword ptr [rbp-0x130], rcx
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG107
 
G_M000_IG70:
       cmp      byte  ptr [(reloc)], 0
       je       G_M000_IG109
       mov      rdx, 0xD1FFAB1E
 
G_M000_IG71:
       vzeroupper 
       call     rdx ; Interop+Sys:SchedGetCpu():int
       mov      ebx, eax
       cmp      dword ptr [(reloc)], 0
       jne      G_M000_IG108
 
G_M000_IG72:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG111
 
G_M000_IG73:
       mov      eax, ebx
       xor      edx, edx
       div      edx:eax, dword ptr [(reloc)]
       mov      ebx, edx
       mov      rax, gword ptr [rbp-0x130]
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xB4], ecx
       test     ecx, ecx
       jle      G_M000_IG114
       mov      dword ptr [rbp-0xBC], ecx
 
G_M000_IG74:
       cmp      ebx, ecx
       jae      G_M000_IG129
       mov      edi, ebx
       mov      rsi, gword ptr [rax+8*rdi+0x10]
       mov      gword ptr [rbp-0x138], rsi
       cmp      byte  ptr [rsi], sil
       xor      r8d, r8d
       mov      dword ptr [rbp-0xA4], r8d
       mov      rdi, rsi
       call     System.Threading.Monitor:TryEnter_FastPath(System.Object):bool
       test     eax, eax
       jne      SHORT G_M000_IG76
 
G_M000_IG75:
       mov      rdi, gword ptr [rbp-0x138]
       call     [System.Threading.Monitor:Enter_Slowpath(System.Object)]
 
G_M000_IG76:
       mov      rax, gword ptr [rbp-0x138]
       mov      rdi, gword ptr [rax+0x08]
       mov      ecx, dword ptr [rax+0x10]
       mov      dword ptr [rbp-0xA8], ecx
       cmp      dword ptr [rdi+0x08], ecx
       jbe      SHORT G_M000_IG79
 
G_M000_IG77:
       test     ecx, ecx
       je       G_M000_IG83
 
G_M000_IG78:
       movsxd   rsi, ecx
       lea      rdi, bword ptr [rdi+8*rsi+0x10]
       mov      rsi, gword ptr [rbp-0x128]
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, dword ptr [rbp-0xA8]
       inc      edi
       mov      rax, gword ptr [rbp-0x138]
       mov      dword ptr [rax+0x10], edi
       mov      dword ptr [rbp-0xA4], 1
 
G_M000_IG79:
       mov      rdi, rax
       call     System.Threading.Monitor:Exit_FastPath(System.Object):int
       test     eax, eax
       je       SHORT G_M000_IG81
 
G_M000_IG80:
       mov      edi, eax
       mov      rsi, gword ptr [rbp-0x138]
       call     [System.Threading.Monitor:Exit_Slowpath(int,System.Object)]
 
G_M000_IG81:
       cmp      dword ptr [rbp-0xA4], 0
       je       G_M000_IG112
       mov      ecx, 1
 
G_M000_IG82:
       mov      dword ptr [rbp-0x90], ecx
       mov      rbx, gword ptr [rbp-0x100]
       mov      r14, gword ptr [rbp-0x110]
       mov      r15, gword ptr [rbp-0x108]
       jmp      G_M000_IG62
 
G_M000_IG83:
       xor      esi, esi
       mov      dword ptr [rax+0x14], esi
       jmp      G_M000_IG78
 
G_M000_IG84:
       mov      edi, dword ptr [(reloc)]
       mov      r14d, edi
       test     r14d, r14d
       jg       G_M000_IG125
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r13, rax
       lea      rdi, bword ptr [r13+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [r13+0x18]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      r15, qword ptr [rbp+0xC0]
       test     r15, r15
       jg       G_M000_IG127
 
G_M000_IG86:
       mov      rax, rbx
 
G_M000_IG87:
       vzeroupper 
       add      rsp, 552
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_GCSTATIC_BASE]
       jmp      G_M000_IG09
 
G_M000_IG89:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_GCSTATIC_BASE]
       jmp      G_M000_IG10
 
G_M000_IG90:
       mov      edi, 2
       call     [CORINFO_HELP_GETDYNAMIC_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED]
       mov      r13, rax
       jmp      G_M000_IG11
 
G_M000_IG91:
       mov      rdi, gword ptr [rbp-0xE0]
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      r12d, eax
       jmp      G_M000_IG16
 
G_M000_IG92:
       mov      rdi, rbx
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      ecx, eax
       jmp      G_M000_IG17
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_NONGCSTATIC_BASE]
       jmp      G_M000_IG22
 
G_M000_IG94:
       call     CORINFO_HELP_POLL_GC
       jmp      G_M000_IG24
 
G_M000_IG95:
       mov      edi, 3
       call     [CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED]
       mov      eax, dword ptr [rax+0x10]
       mov      dword ptr [rbp-0x84], eax
       mov      edi, 3
       call     [CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED]
       mov      ecx, dword ptr [rbp-0x84]
       lea      edx, [rcx-0x01]
       mov      dword ptr [rax+0x10], edx
       movzx    rax, cx
       test     eax, eax
       jne      SHORT G_M000_IG96
       call     [System.Threading.ProcessorIdCache:RefreshCurrentProcessorId():int]
       mov      edx, eax
       mov      ebx, edx
       jmp      G_M000_IG24
 
G_M000_IG96:
       mov      edx, ecx
       sar      edx, 16
       mov      ebx, edx
       jmp      G_M000_IG24
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_NONGCSTATIC_BASE]
       jmp      G_M000_IG25
 
G_M000_IG98:
       xor      rax, rax
       jmp      G_M000_IG34
 
G_M000_IG99:
       mov      rdi, r12
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      dword ptr [rbp-0x88], eax
       jmp      G_M000_IG38
 
G_M000_IG100:
       mov      rdi, rbx
       call     [System.Runtime.CompilerServices.RuntimeHelpers:<GetHashCode>g__GetHashCodeWorker|15_0(System.Object):int]
       mov      ecx, eax
       jmp      G_M000_IG39
 
G_M000_IG101:
       lea      rdi, [rbp-0x78]
       mov      rsi, 0xD1FFAB1E
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:GrowThenCopyString(System.String):this]
       jmp      G_M000_IG52
 
G_M000_IG102:
       lea      rdi, [rbp-0x78]
       mov      rsi, 0xD1FFAB1E
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:GrowThenCopyString(System.String):this]
       jmp      G_M000_IG54
 
G_M000_IG103:
       lea      rdi, [rbp-0x78]
       mov      rsi, 0xD1FFAB1E
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:GrowThenCopyString(System.String):this]
       jmp      G_M000_IG56
 
G_M000_IG104:
       mov      rdi, r14
       call     [System.Buffers.SharedArrayPool`1[char]:InitializeTlsBucketsAndTrimming():System.Buffers.SharedArrayPoolThreadLocalArray[]:this]
       jmp      G_M000_IG59
 
G_M000_IG105:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       call     [System.SR:get_ArgumentException_BufferNotFromPool():System.String]
       mov      r14, rax
       mov      edi, 0x46D
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r15
       call     [System.ArgumentException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG106:
       mov      rdi, r14
       mov      esi, r12d
       call     [System.Buffers.SharedArrayPool`1[char]:CreatePerCorePartitions(int):System.Buffers.SharedArrayPoolPartitions:this]
       mov      rcx, rax
       mov      r14, gword ptr [rbp-0x110]
       jmp      G_M000_IG69
 
G_M000_IG107:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_NONGCSTATIC_BASE]
       jmp      G_M000_IG70
 
G_M000_IG108:
       call     CORINFO_HELP_POLL_GC
       jmp      G_M000_IG72
 
G_M000_IG109:
       mov      edi, 3
       call     [CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED]
       mov      eax, dword ptr [rax+0x10]
       mov      dword ptr [rbp-0xA0], eax
       mov      edi, 3
       call     [CORINFO_HELP_GETDYNAMIC_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED]
       mov      ecx, dword ptr [rbp-0xA0]
       lea      edx, [rcx-0x01]
       mov      dword ptr [rax+0x10], edx
       movzx    rax, cx
       test     eax, eax
       jne      SHORT G_M000_IG110
       call     [System.Threading.ProcessorIdCache:RefreshCurrentProcessorId():int]
       mov      edx, eax
       mov      ebx, edx
       jmp      G_M000_IG72
 
G_M000_IG110:
       mov      edx, ecx
       sar      edx, 16
       mov      ebx, edx
       jmp      G_M000_IG72
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_GET_NONGCSTATIC_BASE]
       jmp      G_M000_IG73
 
G_M000_IG112:
       inc      ebx
       mov      edi, dword ptr [rbp-0xB4]
       cmp      edi, ebx
       jne      SHORT G_M000_IG113
       xor      ebx, ebx
 
G_M000_IG113:
       mov      edx, dword ptr [rbp-0xBC]
       dec      edx
       mov      dword ptr [rbp-0xBC], edx
       mov      ecx, edi
       mov      rax, gword ptr [rbp-0x130]
       jne      G_M000_IG74
 
G_M000_IG114:
       xor      ecx, ecx
       jmp      G_M000_IG82
 
G_M000_IG115:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG63
       mov      rdi, r15
       call     [System.Runtime.CompilerServices.RuntimeHelpers:GetHashCode(System.Object):int]
       mov      dword ptr [rbp-0x94], eax
       mov      ecx, dword ptr [r15+0x08]
       mov      dword ptr [rbp-0x98], ecx
       cmp      byte  ptr [r14], r14b
       mov      rdi, r14
       call     [System.Runtime.CompilerServices.RuntimeHelpers:GetHashCode(System.Object):int]
       mov      r8d, eax
       mov      rdi, gword ptr [rbp-0x118]
       mov      edx, dword ptr [rbp-0x94]
       mov      ecx, dword ptr [rbp-0x98]
       mov      esi, 3
       call     [System.Diagnostics.Tracing.EventSource:WriteEvent(int,int,int,int):this]
       test     r13d, dword ptr [rbp-0x90]
       jne      G_M000_IG63
       mov      rdi, r15
       call     [System.Runtime.CompilerServices.RuntimeHelpers:GetHashCode(System.Object):int]
       mov      dword ptr [rbp-0x9C], eax
       mov      r15d, dword ptr [r15+0x08]
       mov      rdi, r14
       call     [System.Runtime.CompilerServices.RuntimeHelpers:GetHashCode(System.Object):int]
       mov      ecx, eax
       test     r13d, r13d
       jne      SHORT G_M000_IG116
       mov      r8d, -1
       mov      r9d, 1
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:
       mov      r8d, r12d
       xor      r9d, r9d
 
G_M000_IG117:
       mov      rdi, gword ptr [rbp-0x118]
       mov      esi, dword ptr [rbp-0x9C]
       mov      edx, r15d
       cmp      dword ptr [rdi], edi
       call     [System.Buffers.ArrayPoolEventSource:BufferDropped(int,int,int,int,int):this]
       jmp      G_M000_IG63
 
G_M000_IG118:
       lea      rdi, [rbp-0x50]
       mov      esi, 37
       mov      edx, 1
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:.ctor(int,int):this]
       mov      edi, dword ptr [rbp-0x40]
       cmp      edi, dword ptr [rbp-0x30]
       ja       SHORT G_M000_IG121
       mov      rsi, bword ptr [rbp-0x38]
       mov      eax, edi
       lea      rsi, bword ptr [rsi+2*rax]
       mov      eax, dword ptr [rbp-0x30]
       sub      eax, edi
       cmp      eax, 36
       jb       SHORT G_M000_IG119
       vmovups  zmm0, zmmword ptr [reloc @RWD64]
       vmovups  zmmword ptr [rsi], zmm0
       mov      rdi, 0xD1FFAB1E
       mov      qword ptr [rsi+0x40], rdi
       mov      edi, dword ptr [rbp-0x40]
       add      edi, 36
       mov      dword ptr [rbp-0x40], edi
       jmp      SHORT G_M000_IG120
 
G_M000_IG119:
       lea      rdi, [rbp-0x50]
       mov      rsi, 0xD1FFAB1E
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:GrowThenCopyString(System.String):this]
 
G_M000_IG120:
       lea      rdi, [rbp-0x50]
       mov      esi, 2
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:AppendFormatted[long](long):this]
       mov      edi, dword ptr [rbp-0x40]
       cmp      edi, dword ptr [rbp-0x30]
       jbe      SHORT G_M000_IG122
 
G_M000_IG121:
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG122:
       mov      rsi, bword ptr [rbp-0x38]
       mov      eax, edi
       lea      rsi, bword ptr [rsi+2*rax]
       mov      eax, dword ptr [rbp-0x30]
       sub      eax, edi
       je       SHORT G_M000_IG123
       mov      word  ptr [rsi], 41
       mov      edi, dword ptr [rbp-0x40]
       inc      edi
       mov      dword ptr [rbp-0x40], edi
       jmp      SHORT G_M000_IG124
 
G_M000_IG123:
       lea      rdi, [rbp-0x50]
       mov      rsi, 0xD1FFAB1E
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:GrowThenCopyString(System.String):this]
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       lea      rdi, [rbp-0x50]
       call     [System.Runtime.CompilerServices.DefaultInterpolatedStringHandler:ToStringAndClear():System.String:this]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyValueError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG125:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rdi, rsi
       lea      ecx, [r14-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG129
       cmp      dword ptr [rdi+4*rdx+0x10], 10
       je       G_M000_IG85
 
G_M000_IG126:
       mov      rdi, rsi
       mov      esi, ecx
       cmp      esi, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG129
       mov      dword ptr [rdi+4*rsi+0x10], 10
       jmp      G_M000_IG85
 
G_M000_IG127:
       movsxd   rax, r14d
       cmp      r15, rax
       jg       G_M000_IG86
 
G_M000_IG128:
       dec      r15d
       mov      dword ptr [(reloc)], r15d
       jmp      G_M000_IG86
 
G_M000_IG129:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG130:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	00200074006F006Eh, 0075006F006E0065h, 0076002000680067h, 00650075006C0061h, 006F007400200073h, 0070006E00750020h, 0020006B00630061h, 0070007800650028h
RWD64  	dq	0020006F006F0074h, 0079006E0061006Dh, 006C006100760020h, 0020007300650075h, 00750020006F0074h, 006300610070006Eh, 006500280020006Bh, 0063006500700078h

; Total bytes of code 3727

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 0 inlinees with PGO data; 3 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 64
       mov      qword ptr [rsp+0x128], r15
       mov      qword ptr [rsp+0x120], r14
       mov      qword ptr [rsp+0x118], r13
       mov      qword ptr [rsp+0x110], r12
       mov      qword ptr [rsp+0x108], rbx
       lea      rbp, [rsp+0x40]
       mov      rbx, gword ptr [rbp+0xB8]
       mov      r15, gword ptr [rbp+0xB0]
       mov      rax, gword ptr [rbp+0xA8]
       mov      r14, qword ptr [rbp+0x78]
       mov      r13, qword ptr [rbp+0x70]
       mov      r12, qword ptr [rbp+0x68]
 
G_M000_IG02:
       test     r12, r12
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0x2C], edx
       mov      r8d, edx
       cmp      r14, r13
       setl     dil
       movzx    rdi, dil
       and      edi, r8d
       cmp      r14, r13
       setg     sil
       movzx    rsi, sil
       test     r8d, r8d
       sete     r9b
       movzx    r9, r9b
       mov      dword ptr [rbp-0x30], r9d
       and      esi, r9d
       or       edi, esi
       jne      G_M000_IG11
 
G_M000_IG03:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG06
 
G_M000_IG04:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG14
       cmp      dword ptr [rdi+4*r8+0x10], 17
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG14
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 17
 
G_M000_IG06:
       mov      rbx, qword ptr [rbp+0x98]
       test     rbx, rbx
       jle      SHORT G_M000_IG09
 
G_M000_IG07:
       movsxd   rcx, ecx
       cmp      rbx, rcx
       jg       SHORT G_M000_IG09
 
G_M000_IG08:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG09:
       add      rsp, 264
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x38], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x38]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x40], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x38]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       add      r14, r12
       jo       SHORT G_M000_IG15
       mov      edx, dword ptr [rbp-0x2C]
       mov      r8d, edx
       cmp      r14, r13
       setl     cl
       movzx    rcx, cl
       and      r8d, ecx
       cmp      r14, r13
       setg     cl
       movzx    rcx, cl
       mov      r9d, dword ptr [rbp-0x30]
       and      ecx, r9d
       or       ecx, r8d
       je       G_M000_IG03
 
G_M000_IG11:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      G_M000_IG10
 
G_M000_IG12:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG14
       cmp      dword ptr [rax+4*r8+0x10], 16
       je       G_M000_IG10
 
G_M000_IG13:
       cmp      edi, dword ptr [rsi+0x08]
       jae      SHORT G_M000_IG14
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 16
       jmp      G_M000_IG10
 
G_M000_IG14:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG15:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 508

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

