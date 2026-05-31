namespace CustomPlantClass
{
    public static class CreateMgr
    {
        public static Plant SetPlant(int row, int column, PlantType thePlantType, Plant targetPlant = null, Vector2 puffV = default, bool isFreeSet = false, bool withEffect = true, Plant hidplant = null)
        {
            return InstanceManager.CreatePlant.SetPlant(column,row,thePlantType,targetPlant,puffV,isFreeSet,withEffect,hidplant);
        }
        public static Zombie SetZombie(int row, ZombieType theZombieType, bool isMindControlled, float x=9.9f, bool isIdleOrWithEffect = false)
        {
            if(isMindControlled) return InstanceManager.CreateZombie.SetZombieWithMindControl(row, theZombieType,x,isIdleOrWithEffect);
            return InstanceManager.CreateZombie.SetZombie(row, theZombieType,x,isIdleOrWithEffect);
        }
        public static BombCherry CreateCherryExplode(Vector2 v, int theRow, CherryBombType bombType = CherryBombType.Normal, int damage = 1800, PlantType fromType = PlantType.Nothing, Action<Zombie> action=null, bool immediately = true)
        {
            return InstanceManager.Board.boardAction.CreateCherryExplode(v, theRow, bombType, damage, fromType, action, immediately);
        }
        public static void SetCrater(int row, int column)
        {
            InstanceManager.Board.boardAction.SetPit(column,row);
        }
        public static Doom SetDoom(Board board, Vector2 position, DoomType doomType, Il2CppSystem.Action action = null, bool sprit = true)
        {
            return Doom.SetDoom(board,position,doomType,action,sprit);
        }
        public static void SetDoom(int theColumn, int theRow, bool setPit, bool iceDoom = false, Vector2 position=default, int damage = 1800, int effect = 0, Action<Zombie> action=null, bool existParticle = true, PlantType fromType = PlantType.Nothing)
        {
            InstanceManager.Board.boardAction.SetDoom(theColumn,theRow,setPit,iceDoom,position,damage,effect,action,existParticle,fromType);
        }
        public static void CreateFireLine(int theFireRow, int damage = 1800, bool fromZombie = false, bool fix = false, bool shake = true, Action<Zombie> action=null, PlantType fromType = PlantType.Nothing)
        {
            InstanceManager.Board.boardAction.CreateFireLine(theFireRow,damage,fromZombie,fix,shake,action,fromType);
        }
    }
}
