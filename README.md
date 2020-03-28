# Air Support
A GTA5 mod that that enables the player to call in air support.

---
## Installation
* place `SupportHeli.dll` and `SupportHeli.ini` in your `scripts` folder
* **Highly recommended**: replace `vehicleweapons_strikeforce.meta` in `update/x64/dlcpacks/common/data/ai`
  * this modified file moderately extends the range and fire rate of the B-11 Strikeforce's cannon
* Make sure you have [ScriptHookVDotNet v3.x](https://www.gta5-mods.com/tools/scripthookv-net)
* Make sure you have installed [.NET 4.8 Runtime](https://dotnet.microsoft.com/download/dotnet-framework/net48)

---
## Precision Air Strike (Strafe Run) - BETA
A formation of jets (B-11 Strikeforce) is spawned and attacks any NPCs (except friendly NPCs) near a target position of your choice. Be careful not to stand too close! Jets use explosive cannons as well as homing missiles.

*This feature is currently in beta testing. I appreciate your patience while I work out the bugs, and welcome all feedback to help improve this feature.*

### Usage
By default, `[activateKey]` is `F12`. This can be changed in settings.
* spawn: press `[activateKey]` while aiming at some position

### Tips
* It is **highly recommended** to use a modified `vehicleweapons_strikeforce.meta`, to extend the range and fire rate of the Strikeforce's cannon. See installation details on how to do this.
* the number of jets used in the strafe run depends on the number of targets in the target area
* you can change the size of the target area by modifying `targetRadius` in the INI
* you can change the initial altitude and distance away from the target position by changing `spawnHeight` and `spawnRadius`, respectively
* if you choose to use the cinematic cam, you are invincible while it is active

---
## Attack Heli
A manned heli follows the player and engages enemies. The Attack Heli can also be tasked with targeting NPCs.

### Supported Models
* Hunter
* Akula

### Usage
By default, `[activateKey]` is `F10`. This can be changed in settings.
* spawn: `[activateKey]`
* target NPC: press `[activateKey]` while aiming at an NPC or occupied vehicle


---
## Support Heli
A manned heli that can:
* spawn ground crew (bodyguards) that rappel from the heli
* Seats 2 door gunners
* Can be tasked to land and fly to a waypoint (heli taxi)

### Supported Models
The following models support rappeling:
* Maverick
* Polmav

It is recommended to use Polmav (Police Maverick). The following models can also be used, without rappeling support and/or door gunners:
* Buzzard
* Akula
* Hunter
* Valkyrie


### Usage
By default, `[activateKey]` is `F10`. This can be changed in settings.
* spawn: `Shift + [activateKey]`
* spawn rappeling ground crew: `Shift + [activateKey]`, after Support Heli is spawned
* land near player: `PageDown + [activateKey]`
* enter heli: hold `[enterVehicle]` after heli has landed
* fly to destination (or hover if no waypoint is set): `Tab + [activateKey]`
* resume chasing player (after landing): `PageUp + [activateKey]`

---
## Feedback & Contributing
If you have feedback or questions, I want to hear them. You can leave a comment on the gta5-mods.com page, or as an issue on the GitHub repo. I also welcome contributions to the code - just open a pull request.

### Dev Dependencies
This mod relies on the following:
* [Optimized Priority Queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started)
