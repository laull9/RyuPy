public = 'before def'
import binding_cycle_b
observed_before = binding_cycle_b.seen
def public():
    return 'after def'
