#WIP 
#system 

[[Tower]]s use it to acquire targets
handles which [[Attacker]]s are in range and which one is chosen as the current target 

can require line of sight to the enemy

different targeting types 
- rotation
    - free
    - locked
- heights
    - above
    - below
    - only level
- possibly ensure trajectory
    - straight line 
        - blocked by terrian
    - other  
        - ballistic
        - ...
    - or don't
- preferred target (configurable)
    - closest to [[Hub]]
    - closest to tower
    - with highest HP


Visualisation
- Draw the range on the terrain mesh
- Draw on which parts of paths will [[Attacker]]s be targeted
    - green - all sizes
    - yellow - only large 
- right now it is done with line renderers
    - sample terrain height along the line to draw it right above the terrain
    - later maybe use a shader on the terrain itself