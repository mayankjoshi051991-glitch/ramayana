using System.Collections;
using Ramayana.Combat;
using Ramayana.Core;
using UnityEngine;

namespace Ramayana.Gameplay
{
    // Counts toward a HitTargets task only while that task is active, so early shots can't soft-lock the chapter.
    [RequireComponent(typeof(Collider2D))]
    public class ArcheryTarget : MonoBehaviour, IArrowTarget
    {
        [SerializeField] string taskId = "hit_targets";
        [SerializeField] SpriteRenderer sprite;

        public bool OnArrowHit(int damage)
        {
            var cm = ChapterManager.Instance;
            if (cm != null && cm.IsCurrent(taskId))
            {
                cm.ReportProgress(taskId);
                gameObject.SetActive(false);
            }
            else if (sprite != null)
            {
                StartCoroutine(Flash());
            }
            return true;
        }

        IEnumerator Flash()
        {
            Color original = sprite.color;
            sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            sprite.color = original;
        }
    }
}
