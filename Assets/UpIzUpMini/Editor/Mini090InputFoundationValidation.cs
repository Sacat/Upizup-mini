using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.InputSystem;

namespace UpIzUpMini.EditorTools
{
    public static class Mini090InputFoundationValidation
    {
        private static readonly string[] CoreConsumers =
        {
            "Assets/UpIzUpMini/Scripts/Character/PlayerController.cs",
            "Assets/UpIzUpMini/Scripts/Interaction/InteractionDetector.cs",
            "Assets/UpIzUpMini/Scripts/Character/CharacterSwitchManager.cs",
            "Assets/UpIzUpMini/Scripts/Combat/SimpleMeleeCombat.cs",
            "Assets/UpIzUpMini/Scripts/UI/ControlsPanelController.cs",
            "Assets/UpIzUpMini/Scripts/Camera/ThirdPersonFollowCamera.cs"
        };

        [MenuItem("Up Iz Up Mini/MINI-090/Validate Input Foundation")]
        public static void Validate()
        {
            ValidateBindings();
            ValidateVirtualInput();
            ValidateConsumersUseFacade();
            Debug.Log("MINI-090 INPUT FOUNDATION PASS: keyboard bindings preserved, virtual move/look/button injection works, and all six core consumers use GameInput.");
        }

        private static void ValidateBindings()
        {
            Expect(GameInput.PrimaryKey(GameAction.Jump) == KeyCode.Space, "Jump must remain Space.");
            Expect(GameInput.PrimaryKey(GameAction.Sprint) == KeyCode.LeftShift, "Sprint must retain Left Shift.");
            Expect(GameInput.SecondaryKey(GameAction.Sprint) == KeyCode.RightShift, "Sprint must retain Right Shift.");
            Expect(GameInput.PrimaryKey(GameAction.Interact) == KeyCode.E, "Interact must remain E.");
            Expect(GameInput.PrimaryKey(GameAction.SecondaryInteract) == KeyCode.R, "Clone/secondary interact must remain R.");
            Expect(GameInput.PrimaryKey(GameAction.AssignFarmhand) == KeyCode.G, "Farmhand assignment must remain G.");
            Expect(GameInput.PrimaryKey(GameAction.SwitchCharacter) == KeyCode.Tab, "Character switching must remain Tab.");
            Expect(GameInput.PrimaryKey(GameAction.Attack) == KeyCode.F, "Melee must remain F.");
            Expect(GameInput.PrimaryKey(GameAction.Tutorial) == KeyCode.H, "Tutorial must remain H.");
        }

        private static void ValidateVirtualInput()
        {
            GameInput.ResetVirtualInput();
            try
            {
                var move = new Vector2(0.4f, 0.75f);
                var look = new Vector2(-0.25f, 0.5f);
                GameInput.SetVirtualMove(move);
                GameInput.SetVirtualLook(look);

                Expect(Vector2.Distance(GameInput.Move, move) < 0.0001f, "Virtual move vector was not returned.");
                Expect(Vector2.Distance(GameInput.Look, look) < 0.0001f, "Virtual look vector was not returned.");

                GameInput.SetVirtualButton(GameAction.Jump, true);
                Expect(GameInput.IsHeld(GameAction.Jump), "Virtual Jump did not enter held state.");
                Expect(GameInput.WasPressed(GameAction.Jump), "Virtual Jump did not report its press edge.");

                GameInput.SetVirtualButton(GameAction.Jump, false);
                Expect(!GameInput.IsHeld(GameAction.Jump), "Virtual Jump remained held after release.");
                Expect(GameInput.WasReleased(GameAction.Jump), "Virtual Jump did not report its release edge.");
            }
            finally
            {
                GameInput.ResetVirtualInput();
            }
        }

        private static void ValidateConsumersUseFacade()
        {
            foreach (string relativePath in CoreConsumers)
            {
                string fullPath = Path.GetFullPath(relativePath);
                string source = File.ReadAllText(fullPath);
                Expect(source.Contains("GameInput."), $"{relativePath} does not use GameInput.");
                Expect(!source.Contains("Input.Get"), $"{relativePath} still reads hardware input directly.");
                Expect(!source.Contains("Input.inputString"), $"{relativePath} still reads typed hardware input directly.");
            }
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"MINI-090 INPUT FOUNDATION FAIL: {message}");
        }
    }
}
