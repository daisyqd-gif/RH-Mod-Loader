namespace CustomPlantClass
{
    public class PlantMgr : MonoBehaviour
    {
        //Position helpers
        public static int GetRandomBoardRow()
        {
            Board board = Board.Instance;
            if (board == null)
            {
                return 0;
            }
            return Random.Range(0, board.rowNum);
        }
        public static int GetRandomBoardColumn()
        {
            Board board = Board.Instance;
            if (board == null)
            {
                return 0;
            }
            return Random.Range(0, board.columnNum);
        }
        public static float getX(int column)
        {
            return Mouse.Instance.GetBoxXFromColumn(column);
        }
        public static float getY(int row)
        {
            return Mouse.Instance.GetBoxYFromRow(row);
        }
        public static int getCol(float x)
        {
            return Mouse.Instance.GetColumnFromX(x);
        }
        public static int getRow(float y)
        {
            return Lawnf.GetRowFromY(y);
        }
        public static Vector2 GetPos(int row, int column)
        {
            Vector2 pos = new Vector2(getX(column), getY(row));
            return pos;
        }

        //Meteor creator
        public static GameObject MakeMeteor(GameObject customMeteorPrefab) => CustomBigStar.SetStar(customMeteorPrefab);

        //Central zombie spawn wrapper
        public static Zombie SetZombie(ZombieType type, int row, int column, bool isHypno = true)
        {
            CreateZombie createZombie = CreateZombie.Instance;
            if (createZombie == null)
                return null;

            if (isHypno)
            {
                return createZombie.SetZombieWithMindControl(row, type, getX(column), false);
            }
            else
            {
                return createZombie.SetZombie(row, type, getX(column), false);
            }
        }

        //Percent getter
        public static bool GetPercent(float percent) => Random.Range(0f, 100f) < percent;

        //Plant text getter
        public static string GetTextString(Dictionary<string, string> dic)
        {
            string s = "";
            foreach (var i in dic.Keys)
            {
                s += $"{i} : {dic[i]}";
            }
            return s;
        }

        //Type getters
        public static List<PlantType> GetAllPlantTypes(Func<PlantType, bool> selector = null)
        {
            if (selector == null) selector = (PlantType pt) => pt != PlantType.VectorPlant;
            return (List<PlantType>)GameAPP.resourcesManager.allPlants.ToArray().ToList().Where(selector);
        }
        public static List<PlantType> GetAllPlantTypes(CardLevel cardLevel)
        {
            var selector = (PlantType pt) => TreasureData.GetCardLevel(pt) == cardLevel && pt != PlantType.VectorPlant;
            return (List<PlantType>)GameAPP.resourcesManager.allPlants.ToArray().ToList().Where(selector);
        }
        public static List<PlantType> GetAllNormalPlantTypes()
        {
            var selector = (PlantType pt) => !Lawnf.IsUltiPlant(pt) && !Lawnf.IsSuperPlant(pt) && pt != PlantType.VectorPlant;
            return (List<PlantType>)GameAPP.resourcesManager.allPlants.ToArray().ToList().Where(selector);
        }
        public static List<PlantType> GetAllUltimatePlantTypes()
        {
            var selector = (PlantType pt) => Lawnf.IsUltiPlant(pt) && pt != PlantType.VectorPlant;
            return (List<PlantType>)GameAPP.resourcesManager.allPlants.ToArray().ToList().Where(selector);
        }
        public static List<ZombieType> GetAllZombieTypes(Func<ZombieType, bool> selector = null)
        {
            if (selector == null) selector = (ZombieType zt) => zt != ZombieType.TrainingDummy;
            return (List<ZombieType>)GameAPP.resourcesManager.allZombieTypes.ToArray().ToList().Where(selector);
        }

        //Get plants in area
        public static bool IsTypeIn3x3(int theColumn, int theRow, Func<Plant, bool> selector)
        {
            return Lawnf.Get3x3Plants(theColumn, theRow).ToSystemList().Any(selector);
        }
        public static bool IsTypeIn3x3(int theColumn, int theRow, PlantType thePlantType)
        {
            return IsTypeIn3x3(theColumn, theRow, (Plant p) => p.thePlantType == thePlantType);
        }
        public static Plant GetPlantIn3x3(int theColumn, int theRow, Func<Plant, bool> selector)
        {
            return Lawnf.Get3x3Plants(theColumn, theRow).ToSystemList().FirstOrDefault(selector);
        }
        public static Plant GetPlantIn3x3(int theColumn, int theRow, PlantType thePlantType)
        {
            return GetPlantIn3x3(theColumn, theRow, (Plant p) => p.thePlantType == thePlantType);
        }
        public static bool IsTypeIn1x1(int theColumn, int theRow, Func<Plant, bool> selector)
        {
            return Lawnf.Get1x1Plants(theColumn, theRow).ToSystemList().Any(selector);
        }
        public static bool IsTypeIn1x1(int theColumn, int theRow, PlantType thePlantType)
        {
            return IsTypeIn1x1(theColumn, theRow, (Plant p) => p.thePlantType == thePlantType);
        }
        public static Plant GetPlantIn1x1(int theColumn, int theRow, Func<Plant, bool> selector)
        {
            return Lawnf.Get1x1Plants(theColumn, theRow).ToSystemList().FirstOrDefault(selector);
        }
        public static Plant GetPlantIn1x1(int theColumn, int theRow, PlantType thePlantType)
        {
            return GetPlantIn1x1(theColumn, theRow, (Plant p) => p.thePlantType == thePlantType);
        }
        public static PlantType GetRandomPlantType(Func<PlantType, bool> selector = null)
        {
            return GetAllPlantTypes(selector).GetRandomItem();
        }

        //Null checkers
        public static bool IsNotNull<T>(T input, out T output)
        {
            output = input;
            if (input == null) return false;
            else return true;
        }
        public static bool IsNotNullMonoBehaviour<T>(T input, out T output) where T : MonoBehaviour
        {
            output = input;
            if (input == null || !input || input.IsDestroyed()) return false;
            else return true;
        }

        //SetBullet wrappers
        public static Bullet SetBullet(Plant fromPlant, BulletType theBulletType, BulletMoveWay theMovingWay, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, fromPlant.shoot.position, fromPlant.thePlantRow, theBulletType, theMovingWay, fromPlant.attackDamage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, BulletType theBulletType, BulletMoveWay theMovingWay, int damage, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, fromPlant.shoot.position, fromPlant.thePlantRow, theBulletType, theMovingWay, damage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, int theRow, BulletType theBulletType, BulletMoveWay theMovingWay, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, fromPlant.shoot.position, theRow, theBulletType, theMovingWay, fromPlant.attackDamage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, int theRow, BulletType theBulletType, BulletMoveWay theMovingWay, int damage, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, fromPlant.shoot.position, theRow, theBulletType, theMovingWay, damage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, Vector2 pos, BulletType theBulletType, BulletMoveWay theMovingWay, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, pos, fromPlant.thePlantRow, theBulletType, theMovingWay, fromPlant.attackDamage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, Vector2 pos, BulletType theBulletType, BulletMoveWay theMovingWay, int damage, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, pos, fromPlant.thePlantRow, theBulletType, theMovingWay, damage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, Vector2 pos, int theRow, BulletType theBulletType, BulletMoveWay theMovingWay, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            return SetBullet(fromPlant, pos, theRow, theBulletType, theMovingWay, fromPlant.attackDamage, offset, rotation, fromEnemy);
        }
        public static Bullet SetBullet(Plant fromPlant, Vector2 pos, int theRow, BulletType theBulletType, BulletMoveWay theMovingWay, int damage, Vector2 offset = new Vector2(), float rotation = 0f, bool fromEnemy = false)
        {
            Bullet b = InstanceManager.CreateBullet.SetBullet(pos.x + offset.x, pos.y + offset.y, theRow, theBulletType, theMovingWay, fromEnemy);
            if (b == null) return null;
            b.Damage = damage;
            b.fromType = fromPlant.thePlantType;
            b.transform.Rotate(0, 0, rotation);
            return b;
        }

        //Buff helpers
        public static bool IsTravelStore()
        {
            return InstanceManager.TravelStore != null;
        }
        public static bool GetBuffByString(string str)
        {
            bool result = CoreTools.TravelAdvanced(str);
            if (result) return true;
            result = CoreTools.TravelUltimate(str);
            if (result) return true;
            result = Lawnf.TravelUnlock(CoreTools.GetTravelUnlocksByString(str));
            if (result) return true;
            return Lawnf.TravelDebuff(CoreTools.GetTravelDebuffByString(str));
        }
        public static bool IsInGame() => InstanceManager.Board != null && GameAPP.theGameStatus is GameStatus.InGame;
    }
}