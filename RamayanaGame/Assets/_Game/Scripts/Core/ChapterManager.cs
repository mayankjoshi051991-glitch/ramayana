using System;
using Ramayana.Data;
using UnityEngine;

namespace Ramayana.Core
{
    // Runs a chapter's tasks strictly in order and saves after each one.
    public class ChapterManager : MonoBehaviour
    {
        public static ChapterManager Instance { get; private set; }

        [SerializeField] ChapterData chapter;

        public event Action<ChapterTask, int> TaskChanged;
        public event Action<ChapterTask, int> ProgressChanged;
        public event Action ChapterCompleted;

        SaveData save;
        int taskIndex;
        int progress;

        public ChapterData Chapter => chapter;
        public ChapterTask CurrentTask => taskIndex < chapter.tasks.Count ? chapter.tasks[taskIndex] : null;
        public int CurrentProgress => progress;
        public bool IsComplete => CurrentTask == null;

        void Awake()
        {
            Instance = this;
            save = SaveSystem.Load();
            bool resume = save.chapterId == chapter.chapterId && save.taskIndex < chapter.tasks.Count;
            taskIndex = resume ? save.taskIndex : 0;
        }

        void Start() => Announce();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsCurrent(string taskId) => CurrentTask != null && CurrentTask.id == taskId;

        public void ReportProgress(string taskId, int amount = 1)
        {
            if (!IsCurrent(taskId)) return;

            progress += amount;
            ProgressChanged?.Invoke(CurrentTask, progress);
            if (progress < CurrentTask.requiredCount) return;

            progress = 0;
            taskIndex++;
            save.chapterId = chapter.chapterId;
            save.taskIndex = taskIndex;
            if (IsComplete)
                save.highestChapterUnlocked = Mathf.Max(save.highestChapterUnlocked, chapter.chapterNumber + 1);
            SaveSystem.Save(save);
            Announce();
        }

        void Announce()
        {
            if (IsComplete) ChapterCompleted?.Invoke();
            else TaskChanged?.Invoke(CurrentTask, progress);
        }
    }
}
