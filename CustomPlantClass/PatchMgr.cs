#nullable enable
namespace CustomPlantClass
{
    [HarmonyPatch(typeof(Plant))]
    public static class Plant_Patches
    {
        // -------------------------
        //  Die()
        // -------------------------
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Plant.Die))]
        public static bool Die_Prefix(Plant __instance, DieReason reason)
        {
            if (__instance == null || !Utils.InGame()) return true;
            if (__instance.TryGetComponent<BaseCustomPlant>(out var p))
            {

                if (p.isPF &&
                (reason == DieReason.CrashInWater ||
                reason == DieReason.Default))
                {
                    __instance.thePlantHealth = __instance.thePlantMaxHealth;
                    __instance.UpdateText();
                    return false;
                }
                else if (p.isPF && reason == DieReason.BySteal)
                {
                    CreatePlant.Instance.SetPlant(
                        __instance.thePlantColumn,
                        __instance.thePlantRow,
                        __instance.thePlantType,
                        null,
                        default,
                        true,
                        false,
                        null
                    );
                }
                else
                {
                    p.OnDie(reason);
                }
            }
            return true;
        }

        // -------------------------
        //  Crashed()
        // -------------------------
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Plant.Crashed))]
        public static bool Crashed_Prefix(Plant __instance)
        {
            if (__instance.TryGetComponent<BaseCustomPlant>(out var p))
            {
                if (p.isPF)
                {
                    __instance.thePlantHealth = __instance.thePlantMaxHealth;
                    return false;
                }
                else
                {
                    p.OnDie(DieReason.Crash);
                }
            }
            return true;
        }

        // -------------------------
        //  StarUp()
        // -------------------------
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Plant.StarUp))]
        public static bool StarUp_Prefix(Plant __instance)
        {
            if (DataMgr.CustomStarUps.Contains((int)__instance.thePlantType))
            {
                __instance.starUp = true;
                __instance.UpdateStarIcon();
                return false;
            }
            return true;
        }

        // -------------------------
        //  InitText()
        // -------------------------
        [HarmonyPatch(nameof(Plant.InitText))]
        [HarmonyPostfix]
        public static void InitText_Postfix(Plant __instance, ref GameObject __result)
        {
            if (__instance.TryGetComponent<BaseCustomPlant>(out var BaseCustomPlant))
            {
                BaseCustomPlant.InitText(__result);
            }
        }
    }
    [HarmonyPatch(typeof(CreatePlant))]
    public static class CreatePlant_Patch
    {
        // -----------------------------
        // MIX REPLACEMENT
        // -----------------------------
        [HarmonyPatch(nameof(CreatePlant.CheckMix))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void CheckMix_Postfix(CreatePlant __instance, ref Plant __result)
        {
            if (__result == null || !Utils.InGame()) return;

            if (!DataMgr.replaceList.TryGetValue(__result.thePlantType, out var entry))
                return;

            var (newType, condition) = entry;

            if (condition(__result) && GameAPP.theGameStatus == GameStatus.InGame)
            {
                int row = __result.thePlantRow;
                int col = __result.thePlantColumn;

                __result.Die(DieReason.ByMix);
                __result = __instance.SetPlant(col, row, newType, null, default, true);
            }
        }

        // -----------------------------
        // TRAVEL LIMITS FOR CUSTOM ULTIMATES
        // -----------------------------
        [HarmonyPatch(nameof(CreatePlant.LimTravel))]
        [HarmonyPostfix]
        public static void LimTravel_Postfix(CreatePlant __instance, PlantType theSeedType, ref bool __result)
        {
            // If already blocked by base game, don't override
            if (__result) return;

            Board board = __instance.board;
            if (board == null)
            {
                __result = false;
                return;
            }

            bool isWeak = DataMgr.CustomWeakUltiPlants.Contains((int)theSeedType);
            bool isStrong = DataMgr.CustomStrongUltiPlants.Contains((int)theSeedType);

            // Not an ultimate → do nothing
            if (!isWeak && !isStrong)
                return;

            Board.BoardTag tag = board.boardTag;

            // Travel mode disabled entirely
            if (!Utils.EnableTravelPlant() || !tag.enableTravelPlant)
            {
                __result = true;
                InGameText.Instance.ShowText("该配方仅旅行模式或深渊可用", 3f, false);
                return;
            }

            // Travel mode enabled → check unlock lists
            if (isWeak)
            {
                if (TravelMgr.Instance == null ||
                    !TravelMgr.Instance.data.unlockedWeaks.Contains(theSeedType))
                {
                    __result = true;
                    InGameText.Instance.ShowText("未选取此植物", 3f, false);
                    return;
                }
            }

            if (isStrong)
            {
                if (TravelMgr.Instance == null ||
                    !TravelMgr.Instance.data.unlockedPlants.Contains((TravelUnlocks)theSeedType))
                {
                    __result = true;
                    InGameText.Instance.ShowText("该配方需要抽取", 3f, false);
                    return;
                }
            }

            // All checks passed → allow planting
            __result = false;
        }
    }
    [HarmonyPatch(typeof(CreateZombie))]
    [HarmonyPriority(Priority.Last)]
    public static class CreateZombie_Patch
    {
        [HarmonyPatch(nameof(CreateZombie.SetZombie))]
        [HarmonyPostfix]
        public static void SetZombie_Postfix(CreateZombie __instance, ref Zombie __result, ref bool isIdle)
        {
            if (__result == null || !Utils.InGame()) return;
            if (__instance.board.TryGetComponent<CustomLevelComponent>(out var customLevelComponent))
            {
                customLevelComponent.OnZombieCreate(__result);
            }
            if (!DataMgr.onZombieTypeSpawnActionList.ContainsKey(__result.theZombieType)) return;
            var a = DataMgr.onZombieTypeSpawnActionList[__result.theZombieType];
            if (a.Item2.Invoke(__result) && GameAPP.theGameStatus == GameStatus.InGame)
            {
                var row = __result.theZombieRow;
                var x = __result.transform.position.x;
                __result = __instance.SetZombie(row, a.Item1, x, isIdle);//;)
            }
        }
    }
    [HarmonyPatch(typeof(Zombie))]
    public static class Zombie_Patch
    {
        [HarmonyPatch(nameof(Zombie.AttackEffect))]
        [HarmonyPrefix]
        public static bool AttackEffect_Prefix(Zombie __instance, Plant plant)
        {
            if (__instance == null || plant == null) return true;
            if (__instance.TryGetComponent<BaseCustomZombie>(out var z))
            {
                return z.AttackEffect(plant);
            }
            return true;
        }
        [HarmonyPatch(nameof(Zombie.Die))]
        [HarmonyPrefix]
        public static void Die_Prefix(Zombie __instance, ref int reason)
        {
            if (__instance == null || !Utils.InGame()) return;
            if (__instance.theStatus == ZombieStatus.Dying)
            {
                if (reason == __instance.dieReason)
                {
                    return;
                }
            }
            if (__instance.TryGetComponent<CustomLevelComponent>(out var customLevelComponent))
            {
                customLevelComponent.OnZombieDie(__instance, reason);
            }
        }
        [HarmonyPatch(nameof(Zombie.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(Zombie __instance)
        {
            if (__instance == null || !Utils.InGame()) 
                return;

            if (!DataMgr.zombieDatas.TryGetValue(__instance.theZombieType, out var data))
                return;

            // Attack damage
            __instance.theAttackDamage = data.theAtackDamage;

            // FIRST ARMOR
            if (!string.IsNullOrEmpty(data.theFirstArmorPath))
            {
                var firstObj = __instance.transform.FindChild(data.theFirstArmorPath);
                if (firstObj != null)
                {
                    __instance.theFirstArmor = firstObj.gameObject;
                    __instance.theFirstArmorType = data.theFirstArmorType;
                    __instance.theFirstArmorMaxHealth = data.theFirstArmorHealth;
                }
            }

            // SECOND ARMOR
            if (!string.IsNullOrEmpty(data.theSecondArmorPath))
            {
                var secondObj = __instance.transform.FindChild(data.theSecondArmorPath);
                if (secondObj != null)
                {
                    __instance.theSecondArmor = secondObj.gameObject;
                    __instance.theSecondArmorType = data.theSecondArmorType;
                    __instance.theSecondArmorMaxHealth = data.theSecondArmorHealth;
                }
            }

            __instance.UpdateHealthText();
        }
    }

    [HarmonyPatch(typeof(Shooter))]
    public class Shooter_Patch
    {
        [HarmonyPatch(nameof(Shooter.AnimShoot))]
        [HarmonyPrefix]
        public static bool AnimShoot_Prefix(Shooter __instance, ref Bullet __result)
        {
            if (__instance.TryGetComponent<BaseCustomPlant>(out var BaseCustomPlant))
            {
                __result = BaseCustomPlant.AnimShoot_Custom();
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(Shooter.AnimShoot2))]
        [HarmonyPrefix]
        public static bool AnimShoot2_Prefix(Shooter __instance)
        {
            if (__instance.TryGetComponent<BaseCustomPlant>(out var baseCustomPlant))
            {
                baseCustomPlant.AnimShoot2_Custom();
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Board))]
    public class Board_Patch
    {
        [HarmonyPatch(nameof(Board.OnPlantCreate))]
        [HarmonyPrefix]
        public static void OnPlantCreate_Prefix(Board __instance, Plant plant)
        {
            if (__instance == null) return;
            if (__instance.TryGetComponent<CustomLevelComponent>(out var customLevelComponent))
            {
                customLevelComponent.OnPlantCreate(plant);
            }
        }
        [HarmonyPatch(nameof(Board.OnPlantDie))]
        [HarmonyPrefix]
        public static void OnPlantDie_Prefix(Board __instance, Plant plant, ref DieReason plantDieReason)
        {
            if (__instance == null) return;
            if (__instance.TryGetComponent<CustomLevelComponent>(out var customLevelComponent))
            {
                customLevelComponent.OnPlantDie(plant, plantDieReason);
            }
        }
    }
    [HarmonyPatch(typeof(TreasureData))]
    public static class TreasureDataPatch
    {
        [HarmonyPatch(nameof(TreasureData.GetCardLevel))]
        [HarmonyPrefix]
        public static bool GetCardLevel(TreasureData __instance, ref PlantType thePlantType, ref CardLevel __result)
        {
            if (DataMgr.CustomCardLevel.ContainsKey((int)thePlantType))
            {
                __result = DataMgr.CustomCardLevel[(int)thePlantType];
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Bullet_pea))]
    public static class Bullet_pea_Patch
    {
        [HarmonyPatch(nameof(Bullet_pea.HitZombie))]
        [HarmonyPrefix]
        public static bool HitZombie_Prefix(Bullet_pea __instance, Zombie zombie)
        {
            if (__instance.TryGetComponent<BaseCustomBullet>(out var a))
            {
                return a.HitZombie(zombie);
            }
            return true;
        }
        [HarmonyPatch(nameof(Bullet_pea.HitPlant))]
        [HarmonyPrefix]
        public static bool HitPlant_Prefix(Bullet_pea __instance, Plant plant)
        {
            if (__instance.TryGetComponent<BaseCustomBullet>(out var a))
            {
                return a.HitPlant(plant);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Bullet))]
    public static class Bullet_Patch
    {
        [HarmonyPatch(nameof(Bullet.CheckZombie))]
        [HarmonyPrefix]
        public static bool HitZombie_Prefix(Bullet __instance, Zombie zombie)
        {
            if (__instance.TryGetComponent<BaseCustomBullet>(out var a))
            {
                return a.HitZombieCondition(zombie);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Bullet_cabbage))]
    public static class Bullet_cabbage_Patch
    {
        [HarmonyPatch(nameof(Bullet_cabbage.HitZombie))]
        [HarmonyPrefix]
        public static bool HitZombie_Prefix(Bullet_cabbage __instance, Zombie zombie)
        {
            if (__instance.TryGetComponent<BaseCustomBullet>(out var a))
            {
                return a.HitZombie(zombie);
            }
            return true;
        }
        [HarmonyPatch(nameof(Bullet_cabbage.HitLand))]
        [HarmonyPrefix]
        public static bool HitPlant_Prefix(Bullet_cabbage __instance)
        {
            if (__instance.TryGetComponent<BaseCustomBullet>(out var a))
            {
                return a.HitLand();
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(HealthSliderManager),nameof(HealthSliderManager.Awake))]
    public static class HealthSliderManagerAwakePatch
    {
        [HarmonyPostfix]
        public static void Postfix(HealthSliderManager __instance)
        {
            Transform layout = __instance.transform
                .Find("Scroll View/Viewport/Content/Layout");

            if (layout == null)
                return;

            foreach(ZombieType i in DataMgr.bossHealthSliders.Keys)
            {
                GameObject barObj = Object.Instantiate(DataMgr.bossHealthSliders[i], layout, false);

                var controller = barObj.AddComponent<BoardHealthSlider>();
                controller.zombieType = i;

                controller.slider = barObj.GetComponent<Slider>();
                if(controller.slider==null)controller.slider = barObj.GetComponentInChildren<Slider>();
                controller.backImage = barObj.transform
                    .Find("Fill Area/Back")
                    .GetComponent<Image>();

                __instance.sliders.Add(controller);
            }
        }
    }
    [HarmonyPatch(typeof(PlantDataMenu))]
    public static class PlantDataMenuPatch
    {
        [HarmonyPatch(nameof(PlantDataMenu.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(PlantDataMenu __instance)
        {
            if (__instance == null || __instance.IsDestroyed()) return;
            if (__instance.plant == null || __instance.plant.IsDestroyed()) return;

            var basePlant = __instance.plant.GetComponent<BaseCustomPlant>();
            if (basePlant == null) return;

            var info = basePlant.GetLiveInfo();
            if (info.Count == 0) return;

            var sb = new StringBuilder();
            foreach (var pair in info)
                sb.AppendLine($"{pair.Key}：{pair.Value}");

            foreach (var text in __instance.infoText)
                text.text += sb.ToString();
        }
    }
	[HarmonyPatch (typeof(TravelHelper))]
	public class TravelHelperGetAllUltimatePlantTypesPatch
	{
        [HarmonyPatch (nameof(TravelHelper.GetAllUltimatePlantTypes))]
		[HarmonyPostfix]
		public static void Postfix (ref bool isStrongUltimate, ref bool withSub, ref List<PlantType> __result)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (isStrongUltimate)
            {
                foreach (var i in DataMgr.CustomStrongUltiPlants)
                {
				    __result.Add ((PlantType)i);
                }
			}
            else
            {
                foreach (var i in DataMgr.CustomWeakUltiPlants)
                {
				    __result.Add ((PlantType)i);
                }
			}
		}
	}
    [HarmonyPatch (typeof(AlmanacPlantMenu))]
	public class AlmanacPlantAwakeMenuPatch
	{
        [HarmonyPatch (nameof(AlmanacPlantMenu.Awake))]
		[HarmonyPostfix]
		public static void Awake_Postfix (AlmanacPlantMenu __instance)
		{
			Transform obj = __instance.transform.Find ("Scroll View/Viewport/Content").transform;
			if (PlantMgr.IsNotNull((obj != null) ? obj.Find ("LookUlti_1").GetComponent<UIButton> () : null,out var button)) {
                var a=() =>
                {
                    __instance.ShowPlants (TravelHelper.GetAllUltimatePlantTypes(false,true));
                };
                if (button?.clickEvent != null)
                button.clickEvent.AddListener(a);
			}
            var btn  = obj?.Find("LookUlti_2")?.GetComponent<UIButton>();
            if (btn == null) return;
            var action = () =>
            {
                var list = TravelHelper.GetAllUltimatePlantTypes(true, true);
                __instance.ShowPlants(list);
            };
            btn.clickEvent.AddListener(action);
		}
	}
    [HarmonyPatch(typeof(GameAPP))]
    [HarmonyPriority(Priority.Last)]
    public class GameAPP_Patch
    {
        [HarmonyPatch(nameof(GameAPP.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(GameAPP __instance)
        {
            foreach(Action a in DataMgr.GameStartActions)
            {
                try
                {
                    a.Invoke();
                }
                catch (Exception e)
                {
                    ModLogger.LogError("Startup exception!\n" + e.ToString());
                }
            }
            DataMgr.IsGameStarted=true;
            //__instance.AddComponent<GameAppInitBehaviour>();
        }
        [HarmonyPatch(nameof(GameAPP.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(GameAPP __instance)
        {
            //no null check because if it is null, the game does not exist
            Core.Logger.LogInfo("================================");
            Core.Logger.LogInfo($"     Registered {DataMgr.CustomPlantCount} custom plants, {DataMgr.CustomSkinCount} custom plant skins, {DataMgr.CustomBigStarCount} custom big stars, and {DataMgr.CustomBulletCount} custom bullets.");
            Core.Logger.LogInfo($"     In total, there are now {GameAPP.resourcesManager.allPlants.Count} plants");

            foreach (var msg in DataMgr.StartUpMessages)
                Core.Logger.LogInfo("     " + msg);

            foreach (var wrn in DataMgr.StartUpWarnings)
                Core.Logger.LogWarning("     " + wrn);

            foreach (var err in DataMgr.StartUpErrors)
                Core.Logger.LogError("     " + err);

            Core.Logger.LogInfo("================================");
        }
    }
    [HarmonyPatch(typeof(UltimateFootballZombie))]
    public static class UltimateFootballZombie_Patch
    {
        [HarmonyPatch(nameof(UltimateFootballZombie.AttackEffect))]
        [HarmonyPrefix]
        public static bool AttackEffect_Prefix(Plant plant)
        {
            if (plant.TryGetComponent<BaseCustomPlant>(out var p) && p.isPF)
            {
                plant.thePlantHealth = plant.thePlantMaxHealth;
                plant.UpdateText();
                return false;
            }
            return true;
        }
    }
    // =========================
    // WaveManager.GetMaxWave
    // =========================

    [HarmonyPatch(typeof(WaveManager))]
    public static class WaveManagerPatch
    {
        [HarmonyPatch(nameof(WaveManager.GetMaxWave))]
        [HarmonyPrefix]
        public static bool GetSceneType_Prefix(LevelType levelType, int level, ref int __result)
        {
            var entity = DataMgr.LoadedCustomLevels.FirstOrDefault(
                x => x.LevelType == levelType && x.LevelID == level);

            if (entity.IsNotNull())
            {
                __result = entity.MaxWave;
                return false;
            }
            return true;
        }
    }

    // =========================
    // InGameUI.SetUniqueText
    // =========================

    [HarmonyPatch(typeof(InGameUI))]
    public static class InGameUIPatch
    {
        [HarmonyPatch(nameof(InGameUI.SetUniqueText))]
        [HarmonyPostfix]
        public static void SetUniqueText_Postfix(InGameUI __instance)
        {
            var entity = DataMgr.LoadedCustomLevels.FirstOrDefault(
                x => x.LevelType == GameAPP.theBoardType &&
                        x.LevelID == GameAPP.theBoardLevel);

            if (entity.IsNotNull())
                __instance.SetLevelName(entity.LevelName);
        }
    }

    // =========================
    // InitZombieList.SetAllowZombieTypeSpawn
    // =========================

    [HarmonyPatch(typeof(InitZombieList))]
    public static class InitZombieListPatch
    {
        [HarmonyPatch(nameof(InitZombieList.SetAllowZombieTypeSpawn))]
        [HarmonyPrefix]
        public static bool SetAllowZombieTypeSpawn_Prefix(LevelType theLevelType, int theLevelIDber)
        {
            var entity = DataMgr.LoadedCustomLevels.FirstOrDefault(
                x => x.LevelType == theLevelType &&
                        x.LevelID == theLevelIDber);

            if (entity.IsNotNull())
            {
                foreach (var z in entity.ZombieTypes)
                    InitZombieList.allow[(int)z] = true;

                return false;
            }
            return true;
        }
    }

    // =========================
    // Lawnf.SetMusic
    // =========================

    [HarmonyPatch(typeof(Lawnf))]
    public static class Lawnf_Patch
    {
        [HarmonyPatch(nameof(Lawnf.SetMusic))]
        [HarmonyPostfix]
        public static void SetMusic_Postfix(ref Board board)
        {
            var entity = DataMgr.LoadedCustomLevels.FirstOrDefault(
                x => x.LevelType == GameAPP.theBoardType &&
                        x.LevelID == GameAPP.theBoardLevel);

            if (entity.IsNotNull() && entity.MusicType != (MusicType)(-1))
                GameAPP.Instance.PlayMusic(entity.MusicType);
        }
    }

    // =========================
    // UIMgr
    // =========================

    [HarmonyPatch(typeof(UIMgr))]
    public static class UIMgr_Patch
    {
        [HarmonyPatch(nameof(UIMgr.GetSceneType))]
        [HarmonyPrefix]
        public static bool GetSceneType_Prefix(UIMgr __instance,
                                            LevelType theLevelType,
                                            int theLevelIDber,
                                            ref SceneType __result)
        {
            var entity = DataMgr.LoadedCustomLevels.FirstOrDefault(
                x => x.LevelType == theLevelType &&
                        x.LevelID == theLevelIDber);

            if (entity.IsNotNull())
            {
                __result = entity.SceneType;
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(UIMgr.EnterGame))]
        [HarmonyPostfix]
        public static void EnterGame_Postfix(UIMgr __instance,
                                        LevelType levelType,
                                        int levelNumber)
        {
            var entity = DataMgr.LoadedCustomLevels.FirstOrDefault(
                x => x.LevelType == levelType &&
                    x.LevelID   == levelNumber);

            if (!entity.IsNotNull())
            {
                GlobalTracker.IsCustomLevel=false;
                GlobalTracker.CustomLevelID=-1;
                return;
            }

            // Custom enter action
            entity.EnterAction?.Invoke();

            var map = entity.MapRoadTypes;
            if (map == null)
                return;

            int rows = map.GetLength(0);
            int cols = map.GetLength(1);

            // Update board row count
            InstanceManager.Board.rowNum = rows;

            var gs = InstanceManager.Board.gridSystem;

            // Update lane types (roadType)
            for (int row = 0; row < rows; row++)
            {
                var grid = gs.GetGrid(0, row); // first column defines lane
                grid.boxType = (BoxType)map[row, 0];
            }

            // Update every tile
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var grid = gs.GetGrid(col, row);
                    grid.boxType = (BoxType)map[row, col];
                }
            }
            if(DataMgr.CustomLevelComponents.TryGetValue(entity.LevelID,out var type))
            {
                var typeil2cpp=type.ToIl2CppType();
                InstanceManager.Board.AddComponent(typeil2cpp);
            }
            GlobalTracker.IsCustomLevel=true;
            GlobalTracker.CustomLevelID=entity.LevelID;
        }
        [HarmonyPatch(nameof(UIMgr.EnterChallengeMenu))]
        [HarmonyPostfix]
        public static void PostEnterChallengeMenu()
        {
            GameAPP.Instance.StartCoroutine(init());
            IEnumerator init()
            {
                yield return null;
                yield return null;
                var levels = GameAPP.canvas.GetChild(0).FindChild("Levels");
                var firstBtns = levels.FindChild("FirstBtns");
                if (firstBtns.FindChild("LoadedCustomLevels") == null || firstBtns.FindChild("LoadedCustomLevels").IsDestroyed())
                {
                    GameObject custom = Object.Instantiate(firstBtns.GetChild(0).gameObject, firstBtns);
                    custom.name = "LoadedCustomLevels";
                    custom.transform.localPosition = MathHelper.GetLevelButtonPosition((firstBtns.childCount - 1) % 6, (firstBtns.childCount - 1) / 6);
                    var window = custom.transform.FindChild("Window");
                    window.FindChild("Name").GetComponent<TextMeshProUGUI>().text = "更多二创关卡";
                    var adv = levels.FindChild("PageAdvantureLevel");
                    var customLevels = Object.Instantiate(adv.gameObject, levels);
                    customLevels.active = false;
                    customLevels.name = "PageCustomLevel";
                    var pages = customLevels.transform.FindChild("Pages");
                    var levelSample = Object.Instantiate(pages.FindChild("Page1").FindChild("Lv1").gameObject);
                    foreach (var l in pages.FindChild("Page1").GetComponentsInChildren<Transform>(true))
                    {
                        Object.Destroy(l.gameObject);
                    }
                    var pageSample = Object.Instantiate(pages.FindChild("Page1").gameObject);
                    Object.Destroy(pages.FindChild("Page1").gameObject);
                    Object.Destroy(pages.FindChild("Page2").gameObject);
                    Object.Destroy(pages.FindChild("Page3").gameObject);
                    int levelIndex = 0;
                    int columnIndex;
                    int rowIndex;
                    int pageIndex;
                    foreach (var level in DataMgr.LoadedCustomLevels)
                    {
                        // Create a new page every 18 levels
                        if (levelIndex % 18 == 0)
                        {
                            Object.Instantiate(pageSample, pages)
                                .name = $"Pages{(levelIndex / 18) + 1}";
                        }

                        // Grid math
                        columnIndex = levelIndex % 6;
                        rowIndex = (levelIndex / 6) % 3;   // row inside page (0–2)
                        pageIndex = levelIndex / 18;

                        // Get the correct page
                        var pageObj = pages.FindChild($"Pages{pageIndex + 1}");

                        // Create the level button
                        var item = Object.Instantiate(levelSample, pageObj);
                        item.name = $"Lv{level.LevelID}";

                        // Position using your MathHelper (clean, consistent)
                        item.transform.localPosition =
                            MathHelper.GetLevelButtonPosition(columnIndex, rowIndex);

                        // ============================
                        // YOUR BaseMenu logic
                        // ============================

                        // Level background sprite
                        if (level.LevelSprite != null)
                            item.GetComponent<Image>().sprite = level.LevelSprite;

                        // Window → Name
                        var win = item.transform.Find("Window");
                        win.Find("Name").GetComponent<TextMeshProUGUI>().text = level.LevelName;

                        var victory = item.transform.Find("Window/Trophy").gameObject;
                        victory.SetActive(LevelProgressionManager.IsCompleted(level.LevelID));

                        // Button logic
                        var btn = win.GetComponent<Advanture_Btn>();
                        btn.levelType = level.LevelType;
                        btn.buttonNumber = level.LevelID;

                        // Next level
                        levelIndex++;
                    }
                    window.GetComponent<FirstBtns>().pageToOpen = customLevels;
                    window.GetComponent<FirstBtns>().originPosition = custom.transform.localPosition;
                    Object.Destroy(pageSample);
                    Object.Destroy(levelSample);
                }
            }
        }
    }
    [HarmonyPatch(typeof(CustomMenu))]
    public static class CustomMenuPatch
    {
        [HarmonyPatch(nameof(CustomMenu.Awake))]
        [HarmonyPostfix]
        public static void PostAwake()
        {
            if (GameAPP.canvas.IsObjExist() && GameAPP.canvas.GetChild(0).FindChild("Levels").IsObjExist())
            {
                var child = GameAPP.canvas.GetChild(0).FindChild("Levels").FindChild("FirstBtns").FindChild("LoadedCustomLevels");
                if (child.IsObjExist())
                    child.GetChild(1).GetComponent<BoxCollider2D>().enabled = false;
            }
        }

        [HarmonyPatch(nameof(CustomMenu.OnDestroy))]
        [HarmonyPostfix]
        public static void PostOnDestroy()
        {
            if (GameAPP.canvas.IsObjExist() && GameAPP.canvas.GetChild(0).FindChild("Levels").IsObjExist())
            {
                var child = GameAPP.canvas.GetChild(0).FindChild("Levels").FindChild("FirstBtns").FindChild("LoadedCustomLevels");
                if (child.IsObjExist())
                    child.GetChild(1).GetComponent<BoxCollider2D>().enabled = true;
            }
        }
    }
    [HarmonyPatch(typeof(PrizeMgr))]
    internal static class Patch_PrizeMgr_EnterNextMenu
    {
        [HarmonyPatch(nameof(PrizeMgr.EnterNextMenu))]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!GlobalTracker.IsCustomLevel)
                return;

            // Mark this custom level as completed in JSON progression
            LevelProgressionManager.MarkCompleted(GlobalTracker.CustomLevelID);

            // Optional: clear state so it doesn't leak into non‑custom levels
            GlobalTracker.IsCustomLevel = false;
            GlobalTracker.CustomLevelID = -1;
        }
    }
}