namespace CustomPlantClass
{
    public static class GlobalTracker
    {
        public static bool IsCustomLevel = false;
        public static int CustomLevelID = -1;
    }

    public static class LevelProgressionManager
    {
        private static readonly string ProgressPath =
            Path.Combine(Paths.ConfigPath, "CustomLevelProgress.json");

        private static Dictionary<int, bool> CompletedLevels = new();
        private static bool Loaded = false;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
        };

        // Load progression file
        public static void Load()
        {
            if (Loaded) return;

            try
            {
                if (File.Exists(ProgressPath))
                {
                    string json = File.ReadAllText(ProgressPath);
                    CompletedLevels =
                        JsonSerializer.Deserialize<Dictionary<int, bool>>(json, JsonOptions)
                        ?? new Dictionary<int, bool>();
                }
            }
            catch (Exception e)
            {
                ModLogger.LogError($"[CustomLevels] Failed to load progression: {e}");
                CompletedLevels = new Dictionary<int, bool>();
            }

            Loaded = true;
        }

        // Save progression file
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ProgressPath));
                string json = JsonSerializer.Serialize(CompletedLevels, JsonOptions);
                File.WriteAllText(ProgressPath, json);
            }
            catch (Exception e)
            {
                ModLogger.LogError($"[CustomLevels] Failed to save progression: {e}");
            }
        }

        // Mark a level as completed
        public static void MarkCompleted(int levelID)
        {
            Load();
            CompletedLevels[levelID] = true;
            Save();
        }

        // Check if a level is completed
        public static bool IsCompleted(int levelID)
        {
            Load();
            return CompletedLevels.TryGetValue(levelID, out bool done) && done;
        }
    }
}
