using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Character;
using UpIzUpMini.InputSystem;
using UpIzUpMini.Interaction;
using System.Reflection;
using Object=UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static class Mini143CombatValidation
    {
        const string Output="Logs/Tasks/MINI-143";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static void Invoke(object obj,string name)=>obj.GetType().GetMethod(name,Private).Invoke(obj,null);
        static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
        static void Require(bool result,string message){if(!result)throw new Exception("MINI143: "+message);}
        public static void ValidateAndBuild()
        {
            Validate();Mini132ImpactCombatValidation.Validate();
            // Build the saved canonical scene, NOT these unsaved test poses.
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity",OpenSceneMode.Single);
            Mini001Build.BuildWindowsPlayer();
        }
        public static void Validate()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            foreach(string name in new[]{"Sacat","Franki"})
            foreach(int step in new[]{0,4})
            {
                var actor=new GameObject(name);var combat=actor.AddComponent<SimpleMeleeCombat>();
                var player=actor.AddComponent<PlayerController>();Invoke(player,"Awake");
                var victim=new GameObject("ContactDummy");victim.transform.position=Vector3.forward*3;
                var health=victim.AddComponent<NpcCombatHealth>();Invoke(health,"Awake");Invoke(health,"OnEnable");
                Set(combat,"_comboStep",step);combat.Attack();
                var move=MeleeMoveLibrary.GetChainFor(name)[step];
                Require(combat.BlocksMovement==(step==4),"kick-only lock");
                if(step==4)
                {
                    GameInput.SetVirtualMove(Vector2.up);GameInput.SetVirtualButton(GameAction.Sprint,true);GameInput.SetVirtualButton(GameAction.Jump,true);
                    Set(player,"_speed",5.335f);var before=actor.transform.position;var rotation=actor.transform.rotation;
                    Invoke(player,"Update");
                    Require(player.CurrentSpeed==0&&!player.IsRunning,"held move/sprint not blocked during kick");
                    Require(Quaternion.Angle(rotation,actor.transform.rotation)<.001f,"kick rotation changed");
                    Require(Vector3.ProjectOnPlane(actor.transform.position-before,Vector3.up).sqrMagnitude<.000001f,"kick drifted horizontally");
                    GameInput.ResetVirtualInput();
                }
                combat.AdvanceAttack(move.windupSeconds+.005f);Require(Mathf.Approximately(health.Health,100),"miss incorrectly damaged distant target");
                victim.transform.position=Vector3.forward*.8f;Physics.SyncTransforms();
                combat.AdvanceAttack(.02f);
                float expected=100-move.damage*MeleeMoveLibrary.GetDamageMultiplierFor(name);
                Require(Mathf.Abs(health.Health-expected)<.01f,"late contact did not apply exact damage");
                for(int i=0;i<50;i++)combat.AdvanceAttack(.03f);
                Require(Mathf.Abs(health.Health-expected)<.01f,"repeat damage in one swing");
                combat.AdvanceAttack(3);Require(!combat.BlocksMovement,"movement lock did not release");
                Object.DestroyImmediate(actor);Object.DestroyImmediate(victim);
            }
            var a=new GameObject("ContactAttacker");var b=new GameObject("ContactTarget");
            var profile=MeleeMoveLibrary.GetChainFor("Sacat")[0].BuildProfile();
            b.transform.position=Vector3.forward*.8f;Require(MeleeContactResolver.CanHitTarget(a.transform,b.transform,profile),"front contact rejected");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1,.4f);wall.transform.localScale=new Vector3(2,2,.1f);Physics.SyncTransforms();
            Require(!MeleeContactResolver.CanHitTarget(a.transform,b.transform,profile),"damage through wall");Object.DestroyImmediate(wall);
            b.transform.position=Vector3.back*.8f;Require(!MeleeContactResolver.CanHitTarget(a.transform,b.transform,profile),"damage behind attacker");
            b.transform.position=Vector3.forward*2;Require(!MeleeContactResolver.CanHitTarget(a.transform,b.transform,profile),"damage out of reach");
            Object.DestroyImmediate(a);Object.DestroyImmediate(b);
            Mini143GangValidation.Validate();
            File.WriteAllText(Output+"/validation.txt","PASS: both protagonists' jab/kick, held move/sprint/jump lock, measured kick timing, late contact, exact damage once, lock release, wall/behind/range rejection, first-inactive gang activation and cooldown. Edit-mode tests are not normal-speed motion sign-off.");
            CaptureGang();Debug.Log("MINI143 COMBAT/GANG VALIDATION PASS");
        }
        static void CaptureGang()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity",OpenSceneMode.Single);
            var pool=Object.FindFirstObjectByType<RivalGangSpawner>();Require(pool!=null,"missing Dog Life pool");
            Invoke(pool,"ReactivateEligibleMembers");Physics.SyncTransforms();
            var members=((List<GameObject>)typeof(RivalGangSpawner).GetField("_pool",Private).GetValue(pool)).Where(g=>g!=null).Select(g=>g.transform).ToArray();
            Require(members.Length==4,"expected four authored Dog Life members");
            var lines=new List<string>();Vector3 centre=Vector3.zero;
            foreach(var member in members)
            {
                Require(member.gameObject.activeInHierarchy&&member.lossyScale.sqrMagnitude>.1f,"invisible gang member");centre+=member.position;
                var overlaps=Physics.OverlapSphere(member.position+Vector3.up,.23f).Where(c=>!c.transform.IsChildOf(member)&&c.transform!=member&&c is BoxCollider).Select(c=>c.name).ToArray();
                lines.Add(member.name+" pos="+member.position+" scale="+member.lossyScale+" chest box overlaps="+string.Join(",",overlaps));
            }
            File.WriteAllLines(Output+"/gang-placement.txt",lines);centre/=members.Length;
            var cameraObject=new GameObject("MINI143Evidence");var camera=cameraObject.AddComponent<Camera>();
            camera.transform.position=centre+new Vector3(0,9,12);camera.transform.LookAt(centre+Vector3.up);camera.fieldOfView=65;
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes(Output+"/DogLife-Visible.png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;rt.Release();
            Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraObject);
            // Runtime activation proof only: do not save an always-active gang into the scene.
        }
        public static void Survey()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity",OpenSceneMode.Single);
            var lines=new List<string>();
            foreach(string name in new[]{"Sacat","Franki"})
            {
                var root=GameObject.Find(name);var visual=root.transform.Find("Visual");var animator=root.GetComponentInChildren<Animator>();
                var move=MeleeMoveLibrary.GetChainFor(name).Last();var clip=AssetDatabase.LoadAllAssetsAtPath(move.clipPath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
                float speed=name=="Sacat"?1.5f:1;float peak=0,peakTime=0;
                for(float fraction=0;fraction<=1;fraction+=.025f)
                {
                    clip.SampleAnimation(visual.gameObject,clip.length*fraction);
                    var hips=animator.GetBoneTransform(HumanBodyBones.Hips).position;
                    float extension=new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}.Max(b=>Vector3.ProjectOnPlane(animator.GetBoneTransform(b).position-hips,Vector3.up).magnitude);
                    if(extension>peak){peak=extension;peakTime=clip.length*fraction/speed;}
                }
                lines.Add(name+" clip="+clip.length+" effective="+clip.length/speed+" maximum horizontal foot extension="+peak+" at effective seconds="+peakTime);
            }
            File.WriteAllLines(Output+"/kick-survey.txt",lines);foreach(var line in lines)Debug.Log("MINI143 "+line);
            // Diagnostic animation samples are not saved to the canonical scene.
        }
    }
}
