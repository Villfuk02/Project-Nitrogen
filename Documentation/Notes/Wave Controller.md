#WIP
#system 

handles planning and spawning waves of [[Attacker]]s during [[Battle]]

gets a set of available [[Attacker]] types
creates a randomized plan of waves

two types of waves
- combine different attackers in sequence
- combine different attackers in parallel (only possible with multiple paths, rarer)

each wave gets some throughput budget and buffer
each attacker has a given cost
when planning a wave, select attackers and spacing, such that the througput budget is exceeded
for each attacker subtract the throughput overshoot from buffer
fit such that as much of buffer gets used without going over