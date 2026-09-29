using Ramayana.Core;
using Ramayana.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Ramayana.UI
{
    public class TaskHUD : MonoBehaviour
    {
        [SerializeField] Text label;

        ChapterManager cm;

        void Start()
        {
            cm = ChapterManager.Instance;
            if (cm == null) return;
            cm.TaskChanged += Show;
            cm.ProgressChanged += Show;
            cm.ChapterCompleted += ShowComplete;
            if (cm.IsComplete) ShowComplete();
            else Show(cm.CurrentTask, cm.CurrentProgress);
        }

        void OnDestroy()
        {
            if (cm == null) return;
            cm.TaskChanged -= Show;
            cm.ProgressChanged -= Show;
            cm.ChapterCompleted -= ShowComplete;
        }

        void Show(ChapterTask task, int progress)
        {
            label.text = task.requiredCount > 1
                ? $"{task.description}  ({progress}/{task.requiredCount})"
                : task.description;
        }

        void ShowComplete() => label.text = $"Chapter complete: {cm.Chapter.title}";
    }
}
