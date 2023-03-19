#MVP
#system 

Placed on [[Tile]]s during generation
Ores and other obstacles
similar to [[Decoration]]
models placed by [[Scatterer]]

Different types defined for each [[Terrain Type]]

Constraints
- specific [[Tile]] type
- non-slant / slant

"Forces" used to affect generation
 - attracted / repelled by others


ALGORITHM
- set up as a few stages
    - each stage has:
        - one type of blocker (e.g. ore, small rocks, big rocks)
        - *min* and *max* amounts
        - *base chance* to place
        - whether they can be placed on slanted tiles
        - which [[Terrain Type]]s they can be on (currently there is only one)
        - *forces* - effect on chance based on already placed blockers
            - for example: negative force with magnitude *m* from stage *s* means the chance to place a blocker on a given tile is decreased by *m/d*  for each blocker placed in stage *s*, where *d* is its distance from the considered tile
- for each stage:
    - repeat until at least min blockers have been placed (in this stage)
    - for each tile without a path or blocker (in random order):
        - if random number between 0 and 1 < modified chance:
            - place the blocker of the given type
            - if there are max blockers (placed in this stage), end the stage
- then an additional stage
    - assume all tiles not on paths are blocked
    - repeat in random order
        - make one not blocked
        - recalculate distance from the center to each path start
        - if any of them gets shorter, this tile must remain blocked, so place a blocker
    - this ensures the shortest paths are the right length