namespace CustomPlantClass
{
    public class CustomBigStar : MonoBehaviour
    {
        public float g = -9.8f;
        public float minY;
        public float speedX = 7f;
        public float speedY = 0f;
        public bool zombie = false;
        public PlantType fromType = 0;
        public int damage = 0;

        private Board board;
        public virtual Vector3 SetSize() => new Vector3(20f, 20f, 20f);

        public void Start()
        {
            board = Board.Instance;
            if (board == null)
            {
                Destroy(gameObject);
                return;
            }

            // Zombie variant (IL accurate)
            if (zombie)
            {
                speedX = -speedX;
                transform.position = new Vector3(15f, 9f, 0f);
            }

            // Play spawn sound (0x5F)
            GameAPP.PlaySound(95, 1f, 1f);
            transform.localScale = SetSize();

            // IL-accurate minY calculation
            if (board.rowNum % 2 == 0)
            {
                float y1 = Mouse.Instance.GetBoxYFromRow(board.rowNum / 2 - 1);
                float y2 = Mouse.Instance.GetBoxYFromRow(board.rowNum / 2);
                minY = (y1 + y2 + 1f) * 0.5f;
            }
            else
            {
                float yMid = Mouse.Instance.GetBoxYFromRow(board.rowNum / 2);
                minY = yMid + 0.5f;
            }
        }

        public void Update()
        {
            if (board == null) Destroy(gameObject);
            // Gravity
            speedY += g * Time.deltaTime;

            // Movement
            transform.position += new Vector3(speedX, speedY, 0f) * Time.deltaTime;

            // IL-accurate rotation (360°/sec)
            transform.Rotate(0f, 0f, 360f * Time.deltaTime);

            // Landing detection (IL accurate)
            if (transform.position.y <= minY)
            {
                Crash();
            }
        }

        public static GameObject SetStar(GameObject StarPrefab)
        {
            if (Board.Instance == null) return null;

            var go = Instantiate(StarPrefab, Board.Instance.transform);

            // IL-accurate spawn: above board, same X as Big Star
            go.transform.position = new Vector3(-9f, 13f, 0f);
            return go;
        }

        public void Crash()
        {
            // Destroy meteor object (IL accurate)
            Destroy(gameObject);
            CrashEvent();
        }

        public virtual void CrashEvent()
        {
            GlobalEvent();
            var zombies = Board.Instance.zombieArray;
            if (zombies != null)
            {
                for (int i = zombies.Count - 1; i >= 0; i--)
                {
                    var z = zombies[i];
                    if (z == null || z.theStatus == ZombieStatus.Dying) continue;
                    if (z.isMindControlled) continue;
                    PerZombieEvent(z, DmgType.Normal, damage, fromType);
                }
            }
            ScreenShake.TriggerShake(0.15f);
            CreateCustomStars();
        }
        public virtual void GlobalEvent() { }
        public virtual void PerZombieEvent(Zombie zombie, DmgType theDamageType, int theDamage, PlantType reportType = PlantType.Nothing, bool fix = false)
        {
            zombie.TakeDamage(theDamageType, theDamage, reportType, fix);
        }

        public virtual void CreateCustomStars(BulletType theBulletType = BulletType.Bullet_star)
        {
            Vector3 center = transform.position;

            for (int ring = 0; ring < 5; ring++)
            {
                float radius = (ring + 1) * 0.5f;   // 0.5, 1.0, 1.5, 2.0, 2.5

                for (int angle = 0; angle < 360; angle += 10)
                {
                    float rad = angle * Mathf.Deg2Rad;

                    float x = center.x + Mathf.Cos(rad) * radius;
                    float y = center.y + Mathf.Sin(rad) * radius;

                    Bullet bullet = CreateBullet.Instance.SetBullet(
                        x,
                        y,
                        2,                  // row (IL hardcoded)
                        theBulletType,
                        BulletMoveWay.Free, // IL uses 2
                        false
                    );

                    if (bullet == null)
                        continue;

                    // Rotate bullet to face outward
                    bullet.transform.Rotate(0f, 0f, angle);

                    // IL sets fromType
                    bullet.fromType = fromType;
                }
            }
        }
    }
}
