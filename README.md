# Air Support
A GTA5 mod that that enables the player to call in air support. This script relies on ScriptHookVDotNet 3.x, as well as .NET 4.8 Runtime.

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

The following models can also be used, without rappeling support and/or door gunners:
* Buzzard
* Akula
* Hunter
* Valkyrie

It is recommended to use Maverick/Polmav

### Usage
By default, `[activateKey]` is `F10`. This can be changed in settings.
* spawn: `Shift + [activateKey]`
* spawn rappeling ground crew: `Shift + [activateKey]`, after Support Heli is spawned
* land near player: `PageDown + [activateKey]`
* enter heli: hold `[enterVehicle]` after heli has landed
* fly to destination (or hover if no waypoint is set): `Tab + [activateKey]`

---


---

## Development & Contributing

### Dependencies
* [Optimized Priority Queue](https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started)