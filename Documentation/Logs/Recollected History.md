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

 - Need for granularity - tile based levels ✅ (as opposed to unrestricted)
    the player can learn that some tower shoots at some enemy once per each tile the enemy travels 
    -> damage dealt is proportional to tiles traveled within range 
    -> very predictable behavior

- 3D terrain ✅
    - Simple and intuitive way to make the level itself more interesting
        Some towers won't be able to shoot uphill or downhill - more interesting decisions for the player


## [[Level Generation]]
Consists of multiple steps

### Terrain
- Fractal noise ❌ **ALGO OVERVIEW MISSING**
    - There are some rules the levels need to follow
        - I would have to generate the terrain and then modify it to conform to constraints
    - https://www.youtube.com/playlist?list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3 , https://github.com/SebLague/Procedural-Landmass-Generation (2.3.23)
- Wave-Function Collapse ✅ (Randomised Constraint Solving Programming) **ALGO OVERVIEW MISSING**
    - start with constraints and randomise what's flexible
    - https://oskarstalberg.com/game/wave/wave.html (2.3.23)
    - https://github.com/mxgmn/WaveFunctionCollapse (2.3.23)

- Slots offset compared to tiles 
    - less distinct variants
    - more control over transitions

- module database in editor
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

actually the 2nd step when generating the level

### [[Path Generator]] ([video 06](https://drive.google.com/file/d/1_5gqkUeMoYIyjKYKoD9rB_CNYFdWax2P/view?usp=share_link)) **ALGO OVERVIEW MISSING**
the 1st step
- generate fixed number of paths with fixed lengths
- these are just plans to ensure paths of required length exist

- then generate terrain so they're not blocked

### [[Terrain Blocker]]s ([video 07](https://drive.google.com/file/d/1SF2ssneR83ZIX0ZFKJetCYz9MsIJdi-0/view?usp=share_link)) **ALGO OVERVIEW MISSING**
the 3rd step
- after terrain generation, place blockers on tiles
    - materials for the player to mine
    - just rocks for variety - the player cant build on these
- placed randomly, but not on paths

### 3D-fication ([video 08](https://drive.google.com/file/d/18S1WyBbxvU4jj6ue3GDhy1m_PwhlXwuZ/view?usp=share_link))
- so far the level was displayed with simple 2D sprites and unity gizmos (lines)
- switching to 3D wasn't so difficult in code, because I anticipated it
- the most time-consuming part was making simple models and orienting them properly

- [[Scatterer]] ([image 10](https://drive.google.com/file/d/1quDf0rqDpr6ax1N53ssnwL6P2YRin_jL/view?usp=share_link)) **ALGO OVERVIEW MISSING**
the 4th step
- For the blockers, I didn't want repetitive obstacle models, so they are generated proceduraly by scattering many simplar models on one tile
- first compute weights based on various factors ([image 09](https://drive.google.com/file/d/1wDKBnXr5L354BryV9LqixjqxVWBHhlmu/view?usp=share_link))
    - distance to path
    - height
    - distance to other blockers
- customizable thanks to modular approach
- then scatter based on the weight
    - affects where to place
    - affects minimum distance to other objects
    - affects objects size
- I tried scattering decorative grass ([video 11](https://drive.google.com/file/d/17LniTN5iHf_n9-aTGm6tU8LlOh8W9OhF/view?usp=share_link), [image 12](https://drive.google.com/file/d/1m5Tqv1IPmuX9cUlbU9w9n-vd1ReXhs1E/view?usp=share_link))
    - low fps, better to do with shaders

## [[Level Generation]] - PHASE 2
DO EVERYTHING AGAIN, BUT BETTER

- Use JOBS instead of coroutines
    - allofs for parallelism
    - can run uniterrupted when not stepping

- How to transfer data between threads? 
    - **MISSING**

- Plan paths
    - basically the same as before 

- WFC 
    - basically the same as before
    - takes to longest to run, should be parallelised
    - It won't be simple, so it's not worth it yet

- Blockers **ALGO OVERVIEW MISSING**
    - block paths such that the planned ones are also the shortest

- Finalise paths **ALGO OVERVIEW MISSING**
    - some branching, but only where there are non-blocked paths of same length

- Scatterer **ALGO OVERVIEW MISSING**
    - very parallelized

New order of operations
1. Paths
2. Terrain
3. Blockers
4. Finalise paths
5. Scatter blocker models

## Game praparation

- Transfer data from generation to gameplay
- Assemble the scene
    - done in parallel with generation
    - place terrain models
    - place objects from scatterer
    - visualise paths ([image 13](https://drive.google.com/file/d/1Fy55vLD-yqXqLLYJ_h0HLU-jsLWzyKAe/view?usp=share_link))

- [[Camera Controller]] ([video 14](https://drive.google.com/file/d/1I3GP3dy-vdkpstibATpflEMG0bL92277/view?usp=share_link))
    - simple camera controls

TODO: ALGORITHMS