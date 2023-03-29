#TODO 
#system 

[[Microeconomy]]

each consists of several waves of [[Attacker]]s, see [[Wave Controller]]
each wave starts a fixed amount of time after the previous one, giving the player some time to prepare

aftear each battle, get a few rewards 
- some [[Credit]]s
- potentially some [[Augment]]s
- choose one of a few [[Blueprint]]s selected randomly, based on [[Blueprint Rarity]]

## Gameplay Implementation
- use fixed updates for game logic
    - 20Hz
    - fixed time step 0.05s
    - options to speed up or possibly pause - changing fixed update rate
- interpolate positions and visuals on Update
- many visuals are game-speed agnostic 
    - use unscaledDeltaTime

- I thought abous some custom mini-framework for this, but many of the simulated variables the visuals are based on should be handled on case-by-case basis