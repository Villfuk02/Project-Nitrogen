#LOG

## Game concept
- Tower defense
    - Plants vs Zombies
    - Bloons TD
    - flash games
- Rogue-Like
    - Slay the spire
    - FTL
    - Crypt of the necrodancer

the player needs to defend for some time and then move on to the next battle
    -> have to stop to collect more resources for further travel
        -> balance defense and offense (collect resources faster but neglect defense)

## [[Battle]]
- Enemy movement options
    - Enemies coming from all sides with individual pathfinding ‎❌
        - too unpredictable for the player 
            The player needs to plan in advance in order to maximize the possibility to take calculated risks
        - solution: visualize enemy paths 
            Too many different paths - too much clutter
            Still planning at most one wave in advance
    - Enemies come from one side in distinct lanes ❌
        - not enough space for interesting building placement 
            just one degree of freedom
    - A few predefined paths in 2D ✅
        - they can also merge or split

 - Need for granularity - *tile* based levels ✅ (as opposed to unrestricted)
    - each tile can only have one building
    - enemy paths and terrain also can't have more detail than the size of a tile

- 3D terrain ✅
    - Simple and intuitive way to make the level itself more interesting
        Some towers won't be able to shoot uphill or downhill - more interesting decisions for the player


## [[Level Generation]]
Consists of multiple steps

### Terrain
- Fractal noise ❌
    - There are some rules the levels need to follow
        - I would have to generate the terrain and then modify it to conform to constraints
    - https://www.youtube.com/playlist?list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3 , https://github.com/SebLague/Procedural-Landmass-Generation (2.3.23)
- Wave-Function Collapse (WFC) ✅ 
    - Randomised Constraint Solving Programming
    - https://github.com/mxgmn/WaveFunctionCollapse (2.3.23)
    - https://oskarstalberg.com/game/wave/wave.html (2.3.23)

### [[WFC Algorithm]]
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

- *slots* offset compared to *tiles* 
    - less distinct variants
    - more control over transitions

- set-up modules in editor
    - generate rotations and flips at start

- using coroutines to be able to step through th generation process

- At first displayed as 2D sprites

