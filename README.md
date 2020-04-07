# Air Support
A GTA5 mod that that enables the player to call in air support, in the form of Precision Air Strike (Strafe Run), Attack Heli, and Support Heli.

---
## Installation
* place `Priority Queue.dll`, `SupportHeli.dll`, and `SupportHeli.ini` in your `scripts` folder
* **Highly recommended**: replace `vehicleweapons_strikeforce.meta` in `update/x64/dlcpacks/mpbattle/common/data/ai`
  * this modified file moderately extends the range and increases the fire rate of the B-11 Strikeforce's cannon
* Make sure you have [ScriptHookVDotNet v3.x](https://www.gta5-mods.com/tools/scripthookv-net)
* Make sure you have installed [.NET 4.8 Runtime](https://dotnet.microsoft.com/download/dotnet-framework/net48)

### DLC requirements
* After Hours DLC is required to use Strikeforce (strafe run)
* Doomsday DLC is required to use Akula
* Heists DLC is required to use Valkyrie
* Smuggler's Run DLC is required to use Hunter

---
## Precision Air Strike (Strafe Run) - BETA
A formation of jets (B-11 Strikeforce) is spawned and targets any NPCs (except friendly NPCs) near the target position of your choice. Be careful not to stand too close! Jets use explosive cannons as well as homing missiles.

*This feature is currently in beta testing. I appreciate your patience while I work out the bugs, and welcome all feedback to help improve this feature.*

### Usage
By default, `[activateKey]` is `F12`. This can be changed in settings. Call in a strafe run using any method below
1. While aiming, press `[activateKey]`
2. Press `[activateKey]`. You will be given a flare gun to mark the position of the strafe run. The run begins after the flare hits something (e.g. the ground, vehicles, NPCs, etc.)


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
## Development
If you have feedback or questions, I want to hear them. You can leave a comment on the [gta5-mods.com](https://www.gta5-mods.com/scripts/support-helicopter) page, or as an issue on the [GitHub repo](https://github.com/DavidLiuGit/GTAV_SupportHeli). I also welcome contributions to the code - just open a pull request.


### Changelog
#### 3.1.1
- added 2 more cinematic cam sequences
#### 3.1
- revamped strafe run cinematic cams, now featuring 4 sequences
#### 3.0.6 (beta)
- smoothed the transition from gameplay camera to cinematic cam
#### 3.0.5 (unreleased)
- added error checking to prevent the script from crashing when user doesn't have necessary DLC
  - add DLC requirements to readme
- fixed (hopefully) issue with flare gun not getting ammo while player is in a vehicle
#### 3.0.4 (beta)
- improve strafe run spawn position choosing logic
- randomized strafe run pilot tasking order for better targeting distribution
#### 3.0.3 (beta)
- tweaked default values in INI file
- implemented various attempts to fix user crashes
#### 3.0.2 (unreleased)
- implemented an alternative method of calling in a strafe run, using the flare gun
- improved logic of choosing spawn position for the strafe run
  - this should result in fewer strafe runs blocked by the map (buildings and terrain)
- updated `vehicleweapons_strikeforce.meta` to increase range and fire rate of Strikeforce's missiles
#### 3.0.1 (beta)
- hotfix: corrected vehicleweapons_strikeforce.meta installation path 
#### 3.0 (beta)
- first release to include precision air strike strafe runs!
#### 2.3.1
- fixed Attack Heli gunner tasking after chase & engage
#### 2.3
- added chase & engage for Attack Heli
#### 2.2.2
- Support Heli will now stay on the ground after player enters it; use Tab+F10 (or your custom activate key) to fly to destination/hover
- bodyguards will now enter the Heli with you
#### 2.2.1
- Fixed broken heli spawning after switching playable characters
- slight change to heli "taxi" cruise altitude
#### 2.2
- Implemented support heli landing & fly-to-destination
#### 2.1.3
- Support Heli will now spawn with gunners instead of having crew rappeling immediately when the heli is spawned. This makes the Support Heli more useful
#### 2.1.2
- made deletion more graceful: Helis will fly away and ground crew will no longer follow player
#### 2.1.1
- added ability to delete all ground crew and helis using Delete + activateKey
#### 2.1
- added ini settings for ground crew


### Known bugs
- ENT bodyguards do not work well with gunners and guards spawned with this script. Avoid spawning ENT bodyguards while using this script.
- Valkyrie gunners are still dumb


### Contributing
Pull requests on [GitHub](https://github.com/DavidLiuGit/GTAV_SupportHeli) are welcomed. This mod relies on the following 3rd party libraries:
* [Optimized Priority Queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started)
