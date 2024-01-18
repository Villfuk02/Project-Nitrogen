#TODO
#system 

get them as rewards after a succesful [[Battle]] or in a [[Shop]] or [[Event]]

Represent [[Building]] and [[Ability]] (and [[Upgrade]]?)
Manage your collection in [[Inventory]]

Important stats:
-  [[Blueprint Rarity]] - does not change, used for determining its rarity as a reward
- cooldown - during [[Battle]], how many waves before it can be used again
- cost - either in [[Energy]] or [[Material]]
- placement type - where can it be used + predicate for filtering if targets are valid
    - on a [[Tile]], possibly with area of effect
    - on a single [[Building]]
    - on a single [[Attacker]] (only for [[Ability]]s)
    - on a point, unconstrained by [[Tile]] grid, with area of effect (only for [[Ability]]s)
    - global (only for [[Ability]]s)

[lenticular design](https://magic.wizards.com/en/news/making-magic/lenticular-design-2014-03-31) (18.3.23)

Each can be modified with ~ 1 to 2 [[Augment]]s

Blueprint holds **stats** for itself and the [[Building]]/[[Ability]]/... it provides - for example damage, firerate
[[Building]]s / [[Ability]]s provide behavior along with the necessary values needed fo executing the behavior

Icon outline layers:
    - 2x Black 1px blur
    - 5x White 5px blur
    - 1x Black 35px blur
