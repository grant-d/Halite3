Uses FLowFields per the following references:
https://leifnode.com/2013/12/flow-field-pathfinding/
https://gamedevelopment.tutsplus.com/tutorials/understanding-goal-based-vector-field-pathfinding--gamedev-9007
http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf


TODO
- DONE: Get rid of CostCell and WaveCell
- DONE: Get rid of Map.this[Position] and use [x, y] instead
- DONE: CostField factories
- DONE: go home should use an inverted curve, ie steep sides. So that bot follows ravines home.
- DONE: Pass in peaks/walls as a list
- DONE: More than 1 ship picked the same target
- DONE: Check if IsWorthMining is correct - it might be ok to mine to 0
- DONE: Anti-squat still not working

- overlay multi-flows so that all rich areas are peaks
- For path finding, mark all target positions as walls
- Use a compressed WaveField for mining
