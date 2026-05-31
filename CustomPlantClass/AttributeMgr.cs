namespace CustomPlantClass
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomPlantAttribute : Attribute
    {
        public Type BaseType { get; }
        public BaseCustomPlantData Data { get; }

        public CustomPlantAttribute(Type baseType, BaseCustomPlantData data)
        {
            BaseType = baseType;
            Data = data;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomBulletAttribute : Attribute
    {
        public Type BaseType { get; }
        public BaseCustomBulletData Data { get; }

        public CustomBulletAttribute(Type baseType, BaseCustomBulletData data)
        {
            BaseType = baseType;
            Data = data;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomZombieAttribute : Attribute
    {
        public Type BaseType { get; }
        public BaseCustomZombieData Data { get; }

        public CustomZombieAttribute(Type baseType, BaseCustomZombieData data)
        {
            BaseType = baseType;
            Data = data;
        }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class CustomModAttribute : Attribute
    {
        public string Name;
        public CustomModAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Module)]
    public class CustomModModuleAttribute : Attribute
    {
        public string Name;
        public CustomModModuleAttribute(string name)
        {
            Name = name;
        }
    }
    public static class AttributeMgr
    {

        public static void LoadAllAttributes(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                // --- Plants ---
                var plantAttr = t.GetCustomAttribute<CustomPlantAttribute>();
                if (plantAttr != null)
                {
                    Type tBase = plantAttr.BaseType;
                    Type tClass = t;

                    var method = typeof(DataMgr)
                        .GetMethod("RegisterCustomPlant")
                        .MakeGenericMethod(tBase, tClass);

                    method.Invoke(null, new object[] { plantAttr.Data });
                    continue;
                }

                // --- Bullets ---
                var bulletAttr = t.GetCustomAttribute<CustomBulletAttribute>();
                if (bulletAttr != null)
                {
                    Type tBase = bulletAttr.BaseType;
                    Type tClass = t;

                    var method = typeof(DataMgr)
                        .GetMethod("RegisterCustomBullet")
                        .MakeGenericMethod(tBase, tClass);

                    method.Invoke(null, new object[] { bulletAttr.Data });
                    continue;
                }

                // --- Zombies ---
                var zombieAttr = t.GetCustomAttribute<CustomZombieAttribute>();
                if (zombieAttr != null)
                {
                    Type tBase = zombieAttr.BaseType;
                    Type tClass = t;

                    var method = typeof(DataMgr)
                        .GetMethod("RegisterCustomZombie")
                        .MakeGenericMethod(tBase, tClass);

                    method.Invoke(null, new object[] { zombieAttr.Data });
                    continue;
                }
            }
        }

        public static string GetModName(Assembly asm)
        {
            // 1. CustomModAttribute (assembly-level)
            var modAttr = asm.GetCustomAttribute<CustomModAttribute>();
            if (modAttr != null && !string.IsNullOrWhiteSpace(modAttr.Name))
                return modAttr.Name;

            // 2. CustomModAttribute (module-level, ConfuserEx style)
            foreach (var module in asm.GetModules())
            {
                var moduleAttr = module.GetCustomAttribute<CustomModModuleAttribute>();
                if (moduleAttr != null && !string.IsNullOrWhiteSpace(moduleAttr.Name))
                    return moduleAttr.Name;
            }

            // 3. MyPluginInfo locator
            foreach (var type in asm.GetTypes())
            {
                // Look for a public static field named PluginName
                var field = type.GetField("PluginName",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (field != null && field.FieldType == typeof(string))
                {
                    // Must be const or static readonly
                    string name = field.GetValue(null) as string;
                    if (!string.IsNullOrWhiteSpace(name))
                        return name;
                }
            }


            // 4. BepInPlugin fallback
            var bep = asm.GetCustomAttribute<BepInPlugin>();
            if (bep != null && !string.IsNullOrWhiteSpace(bep.Name))
                return bep.Name;

            if(!asm.FullName.IsNullOrWhiteSpace()) return asm.FullName;

            // 5. Final fallback (no file name, per your request)
            return "Unknown";
        }
    }
}
