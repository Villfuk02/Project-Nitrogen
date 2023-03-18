#MVP
#system 

- [x] pregenerate all [[Terrain Module]] variants (flips and rotations)


ALG
- [x] init
- [x] randomly collapse slot
    - [x] propagate constraints
- [x] if invalid, backtrack

NOTES
- keep slots sorted by entropy
- each slot has possibilities per constraint
    - [x] [[Tile]] height x4
    - [x] [[Tile]] type x4 (can be unpassable)
    - [x] [[Tile]] slant x4
    - [x] passages x4
- each slot updates possible modules lazily