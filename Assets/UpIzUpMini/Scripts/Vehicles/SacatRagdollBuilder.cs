using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "i want to ragdoll my main characters
    /// and put them on the bike... how the ragdoll parts was mapped to
    /// the bike... i want to mimic the original ragdoll to my
    /// character." Builds a real Unity ragdoll (per-bone Rigidbody +
    /// Collider + CharacterJoint) directly on a Humanoid Animator at
    /// runtime - the same standard technique Unity's own Ragdoll Wizard
    /// uses, just scripted since this has to happen on a live spawned
    /// character rather than being authored once on a prefab.
    ///
    /// Built at whatever scale the character's Transform actually is at
    /// call time - Sacat's own world scale stays 1.0 regardless of the
    /// bike's own scale (VehicleRider.Mount already compensates for
    /// that), so as long as this runs after Mount(), the ragdoll comes
    /// out at his true size, not skewed by the bike.
    /// </summary>
    public static class SacatRagdollBuilder
    {
        public struct RagdollBone
        {
            public Transform transform;
            public Rigidbody rigidbody;
            public Collider collider;
        }

        public static RagdollBone[] Build(Animator animator)
        {
            if (animator == null || !animator.isHuman) return new RagdollBone[0];

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? spine;
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck) ?? chest;

            Transform lUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform lLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform lHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            Transform lUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform lLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform lFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform rLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            Transform rFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            var bones = new System.Collections.Generic.List<RagdollBone>();

            // Hips is the ragdoll's own root - a Rigidbody but no joint.
            var hipsRb = AddRigidbody(hips, 8f);
            var hipsCol = AddCapsule(hips, spine, 0.16f);
            bones.Add(new RagdollBone { transform = hips, rigidbody = hipsRb, collider = hipsCol });

            bones.Add(MakeLimb(chest, hips, neck, 0.15f, mass: 10f, twist: 30f, swing1: 40f, swing2: 40f));
            if (head != null)
                bones.Add(MakeLimb(head, chest, null, 0.12f, mass: 4f, twist: 40f, swing1: 30f, swing2: 30f, isHead: true));

            bones.Add(MakeLimb(lUpperArm, chest, lLowerArm, 0.07f, mass: 2.5f, twist: 60f, swing1: 70f, swing2: 40f));
            bones.Add(MakeLimb(lLowerArm, lUpperArm, lHand, 0.06f, mass: 1.5f, twist: 50f, swing1: 60f, swing2: 5f));
            bones.Add(MakeLimb(rUpperArm, chest, rLowerArm, 0.07f, mass: 2.5f, twist: 60f, swing1: 70f, swing2: 40f));
            bones.Add(MakeLimb(rLowerArm, rUpperArm, rHand, 0.06f, mass: 1.5f, twist: 50f, swing1: 60f, swing2: 5f));

            bones.Add(MakeLimb(lUpperLeg, hips, lLowerLeg, 0.11f, mass: 5f, twist: 40f, swing1: 60f, swing2: 40f));
            bones.Add(MakeLimb(lLowerLeg, lUpperLeg, lFoot, 0.08f, mass: 3f, twist: 20f, swing1: 70f, swing2: 5f));
            bones.Add(MakeLimb(rUpperLeg, hips, rLowerLeg, 0.11f, mass: 5f, twist: 40f, swing1: 60f, swing2: 40f));
            bones.Add(MakeLimb(rLowerLeg, rUpperLeg, rFoot, 0.08f, mass: 3f, twist: 20f, swing1: 70f, swing2: 5f));

            return bones.ToArray();
        }

        private static RagdollBone MakeLimb(Transform bone, Transform parentBone, Transform childBone,
            float radius, float mass, float twist, float swing1, float swing2, bool isHead = false)
        {
            if (bone == null) return default;

            var rb = AddRigidbody(bone, mass);
            Collider col = isHead ? (Collider)AddSphere(bone, radius) : AddCapsule(bone, childBone, radius);

            var parentRb = parentBone != null ? parentBone.GetComponent<Rigidbody>() : null;
            if (parentRb != null)
            {
                var joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parentRb;
                joint.autoConfigureConnectedAnchor = true;
                joint.axis = Vector3.right;
                joint.swingAxis = Vector3.forward;
                joint.lowTwistLimit = new SoftJointLimit { limit = -twist };
                joint.highTwistLimit = new SoftJointLimit { limit = twist };
                joint.swing1Limit = new SoftJointLimit { limit = swing1 };
                joint.swing2Limit = new SoftJointLimit { limit = swing2 };
            }

            return new RagdollBone { transform = bone, rigidbody = rb, collider = col };
        }

        private static Rigidbody AddRigidbody(Transform t, float mass)
        {
            if (t == null) return null;
            var rb = t.GetComponent<Rigidbody>();
            if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.isKinematic = true; // starts inert - SacatRagdollManager wakes it on a real crash
            return rb;
        }

        private static CapsuleCollider AddCapsule(Transform bone, Transform childBone, float radius)
        {
            if (bone == null) return null;
            var col = bone.GetComponent<CapsuleCollider>();
            if (col == null) col = bone.gameObject.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.isTrigger = true; // starts as a trigger - inert while riding, same as the vendor's own rig

            if (childBone != null)
            {
                Vector3 localChild = bone.InverseTransformPoint(childBone.position);
                float length = localChild.magnitude;
                col.height = Mathf.Max(length, radius * 2f);
                col.center = localChild * 0.5f;
                // Orient the capsule's long axis toward the child bone.
                col.direction = LongestAxis(localChild);
            }
            else
            {
                col.height = radius * 3f;
                col.center = Vector3.zero;
            }
            return col;
        }

        private static SphereCollider AddSphere(Transform bone, float radius)
        {
            if (bone == null) return null;
            var col = bone.GetComponent<SphereCollider>();
            if (col == null) col = bone.gameObject.AddComponent<SphereCollider>();
            col.radius = radius;
            col.isTrigger = true;
            return col;
        }

        private static int LongestAxis(Vector3 v)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            if (ax >= ay && ax >= az) return 0; // X
            if (ay >= ax && ay >= az) return 1; // Y
            return 2; // Z
        }
    }
}
