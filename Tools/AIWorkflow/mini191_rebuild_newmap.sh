#!/bin/bash
# MINI-191: rebuild the new-map scene (GrandBayProof_HouseEnhance.unity) from the CURRENT live scene by replaying the
# deterministic MINI-182 chain, then apply the ammunition mission. The live scene is only read (hash checked by each tool).
# Usage: bash Tools/AIWorkflow/mini191_rebuild_newmap.sh   (no other Unity may be open; ~25 minutes)
set -u
cd "E:/Unity/Up Iz Up Mini" || exit 1
U="C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe"
SCENE=Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity
STAMP=$(date +%Y%m%d-%H%M)
mkdir -p Logs/Tasks/MINI-191 Backups/MINI-191-pre-rebuild
if [ -f "$SCENE" ] && [ ! -f "Backups/MINI-191-pre-rebuild/GrandBayProof_HouseEnhance.unity" ]; then
  cp "$SCENE" "Backups/MINI-191-pre-rebuild/GrandBayProof_HouseEnhance.unity"; cp "$SCENE.meta" "Backups/MINI-191-pre-rebuild/GrandBayProof_HouseEnhance.unity.meta"
fi
step() {
  name="$1"; method="$2"; shift 2
  echo "=== $name $(date +%H:%M:%S)" | tee -a Logs/Tasks/MINI-191/chain.txt
  "$U" -batchmode -quit -projectPath "E:/Unity/Up Iz Up Mini" -executeMethod "$method" -logFile "Logs/Tasks/MINI-191/chain-$name.log"
  rc=$?
  echo "    exit=$rc" | tee -a Logs/Tasks/MINI-191/chain.txt
  if [ $rc -ne 0 ]; then echo "CHAIN FAILED AT $name" | tee -a Logs/Tasks/MINI-191/chain.txt; exit $rc; fi
}
# the copy scene is recreated by HouseKit.Run only when it does not exist
rm -f "$SCENE"
export MINI182_FAMILY=flat_concrete
step 01-kit      UpIzUpMini.EditorTools.Mini182HouseKit.Run
unset MINI182_NOROW
step 02-lalay    UpIzUpMini.EditorTools.Mini182LalayPlace.Apply
step 03-trees    UpIzUpMini.EditorTools.Mini182LalayTrees.Apply
step 04-ground   UpIzUpMini.EditorTools.Mini182GroundPatches.Apply
step 05-props    UpIzUpMini.EditorTools.Mini182LalayProps.Apply
step 06-coast    UpIzUpMini.EditorTools.Mini182BayCoast.Apply
step 07-expand   UpIzUpMini.EditorTools.Mini182Expansion.Apply
step 08-complex  UpIzUpMini.EditorTools.Mini182Complex.Apply
step 09-terrace  UpIzUpMini.EditorTools.Mini182ApartmentTerrace.Apply
step 10-coast2   UpIzUpMini.EditorTools.Mini182BayCoast.Apply
step 10b-roads  UpIzUpMini.EditorTools.Mini193RoadJunctions.Apply
export MINI191_SCENE="$SCENE"
step 11-ammo     UpIzUpMini.EditorTools.Mini191AmmoMission.Apply
echo "CHAIN DONE $(date +%H:%M:%S)" | tee -a Logs/Tasks/MINI-191/chain.txt
