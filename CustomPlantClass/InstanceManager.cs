namespace CustomPlantClass
{
    public static class InstanceManager
    {
        public static CreatePlant CreatePlant => CreatePlant.Instance;
        public static CreateZombie CreateZombie => CreateZombie.Instance;
        public static CreateBullet CreateBullet => CreateBullet.Instance;
        public static CreateItem CreateItem => CreateItem.Instance;
        public static ParticleManager ParticleManager => ParticleManager.Instance;
        public static Board Board => Board.Instance;
        public static TravelMgr TravelMgr => TravelMgr.Instance;
        public static Mouse Mouse => Mouse.Instance;
        public static TravelStore TravelStore => TravelStore.Instance;
        public static TreasureManager TreasureManager => TreasureManager.Instance;
        public static GameAPP GameAPP => GameAPP.Instance;
    }
    public static class InstanceManager_Safe
    {
        private static T Require<T>(T instance, string name) where T : class
        {
            if (instance == null)
                throw new InvalidOperationException($"{name} singleton is null (scene not loaded?)");
            return instance;
        }

        // Gameplay
        public static CreatePlant CreatePlant => Require(CreatePlant.Instance, nameof(CreatePlant));
        public static CreateZombie CreateZombie => Require(CreateZombie.Instance, nameof(CreateZombie));
        public static CreateBullet CreateBullet => Require(CreateBullet.Instance, nameof(CreateBullet));
        public static CreateItem CreateItem => Require(CreateItem.Instance, nameof(CreateItem));
        public static ParticleManager ParticleManager => Require(ParticleManager.Instance, nameof(ParticleManager));
        public static Board Board => Require(Board.Instance, nameof(Board));
        public static Mouse Mouse => Require(Mouse.Instance, nameof(Mouse));

        // Meta / Menu
        public static TravelMgr TravelMgr => Require(TravelMgr.Instance, nameof(TravelMgr));
        public static TravelStore TravelStore => Require(TravelStore.Instance, nameof(TravelStore));
        public static TreasureManager TreasureManager => Require(TreasureManager.Instance, nameof(TreasureManager));
        public static GameAPP GameAPP => Require(GameAPP.Instance, nameof(GameAPP));
    }
}