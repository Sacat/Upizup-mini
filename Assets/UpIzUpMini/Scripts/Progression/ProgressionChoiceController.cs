using UnityEngine;
using UpIzUpMini.Missions;

namespace UpIzUpMini.Progression
{
    public class ProgressionChoiceController : MonoBehaviour
    {
        void Update()
        {
            if (MissionSystem.Instance == null || !MissionSystem.Instance.IsCurrentObjective(ObjectiveKind.ChoosePath)) return;
            if (Input.GetKeyDown(KeyCode.L)) { ProgressionManager.Instance?.ChoosePath(CareerPath.LegitimateFarmer); MissionSystem.Instance.Notify(ObjectiveKind.ChoosePath, "legit"); }
            if (Input.GetKeyDown(KeyCode.K)) { ProgressionManager.Instance?.ChoosePath(CareerPath.WeedRoute); MissionSystem.Instance.Notify(ObjectiveKind.ChoosePath, "weed"); }
        }
    }
}
