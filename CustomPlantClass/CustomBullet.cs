namespace CustomPlantClass
{
    public class BaseCustomBullet : MonoBehaviour
    {
        public Bullet _bullet => GetComponent<Bullet>();

        //Unity Methods
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }

        //Base Overrides
        public virtual bool HitPlant(Plant plant)
        {
            return true;
        }
        public virtual bool HitZombie(Zombie zombie)
        {
            return true;
        }
        public virtual bool HitLand()
        {
            return true;
        }
        public virtual bool JumpLand()
        {
            return true;
        }
        public virtual bool HitZombieCondition(Zombie zombie)
        {
            return true;
        }

        //Helpers
        public virtual ParticleType GetParticleType() => ParticleType.PeaSplat;
        public virtual Particle SetParticle(Vector2 pos, int layerrow) => ParticleManager.Instance.SetParticle(GetParticleType(), pos, layerrow);
    }
}