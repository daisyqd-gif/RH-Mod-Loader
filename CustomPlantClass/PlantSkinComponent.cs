namespace CustomPlantClass
{
    public class PlantSkinComponent : MonoBehaviour
    {
        public Plant plant => GetComponent<Plant>();
    }
    public class BulletComponent : MonoBehaviour
    {
        public Bullet bullet => GetComponent<Bullet>();
    }
    public class ZombieComponent : MonoBehaviour
    {
        public Zombie zombie => GetComponent<Zombie>();
    }
}