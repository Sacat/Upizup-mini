using UnityEngine;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-190 pistol handling for ANY humanoid (Sacat, Franki, NPCs). Runs in the Animator IK pass on the same object as the Animator:
    ///  - two-handed grip: dominant hand to the aim line, support hand wraps the grip from the left (target measured from the weapon's SupportAnchor),
    ///  - torso/head aim: body + head look along the aim direction (spread over the spine by Mecanim, clamped),
    ///  - pistol-specific finger curls on both hands (trigger finger extended, others wrapped), chosen per rig by geometry so no clip is needed,
    ///  - recoil: spring-damper muzzle flip + push-back + small torso kick after every shot,
    ///  - draw / holster blend, lowered "low ready" stance for alert NPCs,
    ///  - reload: gun tilts to the chest, support hand goes to the belt, brings a magazine up, seats it, racks, and returns to aim.
    /// Arm reach is derived from each rig's own arm length, so it fits every body size. All numbers live in <see cref="Profile"/>.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class FirearmPose : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            public float drawSeconds = .28f, holsterSeconds = .22f;
            public float reachFraction = .93f;                 // of full arm length, aimed
            public float lowReadyReachFraction = .9f, lowReadyPitch = 46f;   // degrees DOWN
            public float lookBodyWeight = .42f, lookHeadWeight = .75f, lookClamp = .6f;
            public float recoilPitchDegrees = 17f, recoilPushback = .075f, recoilBodyKick = 4.5f;
            public float recoilStiffness = 200f, recoilDamping = 16f;
            public float elbowOut = .22f, elbowDown = .16f, supportWristBack = .055f;
            public float noFingerHandPitch = 90f, noFingerFistSquash = .62f;   // rigs whose hands have no finger bones (low-poly NPCs): hand mesh points up at identity, pitch it forward
            public float trigerFingerCurl = 4f, gripFingerCurl = 62f, supportFingerCurl = 58f, thumbCurl = 22f;
            public Vector3 supportAnchorLocal = new Vector3(-.022f, -.048f, -.012f);   // under-trigger-guard left side of the grip (weapon local)
            public Vector3 beltLocal = new Vector3(.2f, -.18f, .1f);                  // left hip in character space (x right, y up from hips)
        }

        public Settings Profile = new Settings();
        public IPistolUser User { get; set; }
        public FirearmController Controller { get { return User as FirearmController; } set { User = value; } }
        public bool WeaponVisible { get; private set; }
        public float AimBlend { get { return aim; } }
        /// <summary>Direction the weapon points this frame (aim line + recoil flip + reload tilt); read by FirearmController.LateUpdate.</summary>
        public Vector3 GunDirection { get; private set; } = Vector3.forward;
        public Vector3 SupportAnchorWorld { get { return supportAnchor != null ? supportAnchor.position : Vector3.zero; } }

        Animator animator; int ikFrame = -1; bool noFingers;
        float aim, low, reloadWeight, lastReload;
        float recoil, recoilVelocity;
        Transform supportAnchor, magazine;
        Vector3 supportLocalToRightHand = new Vector3(-.03f, -.02f, .04f);   // measured every frame once the weapon has been placed

        void Awake() { animator = GetComponent<Animator>(); noFingers = animator != null && animator.isHuman && animator.GetBoneTransform(HumanBodyBones.RightIndexProximal) == null; }

        public void NotifyShot() { recoilVelocity += 26f; }

        void EnsureAnchors()
        {
            var weapon = User != null ? User.WeaponRoot : null;
            if (weapon == null) return;
            if (supportAnchor == null)
            {
                var visual = weapon.Find("LalayTool_Visual") ?? weapon;
                supportAnchor = visual.Find("SupportAnchor");
                if (supportAnchor == null)
                {
                    var go = new GameObject("SupportAnchor"); go.transform.SetParent(visual, false);
                    go.transform.localPosition = Profile.supportAnchorLocal; supportAnchor = go.transform;
                }
            }
            if (magazine == null && animator != null && animator.isHuman)
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (hand != null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Sidearm Magazine (reload prop)";
                    Destroy(go.GetComponent<Collider>()); go.transform.SetParent(hand, false);
                    go.transform.localScale = new Vector3(.024f, .07f, .034f) / Mathf.Max(.01f, hand.lossyScale.x);
                    var r = go.GetComponent<Renderer>(); r.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(.07f, .07f, .08f) }; go.SetActive(false); magazine = go.transform;
                }
            }
        }

        void Update()
        {
            if (User == null) return;
            bool aiming = User.IsAiming, ready = User.IsLowReady || User.IsReloading, any = aiming || ready;
            float dt = Time.deltaTime;
            float speed = any ? 1f / Mathf.Max(.05f, Profile.drawSeconds) : 1f / Mathf.Max(.05f, Profile.holsterSeconds);
            aim = Mathf.MoveTowards(aim, any ? 1f : 0f, dt * speed);
            low = Mathf.MoveTowards(low, (any && !aiming) ? 1f : 0f, dt * 5f);
            reloadWeight = Mathf.MoveTowards(reloadWeight, User.IsReloading ? 1f : 0f, dt * 7f);
            lastReload = User.IsReloading ? User.ReloadProgress : lastReload;
            // spring-damper recoil (impulse comes from NotifyShot)
            // sub-stepped and clamped: a frame hitch must never blow the explicit spring up
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / .008f), 1, 6); float h = Mathf.Min(dt, .05f) / steps;
            for (int i = 0; i < steps; i++) { recoilVelocity += (-Profile.recoilStiffness * recoil - Profile.recoilDamping * recoilVelocity) * h; recoil += recoilVelocity * h; }
            recoil = Mathf.Clamp(recoil, -1.2f, 1.6f);
            WeaponVisible = aim > .38f;
            EnsureAnchors();
            if (magazine != null) magazine.gameObject.SetActive(User.IsReloading && User.ReloadProgress > .3f && User.ReloadProgress < .72f);
        }

        static float Window(float t, float a, float b) { return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t)); }

        Vector3 rightHandScale0, leftHandScale0; bool scalesCaptured; Vector3 rightFistAxis, leftFistAxis;
        void LateUpdate()
        {
            if (noFingers && User != null && animator != null && animator.isHuman) ApplyFist();
            // measure where the support anchor sits relative to the right wrist, in the aim frame, for the NEXT IK pass
            if (User == null || animator == null || !animator.isHuman || supportAnchor == null) return;
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null || aim < .9f || User.IsReloading) return;
            var rot = Quaternion.LookRotation(User.AimDirection, Vector3.up);
            supportLocalToRightHand = Quaternion.Inverse(rot) * (supportAnchor.position - hand.position);
        }

        // Fingerless low-poly hands: a flat open mitten cannot hold a pistol, so squash the hand mesh along its finger axis into a fist-like block.
        void ApplyFist()
        {
            var r = animator.GetBoneTransform(HumanBodyBones.RightHand); var l = animator.GetBoneTransform(HumanBodyBones.LeftHand); if (r == null || l == null) return;
            if (!scalesCaptured) { rightHandScale0 = r.localScale; leftHandScale0 = l.localScale; scalesCaptured = true; }
            float t = Mathf.SmoothStep(0f, 1f, aim);
            Vector3 Sq(Transform h, Vector3 s0)
            {
                // finger axis = the hand-local axis most aligned with the forearm direction (the hand continues the arm)
                var elbow = h.parent; Vector3 along = (h.position - elbow.position).normalized; Vector3 local = h.InverseTransformDirection(along);
                int axis = Mathf.Abs(local.x) > Mathf.Abs(local.y) ? (Mathf.Abs(local.x) > Mathf.Abs(local.z) ? 0 : 2) : (Mathf.Abs(local.y) > Mathf.Abs(local.z) ? 1 : 2);
                var s = s0; s[axis] = s0[axis] * Mathf.Lerp(1f, Profile.noFingerFistSquash, t); return s;
            }
            r.localScale = Sq(r, rightHandScale0); l.localScale = Sq(l, leftHandScale0);
        }

        void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || !animator.isHuman || User == null || aim <= .001f) return;
            if (ikFrame == Time.frameCount) return;   // several Animator layers have an IK pass; solve once per frame
            ikFrame = Time.frameCount;
            var shoulderR = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var elbowR = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var handR = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var shoulderL = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var elbowL = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var handL = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (shoulderR == null || handR == null || shoulderL == null || handL == null) return;

            float armR = Vector3.Distance(shoulderR.position, elbowR.position) + Vector3.Distance(elbowR.position, handR.position);
            float armL = Vector3.Distance(shoulderL.position, elbowL.position) + Vector3.Distance(elbowL.position, handL.position);
            Vector3 aimDir = User.AimDirection.normalized;
            Vector3 flat = Vector3.ProjectOnPlane(aimDir, Vector3.up); if (flat.sqrMagnitude < 1e-4f) flat = transform.forward; flat.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flat).normalized;

            // stance direction: aimed or lowered two-hand ready; reload tilts the gun toward the chest
            float pitchRecoil = recoil * Profile.recoilPitchDegrees;
            float reloadTilt = reloadWeight * (Window(lastReload, 0f, .16f) - Window(lastReload, .86f, 1f));
            Vector3 dir = Quaternion.AngleAxis(Mathf.Lerp(0f, Profile.lowReadyPitch, low) - pitchRecoil + reloadTilt * 36f, right) * aimDir;
            dir = Quaternion.AngleAxis(reloadTilt * -14f, Vector3.up) * dir;
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            GunDirection = dir;

            // dominant hand target: shoulder + dir * reach (+ recoil push-back), blended from the lowered holster point
            float reach = armR * Mathf.Lerp(Profile.reachFraction, Profile.lowReadyReachFraction, low) * Mathf.Lerp(1f, .8f, reloadTilt);
            Vector3 holsterPoint = hips != null ? hips.position + right * .2f - Vector3.up * .05f + flat * .08f : shoulderR.position - Vector3.up * .45f;
            Vector3 aimPoint = shoulderR.position + dir * reach - dir * (recoil * Profile.recoilPushback) - Vector3.up * (.02f * reloadTilt);
            float drawT = Mathf.SmoothStep(0f, 1f, aim);
            Vector3 handTarget = Vector3.Lerp(holsterPoint, aimPoint, drawT);
            Quaternion handRot = rot * Quaternion.Euler(0f, 0f, -90f) * Quaternion.Euler(0f, -90f, 0f);   // right hand: fingers forward, palm toward the gun's left side

            if (noFingers) handRot = rot * Quaternion.Euler(Profile.noFingerHandPitch, 0f, 0f) * Quaternion.Inverse(rot) * handRot;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, drawT); animator.SetIKRotationWeight(AvatarIKGoal.RightHand, drawT);
            animator.SetIKPosition(AvatarIKGoal.RightHand, handTarget); animator.SetIKRotation(AvatarIKGoal.RightHand, handRot);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, drawT * .8f);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, (shoulderR.position + handTarget) * .5f + right * Profile.elbowOut - Vector3.up * Profile.elbowDown);

            // support hand: on the weapon (measured offset), or belt -> magazine well -> slide during the reload
            Vector3 onGun = handTarget + rot * supportLocalToRightHand;
            Vector3 belt = hips != null ? hips.position + right * (-Profile.beltLocal.x) - Vector3.up * (-Profile.beltLocal.y - .02f) + flat * .05f : onGun - Vector3.up * .3f;
            Vector3 magWell = handTarget + rot * new Vector3(-.01f, -.115f, -.045f);
            Vector3 slide = handTarget + rot * new Vector3(-.03f, .03f, -.07f);
            Vector3 supportTarget = onGun;
            float t = lastReload;
            if (reloadWeight > .001f)
            {
                Vector3 rl = Vector3.Lerp(onGun, belt, Window(t, .1f, .3f));
                rl = Vector3.Lerp(rl, magWell, Window(t, .42f, .66f));
                rl = Vector3.Lerp(rl, Vector3.Lerp(magWell + rot * new Vector3(0, .012f, 0), magWell, Window(t, .66f, .72f)), Window(t, .64f, .7f));
                rl = Vector3.Lerp(rl, slide, Window(t, .78f, .88f));
                rl = Vector3.Lerp(rl, onGun, Window(t, .9f, 1f));
                supportTarget = Vector3.Lerp(onGun, rl, reloadWeight);
            }
            supportTarget = Vector3.Lerp(Vector3.Lerp(hips != null ? hips.position - right * .2f - Vector3.up * .05f : supportTarget, supportTarget, 1f), supportTarget, 1f);
            supportTarget = Vector3.Lerp(handTarget - right * .08f - Vector3.up * .25f, supportTarget, drawT);
            Quaternion supportRot = rot * Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, 90f, 0f);   // left hand: fingers forward, palm toward the gun
            supportTarget -= dir * Profile.supportWristBack;   // the IK goal is the wrist; the palm centre sits this far ahead along the fingers
            if (noFingers) supportRot = rot * Quaternion.Euler(Profile.noFingerHandPitch, 0f, 0f) * Quaternion.Inverse(rot) * supportRot;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, drawT); animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, drawT);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, supportTarget); animator.SetIKRotation(AvatarIKGoal.LeftHand, supportRot);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, drawT * .8f);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, (shoulderL.position + supportTarget) * .5f - right * Profile.elbowOut - Vector3.up * (Profile.elbowDown + .06f * reloadWeight));

            // torso + head follow the muzzle (spread over the spine by Mecanim)
            Vector3 lookPoint = (shoulderR.position + shoulderL.position) * .5f + Quaternion.AngleAxis(-recoil * Profile.recoilBodyKick, right) * aimDir * 12f;
            animator.SetLookAtWeight(drawT * (1f - .6f * low) , Profile.lookBodyWeight, Profile.lookHeadWeight, 0f, Profile.lookClamp);
            animator.SetLookAtPosition(lookPoint);

            CurlFingers(true, handTarget, rot);
            CurlFingers(false, supportTarget, rot);
        }

        // Pistol finger pose without any clip: rotate each finger bone about the knuckle axis, sign chosen so the tips close toward the weapon.
        void CurlFingers(bool rightHand, Vector3 palmPoint, Quaternion rot)
        {
            float weight = Mathf.SmoothStep(0f, 1f, aim) * (rightHand ? 1f : 1f);
            if (weight < .01f) return;
            var h = rightHand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
            var hand = animator.GetBoneTransform(h); if (hand == null) return;
            Vector3 gunPoint = supportAnchor != null && rightHand ? User.WeaponRoot.position : (supportAnchor != null ? supportAnchor.position : palmPoint);
            // knuckle line from the index to the little finger root gives the curl axis (hand-rig independent)
            var idx = animator.GetBoneTransform(rightHand ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal);
            var lit = animator.GetBoneTransform(rightHand ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal);
            if (idx == null || lit == null) return;
            Vector3 across = (idx.position - lit.position).normalized;
            var set = rightHand
                ? new[] { (HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal, Profile.trigerFingerCurl),
                          (HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal, Profile.gripFingerCurl),
                          (HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal, Profile.gripFingerCurl),
                          (HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal, Profile.gripFingerCurl) }
                : new[] { (HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal, Profile.supportFingerCurl),
                          (HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal, Profile.supportFingerCurl),
                          (HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal, Profile.supportFingerCurl),
                          (HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal, Profile.supportFingerCurl) };
            Vector3 target = rightHand ? User.WeaponRoot.position + rot * new Vector3(0f, -.035f, -.045f) : (supportAnchor != null ? supportAnchor.position : palmPoint);
            foreach (var (prox, mid, dist, angle) in set)
            {
                var bones = new[] { animator.GetBoneTransform(prox), animator.GetBoneTransform(mid), animator.GetBoneTransform(dist) };
                if (bones[0] == null) continue;
                // choose the sign that moves the fingertip toward the grip target
                var tip = (bones[2] != null ? bones[2] : bones[1] != null ? bones[1] : bones[0]);
                Vector3 pivot = bones[0].position;
                float sign = Vector3.Distance(pivot + Quaternion.AngleAxis(25f, across) * (tip.position - pivot), target) <= Vector3.Distance(pivot + Quaternion.AngleAxis(-25f, across) * (tip.position - pivot), target) ? 1f : -1f;
                for (int i = 0; i < 3; i++)
                {
                    var b = bones[i]; if (b == null) continue;
                    float a = sign * angle * weight * (i == 0 ? .5f : i == 1 ? .8f : .6f);
                    Vector3 axisLocal = Quaternion.Inverse(b.rotation) * across;
                    animator.SetBoneLocalRotation(i == 0 ? prox : i == 1 ? mid : dist, b.localRotation * Quaternion.AngleAxis(a, axisLocal));
                }
            }
            var thumb = animator.GetBoneTransform(rightHand ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal);
            if (thumb != null)
            {
                Vector3 tp = thumb.position; var tipT = animator.GetBoneTransform(rightHand ? HumanBodyBones.RightThumbDistal : HumanBodyBones.LeftThumbDistal) ?? thumb;
                Vector3 axis = Vector3.Cross(tipT.position - tp, across).normalized; if (axis.sqrMagnitude < .1f) return;
                float s = Vector3.Distance(tp + Quaternion.AngleAxis(25f, axis) * (tipT.position - tp), target) <= Vector3.Distance(tp + Quaternion.AngleAxis(-25f, axis) * (tipT.position - tp), target) ? 1f : -1f;
                animator.SetBoneLocalRotation(rightHand ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal, thumb.localRotation * Quaternion.AngleAxis(s * Profile.thumbCurl * weight, Quaternion.Inverse(thumb.rotation) * axis));
            }
        }
    }
}
