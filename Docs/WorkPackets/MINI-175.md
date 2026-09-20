# MINI-175 — Grand Bay Credit Union on Lalay
User authorizes autonomous research and placement. Official https://gbccu.com/contact/ confirms Lalay Main Road. https://dm.near-place.com/grand-bay-cooperative-credit-union-lalay-main-road gives15.2407793,-61.3166228, matching existing candidate anchor. DOM767 embedded pin15.245064,-61.318104 is town-centre coordinate and rejected for precise building placement. Google Maps queries failed to provide place data, browser initialization unavailable, public photo links could not be inspected. Use coordinate-backed location with explicit compressed-map frontage adjustment, not an unsupported claim of surveyed footprint or exact facade. Scene target expansion import only; preserve canonical and source maps, roads, school, houses except generic conflicting lot placeholders. No financial gameplay added. Validate road/house clearance and render. Document tools and results.

## Claude reproduction and measurements
Editor tool: Assets/UpIzUpMini/Editor/Mini175CreditUnion.cs. Unity6000.3.10f1. Run with -batchmode -projectPath "E:\Unity\Up Iz Up Mini" -executeMethod UpIzUpMini.EditorTools.Mini175CreditUnion.Apply -logFile "E:\Unity\Up Iz Up Mini\Logs\mini175.log". Apply renders; omit -nographics. Then RepairShop fixes the known clothing-stall conflict and renders. Validate is read-only and supports -nographics. One editor at a time. Do not run older map regeneration tools over this scene.

Published pin converts using compression3 to (53.11,0,-158.03). Road cross-section at pin easting: (53.11,7.12,-160.98), ImportTrim_Road_way_22917921. Final building centre (54.11,6.55,-152.94),5.187216 compressed metres behind directory pin. Location along street follows the pin; frontage setback keeps canopy off road. Exact footprint/entrance unverified. Initial attempts assumed Road_* names and paired ribbon vertices; rejected after failures before save. Final code samples actual collider cross-sections at pin X and +/-2m, uses side of pin relative to road centre, synchronizes physics, checks footprint against all Road-named meshes.

Stylized owned geometry:6.8x4.8m body,5.3m height;7.05x5.08m parapet;6.9m canopy, columns, framed glass, labelled fascia, decorative ATM. No banking gameplay. Facade not photo-matched: accessible page text and map pin were obtained, but reference image viewing failed. Never claim surveyed accuracy. Generic Lalay_House_SideB_054_OneStorey disabled for parcel, retained for rollback.

Actual render revealed Market_CLOTHES and NPC_ApparelShop overlapping the bank. Inspect produced NeighbourAudit.txt; RepairShop searched nearby road-parallel locations against house/market envelopes and road rays, moving stall and NPC together by (15.88,-0.39,-1.98). Interaction components unchanged. This does not prove live shopping interaction; hands-on check remains.

Evidence: Logs/Tasks/MINI-175/Placement.txt, ShopRelocation.txt, NeighbourAudit.txt, Validation.txt; Renders/CreditUnion.png and LalayLocation.png at1400x1000. Final CreditUnion.png opened and inspected on2026-09-20: label no longer clips facade, entrance and road clear. Compile logs mini175.log, mini175-shop.log, mini175-validation.log. Existing NUnit folders empty; focused scene checks used. No EXE rebuild. Canonical/source scenes unchanged. Preserve unrelated ObjectiveMarker.mat and packages-lock.json edits.

Final fresh-process verification: MINI175_VALIDATION_PASS; roadFootprintHits=0 and houseMarketEnvelopeOverlaps=0. Compile succeeded. Static appearance inspected; live NPC shop interaction and user visual approval remain pending.

2026-09-20 user colour correction: exterior WarmConcrete changed to white (1,1,1,1), including saved material and builder default. Geometry, placement and green sign unchanged. Verification render: Logs/mini175-white.log; no need to rerun placement/relocation.

Follow-up colour instruction: WindowGlass is black (0,0,0,1), applied to window/entrance glass and shared ATM screen. White window frames retained. Saved material and builder default updated.

Final user direction: mostly white and black. WarmConcrete/WhiteTrim now white; WindowGlass/DeepGreen black. Legacy material names retained for GUID stability. Sign lettering white, sign background/columns/ATM black. Builder defaults match. No geometry changes.

Final monochrome render opened and inspected; MINI175_RENDER_PASS in Logs/mini175-monochrome.log, compilation without errors. Material-only change; prior placement validation remains applicable.
