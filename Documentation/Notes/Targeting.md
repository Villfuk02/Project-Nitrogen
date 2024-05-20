#MVP 
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
- check line of sight
    - blocked by terrian
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

Terrain shader uses compressed texture format instead of raw texture 
- Options:
    - quadrant compression format, 2bytes per node
        - less CPU time, because the data is already in this format
        - up to 48KiB per frame
        - more GPU time
    - 256x256 texture, 1byte per pixel 
        - more CPU time
        - 64KiB per frame
        - fast on GPU
            - only 1 channel - cannot interpolate
            - ![[Pasted image 20240225190014.png]]
    - mipmaps -> one additional state
        - less CPU time
        - 33% more data
    - more pixels per byte
        - possible future optimization
        - less data
        - more difficult indexing and stuff both on CPU and GPU

Make sure towers that test line of sight don't have misleading range visualisation. When a path is on a border, the more forgiving option should be true. For example the following path is on a border, but must act like green: 
![[Pasted image 20230921191841.png]]