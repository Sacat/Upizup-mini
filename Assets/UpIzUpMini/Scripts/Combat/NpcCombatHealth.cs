using UnityEngine;

namespace UpIzUpMini.Combat
{
    public class NpcCombatHealth : MonoBehaviour
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float recoverSeconds = 12f;
        float health;
        float recoverAt;
        CharacterController controller;
        Renderer[] renderers;
        void Awake() { health = maxHealth; controller = GetComponent<CharacterController>(); renderers = GetComponentsInChildren<Renderer>(true); }
        public void Hit(float damage, Vector3 push)
        {
            if (recoverAt > 0f) return;
            health = Mathf.Max(0f, health - damage);
            if (controller != null && controller.enabled) controller.Move(push);
            if (health <= 0f) { recoverAt = Time.time + recoverSeconds; foreach (var r in renderers) r.enabled = false; if (controller != null) controller.enabled = false; }
        }
        void Update()
        {
            if (recoverAt <= 0f || Time.time < recoverAt) return;
            health = maxHealth; recoverAt = 0f;
            foreach (var r in renderers) r.enabled = true;
            if (controller != null) controller.enabled = true;
        }
    }
}