- Started with "passable" constraints ([video 01](https://drive.google.com/file/d/1aUbt1D6VqryiPx3p48OESA6Koo1_bR_U/view?usp=share_link), [video 02](https://drive.google.com/file/d/1NxLuShSfvmU98hDadEoGi3lO0KYX-45H/view?usp=share_link))
    - enemies can't climb up or down cliffs
- Later added terrain heights ([video 03](https://drive.google.com/file/d/1qWk-V4ExRoStWleThw-vnjyptBmdLkww/view?usp=share_link))

- Which slot to collapse?
    - fail fast approach
        - prioritize slot with least options
        - changed to slot with least entropy, because that's more accurate
        - slots near most constraining modules were prioritized, making them more common and leading to repetitive terrain features
    - better to just collapse a random slot ([video 04](https://drive.google.com/file/d/1WWfNP4MNy2YMBvMlyDKFH9OnVFFaPaOA/view?usp=share_link))
    - in the end still weighted by entropy
        - at first I tried to prefer slots with more entropy
            - define overall structure first by sparsely covering the world, then fill in details
            - often led to deep dead-ends with a lot of backtracking
        - in the end, tiles with less entropy are preferred

- Limited backtracking depth 
    - usually when more backtracking is required, the search would take too long and it's faster to restart the algorithm

- slants ([video 05](https://drive.google.com/file/d/1mni0c9OLa1kbcEjpuNMODQy0EAVyF3iX/view?usp=share_link))
    - enemies need a smooth ramp to climb up or down terrain

this is actually the 2nd step when generating the level, but I implemented it first

### [[Path Generator]] ([video 06](https://drive.google.com/file/d/1_5gqkUeMoYIyjKYKoD9rB_CNYFdWax2P/view?usp=share_link))
the 1st step
- generate fixed number of paths with given lengths
- these are just plans to ensure paths of required length exist

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

- then generate terrain such that the paths aren't blocked

### [[Terrain Blocker]]s ([video 07](https://drive.google.com/file/d/1SF2ssneR83ZIX0ZFKJetCYz9MsIJdi-0/view?usp=share_link)) 
the 3rd step
- after terrain generation, place blockers on tiles
    - materials for the player to mine
    - just rocks for variety - the player cant build on these

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

### 3D-fication ([video 08](https://drive.google.com/file/d/18S1WyBbxvU4jj6ue3GDhy1m_PwhlXwuZ/view?usp=share_link))
- so far the level was displayed with simple 2D sprites and unity gizmos (lines)
- switching to 3D wasn't so difficult in code, because I anticipated it
- the most time-consuming part was making simple models and orienting them properly

- [[Scatterer]] ([image 10](https://drive.google.com/file/d/1quDf0rqDpr6ax1N53ssnwL6P2YRin_jL/view?usp=share_link)) 
the 4th step
- For the blockers, I didn't want repetitive obstacle models, so they are generated proceduraly by scattering many simpler models (decorations) on each tile

- first compute weights based on various factors ([image 09](https://drive.google.com/file/d/1wDKBnXr5L354BryV9LqixjqxVWBHhlmu/view?usp=share_link))
    - distance to path
    - height
    - distance to other blockers
- customizable thanks to modular approach

- then scatter decorations in stages, each stage again having one type of decoration and many parameters
    - for each tile in random order repeat x (specified for this stage) times:
        - pick a random position within it
            - calcualte the weight at this position (based on settings)
            - check that it is greater than some threshold (based on settings)
            - calculate the minimum distance to other decorations (from weight, based on settings)
            - check that the position is far enough from other decorations
            - calculate the decoration size (from weight, based on settings)
            - place the decoration on this position, with the given size

- I tried scattering decorative grass ([video 11](https://drive.google.com/file/d/17LniTN5iHf_n9-aTGm6tU8LlOh8W9OhF/view?usp=share_link), [image 12](https://drive.google.com/file/d/1m5Tqv1IPmuX9cUlbU9w9n-vd1ReXhs1E/view?usp=share_link))
    - low fps, better to do with shaders

## [[Level Generation]] - PHASE 2
DO EVERYTHING AGAIN, BUT BETTER

- Use JOBS instead of coroutines
    - allows for parallelism
    - can run uniterrupted when not stepping
    - unlike coroutines, can easily be nested

- How to transfer data to and from jobs? 
    - Jobs are structs and can only modify data directly stored in them
    - they can contain only unmanaged types, NativeArrays and NativeLists
    - at start copy data in, then copy it out
    - lots of repetitive code, so I made a helper class JobDataInterface
        - prepare Arrays and Lists and assign them to NativeArrays and NativeLists of a Job
            - specify if they are used as input or as output
        - then register the job itself
        - when the job is supposed to start, copies data from input arrays/lists to the job's native counterparts
        - provides information if the Job is finished, or if it failed
        - when the job is finished, copies data from the job to corresponding output arrays / lists

- How to display debug information
    - Unity Gizmos
        - but they can only ba drawn from the OnDrawGizmos callback
        - so I made a GizmoManager class
            - add stuff to draw into a list, even from jobs (when enabled)
            - then draw everything from the list in OnDrawGizmos and empty it

- Plan paths
    - basically the same as before 

- WFC 
    - basically the same as before
    - takes to longest to run, should be parallelised
    - It won't be simple, so it's not worth it yet

- Blockers
    - first stages pretty much the same
    - then an additional stage
        - assume all tiles not on paths are blocked
        - repeat in random order
            - make one not blocked
            - recalculate distance from the center to each path start
            - if any of them gets shorter, this tile must remain blocked, so place a blocker
    - this ensures the shortest paths are the right length

- Finalise paths
    - calculate the distance of each tile from the center
    - DFS from path starts to center, only moving to tiles closer to the center
        - first found path is saved
        - whenever a branch would join an existing path, it is only allowed to do so if it is at least 4 tiles after it last split off

- Scatterer
    - basically the same, but parallelized because I wanted to
    - for each stage put all tiles in a list
    - until it is empty
        - take a random subset such that no two picked tiles are adjecent (orthogonally and diagonally)
        - since they are not adjacent, they are independent
        - run scattering jobs in parallel - one for each of these tiles

New order of operations
1. Paths
2. Terrain
3. Blockers
4. Finalise paths
5. Scatter blocker models

## Game preparation

- Transfer data from generation to gameplay
    - Singleton WorldData object
    - at generation start set to null
    - then slowly filled out with data

- Assemble the scene (WorldBuilder)
    - done in parallel with generation
    - whenever new data appears in WorldData, use it to add more stuff to the world
    - place terrain models
    - place objects from scatterer
    - visualise paths ([image 13](https://drive.google.com/file/d/1Fy55vLD-yqXqLLYJ_h0HLU-jsLWzyKAe/view?usp=share_link))

- [[Camera Controller]] ([video 14](https://drive.google.com/file/d/1I3GP3dy-vdkpstibATpflEMG0bL92277/view?usp=share_link))
    - simple camera controls
    - movement, zoom, raotation in 90° increments