using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.InputSystem;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// A safehouse the boys can rest in: fully restores the active
    /// character's health and stamina, cools police heat right down, and
    /// saves the game - the "lie low" beat from Docs/STORY.md, where a
    /// safehouse is where heat stops mattering for a while.
    /// </summary>
    public class SafehouseInteractable : InteractableBase
    {
        [SerializeField] private string safehouseName = "Farm Safehouse";
        [SerializeField] private float heatRemovedOnRest = 60f;
        [SerializeField] private bool savesOnRest = true;

        // MINI-042: gates a second, purchasable safehouse (the Lalay
        // house) the same way LockedFarmPlot gates bought land - empty
        // keeps the original always-usable Highland farm safehouse
        // behaviour unchanged.
        [SerializeField] private string requiredItemId;

        // MINI-062: "selectable respawn" - each safehouse carries its own
        // world spawn point (the bed position, same offset math the farm
        // safehouse always used internally) and can be told to become
        // where death/out-of-bounds respawns send the player, via
        // CharacterSwitchManager.SetRespawnPoint. Previously that spawn
        // point was a single hardcoded value baked at scene-build time
        // with no in-game way to change it, even after buying a second
        // house.
        [SerializeField] private Vector3 spawnPoint;

        private string _lastFeedback;
        private bool _wardrobeOpen;
        private GameObject _menuUser;
        private string _wardrobeFeedback;
        private bool _trialMode;
        private int _trialIndex;
        private CharacterEquipment WardrobeEquipment => _menuUser!=null?_menuUser.GetComponent<CharacterEquipment>():null;
        private bool MenuUserNearby => _menuUser!=null && Vector3.Distance(_menuUser.transform.position,transform.position)<=4f
            && (_menuUser.GetComponent<PlayerController>()==null || _menuUser.GetComponent<PlayerController>().IsControlled)
            && (CharacterSwitchManager.Instance==null || CharacterSwitchManager.Instance.Active.root==_menuUser);

        private bool Owned => string.IsNullOrEmpty(requiredItemId)
            || (EconomyManager.Instance != null && EconomyManager.Instance.OwnsItem(requiredItemId));

        public override string PromptLabel
        {
            get
            {
                if (!Owned) return "[ E ] House (locked)";
                if(_wardrobeOpen)
                {
                    if(_trialMode)return "FREE TRY-ON - "+(CharacterSwitchManager.Instance?.Active.displayName??"Character")+"\n"+CharacterEquipment.TrialItemLabels[_trialIndex]+"\n[1] Try  [2] Undo all trials  [3] Next  [E] Back\nSession only. No purchase or save changes.\nNew shirts/pants/90/97 shoes still in production.";
                    var equipment=WardrobeEquipment;
                    string status=equipment==null||!equipment.WatchOwned?"Buy a gold watch at the Clothes Man first.":equipment.WatchEquipped?"Gold watch: wearing":"Gold watch: stored";
                    return "WARDROBE - "+(CharacterSwitchManager.Instance?.Active.displayName??"Character")+"\n"+status+"\n[1] Wear  [2] Remove  [6] Free try-on  [E] Back\n"+(_wardrobeFeedback??"Rest or save afterward to keep your choice.");
                }
                return _menuOpen ? "[1] Rest  [2] Save  [3] Load\n[4] Set Respawn  [5] Wardrobe  [E] Leave" : "[ E ] Use bed / wardrobe";
            }
        }

        private bool _menuOpen;

        private void Update()
        {
            if (!_menuOpen) return;
            if(!Owned||!MenuUserNearby){_menuOpen=false;_wardrobeOpen=false;return;}
            if(_wardrobeOpen)
            {
                if(_trialMode)
                {
                    if(GameInput.WasPressed(GameAction.WardrobeWear))WardrobeEquipment?.SetTrialItem(CharacterEquipment.TrialItemIds[_trialIndex],true);
                    else if(GameInput.WasPressed(GameAction.WardrobeRemove))WardrobeEquipment?.ClearTrialItems();
                    else if(GameInput.WasPressed(GameAction.WardrobeNext))_trialIndex=(_trialIndex+1)%CharacterEquipment.TrialItemIds.Length;
                    return;
                }
                if(GameInput.WasPressed(GameAction.WardrobeTrial)){_trialMode=true;_trialIndex=0;return;}
                if(GameInput.WasPressed(GameAction.WardrobeWear))ChooseWatch(true);
                else if(GameInput.WasPressed(GameAction.WardrobeRemove))ChooseWatch(false);
                return;
            }
            if(GameInput.WasPressed(GameAction.Wardrobe)){OpenWardrobe();return;}

            if (Input.GetKeyDown(KeyCode.Alpha1)) { Rest(); }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SaveLoadSystem.Instance?.Save();
                _lastFeedback = "Game saved.";
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SaveLoadSystem.Instance?.Load();
                _lastFeedback = "Game loaded.";
                _menuOpen = false;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SetRespawnHere();
            }
        }

        /// <summary>The "[4] Set Respawn" action, exposed as a standalone
        /// method (mirroring the private Rest()) so a validation harness
        /// can trigger it directly rather than needing to fake real OS
        /// Input.GetKeyDown events the way Update()'s menu dispatch reads
        /// them - same reasoning as CheatCodeController.FeedTypedCharacters
        /// being split out from its own Update().</summary>
        public string SetRespawnHere()
        {
            // Guards even though the only in-game path here (Update()'s
            // Alpha4 branch) is already unreachable while locked, since
            // _menuOpen can only be true after an Owned check in Interact() -
            // a direct caller (e.g. a test harness) shouldn't be able to
            // skip that gate.
            if (!Owned)
            {
                _lastFeedback = "Dis house not yours yet. Buy the deed first, nuh.";
                return _lastFeedback;
            }
            CharacterSwitchManager.Instance?.SetRespawnPoint(spawnPoint, safehouseName);
            _lastFeedback = $"{safehouseName} set as where allu wake up from now on.";
            return _lastFeedback;
        }

        public override void Interact(GameObject interactor)
        {
            if (!Owned)
            {
                _lastFeedback = $"Dis house not yours yet. Buy the deed first, nuh.";
                return;
            }

            if(_trialMode){_trialMode=false;_lastFeedback=null;return;}
            if(_wardrobeOpen){_wardrobeOpen=false;_lastFeedback=null;return;}
            _menuUser=interactor;
            // E toggles the bed menu; the actual actions are 1/2/3 so
            // resting, saving and loading are separate deliberate choices.
            _menuOpen = !_menuOpen;
            if (!_menuOpen)
            {
                _lastFeedback = null;
                return;
            }

            _lastFeedback = "Rest to heal and cool off, or save/load.";
        }

        public bool OpenWardrobe()
        {
            if(!Owned||!_menuOpen||!MenuUserNearby)return false;
            _wardrobeOpen=true;_trialMode=false;_wardrobeFeedback=null;return true;
        }
        public bool ChooseWatch(bool wear)
        {
            if(!_wardrobeOpen||!Owned||!MenuUserNearby)return false;
            bool changed=WardrobeEquipment!=null&&WardrobeEquipment.SetWatchEquipped(wear);
            _wardrobeFeedback=changed?(wear?"Yah, I looking more fresh now. Save when ready.":"Watch stored. You still own it. Save when ready."):"This character needs to buy the watch first.";
            return changed;
        }

        private void Rest()
        {
            var slot = CharacterSwitchManager.Instance?.Active;
            var vitals = slot?.vitals;

            if (vitals != null)
            {
                vitals.Restore();
            }

            float heatBefore = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
            EconomyManager.Instance?.AddHeat(-heatRemovedOnRest);
            float heatAfter = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;

            if (savesOnRest)
            {
                SaveLoadSystem.Instance?.Save();
            }

            string who = slot != null ? slot.displayName : "Allu";
            _lastFeedback = heatBefore > 1f
                ? $"{who} rest up at {safehouseName}. Health and stamina full, heat down to {Mathf.RoundToInt(heatAfter)}. Saved."
                : $"{who} rest up at {safehouseName}. Health and stamina full. Saved.";
            _menuOpen = false;
            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.RestAtSafehouse, safehouseName);
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
