



namespace CustomPlantClass
{
    public static class AssetMgr
    {
        private static readonly HttpClient http = new HttpClient();
        // Unified cache for all bundles (file, resource, base64)
        private static readonly Dictionary<string, AssetBundle> _bundles = new();

        // -----------------------------
        //  Utility: Hash Base64 strings
        // -----------------------------
        private static string HashBase64(string base64)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(base64));
            return Convert.ToHexString(hash); // readable, stable key
        }

        // -----------------------------
        //  Load AssetBundle from Base64
        // -----------------------------
        public static AssetBundle LoadBundleBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return null;

            string key = "base64:" + HashBase64(base64);

            if (_bundles.TryGetValue(key, out var cached))
                return cached;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch
            {
                ModLogger.LogInfo(MyPluginInfo.PluginName, "AssetMgr: Invalid Base64 string.");
                return null;
            }

            var bundle = AssetBundle.LoadFromMemory(bytes);
            if (bundle == null)
            {
                ModLogger.LogInfo(MyPluginInfo.PluginName, "AssetMgr: Failed to load AssetBundle from Base64.");
                return null;
            }

            _bundles[key] = bundle;
            return bundle;
        }

        // -----------------------------
        //  Load AssetBundle from file
        // -----------------------------
        public static AssetBundle LoadBundleFromFile(string path, string name, bool deduplicate = true)
        {
            string key = "file:" + name;

            if (deduplicate && _bundles.TryGetValue(key, out var cached))
                return cached;

            if (!File.Exists(path))
            {
                ModLogger.LogInfo(MyPluginInfo.PluginName, $"AssetMgr: File not found: {path}");
                return null;
            }

            // Synchronous load — IL2CPP safe
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                ModLogger.LogInfo(MyPluginInfo.PluginName, $"AssetMgr: Failed to load AssetBundle from file: {path}");
                return null;
            }

            _bundles[key] = bundle;
            return bundle;
        }

        // -----------------------------------------
        //  Load AssetBundle from embedded resources
        // -----------------------------------------
        public static AssetBundle LoadBundleFromResource(Assembly asm, string resourceName, bool deduplicate = true)
        {
            string key = "res:" + resourceName;

            if (deduplicate && _bundles.TryGetValue(key, out var cached))
                return cached;

            Stream stream =
                asm.GetManifestResourceStream(asm.GetName().Name + "." + resourceName) ??
                asm.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                ModLogger.LogInfo(MyPluginInfo.PluginName, $"AssetMgr: Resource not found: {resourceName}");
                return null;
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            var bundle = AssetBundle.LoadFromMemory(ms.ToArray());
            if (bundle == null)
            {
                ModLogger.LogInfo(MyPluginInfo.PluginName, $"AssetMgr: Failed to load AssetBundle from resource: {resourceName}");
                return null;
            }

            _bundles[key] = bundle;
            return bundle;
        }
        public static async Task<string> DownloadAndConvertToBase64Async(string url)
        {
            try
            {
                var bytes = await http.GetByteArrayAsync(url);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Download failed (are you offline?) : \n{ex.Message}");
                return null;
            }
        }
        public static string GetBase64FromCache(string cacheFolder, string filename, string urlfallback)
        {
            try
            {
                Directory.CreateDirectory(cacheFolder);

                string fullPath = Path.Combine(cacheFolder, filename);

                // 1. Cache hit → return Base64
                if (File.Exists(fullPath))
                {
                    ModLogger.LogInfo($"Loaded from cache: {filename}");
                    byte[] cachedBytes = File.ReadAllBytes(fullPath);
                    return Convert.ToBase64String(cachedBytes);
                }

                // 2. No fallback URL → cannot download
                if (string.IsNullOrWhiteSpace(urlfallback))
                {
                    ModLogger.LogWarn($"Cache miss and no fallback URL for: {filename}");
                    return null;
                }

                // 3. Download Base64 string
                ModLogger.LogInfo($"Downloading and caching: {filename}");
                string base64 = DownloadAndConvertToBase64Async(urlfallback).GetAwaiter().GetResult();

                if (string.IsNullOrEmpty(base64))
                    return null;

                // 4. Convert Base64 → bytes
                byte[] bytes = Convert.FromBase64String(base64);

                // 5. Save to cache
                File.WriteAllBytes(fullPath, bytes);

                // 6. Return Base64
                return base64;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Cache error for {filename}: {ex.Message}");
                return null;
            }
        }
    }
}
