namespace CustomPlantClass
{
    public class CustomSolarBomb : MonoBehaviour
    {
        public Board board;
        public int count;
        public int row { get; set; }
        public int column { get; set; }
        public int dmg { get; set; }

        public Vector2 target;
        public Vector2 start;
        public float maxTime;
        public float timer;
        public bool crash;

        public void Start()
        {
            board = Board.Instance;
            if (board == null)
            {
                Destroy(gameObject);
                return;
            }

            // Convert row/column → world coordinates
            float x = Lawnf.GetBoxXFromColumn(column);
            float y = Mouse.Instance.GetBoxYFromRow(row);

            target = new Vector2(x, y);

            // Use Solar Eclipse Bomb’s arc logic
            SetStartPosition();

            // Random flight time like Solar Eclipse Bomb
            maxTime = Random.Range(1.8f, 2.1f);
        }

        public void Update()
        {
            timer += Time.deltaTime;

            // Solar Eclipse Bomb movement
            transform.position = Vector3.Lerp(start, target, timer / maxTime);
            transform.Rotate(0, 0, -180 * Time.deltaTime);

            if (!crash && Vector3.Distance(transform.position, target) < 0.1f)
            {
                crash = true;
                Explode();
            }
        }

        private void SetStartPosition()
        {
            // EXACT Solar Eclipse Bomb arc logic
            float angleRad = Random.Range(63f, 75f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(-Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            float minTime = 1.8f;
            float maxTime = 2.1f;
            float maxSpeedX = 4.7f;
            float maxSpeedY = 7.5f;

            float maxLength = Mathf.Min(maxSpeedX * maxTime / Mathf.Abs(direction.x),
                                        maxSpeedY * maxTime / Mathf.Abs(direction.y));
            float minLength = Mathf.Min(maxSpeedX * minTime / Mathf.Abs(direction.x),
                                        maxSpeedY * minTime / Mathf.Abs(direction.y));

            float displacementLength = (minLength + maxLength) * 0.5f;

            start = target + direction * displacementLength;
            transform.position = start;
        }

        private void Explode()
        {
            var center = (Vector2)transform.position;
            float radius = 2.5f;

            var hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Zombie"));
            foreach (var col in hits)
            {
                if (col != null && col.TryGetComponent<Zombie>(out var z) && z != null && z.Alive)
                {
                    z.TakeDamage(DmgType.RealDamage, dmg);
                }
            }

            GameAPP.PlaySound(41, 0.5f, 1f);
            ParticleManager.Instance.SetParticle(ParticleType.StarSplat, transform.position, 11);

            Destroy(gameObject);
        }

        public static void SpawnAt(int row, int column, int dmg, GameObject gameObject)
        {
            var bomb = Instantiate(gameObject, Board.Instance.transform)
                .GetComponent<CustomSolarBomb>();

            bomb.row = row;
            bomb.column = column;
            bomb.dmg = dmg;
        }
    }
}
