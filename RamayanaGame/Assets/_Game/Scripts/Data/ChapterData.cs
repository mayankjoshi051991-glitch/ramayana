using System.Collections.Generic;
using UnityEngine;

namespace Ramayana.Data
{
    public enum TaskType { ReachPoint, HitTargets, DefeatEnemies, TalkTo, Cutscene }

    [System.Serializable]
    public class ChapterTask
    {
        public string id;
        [TextArea] public string description;
        public TaskType type;
        [Min(1)] public int requiredCount = 1;
    }

    [CreateAssetMenu(menuName = "Ramayana/Chapter", fileName = "NewChapter")]
    public class ChapterData : ScriptableObject
    {
        public string chapterId = "act1_ch01";
        public int act = 1;
        public int chapterNumber = 1;
        public string title;
        public CharacterForm playableForm;
        public List<ChapterTask> tasks = new();
    }
}
