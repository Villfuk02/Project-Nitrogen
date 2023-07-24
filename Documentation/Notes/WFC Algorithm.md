#MVP
#system [[World Generation]]

randomised constraint solving programming

- *slots* offset compared to *tiles* 
    - less distinct variants
    - more control over transitions

- set-up modules in editor
    - generate rotations and flips at start

ALGORITHM
- prepare *modules* 
- generate grid of *slots*, mark them as *uncollapsed*
- each *slot* can become one of many *modules* 
    - the *modules* that can be placed in a given slot are its *domain* 
    - at the start each *slot* has all *modules* in its *domain*
    - from the *domain*, compute all possible *boundary conditions* 
        - for example - there is a *module* in the *domain* with a cliff on its east boundary, so mark cliff to the east as possible
- mark all *slots* as *dirty*
- then repeat:
    - propagate constraints
        - for each *dirty slot*, until there are none:
            - mark as *not dirty*
            - find out which *modules* from its *domain* can be placed here and remove the rest from its *domain*
                - decide only by neighbors' (orthogonal and diagonal) *boundary conditions*
            - update *boundary conditions*
            - if *boundary conditions* changed, mark all *uncollapsed* neighbors as *dirty*
    - *collapse* a slot
        - save the current *state* of all *slots* on a stack
        - pick a *slot*
        - pick one *module* from its *domain* at random
            - weighted - provides control
        - remove each other *module* from its *domain*
        - update *boundary conditions*
        - mark *uncollapsed* neighbors as *dirty*
- if a *slot* ends up with 0 *modules* in its *domain*, backtrack
    - pop a prevoiusly saved *state* from the stack and revert to it
    - remove the previously chosen *module* from the *domain* of the previously *collapsed slot*

NOTES
- keep slots sorted by entropy
- each slot has possibilities per constraint
    - [[Tile]] height x4
    - [[Tile]] type x4 (can be unpassable)
    - [[Tile]] slant x4
    - passages x4
- each slot updates possible modules lazily