# Space

## Description

It's really hard to make large, 2D, tile-based games with modifiable terrain and hundreds of characters. 

For small scale games with static environments, pathfinding is easily solved with A* and reachability can be computed by a single floodfill when the level loads.

However, if the map is too large (say, 4096x4096), we don't want to run the risk of searching every tile in the map (16 million tiles) to get to our target location.
Compound that by the number of characters running around. And if we wish to alter the terrain, we don't want to recompute reachability by floodfilling all those tiles either. 

This is an all-in-one solution to efficient pathfinding, reachability detection, and even spatially storing and querying for entities. It's mostly inspired by
Tynan Sylvester's design for Rimworld (https://www.youtube.com/watch?v=RMBQn_sg7DA) though I've taken a lot of liberties.

Here's proof that we can have our cake and eat it too.

## Efficient pathfinding

## Reachability detection

## Spatial storage
