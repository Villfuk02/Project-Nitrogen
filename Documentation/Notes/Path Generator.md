#MVP 
#system [[World Generation]]

Generate paths for [[Attacker]]s to move along

ALGORITHM
- first pick *start points* from along the edges of the level (all paths end in the center of the level)
    - choose randomly from positions with the correct parity (even / odd path length)
    - spread them out by removing all positions near already chosen one
- then generate paths
    - mark all tiles as *not picked*
    - for each path create a *planned path* 
        - a *finished* planned path consists of a list of tile positions the path vists in order
            - no gaps
            - doesn't have to lead to the center of the level, the last few positions can be implied
        - planned path is a doubly linked list with *tile positions* and their *distances* from the end
        - it also remembers one of the positions as the *previous position* and one as the *next position*
        - at first it contains only:
            - one tile next to the center, in the direction of the path start point, with distance 1
                - when generating more than 4 paths, instead position 2 tiles from the center is chosen, with distance 2
            - the path start point, with distance equal to the path length
            - mark these tiles as *picked*
    - repeat until all planned paths are finished:
        - choose a random planned path
            - weighted, prioritizing those which have the least ratio of picked tile positions to total planned length
            - if it has no next position, set the first one as previous and the second one as the next
            - else
                - pick a random number *d* between the previous position distance (*ppd*) and next position distance (*npd*)
                - randomly select a position
                    - that is reachable from previous in *d - ppd* steps and from next in *npd - d* steps
                        - not going through already picked positions
                - if no viable position exists
                    - if previous isn't the first position in this planned path, remove it and mark its tile as *not picked*
                    - if next isn't the last position in this planned path, remove it and mark its tile as *not picked*
                - else
                    - insert the new position into the list after previous and before next, with distance *d*
                    - mark its tile as *picked*
            - while the distances of the prevoius position and the next position differ only by 1:
                - set previous to next and next to the position after next (can become null)
        - if this loops too many times, restart the whole process, because a solution might not exist

now, the paths are prepared and [[WFC Algorithm]] will generate the terrain in a way that ensures these paths are passable

- Then finalise paths
    - calculate the distance of each tile from the center
    - DFS from path starts to center, only moving to tiles one step closer to the center
        - first found path is saved
        - then brancing is attempted
        - whenever a branch would join an existing path, it is only allowed to do so if it is at least 4 tiles after it last split off