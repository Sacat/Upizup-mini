using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UpIzUpMini.Character
{
    public enum OutfitSlot { Shirt, Pants, Hat, Shoes }
    [Serializable] public class OutfitChoice { public string itemId; public int colour; }
    [Serializable] public class OutfitPiece
    {
        public string id, label;
        public OutfitSlot slot;
        public Mesh mesh;
        public Material[] materials;
        public int[] tintSlots = { 0 };
    }
    [Serializable] public class OutfitBinding { public OutfitSlot slot; public SkinnedMeshRenderer renderer; }

    // Fitted meshes are authored per character; selection never changes their skeleton.
    public sealed class OutfitWardrobe : MonoBehaviour
    {
        public OutfitPiece[] pieces = Array.Empty<OutfitPiece>();
        public OutfitBinding[] bindings = Array.Empty<OutfitBinding>();
        public OutfitChoice[] defaults = Array.Empty<OutfitChoice>();
        readonly Dictionary<OutfitSlot, OutfitChoice> choices = new Dictionary<OutfitSlot, OutfitChoice>();
        public static readonly string[] ColourNames = { "Navy", "White", "Black", "Red", "Green", "Denim" };
        public static readonly Color[] Colours = { new Color(.09f,.16f,.29f),new Color(.9f,.9f,.87f),new Color(.055f,.06f,.07f),new Color(.55f,.075f,.07f),new Color(.075f,.27f,.17f),new Color(.24f,.39f,.53f) };
        void Start() { if (choices.Count == 0) Restore(null); }
        public IEnumerable<OutfitPiece> ForSlot(OutfitSlot slot) => pieces.Where(p => p.slot == slot);
        public OutfitChoice Current(OutfitSlot slot) => choices.TryGetValue(slot, out var value) ? new OutfitChoice { itemId=value.itemId, colour=value.colour } : null;
        public bool Select(string id, int colour)
        {
            var piece=pieces.FirstOrDefault(p=>p.id==id);
            if(piece==null || colour<0 || colour>=Colours.Length)return false;
            var binding=bindings.FirstOrDefault(b=>b.slot==piece.slot);
            if(binding==null || binding.renderer==null)return false;
            choices[piece.slot]=new OutfitChoice{itemId=id,colour=colour};
            var r=binding.renderer;r.enabled=piece.mesh!=null;r.sharedMesh=piece.mesh;
            if(piece.mesh!=null) { r.sharedMaterials=piece.materials;r.localBounds=piece.mesh.bounds; }
            // Indexed property blocks keep skin, buttons and shoe soles independent of fabric tint.
            r.SetPropertyBlock(null);
            for(int i=0;i<r.sharedMaterials.Length;i++)
            {
                var block=new MaterialPropertyBlock();
                if(piece.tintSlots.Contains(i)){block.SetColor("_Color",Colours[colour]);block.SetColor("_BaseColor",Colours[colour]);}
                r.SetPropertyBlock(block,i);
            }
            return true;
        }
        public List<OutfitChoice> Capture() => choices.Values.Select(c=>new OutfitChoice{itemId=c.itemId,colour=c.colour}).ToList();
        public void Restore(IEnumerable<OutfitChoice> saved)
        {
            choices.Clear();foreach(var value in defaults)Select(value.itemId,value.colour);
            if(saved!=null)foreach(var value in saved)if(value!=null)Select(value.itemId,value.colour);
        }
    }
}
