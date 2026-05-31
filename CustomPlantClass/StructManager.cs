#nullable enable
namespace CustomPlantClass
{
    /// <summary>
    /// Core metadata for defining a custom plant.
    /// </summary>
    public struct BaseCustomPlantData
    {
        public ID PlantId;
        public GameObject Prefab;
        public GameObject Preview;
        public List<(ID, ID)> Fusions;
        public float AttackInterval;
        public float ProduceInterval;
        public int AttackDamage;
        public int MaxHealth;
        public float Cd;
        public int Sun;
        public BulletType DefaultBullet;
        public bool CanPF;
        public bool CanStarUp;
        public CardLevel CardColor;
        public bool IsRainbowCard;
        public bool IsUltimatePlant;
        public int CardRepeatAmt;
        public string Name;
        public string AlmanacEntry;
    }

    /// <summary>
    /// Metadata for defining a custom plant skin.
    /// </summary>
    public struct BasePlantSkinData
    {
        public BaseCustomPlantData data;
        public GameObject SkinPrefab;
        public GameObject SkinPreview;
        public List<(BulletType, List<GameObject?>)> BulletSkinList;
    }

    /// <summary>
    /// Metadata for defining a bullet.
    /// </summary>
    public struct BaseCustomBulletData
    {
        public ID BulletId;
        public GameObject Prefab;

    }
    public struct BaseCustomZombieData
    {
        public ID theZombieType;
        public GameObject Prefab;
        public Sprite Preview;
        public int theAtackDamage;
        public int maxHealth;
        public int theFirstArmorHealth;
        public FirstArmorType theFirstArmorType;
        public string theFirstArmorPath;
        public int theSecondArmorHealth;
        public SecondArmorType theSecondArmorType;
        public string theSecondArmorPath;
    }
    public struct BaseCustomGridItemData
    {
        public ID type;
        public GameObject Prefab;
    }
    public struct CustomBossHealthSliderData
    {
        public ZombieType theZombieType;
        public Sprite Icon;
        public Color FillColor;
    }
    public struct BoardPosition
    {
        public int Row { get; }
        public int Column { get; }

        public BoardPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        // BoardPosition → world position
        public static implicit operator Vector2(BoardPosition pos)
        {
            float x = pos.Column * 1.35f - 4.8f;

            Board board = Board.Instance;
            bool roof = board.boardTag.isRoof;
            int rows = board.rowNum;

            float y;

            if (!roof)
            {
                if (rows == 6)
                    y = 2.3f - pos.Row * 1.45f;
                else
                    y = 2.3f - pos.Row * 1.67f;
            }
            else
            {
                // Roof math
                if (x <= 1.5f)
                {
                    float f = (pos.Row * 1.4f);
                    y = 1.6f - f + x * 0.22f + 0.5f;
                }
                else
                {
                    y = 4.0f - pos.Row * 1.45f;
                }
            }

            return new Vector2(x, y);
        }

        // world position → BoardPosition
        public static implicit operator BoardPosition(Vector2 world)
        {
            float x = world.x;
            float y = world.y;

            Board board = Board.Instance;
            bool roof = board.boardTag.isRoof;
            int rows = board.rowNum;

            // Column
            int col = Mathf.FloorToInt((x + 5.6f) / 1.35f);
            col = Mathf.Clamp(col, 0, board.columnNum - 1);

            // Row
            int row;

            if (!roof)
            {
                if (rows == 6)
                    row = Mathf.FloorToInt((3.7f - y) / 1.45f);
                else
                    row = Mathf.FloorToInt((3.7f - y) / 1.67f);
            }
            else
            {
                if (x <= 1.5f)
                {
                    float f = (y - x * 0.22f) - 0.5f;
                    row = Mathf.FloorToInt((1.6f - f) / 1.4f) + 1;
                }
                else
                {
                    row = Mathf.FloorToInt((4.0f - y) / 1.45f);
                }
            }

            row = Mathf.Clamp(row, 0, rows - 1);

            return new BoardPosition(row, col);
        }

        public override string ToString() => $"({Row}, {Column})";
    }
	public struct BaseCustomLevelData
	{
		public LevelType LevelType { readonly get; set; }
		public int LevelID { readonly get; set; }
		public string LevelName { readonly get; set; }
		public string LevelNameEn { readonly get; set; }
		public Sprite LevelSprite { readonly get; set; }
		public SceneType SceneType { readonly get; set; }
		public GameObject ScenePrefab { readonly get; set; }
		public Sprite SceneBackground { readonly get; set; }
		public MusicType MusicType { readonly get; set; }
		public AudioClip MusicAudio { readonly get; set; }
		public int MaxWave { readonly get; set; }
		public List<ZombieType> ZombieTypes { readonly get; set; }
		public BoxType_Short[,] MapRoadTypes { readonly get; set; }
        public CustomLevelSelection selection { readonly get; set; }
        public PlantType[] SelectTypes { readonly get; set; }
		public Action EnterAction { readonly get; set; }
        public int SunCounter { readonly get; set; }
		public List<AdvBuff> AdvBuffs { readonly get; set; }
		public List<UltiBuff> UltiBuffs { readonly get; set; }
		public List<TravelUnlocks> TravelUnlocks { readonly get; set; }
		public List<TravelDebuff> TravelDebuffs { readonly get; set; }
        public Board.BoardTag BoardTag { readonly get; set; }
	}
    public enum CustomLevelSelection
    {
        Normal = 0,
        Convey = 1,
        PreSelected = 2
    }
    public enum BossSliderType
    {
        UltimateSword = 0,
        ObsidianGarcantuar = 1,
        UltimateDrown = 2,
        UltimateFootball = 3,
        UltimateHorse = 4,
        UltimateImp = 5,
        UltimateJackbox = 6,
        UltimateJackson = 7,
        UltimateKirov = 8,
        UltimateLegion = 9,
        UltimateMachineNut = 10,
        UltimatePaper = 11,
        UltimateSnow = 12
    }
    public enum PlantLevelData
    {
        Basic = 0,
        Secondary = 1,
        Super = 2,
        WeakUltimate = 3,
        StrongUltimate = 4,
        FinalUltimate = 5,
        TreasurePlant = 6
    }
    public enum BoxType_Short
    {
        G = 0,    // 草地
        W = 1,    // 水域
        D = 2,     // 泥土
        R = 3,     // 屋顶
        S = 4,    // 石头
        River = 5,    // 河流
        Dirt_water = 6 // 泥水域
    }
}