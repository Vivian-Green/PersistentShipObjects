
# PersistentShipObjects
 
Lethal company mod to keep ya things where ya left 'em. 

## known bugs:
- Object positions are saved but not reloaded properly until relaunching the game. Relevant error:<br>

> Error attempting to load ship unlockables on the host:
> System.NullReferenceException at (wrapper managed-to-native)
> UnityEngine.Transform.get_position_Injected(UnityEngine.Transform,UnityEngine.Vector3&)
> at UnityEngine.Transform.get_position () [0x00000] in
> \<e27997765c1848b09d8073e5d642717a>:IL_0000 at
> PersistentShipObjects.Patches.ShipBuildModeManagerPatch.PlaceShipObject... IL_0067


- Can get stuck in in build mode after moving a placeableShipObject & rejoining. Relevant error (the same one in a different context-):

> [Error  : Unity Log] NullReferenceException Stack trace:
> UnityEngine.Transform.get_position () (at \<e27997765c1848b09d8073e5d642717a>:IL_0000)
> PersistentShipObjects.Patches.ShipBuildModeManagerPatch.PlaceShipObject... IL_0067)
