Udon Unity Source Movement
----

This repository was an attempt to port `Olezen/UnitySourceMovement` to UdonSharp, which can then be used for VRChat worlds.

The primary objective was to come up with prototype as quickly as possible to see if the character controller could be viable in VR.

Therefore the modifications to the code were done in the most minimal way, with disregard for code cleanliness. Most of the modifications involved working around UdonSharp limitations, such as no structs, no constructors, no access to `AddComponent`, limited property and inheritance usage, no ability to mutate struct fields in place, and more.

### Current state

After trying the character controller, it appears that the implementation is heavily dependent on the framerate of the HMD. **This could be an porting error on my part.** Since a HMD framerate can fluctuate wildly from 144Hz to 45Hz or even lower, this is not acceptable.

This porting project being an experiment, I do not have the time to verify whether my porting had a mistake in it, or where the framerate dependency lies, nor take time to fix it.

---

Update 15 August 2019: Added experimental step offset functionality. (not in demos)

# Unity Source Movement
Based on Fragsurf by cr4yz (Jake E.), available here: https://github.com/AwesomeX/Fragsurf-Character-Controller/

Video demo: https://www.youtube.com/watch?v=w5BquYbCBAE


An adaptation of the Source engine's movement mechanics in Unity, including strafe jumping, surfing, bunnyhopping, swimming, crouching and jump-crouching (optional), as well as sliding (optional). The player can also push rigidbodies around.

If you want the player to collide with objects that are not on the default layer, you can set the layer mask in the SurfPhysics class.

You can use the included 'Example player prefab' as a base, or just add SurfCharacter to an empty object and do the looking/aiming system yourself.
