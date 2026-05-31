#nullable enable

namespace CustomPlantClass
{
    /// <summary>
    /// Central utility manager for all custom plant registration,
    /// fusion helpers, skin helpers, ID allocation, and IL2CPP type registration.
    /// This class is the backbone of the new custom plant framework.
    /// </summary>
    public class DataMgr : MonoBehaviour
    {
        #region Fields
        public static HashSet<int> ID_List = new();
        public static int CustomPlantCount = 0;
        public static int CustomSkinCount = 0;
        public static int CustomBigStarCount = 0;
        public static int CustomBulletCount = 0;
        public static Dictionary<int, CardLevel> CustomCardLevel = new();
        public static Dictionary<PlantType, (PlantType, Func<Plant, bool>)> replaceList = new();
        public static Dictionary<PlantType,BulletType> plantBulletTypes = new();
        public static Dictionary<ZombieType, (ZombieType, Func<Zombie, bool>)> onZombieTypeSpawnActionList = new();
        public static Dictionary<ZombieType, GameObject> bossHealthSliders = new();
        public static Dictionary<ZombieType,BaseCustomZombieData> zombieDatas = new();
        public static Dictionary<int,Type> CustomLevelComponents = new();
        public static HashSet<BaseCustomLevelData> LoadedCustomLevels = new();
        public static HashSet<int> UsedLevelIDs = new();
        public static HashSet<int> CustomStarUps = new();
        public static HashSet<int> CustomWeakUltiPlants = new();
        public static HashSet<int> CustomStrongUltiPlants = new();
        public static HashSet<int> CustomGridItemTypes = new();
        public static List<string> StartUpMessages = new();
        public static List<string> StartUpErrors = new();
        public static List<string> StartUpWarnings = new();
        public static List<Action> GameStartActions = new();
        public static bool IsGameStarted = false;
        #endregion

        ///*
        #region ID Allocation
        // ---------------------------------------------------------
        //  ID ALLOCATION
        // ---------------------------------------------------------

        /// <summary>
        /// Allocates unique IDs for custom plants/zombies/bullets.
        /// Deterministic, IL2CPP‑safe, multi‑ID‑per‑mod, collision‑proof.
        /// </summary>

        // === Freeze table backing ===
        static readonly string FreezePath = Path.Combine(Paths.ConfigPath, "IDFreeze.json");
        static Dictionary<string, int> FreezeTable = new();
        static bool FreezeLoaded = false;

        // Per‑mod call index (not saved; order is deterministic)
        static readonly Dictionary<string, int> GuidCallIndex = new();

        static readonly JsonSerializerOptions FreezeJsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
        };

