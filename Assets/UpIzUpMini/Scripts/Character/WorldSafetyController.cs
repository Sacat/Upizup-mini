using UnityEngine;
using UpIzUpMini.Missions;

namespace UpIzUpMini.Character
{
    public class WorldSafetyController : MonoBehaviour
    {
        [SerializeField] float rescueY = -8f;
        [SerializeField] float fallingSeconds = 4f;
        float falling;
        void Update()
        {
            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return;
            var cc = active.root.GetComponent<CharacterController>();
            bool unsafeFall = active.root.transform.position.y < rescueY || (cc != null && !cc.isGrounded && cc.velocity.y < -5f);
            falling = unsafeFall ? falling + Time.deltaTime : 0f;
            if (active.root.transform.position.y < rescueY || falling >= fallingSeconds)
            {
                falling = 0f;
                CharacterSwitchManager.Instance.RespawnAtSafehouse("You fell out of the playable area.");
            }
        }
    }
}
