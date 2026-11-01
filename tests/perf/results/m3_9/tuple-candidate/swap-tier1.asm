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