        static void LoadFreezeTable()
        {
            if (FreezeLoaded) return;

            try
            {
                if (File.Exists(FreezePath))
                {
                    string json = File.ReadAllText(FreezePath);
                    FreezeTable = JsonSerializer.Deserialize<Dictionary<string, int>>(json, FreezeJsonOptions)
                                ?? new Dictionary<string, int>();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[IDFreeze] Failed to load freeze table: {e}");
                FreezeTable = new Dictionary<string, int>();
            }

            FreezeLoaded = true;
        }

        static void SaveFreezeTable()
        {
            try
            {
                string? dir = Path.GetDirectoryName(FreezePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(FreezeTable, FreezeJsonOptions);
                File.WriteAllText(FreezePath, json);
            }
            catch (Exception e)
            {
                ModLogger.LogError("[IDFreeze]", $"Failed to save freeze table: {e}");
            }
        }

        static Assembly? ResolveModAssembly()
        {
            // 1. Fast path: direct callers (Fireworks, etc.)
            var calling = Assembly.GetCallingAssembly();
            if (calling != null &&
                calling != typeof(DataMgr).Assembly &&
                calling.GetCustomAttribute<BepInPlugin>() != null)
            {
                return calling;
            }

            // 2. Fallback: walk the stack for any assembly with BepInPlugin
            var st = new StackTrace();
            var frames = st.GetFrames();
            if (frames != null)
            {
                foreach (var frame in frames)
                {
                    var type = frame.GetMethod()?.DeclaringType;
                    if (type == null) continue;

                    var asm = type.Assembly;
                    if (asm.GetCustomAttribute<BepInPlugin>() != null)
                        return asm;
                }
            }

            // 3. Unknown
            return null;
        }

        // === DROP‑IN REPLACEMENT ===
        public static ID AllocateID()
        {
            LoadFreezeTable();

            // 1. Resolve mod identity
            Assembly? asm = ResolveModAssembly();

            string guidBase;

            if (asm != null)
            {
                var pluginAttr = asm.GetCustomAttribute<BepInPlugin>();
                guidBase = pluginAttr?.GUID ?? asm.FullName!;
            }
            else
            {
                // Last‑resort fallback: calling assembly identity
                var calling = Assembly.GetCallingAssembly();
                guidBase = calling?.FullName ?? "__IDFREEZE_UNKNOWN__";
            }

            // 2. Multi‑ID‑per‑mod: assign per‑mod call index
            if (!GuidCallIndex.TryGetValue(guidBase, out int index))
                index = 0;

            string freezeKey = $"{guidBase}::{index}";
            GuidCallIndex[guidBase] = index + 1;

            // 3. If this specific call already has a frozen ID, return it
            if (FreezeTable.TryGetValue(freezeKey, out int frozen))
                return frozen;

            // 4. Build used set (your original logic)
            HashSet<int> used = new();

            foreach (PlantType pt in Enum.GetValues(typeof(PlantType)))
                used.Add((int)pt);
            foreach (BulletType bt in Enum.GetValues(typeof(BulletType)))
                used.Add((int)bt);
            foreach (ZombieType zt in Enum.GetValues(typeof(ZombieType)))
                used.Add((int)zt);
            foreach (ParticleType pt in Enum.GetValues(typeof(ParticleType)))
                used.Add((int)pt);
            foreach (CherryBombType dt in Enum.GetValues(typeof(CherryBombType)))
                used.Add((int)dt);
            foreach (GridItemType gt in Enum.GetValues(typeof(GridItemType)))
                used.Add((int)gt);
            foreach (int id in CustomGridItemTypes)
                used.Add(id);

            foreach (var id in CustomCore.CustomPlantTypes)
                used.Add((int)id);
            foreach (var id in CustomCore.CustomZombieTypes)
                used.Add((int)id);
            foreach (var id in CustomCore.CustomBullets.Keys)
                used.Add((int)id);
            foreach (var id in CustomCore.CustomParticles.Keys)
                used.Add((int)id);
            foreach (var id in CustomCore.CustomCherrys.Keys)
                used.Add((int)id);
                //customcore does not have grid items

            foreach (var id in ID_List)
                used.Add(id);

            // 5. Deterministic base ID from freezeKey, then +1 until free
            int baseId = Math.Abs(freezeKey.GetHashCode()) % 50000 + 10000;
            int candidate = baseId;
            while (used.Contains(candidate))
                candidate++;

            // 6. Freeze and return
            FreezeTable[freezeKey] = candidate;
            SaveFreezeTable();

            ID_List.Add(candidate);
            return candidate;
        }

        // Wrappers
        public static ID AllocatePlantID() => AllocateID();
        public static ID AllocateZombieID() => AllocateID();
        public static ID AllocateBulletID() => AllocateID();/**/
        
        #endregion
        #region Fusion Helpers

        // ---------------------------------------------------------
        //  FUSION HELPERS
        // ---------------------------------------------------------

        /// <summary>
        /// Returns a list containing the original fusion pair and its mirrored version.
        /// </summary>
        public static List<(ID, ID)> MirrorTuple((ID, ID) input)
        {
            return new List<(ID, ID)>
            {
                (input.Item1, input.Item2),
                (input.Item2, input.Item1)
            };
        }

        /// <summary>
        /// Flattens an array of fusion lists into a single list.
        /// </summary>
        public static List<(ID, ID)> FlattenFusionArray(List<(ID, ID)>[] input)
        {
            var result = new List<(ID, ID)>();
            if (input == null) return result;

            foreach (var sublist in input)
            {
                if (sublist == null) continue;
                foreach (var pair in sublist)
                    result.Add(pair);
            }
            return result;
        }

        /// <summary>
        /// Mirrors every fusion pair in a list.
        /// </summary>
        public static List<(ID, ID)> MirrorList(List<(ID, ID)> input)
        {
            var result = new List<(ID, ID)>();
            if (input == null) return result;

            foreach (var pair in input)
            {
                result.Add((pair.Item1, pair.Item2));
                result.Add((pair.Item2, pair.Item1));
            }
            return result;
        }

        /// <summary>
        /// Flattens an array of lists and mirrors all pairs.
        /// </summary>
        public static List<(ID, ID)> FlattenAndMirror(List<(ID, ID)>[] input)
        {
            var result = new List<(ID, ID)>();
            if (input == null) return result;

            foreach (var sublist in input)
            {
                if (sublist == null) continue;
                foreach (var pair in sublist)
                {
                    result.Add((pair.Item1, pair.Item2));
                    result.Add((pair.Item2, pair.Item1));
                }
            }
            return result;
        }

        /// <summary>
        /// Removes duplicate fusion pairs.
        /// </summary>
        public static List<(ID, ID)> DeduplicateFusions(List<(ID, ID)> input)
        {
            var set = new HashSet<(int, int)>();
            var result = new List<(ID, ID)>();

            foreach (var pair in input)
            {
                var key = ((int)pair.Item1, (int)pair.Item2);
                if (set.Add(key))
                    result.Add(pair);
            }
            return result;
        }

        /// <summary>
        /// Returns a canonical ordering of a fusion pair (lowest ID first).
        /// </summary>
        public static (ID, ID) CanonicalPair((ID, ID) pair)
        {
            return (pair.Item1 < pair.Item2)
                ? pair
                : (pair.Item2, pair.Item1);
        }

        /// <summary>
        /// Canonicalizes and deduplicates a fusion list.
        /// </summary>
        public static List<(ID, ID)> CanonicalizeList(List<(ID, ID)> input)
        {
            var result = new List<(ID, ID)>();
            var set = new HashSet<(int, int)>();

            foreach (var pair in input)
            {
                var canon = CanonicalPair(pair);
                var key = ((int)canon.Item1, (int)canon.Item2);

                if (set.Add(key))
                    result.Add(canon);
            }
            return result;
        }

        /// <summary>
        /// Creates a mirrored fusion list from simple pair definitions.
        /// </summary>
        public static List<(ID, ID)> Fusion(params (ID, ID)[] pairs)
        {
            var result = new List<(ID, ID)>();
            foreach (var p in pairs)
            {
                result.Add((p.Item1, p.Item2));
                result.Add((p.Item2, p.Item1));
            }
            return result;
        }
        
        #endregion
        #region Bullet Skin

        // ---------------------------------------------------------
        //  BULLET SKIN HELPERS
        // ---------------------------------------------------------

        /// <summary>
        /// Creates a bullet skin mapping list for plant skins.
        /// </summary>
        public static List<(BulletType, List<GameObject?>)> BulletSkin(params (BulletType, GameObject?[])[] entries)
        {
            var result = new List<(BulletType, List<GameObject?>)>();
            foreach (var e in entries)
                result.Add((e.Item1, new List<GameObject?>(e.Item2)));
            return result;
        }
        #endregion
        #region Boss Slider

        public static void RegisterCustomBossHealthSlider(ID zombieType, GameObject slider)
        {
            EnsureGameNotStarted();
            if (slider == null)
            {
                ModLogger.LogError($"Boss slider prefab for {zombieType} is null.");
                return;
            }

            // Must have RectTransform
            if (slider.GetComponent<RectTransform>() == null)
            {
                ModLogger.LogError($"Boss slider prefab for {zombieType} must have a RectTransform.");
                return;
            }

            // Must have Fill Area/Back
            var back = slider.transform.Find("Fill Area/Back");
            if (back == null || back.GetComponent<Image>() == null)
            {
                ModLogger.LogError($"Boss slider prefab for {zombieType} is missing Fill Area/Back with an Image component.");
                return;
            }

            // Slider optional — but validate if present
            var slider2 = slider.GetComponent<Slider>();
            if (slider2 != null)
            {
                // Validate required fields
                if (slider2.fillRect == null)
                    ModLogger.LogWarn($"Slider on prefab for {zombieType} has no Fill Rect. It will be auto‑assigned at runtime.");

                if (slider2.handleRect == null)
                    ModLogger.LogWarn($"Slider on prefab for {zombieType} has no Handle Rect. It will be auto‑assigned at runtime.");
            }
            if (bossHealthSliders.ContainsKey(zombieType))
            {
                ModLogger.LogError("Duplicate zombie type : "+zombieType);
            }
            bossHealthSliders[zombieType]=slider;
        }

        public static void RegisterCustomBossHealthSlider(CustomBossHealthSliderData data)
        {
            Action a = () =>
            {
                var prefab = Core.assetBundle?.GetAsset<GameObject>("HealthSlider");
                if (prefab == null)
                {
                    ModLogger.LogError("HealthSlider prefab not found in asset bundle.");
                    return;
                }

                GameObject gameObject = Instantiate(prefab);

                var fill = gameObject.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
                if (fill != null) fill.color = data.FillColor;

                var icon = gameObject.transform.Find("IconBank/Icon")?.GetComponent<Image>();
                if (icon != null) icon.sprite = data.Icon;

                RegisterCustomBossHealthSlider(data.theZombieType, gameObject);
            };
            AddGameStartAction(a);
        }
        #endregion
        #region Data Builder

        // ---------------------------------------------------------
        //  DATA BUILDERS
        // ---------------------------------------------------------

        /// <summary>
        /// Creates a default-initialized plant data struct.
        /// </summary>
        public static BaseCustomPlantData CreatePlantData(ID id, GameObject prefab, GameObject preview)
        {
            return new BaseCustomPlantData
            {
                PlantId = id,
                Prefab = prefab,
                Preview = preview,
                Fusions = new List<(ID, ID)>(),
                AttackInterval = 0f,
                ProduceInterval = 0f,
                AttackDamage = 0,
                MaxHealth = 300,
                Cd = 0f,
                Sun = 0,
                DefaultBullet = BulletType.Bullet_pea,
                CanPF = false,
                CanStarUp = false,
                CardColor = CardLevel.White,
                IsRainbowCard = false,
                IsUltimatePlant = false,
                CardRepeatAmt = 1,
                Name = "",
                AlmanacEntry = ""
            };
        }

        /// <summary>
        /// Creates a skin data struct for a plant.
        /// </summary>
        public static BasePlantSkinData CreateSkin(BaseCustomPlantData data, GameObject skinPrefab, GameObject skinPreview)
        {
            return new BasePlantSkinData
            {
                data = data,
                SkinPrefab = skinPrefab,
                SkinPreview = skinPreview,
                BulletSkinList = new List<(BulletType, List<GameObject?>)>()
            };
        }
        #endregion
        #region Il2cpp Types

        // ---------------------------------------------------------
        //  IL2CPP TYPE REGISTRATION
        // ---------------------------------------------------------

        /// <summary>
        /// Registers all BaseCustomPlant-derived types in an assembly.
        /// </summary>
        public static void AutoRegisterTypes() => AutoRegisterTypes(Assembly.GetCallingAssembly());
        public static void AutoRegisterTypes(Assembly asm)
        {
            foreach (var type in asm.GetTypes())
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    if (!ClassInjector.IsTypeRegisteredInIl2Cpp(type))
                        ClassInjector.RegisterTypeInIl2Cpp(type);
                }
            }
        }

