using Ramayana.Core;
using Ramayana.Player;
using UnityEngine;

namespace Ramayana.Gameplay
{
    // Completes a ReachPoint task when the player stands inside this trigger.
    [RequireComponent(typeof(Collider2D))]
    public class TaskTrigger : MonoBehaviour
    {
        [SerializeField] string taskId;

        void OnTriggerStay2D(Collider2D other)
        {
            var cm = ChapterManager.Instance;
            if (cm == null || !cm.IsCurrent(taskId)) return;
            if (other.GetComponentInParent<PlayerController2D>() != null)
                cm.ReportProgress(taskId);
        }
    }
}
