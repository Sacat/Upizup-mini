# MINI-100 migration role map

Rollback checkpoint: `aed8854`. Source backup: `Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity`.

| Gameplay role | Stable source object/ID | New map anchor/zone | Validation |
|---|---|---|---|
| Sacat / Franki spawn | existing player roots | `upizup_block`, Lalay | both player roots active and above terrain |
| Safehouse | existing safehouse root | `upizup_block`, off-road Lalay lot | respawn/safehouse ID preserved |
| Starting farm | existing active farm/plot | derived off-road `highland_first_farm` gameplay pad at approximately `(114,-129)` | one active plot on a flat pad beside the farm spur |
| Future farms | existing land progression | `highland_future_plot_02..08` | inactive/locked IDs retained |
| Boss/shop/mission NPCs | existing named roots | deterministic Lalay roadside lots; seven shop stalls replace conflicting placeholder houses | interaction IDs/components preserved and paved traffic lanes remain clear |
| Boss J / Normy / Police | existing named roots | short sidewalk walking beats along Lalay | patrol/heat components preserved; runtime motion retest owed |
| TMAX / car | existing vehicle roots | Lalay roadside/dealer anchor | vehicle components preserved |
| Boat route / boatman | existing jetty/Guadeloupe roots | `story_jetty`; boat moored beyond Boat Man | interaction remains land-accessible |
| Brakes / church | new priest on existing licensed NPC body + approved church mass | church forecourt | white clothing and dialogue preserved |
| Paro | existing `NPC_Vagrant` compatibility root | Lalay roadside | Paro display/dialogue role preserved |
| Gangs / recruiter | existing named roots | Dog Life west Lalay; Not Ah Word/Boss C east Lalay | progression locks preserved |
| Backstreet | `user/lalay_backstreet` | south/below Lalay, main-road junction at both ends | one continuous collidable ribbon; no north duplicate |
| Boundaries/fall recovery | existing recovery systems | phase-one perimeter / coast | no sea/edge escape in static gate; live test owed |

Exact object-to-coordinate mappings are generated and logged by the migration editor script. Save keys and gameplay identifiers must not be renamed.
