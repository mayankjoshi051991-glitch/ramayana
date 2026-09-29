using System.IO;
using UnityEngine;

namespace Ramayana.Core
{
    [System.Serializable]
    public class SaveData
    {
        public string chapterId = "";
        public int taskIndex;
        public int highestChapterUnlocked = 1;
    }

    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static SaveData Load()
        {
            if (!File.Exists(FilePath)) return new SaveData();
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Save file unreadable, starting fresh: {e.Message}");
                return new SaveData();
            }
        }

        public static void Save(SaveData data)
        {
            // Write to a temp file first so a crash mid-write can't corrupt the save.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }
}
