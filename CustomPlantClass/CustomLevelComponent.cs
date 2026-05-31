namespace CustomPlantClass
{
    public class CustomLevelComponent : MonoBehaviour
    {
        public Board board => GetComponent<Board>();
        protected virtual Sprite GetBoardBg() => null;
        public void Awake()
        {
            if (PlantMgr.IsNotNull(GetBoardBg(), out var a))
            {
                board.background.transform.GetChild(0).FindChild("bg").GetComponent<SpriteRenderer>().sprite = a;
            }
        }
        public void Start() { }
        public virtual void OnLevelStart() { }
        public virtual void OnPlantCreate(Plant plant) { }
        public virtual void OnPlantDie(Plant plant, DieReason dieReason) { }
        public virtual void OnZombieCreate(Zombie zombie) { }
        public virtual void OnZombieDie(Zombie zombie, int reason) { }
    }
}
