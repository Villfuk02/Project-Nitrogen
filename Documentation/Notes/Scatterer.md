#MVP
#system [[World Generation]]

Scatters [[Decoration]]s on the terrain mesh

ALGORITHM
first compute weights based on various factors - set-up for each stage using scatterer modules
- distance to path
- height
- distance to other blockers
- cliff height (when over/under a cliff)
- fractal noise

then scatter decorations in stages, each stage again having one type of decoration
- for each stage put all tiles in a list
    - until it is empty
        - take a random subset such that no two picked tiles are adjecent (orthogonally and diagonally)
        - since they are not adjacent, they are independent
        - run scattering jobs in parallel - one for each of these tiles
            - pick a random position within it
                - calcualte the weight at this position (based on settings)
                - check that it is greater than some threshold (based on settings)
                - calculate the minimum distance to other decorations (from weight, based on settings)
                - check that the position is far enough from other decorations
                - calculate the decoration size (from weight, based on settings)
                - place the decoration on this position, with the given size