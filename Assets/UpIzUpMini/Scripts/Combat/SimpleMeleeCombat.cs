using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Combat
{
    public class SimpleMeleeCombat : MonoBehaviour
    {
        [SerializeField] float range = 2.1f;
        [SerializeField] float damage = 35f;
        [SerializeField] float cooldown = .65f;
        float nextHit;
        public bool IsControlled => GetComponent<PlayerController>()?.IsControlled == true;
        void Update()
        {
            if (!IsControlled || !Input.GetKeyDown(KeyCode.F) || Time.time < nextHit) return;
            nextHit = Time.time + cooldown;
            Vector3 center = transform.position + transform.forward * 1.1f + Vector3.up;
            foreach (var hit in Physics.OverlapSphere(center, range, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.GetComponentInParent<NpcCombatHealth>();
                if (target == null) continue;
                target.Hit(damage, transform.forward * .8f);
                EconomyManager.Instance?.AddHeat(12f);
                break;
            }
        }
    }
}
