# MINI-100 migration role map

Rollback checkpoint: `aed8854`. Source backup: `Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity`.

| Gameplay role | Stable source object/ID | New map anchor/zone | Validation |
|---|---|---|---|
| Sacat / Franki spawn | existing player roots | `upizup_block`, Lalay | both player roots active and above terrain |
| Safehouse | existing safehouse root | `upizup_block`, off-road Lalay lot | respawn/safehouse ID preserved |
| Starting farm | existing active farm/plot | `highland_first_farm` | one active plot reachable from Highland inroad |
| Future farms | existing land progression | `highland_future_plot_02..08` | inactive/locked IDs retained |
| Boss/shop/mission NPCs | existing named roots | Lalay roadside anchors | interaction IDs/components preserved |
| Police | existing police roots | Lalay/coastal road patrol positions | police/heat components preserved |
| TMAX / car | existing vehicle roots | Lalay roadside/dealer anchor | vehicle components preserved |
| Boat route / boatman | existing jetty/Guadeloupe roots | `story_jetty` | interaction remains land-accessible |
| Gangs / recruiter | existing named roots | approved Lalay blocks | progression locks preserved |
| Boundaries/fall recovery | existing recovery systems | phase-one perimeter / coast | no sea/edge escape in static gate; live test owed |

Exact object-to-coordinate mappings are generated and logged by the migration editor script. Save keys and gameplay identifiers must not be renamed.
