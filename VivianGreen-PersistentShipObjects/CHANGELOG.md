## 0.2.1
 - significant rewrite, (with a lot of help from ghxc2- get credited on)
 - hella cleanup
 - now works with v49- whoops!

## 0.1.3
 - rewrote DebugPrintDescendants - the indent building is now constant (making *one* copy of a string for each descendant) instead of O(n^2).
 - added Debug.doDebugPrints option to the config, defaulting to false.
 - lot of cleanup, sprayed febreeze all over the code

## 0.1.2
 - some cleanup, small tweaks

## 0.1.1
 - actually added the updated readme- whoops. This needs a unique version number for thunderstore

## 0.1.0
 - fixed a bug where placeableShipObject positions weren't reloaded properly until relaunching lethal company
 - fixed a bug where the player could get stuck in in build mode after moving a placeableShipObject, leaving a game without relaunching lethal company, and joining another game
 - saved transforms are now relative to the ship, which should be much more consistent
 - still very early stages, so there's a lot of debug prints-

## 0.0.9
 - edited export.py to give correct version number in the dll

## 0.0.8
 - Saved placeableShipObject transforms are now actually loaded! Sometimes!
 - see known bugs list in README.md

## 0.0.7
 - rewrote most of the base plugin
 - extracted* patches to patches folder *just ShipBuildModeManagerPatch.cs
 - rewrote most of the code to fetch placeableShipObjects' transforms
 - removed transform wrapper class
 - fixed another typo- they're very persistant

## 0.0.6
 - moved to netstandard 2.1 with many headaches
 - removed config's old usage bc that isn't going to work
 - like half the code is commented out but it runs now? Like it doesn't work but it runs

## 0.0.5
 - added post-build script for automatic exporting
 - messed with attributes until BepInEx actually noticed the mod

## 0.0.3
 - added export script that only took 6hrs to write and.. actually I think that one was worth it pog

## 0.0.2
- OHGOD I woke up to 83 downloads when I was just testing thunderstore things- I will get this into a runnable state.
- improved logging, renamed mod folder to spell persistent correctly (was right everywhere else-)
- still very broken but progress is being made

## 0.0.0
- initial commit, untested, literally just built, & definitely broken