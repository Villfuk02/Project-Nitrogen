#MVP 
#system [[World Generation]]

Generate paths for [[Attacker]]s to move along

ALGORITHM
- first pick *start points* from along the edges of the level (all paths end in the center of the level)
    - choose randomly from positions with the correct parity (even / odd path length)
    - spread them out by removing all positions near already chosen one

- then generate paths
    - first draw prototypes of given lengths
        - go node by node from start to center, making sure the center is reachable within remaining number of steps
            - every time go in a random direction, but favor positions further from other picked nodes
    - then move single nodes trying to eliminate crossings and space paths out
        - each position has a crowding value based on distance to other nodes
        - for each node find all positions they can switch to
            - for each calculate a weight = how much will this improve this node's crowding + temperature
                - only switches with positive weights are noted
        - from all the noted switches, pick one randomly taking weights into account
            - make the switch, updating the crowding values 
        - if one path is crossing itself, reverse the direction of the created toop, to eliminate the crossing
        - slightly decrease temperature

now, the paths are prepared and [[WFC Algorithm]] will generate the terrain in a way that ensures these paths are passable

- Then finalise paths
    - calculate the distance of each tile from the center
    - DFS from path starts to center, only moving to tiles one step closer to the center
        - first found path is saved
        - then brancing is attempted
        - whenever a branch would join an existing path, it is only allowed to do so if it is at least 4 tiles after it last split off