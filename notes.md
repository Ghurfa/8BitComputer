# Stack stuff
- No base pointer register
  - All local vars are referenced relative to the stack pointer
- Stack grows upward

# Calling Convention
## Caller
- Arguments are placed in `r0`, `r1`, and `r2`
  - More than 3 arguments is not supported
- The `call` instruction pushes `pc + 1` (the address of the instruction after the `call` instruction) and jumps to the callee
## Callee
- Arguments are stored in local variables if necessary (e.g. `push r0`)
- Extra space for local variables is made on the stack at the beginning of the function if necessary (e.g. `incr3 2`)
- The (optional) return value is placed in `r0`
- At the end of the function, `r3` should be equal to its value at the start of the function
- The `ret` instruction pops the return address from the stack and jumps there