        /// <summary>
        /// Ensures a specific custom plant class is IL2CPP-registered.
        /// </summary>
        public static void EnsureTypeRegistered<TClass>() where TClass : MonoBehaviour
        {
            var type = typeof(TClass);
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp(type))
                ClassInjector.RegisterTypeInIl2Cpp(type);
        }
        #endregion
        #region Plants

        // ---------------------------------------------------------
        //  PLANT REGISTRATION
        // ---------------------------------------------------------

        /// <summary>
        /// Registers a custom plant using BaseCustomPlantData.
        /// Automatically registers TClass in IL2CPP.
        /// </summary>
        public static ID RegisterCustomPlant<TBase, TClass>(BaseCustomPlantData data)
            where TBase : Plant
            where TClass : MonoBehaviour
        {
            EnsureGameNotStarted();
            EnsureTypeRegistered<TClass>(); // Auto IL2CPP registration

            var fusions = data.Fusions?
                .ConvertAll(p => ((int)p.Item1, (int)p.Item2))
                ?? new List<(int, int)>();

            CustomCore.RegisterCustomPlant<TBase, TClass>(
                data.PlantId,
                data.Prefab,
                data.Preview,
                fusions,
                data.AttackInterval,
                data.ProduceInterval,
                data.AttackDamage,
                data.MaxHealth,
                data.Cd,
                data.Sun
            );

            if (data.CanPF)
            {
                CustomCore.RegisterSuperSkill(
                    data.PlantId,
                    (Plant p) => 1000,
                    (Plant p) =>
                    {
                        if (p.TryGetComponent<BaseCustomPlant>(out var plant))
                            plant.StartPF();
                    }
                );
            }

            if (data.CanStarUp)
                CustomStarUps.Add(data.PlantId);

            if (data.IsRainbowCard)
                CustomCore.RegisterCustomCardToColorfulCards(data.PlantId, data.CardRepeatAmt);

            if (data.IsUltimatePlant)
                CustomCore.AddUltimatePlant(data.PlantId);

            plantBulletTypes.TryAdd(data.PlantId,data.DefaultBullet);

            CustomCore.TypeMgrExtra.IsCustomPlant.Add(data.PlantId);
            AddLevelPlant(data.PlantId, data.CardColor);
            CustomCore.AddPlantAlmanacStrings(data.PlantId, data.Name, data.AlmanacEntry);
            CustomPlantCount++;

            return data.PlantId;
        }

        /// <summary>
        /// Registers a custom plant and its skin in one call.
        /// Automatically registers TClass in IL2CPP.
        /// </summary>
        public static ID RegisterCustomPlant<TBase, TClass>(BasePlantSkinData skinData)
            where TBase : Plant
            where TClass : MonoBehaviour
        {
            EnsureTypeRegistered<TClass>(); // Auto IL2CPP registration

            ID id = RegisterCustomPlant<TBase, TClass>(skinData.data);
            RegisterCustomPlantSkin<TBase, TClass>(skinData);
            return id;
        }

        /// <summary>
        /// Registers a custom plant skin.
        /// Automatically registers TClass in IL2CPP.
        /// </summary>
        public static void RegisterCustomPlantSkin<TBase, TClass>(BasePlantSkinData skin)
            where TBase : Plant
            where TClass : MonoBehaviour
        {
            EnsureTypeRegistered<TClass>(); // Auto IL2CPP registration

            var data = skin.data;

            var fusions = data.Fusions?
                .ConvertAll(p => ((int)p.Item1, (int)p.Item2))
                ?? new List<(int, int)>();

            CustomCore.RegisterCustomPlantSkin<TBase, TClass>(
                data.PlantId,
                skin.SkinPrefab,
                skin.SkinPreview,
                fusions,
                data.AttackInterval,
                data.ProduceInterval,
                data.AttackDamage,
                data.MaxHealth,
                data.Cd,
                data.Sun,
                skin.BulletSkinList
            );
            CustomSkinCount++;
        }
        #endregion
        #region Plant Helper
        public static void AddLevelPlant(ID type, CardLevel level)
        {
            int type_internal = type;
            if(CustomCardLevel.ContainsKey(type_internal)) ModLogger.LogError(MyPluginInfo.PluginName,"Duplicate ID type: "+type_internal);
            CustomCardLevel.Add(type_internal,level);
        }
        public static CardLevel GetCardLevel(PlantLevelData data) =>
            data switch
            {
                PlantLevelData.Basic => CardLevel.White,
                PlantLevelData.Secondary => CardLevel.Green,
                PlantLevelData.Super => CardLevel.Blue,
                PlantLevelData.WeakUltimate => CardLevel.Purple,
                PlantLevelData.StrongUltimate => CardLevel.Gold,
                PlantLevelData.FinalUltimate => CardLevel.Gold,
                PlantLevelData.TreasurePlant => CardLevel.Red,
                _ => CardLevel.White
            };
        public static string CreateAlmanacEntry(
            string introduction,
            string specialtext = "removeifthisisdefaulted",
            (string, string) recipe = default,
            (int damage, float interval) attackinterval = default,
            (int amount, float interval, string unit) produceinterval = default,
            string[]? specialeffects = null,
            (string, string) variantswitch = default,
            string feature = "removeifthisisdefaulted",
            string creator = "removeifthisisdefaulted",
            string usageconditions = "removeifthisisdefaulted",
            string flavor = "removeifthisisdefaulted")
        {
            specialeffects ??= Array.Empty<string>();

            string[] skillNumber =
            {
                "①","②","③","④","⑤","⑥","⑦","⑧","⑨","⑩",
                "⑪","⑫","⑬","⑭","⑮","⑯","⑰","⑱","⑲","⑳"
            };

            var sb = new StringBuilder();

            // Introduction
            sb.AppendLine(introduction);
            sb.AppendLine();

            // Special text (blue highlight)
            if (specialtext != "removeifthisisdefaulted")
            {
                sb.AppendLine($"<color=#0000FF>{specialtext}</color>");
                sb.AppendLine();
            }

            // Creator
            if (creator != "removeifthisisdefaulted")
                sb.AppendLine($"<color=#3D1400>作者：</color><color=red>{creator}</color>");

            // Usage conditions
            if (usageconditions != "removeifthisisdefaulted")
                sb.AppendLine($"<color=#3D1400>使用条件：</color><color=red>{usageconditions}</color>");

            // Recipe (optional for infuseable plants)
            if (recipe != default)
                sb.AppendLine($"<color=#3D1400>融合配方：</color><color=red>{recipe.Item1}+{recipe.Item2}</color>");

            // Variant switch
            if (variantswitch != default)
                sb.AppendLine($"<color=#3D1400>转化配方：</color><color=red>{variantswitch.Item1}←→{variantswitch.Item2}</color>");

            // Attack interval
            if (attackinterval != default)
                sb.AppendLine($"<color=#3D1400>伤害：</color><color=red>{attackinterval.damage}/{attackinterval.interval}秒</color>");

            // Produce interval
            if (produceinterval != default)
                sb.AppendLine($"<color=#3D1400>生产：</color><color=red>{produceinterval.amount}{produceinterval.unit}/{produceinterval.interval}秒</color>");

            // Feature
            if (feature != "removeifthisisdefaulted")
                sb.AppendLine($"<color=#3D1400>特性：</color><color=red>{feature}</color>");

            // Special effects list
            if (specialeffects.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#3D1400>特点：</color><color=red>");
                for (int i = 0; i < specialeffects.Length; i++)
                {
                    string num = i < skillNumber.Length ? skillNumber[i] : $"({i + 1})";
                    sb.AppendLine($"{num}{specialeffects[i]}");
                }
                sb.AppendLine("</color>");
            }

            // Flavor text
            if (flavor != "removeifthisisdefaulted")
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#3D1400>{flavor}</color>");
            }

            return sb.ToString();
        }
        public static void AddCustomPlantUpgrade(ID fromType, ID toType, float percentChance)
        {
            percentChance = Mathf.Clamp(percentChance, 0f, 100f);

            // 0% = never, 100% = always, everything else correct
            AddCustomPlantUpgrade(fromType, toType,
                (Plant p) => Random.value <= percentChance / 100f);
        }
        #endregion
        #region Misc Registration

        public static void RegisterCustomBigStar<TClass>(ref GameObject Star) where TClass : CustomBigStar
        {
            EnsureGameNotStarted();
            Star.AddComponent<TClass>();
            Star.GetComponent<SortingGroup>().sortingLayerName = "fog";
            CustomBigStarCount++;
        }

        public static ID RegisterCustomBullet<TBase, TClass>(BaseCustomBulletData data) where TBase : Bullet where TClass : MonoBehaviour
        {
            EnsureGameNotStarted();
            CustomCore.RegisterCustomBullet<TBase, TClass>(data.BulletId, data.Prefab);
            CustomBulletCount++;
            return data.BulletId;
        }

        /// <summary>
        /// Registers a plant as supporting Star-Up.
        /// </summary>
        public static void RegisterCustomStarUp(ID thePlantType)
            => CustomStarUps.Add(thePlantType);

        public static void AddCustomPlantUpgrade(ID fromType, ID toType, Func<Plant, bool> condition)
            => replaceList.TryAdd(fromType, (toType, condition));

        public static void AddCustomPlantUpgrade(ID fromType, ID toType, Func<bool> condition)
            => replaceList.TryAdd(fromType, (toType, (Plant p) => condition.Invoke()));
        public static ID RegisterCustomZombie<TBase,TClass>(BaseCustomZombieData data) where TBase : Zombie where TClass : MonoBehaviour
        {
            CustomCore.RegisterCustomZombie<TBase,TClass>(data.theZombieType,data.Prefab,-1,data.theAtackDamage,data.maxHealth,data.theFirstArmorHealth,data.theSecondArmorHealth);
            AddGameStartAction(() => GameAPP.resourcesManager.zombieSprites[data.theZombieType]=data.Preview);
            return data.theZombieType;
        }

        public static void AddCustomOnZombieSpawnEvent(ID fromType, ID toType, Func<Zombie, bool> condition)
            => onZombieTypeSpawnActionList.TryAdd(fromType,(toType, condition));
        
        public static void AddGameStartAction(Action action)
        {
            GameStartActions.Add(action);
        }

        public static void AddGameStartAction(MethodBase method)
        {
            if (!method.IsStatic)
                throw new InvalidOperationException("Startup method must be static.");

            if (method.GetParameters().Length != 0)
                throw new InvalidOperationException("Startup method must have no parameters.");

            GameStartActions.Add(() => method.Invoke(null, null));
        }
        #endregion
        /*
        //Unfinished
        public static BuffID AddCustomStrongUltimate(ID thePlantType, string unlockText, BuffBgType bg = default)
        {
            CustomCore.AddUltimatePlant(thePlantType);
            CustomStrongUltiPlants.Add(thePlantType);
            return CustomCore.RegisterCustomBuff(unlockText,BuffType.UnlockPlant,()=>PlantMgr.IsTravelStore(),1000,thePlantType,1,bg);
        }
        */
        #region Grid Items

        /// <summary>
        /// Registers a custom grid item.
        /// </summary>
        public static GridItemType RegisterCustomGridItem<TBase,TClass>(BaseCustomGridItemData data) where TBase : GridItem where TClass : MonoBehaviour
        {
            EnsureGameNotStarted();
            data.Prefab.AddComponent<TClass>();
            return RegisterCustomGridItem<TBase>(data);
        }

        /// <summary>
        /// Registers a custom grid item.
        /// </summary>
        public static GridItemType RegisterCustomGridItemWithType<TClass>(BaseCustomGridItemData data) where TClass : MonoBehaviour
        {
            EnsureGameNotStarted();
            data.Prefab.AddComponent<TClass>();
            return RegisterCustomGridItem(data);
        }

        /// <summary>
        /// Registers a custom grid item.
        /// </summary>
        public static GridItemType RegisterCustomGridItem(BaseCustomGridItemData data)
        {
            EnsureGameNotStarted();
            return RegisterCustomGridItem<GridItem>(data);
        }

        /// <summary>
        /// Registers a custom grid item.
        /// </summary>
        public static GridItemType RegisterCustomGridItem<TBase>(BaseCustomGridItemData data) where TBase : GridItem
        {
            EnsureGameNotStarted();
            GameObject gameObject=data.Prefab;
            if (!gameObject.TryGetComponent<GridItem>(out _)) gameObject.AddComponent<TBase>();
            GridItemType id=data.type.ToGridItemType();
            AddGameStartAction(() => GameAPP.resourcesManager.gridItemPrefabs[id]=gameObject);
            CustomGridItemTypes.Add(data.type.id);
            return id;
        }
        #endregion
        #region Levels

        /// <summary>
        /// Registers a custom level with a custom component.
        /// </summary>
        public static int RegisterCustomLevel<T>(BaseCustomLevelData data) where T : CustomLevelComponent
        {
            EnsureGameNotStarted();
            int theLevelID=data.LevelID;
            if (theLevelID < 0)
            {
                throw new ArgumentException("Invalid level ID : ", nameof(data.LevelID));
            }
            if (data.LevelName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Invalid level Name : ", nameof(data.LevelName));
            }
            if (UsedLevelIDs.Contains(theLevelID))
            {
                throw new InvalidOperationException($"Level ID {theLevelID} already exists.");
            }
            if (data.MaxWave<=0 || data.MaxWave>100)
            {
                throw new ArgumentException("Invalid wave count : ", nameof(data.MaxWave));
            }
            if (data.MapRoadTypes == null)
                throw new InvalidOperationException($"Level {theLevelID} must have a layout.");
            else if (data.MapRoadTypes.Length == 0)
                throw new InvalidOperationException($"Level {theLevelID} must have a layout.");
            UsedLevelIDs.Add(theLevelID);
            CustomLevelComponents.Add(theLevelID,typeof(T));
            LoadedCustomLevels.Add(data);
            return theLevelID;
        }

        /// <summary>
        /// Registers a custom level.
        /// </summary>
        public static int RegisterCustomLevel(BaseCustomLevelData data)
        {
            EnsureGameNotStarted();
            int theLevelID=data.LevelID;
            if (theLevelID < 0)
            {
                throw new ArgumentException("Invalid level ID : ", nameof(data.LevelID));
            }
            if (data.LevelName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Invalid level Name : ", nameof(data.LevelName));
            }
            if (UsedLevelIDs.Contains(theLevelID))
            {
                throw new InvalidOperationException($"Level ID {theLevelID} already exists.");
            }
            if (data.MaxWave<=0 || data.MaxWave>100)
            {
                throw new ArgumentException("Invalid wave count : ", nameof(data.MaxWave));
            }
            if (data.MapRoadTypes == null)
                throw new InvalidOperationException($"Level {theLevelID} must have a layout.");
            else if (data.MapRoadTypes.Length == 0)
                throw new InvalidOperationException($"Level {theLevelID} must have a layout.");
            UsedLevelIDs.Add(data.LevelID);
            LoadedCustomLevels.Add(data);
            return data.LevelID;
        }
        #endregion
        #region Level ID
        public static int AllocateLevelID()
        {
            LevelIDAllocator.LoadFreezeTable();

            // 1. Resolve mod identity
            Assembly? asm = LevelIDAllocator.ResolveModAssembly();

            string guidBase;

            if (asm != null)
            {
                var pluginAttr = asm.GetCustomAttribute<BepInPlugin>();
                guidBase = pluginAttr?.GUID ?? asm.FullName!;
            }
            else
            {
                var calling = Assembly.GetCallingAssembly();
                guidBase = calling?.FullName ?? "__LEVELIDFREEZE_UNKNOWN__";
            }

            // 2. Multi‑ID‑per‑mod: assign per‑mod call index
            if (!LevelIDAllocator.GuidCallIndex.TryGetValue(guidBase, out int index))
                index = 0;

            string freezeKey = $"{guidBase}::{index}";
            LevelIDAllocator.GuidCallIndex[guidBase] = index + 1;

            // 3. If frozen, return it
            if (LevelIDAllocator.FreezeTable.TryGetValue(freezeKey, out int frozen))
                return frozen;

            // 4. Build used set (custom levels only)
            HashSet<int> used = new();

            foreach (var lvl in LoadedCustomLevels)
                used.Add(lvl.LevelID);

            foreach (var id in UsedLevelIDs)
                used.Add(id);

            // 5. Deterministic base ID from freezeKey
            int baseId = Math.Abs(freezeKey.GetHashCode()) % 50000 + 10000;
            int candidate = baseId;

            while (used.Contains(candidate))
                candidate++;

            // 6. Freeze and return
            LevelIDAllocator.FreezeTable[freezeKey] = candidate;
            LevelIDAllocator.SaveFreezeTable();

            UsedLevelIDs.Add(candidate);
            return candidate;
        }
        public static class LevelIDAllocator
        {
            // === Freeze table backing ===
            static readonly string FreezePath = Path.Combine(Paths.ConfigPath, "LevelIDFreeze.json");
            public static Dictionary<string, int> FreezeTable = new();
            static bool FreezeLoaded = false;

            // Per‑mod call index (not saved; deterministic)
            public static readonly Dictionary<string, int> GuidCallIndex = new();

            static readonly JsonSerializerOptions FreezeJsonOptions = new()
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                PropertyNameCaseInsensitive = true
            };

            // ---------------------------------------------------------
            //  Freeze table load/save
            // ---------------------------------------------------------

            public static void LoadFreezeTable()
            {
                if (FreezeLoaded) return;

                try
                {
                    if (File.Exists(FreezePath))
                    {
                        string json = File.ReadAllText(FreezePath);
                        FreezeTable = JsonSerializer.Deserialize<Dictionary<string, int>>(json, FreezeJsonOptions)
                                    ?? new Dictionary<string, int>();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[LevelIDFreeze] Failed to load freeze table: {e}");
                    FreezeTable = new Dictionary<string, int>();
                }

                FreezeLoaded = true;
            }

            public static void SaveFreezeTable()
            {
                try
                {
                    string? dir = Path.GetDirectoryName(FreezePath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    string json = JsonSerializer.Serialize(FreezeTable, FreezeJsonOptions);
                    File.WriteAllText(FreezePath, json);
                }
                catch (Exception e)
                {
                    ModLogger.LogError("[LevelIDFreeze]", $"Failed to save freeze table: {e}");
                }
            }

            // ---------------------------------------------------------
            //  Resolve mod assembly (exact same logic)
            // ---------------------------------------------------------

            public static Assembly? ResolveModAssembly()
            {
                // 1. Fast path
                var calling = Assembly.GetCallingAssembly();
                if (calling != null &&
                    calling != typeof(LevelIDAllocator).Assembly &&
                    calling.GetCustomAttribute<BepInPlugin>() != null)
                {
                    return calling;
                }

                // 2. Stack walk
                var st = new StackTrace();
                var frames = st.GetFrames();
                if (frames != null)
                {
                    foreach (var frame in frames)
                    {
                        var type = frame.GetMethod()?.DeclaringType;
                        if (type == null) continue;

                        var asm = type.Assembly;
                        if (asm.GetCustomAttribute<BepInPlugin>() != null)
                            return asm;
                    }
                }

                // 3. Unknown
                return null;
            }

            
        }
        #endregion
        /// <summary>
        /// Throws an InvalidOperationException if the game is started.
        /// </summary>
        public static void EnsureGameNotStarted()
        {
            if (IsGameStarted)
                throw new InvalidOperationException("Can't do this after game start!");
        }
    }
}
