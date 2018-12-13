TODO
- DONE: Get rid of CostCell
- DONE: Get rid of Map.this[Position] and use [x, y] instead
- DONE: CostField factories
- Pass in peaks/walls as a list
- go home should use an inverted curve, ie steep sides. So that bot follows ravines home. Use this formula:
double Potential(double halite) => Math.Log(extra / halite) / log75; // Removed (1 + maxHalite - halite)
- overlay flows so that all rich means are peaks
- merge cost and wave
