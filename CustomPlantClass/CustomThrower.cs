namespace CustomPlantClass
{
    public class CustomThrower : CustomShooter
    {
        protected Vector2 firstPostion;
        protected float firstTime;
        protected virtual float flightTime { get; set; } = 1.5f;
        protected virtual bool CheckRange(Zombie zombie) => _plant.axis.position.x < zombie.axis.position.x;
        protected virtual Zombie ThrowerSearchZombie()
        {
            var board = _plant.board;
            if (board == null || board.zombieArray == null)
                return null;

            Zombie best = null;
            float bestDist = float.MaxValue;

            foreach (var z in board.zombieArray)
            {
                if (z == null) continue;
                if (z.theZombieRow != _plant.thePlantRow) continue;
                if (z.axis == null) continue;

                var zPos = z.axis.position;
                float vision = _plant.vision;

                // must be within vision range
                if (!(zPos.x < vision)) continue;

                // your virtual filter
                if (!CheckRange(z)) continue;

                // extra static filter (umbrella / special cases)
                if (!Thrower.ThrowSearchZombie(z)) continue;

                // pick closest in front
                var plantPos = _plant.axis.position;
                float dist = Mathf.Abs(zPos.x - plantPos.x);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = z;
                }
            }

            _plant.targetZombie = best;

            // fallback: boss
            if (best == null)
            {
                var bossObj = _plant.SearchBoss(); // virtual on plant
                if (bossObj != null)
                {
                    best = bossObj.GetComponent<Zombie>();
                    _plant.targetZombie = best;
                }
            }

            return best;
        }
        public override void PlantShootUpdate()
        {
            _plant.UpdateAttackCountDown();
            if (_plant.thePlantAttackCountDown > 0) return;
            _plant.thePlantAttackCountDown = _plant.thePlantAttackInterval * Random.Range(0.95f, 1.05f);
            Zombie zombie = ThrowerSearchZombie();
            if (zombie == null || zombie.col == null)
                return;
            Vector2 targetPos = GetZombiePosition(zombie);
            firstPostion = targetPos;
            _plant.anim.SetTriggerString("throw");
            firstTime = Time.time;
        }
        protected virtual void ShootZombie(Zombie zombie)
        {
            // must have a shoot point
            if (_plant.shoot == null || zombie == null)
                return;

            // where the projectile starts
            Vector2 projectilePosition = _plant.shoot.position;

            // target motion + position
            Vector2 targetVelocity = zombie.Velocity;
            Vector2 targetCurrentPosition = zombie.ColliderPosition;

            // Lawnf helper: compute arc parameters for a given flight time
            // returns something like: [t, vx, vy, gravityOrDeltaVy]
            float[] arc = Lawnf.CalculateProjectileWithSpeed(
                projectilePosition,
                targetVelocity,
                targetCurrentPosition,
                flightTime
            );

            if (arc == null || arc.Length < 4)
                return;

            // spawn bullet at shoot point
            Bullet b = PlantMgr.SetBullet(
                _plant,
                GetBulletType(),
                BulletMoveWay.Throw,
                _plant.attackDamage * 2
            );

            if (b == null)
                return;

            // apply arc parameters
            b.Vx = arc[1];
            b.Vy = arc[2];
            b.detaVy = -arc[3];

            if (_plant.melonSputter)
                b.melonSputter = true;

            // hook for subclasses (Winter, Lava, Gold, etc.)
            UniqueEffect(b);
        }
        // v = projectile start position (shoot point)
        // returns (arcParams, umbrellaPlant)
        protected virtual (float[] arc, Plant umbrella) FindUmbrella(Vector2 v)
        {
            var board = _plant.board;
            if (board == null)
                return (null, null);

            var gridSystem = board.gridSystem;
            if (gridSystem == null)
                return (null, null);

            int col = _plant.thePlantColumn;
            int row = _plant.thePlantRow;

            float[] bestArc = null;
            Plant bestUmbrella = null;

            // scan columns from this plant to the right
            while (col < board.columnNum)
            {
                var grid = gridSystem.GetGrid(col, row);
                if (grid == null || grid.plants == null)
                    break;

                foreach (var p in grid.plants)
                {
                    if (p == null)
                        continue;

                    // decompiler mangled types; this is really "is umbrella plant?"
                    if (!IsUmbrella(p))
                        continue;

                    var pos = p.axis.position;

                    // must be to the left of firstPostion.x (where we first targeted)
                    if (pos.x >= firstPostion.x)
                        continue;

                    // compute where umbrella will be at catch time
                    // vanilla adds +0.75f on Y
                    var umbrellaPos = new Vector2(pos.x, pos.y + 0.75f);

                    float t1 = firstTime;          // when we first locked target
                    float t2 = Time.time;          // now

                    // Lawnf helper: solve arc that goes from v → umbrellaPos over flightTime,
                    // given timing info (t1, t2)
                    float[] arc = Lawnf.CalculateProjectileParameters(
                        v,
                        t1,
                        umbrellaPos,   // "firstPlace"
                        t2,
                        umbrellaPos,   // "secondPlace" (same here)
                        flightTime
                    );

                    if (arc != null)
                    {
                        bestArc = arc;
                        bestUmbrella = p;
                        break;
                    }
                }

                if (bestArc != null)
                    break;

                col++;
            }

            return (bestArc, bestUmbrella);
        }
        // Called by animation event
        public override Bullet Shoot_Custom()
        {
            // must have a shoot point
            if (_plant.shoot == null)
                return null;

            // start position of projectile
            Vector2 v = _plant.shoot.position;
            float startX = v.x;
            float startY = v.y;

            // 1) Try umbrella interception
            var (arc, umbrella) = FindUmbrella(v);   // (float[] arc, Plant umbrella)

            float[] arcParams = arc;
            Plant targetPlant = umbrella;

            // 2) If no umbrella arc, try zombie-based arc
            if (arcParams == null)
            {
                Zombie z = _plant.targetZombie;
                if (z != null && z.col != null)
                {
                    // recompute arc using firstTime/firstPostion and current zombie position
                    float t1 = firstTime;
                    Vector2 firstPos = firstPostion;
                    float t2 = Time.time;

                    Vector2 zombiePos = GetZombiePosition(z);

                    arcParams = Lawnf.CalculateProjectileParameters(
                        v,
                        t1,
                        firstPos,
                        t2,
                        zombiePos,
                        flightTime
                    );

                    // if zombie is very close to the shoot point, adjust spawn position
                    var bounds = z.col.bounds;
                    float leftX = bounds.center.x - bounds.extents.x;
                    if (Mathf.Abs(leftX - startX) < 1f)
                    {
                        startX = leftX;
                        startY = bounds.center.y + bounds.extents.y - 0.1f;
                    }
                }
                else
                {
                    // 3) Fallback: recompute arc just using firstPostion
                    float t1 = firstTime;
                    Vector2 firstPos = firstPostion;
                    float t2 = Time.time;

                    arcParams = Lawnf.CalculateProjectileParameters(
                        v,
                        t1,
                        firstPos,
                        t2,
                        firstPostion,
                        flightTime
                    );
                }
            }
            var creator = CreateBullet.Instance;

            if (creator == null)
                return null;

            Bullet b = PlantMgr.SetBullet(
                _plant,
                GetBulletType(),
                BulletMoveWay.Throw,
                _plant.attackDamage * 2
            );

            if (b == null || arcParams == null || arcParams.Length < 4)
                return null;

            // apply arc
            b.Vx = arcParams[1];
            b.Vy = arcParams[2];
            b.detaVy = -arcParams[3];

            // if we intercepted an umbrella, mark it
            b.targetPlant = targetPlant;

            if (_plant.melonSputter)
                b.melonSputter = true;

            // pumpkin-type check → melon splash
            if (_plant.PumpkinType == PlantType.MelonPumpkin)
                MelonShoot();

            UniqueEffect(b);

            _plant.targetZombie = null;

            // random throw sound (3–4)
            int soundId = Random.RandomRangeInt(3, 5);
            GameAPP.PlaySound(soundId, 0.5f, 1f);

            return b;
        }
        // Only called when PumpkinType == 0x548 (Melon-on-Pumpkin)
        protected virtual void MelonShoot()
        {
            // 1) Pick a zombie to lob at using LINQ filters
            var all = Lawnf.GetAllZombies(false).ToSystemList();
            // b__12_0: filter zombies (alive, in row, in front, etc.)
            var filtered = all.Where(z => z != null && z.theZombieRow == _plant.thePlantRow && !z.isMindControlled && z.axis.position.x > _plant.axis.position.x && Lawnf.InLandStatus(z.theStatus));
            // b__12_1: key selector (distance / priority)
            var ordered = filtered.OrderBy(z => MelonOrderKey(z));
            var target = ordered.FirstOrDefault();

            if (target == null)
                return;

            var shoot = _plant.shoot;
            var creator = CreateBullet.Instance;
            if (shoot == null || creator == null)
                return;

            // 2) Spawn the melon bullet at shoot point
            var shootPos = (Vector2)shoot.position;

            // 3) Use the pumpkin’s attackDamage for this special shot
            var pumpkin = _plant.Pumpkin;
            if (pumpkin == null)
                return;
            Bullet b = PlantMgr.SetBullet(
                _plant,
                shootPos,
                _plant.thePlantRow,
                BulletType.Bullet_melon,
                BulletMoveWay.Throw,
                pumpkin.attackDamage
            );

            if (b == null) return;

            // 4) Try umbrella interception for this melon
            var startPos = (Vector2)shoot.position;
            var (arc, umbrella) = FindUmbrella(startPos);

            float[] arcParams = arc;
            Plant targetPlant = umbrella;

            // If no umbrella, compute arc directly to the chosen zombie
            if (targetPlant == null)
            {
                var projPos = (Vector2)shoot.position;
                Vector2 targetVel = target.Velocity;
                Vector2 targetPos = target.ColliderPosition;

                arcParams = Lawnf.CalculateProjectileWithSpeed(
                    projPos,
                    targetVel,
                    targetPos,
                    1.7f // hardcoded melon flight time
                );
            }

            if (arcParams == null || arcParams.Length < 4)
                return;

            // 5) Apply arc + metadata
            b.Vx = arcParams[1];
            b.Vy = arcParams[2];
            b.detaVy = -arcParams[3];

            b.targetPlant = targetPlant;
            b.melonSputter = _plant.melonSputter;
            b.fromType = _plant.thePlantType;
        }
        // used in MelonShoot: .OrderBy(z => MelonOrderKey(z))
        protected virtual float MelonOrderKey(Zombie z)
        {
            if (z == null || z.axis == null || _plant.axis == null)
                return float.PositiveInfinity;

            Vector3 zPos = z.axis.position;
            Vector3 plantPos = _plant.axis.position;

            float dx = zPos.x - plantPos.x;
            float dy = zPos.y - plantPos.y;

            // Euclidean distance
            return Mathf.Sqrt(dx * dx + dy * dy);
        }
        protected virtual bool IsUmbrella(Plant p)
        {
            // this is where you map the weird IL2CPP checks
            // to something sane like:
            return p.thePlantType == PlantType.CabbageUmbrella
                || p.thePlantType == PlantType.EmeraldUmbrella;
        }
        // Called by a second animation event (e.g. double‑lob, splash, etc.)
        public override Bullet Shoot2_Custom()
        {
            // must have a shoot point
            if (_plant.shoot == null)
                return null;

            // projectile start position
            Vector2 v = _plant.shoot.position;
            float startX = v.x;
            float startY = v.y;

            // 1) Try umbrella interception
            var (arc, umbrella) = FindUmbrella(v);   // (float[] arc, Plant umbrella)

            float[] arcParams = arc;
            Plant targetPlant = umbrella;

            // 2) If no umbrella arc, try zombie‑based arc
            if (arcParams == null)
            {
                Zombie z = _plant.targetZombie;
                if (z != null && z.col != null)
                {
                    var bounds = z.col.bounds;
                    float leftX = bounds.center.x - bounds.extents.x;
                    float topY = bounds.center.y + bounds.extents.y;

                    float t1 = firstTime;
                    Vector2 firstPos = firstPostion;
                    float t2 = Time.time;

                    Vector2 zombiePos = GetZombiePosition(z);

                    arcParams = Lawnf.CalculateProjectileParameters(
                        v,
                        t1,
                        firstPos,
                        t2,
                        zombiePos,
                        flightTime
                    );

                    // if zombie is very close horizontally, shift spawn to its left edge/top
                    if (Mathf.Abs((leftX - startX)) < 1f)
                    {
                        startX = leftX;
                        startY = topY - 0.1f;
                    }
                }
                else
                {
                    // 3) Fallback: recompute arc using firstPostion only
                    float t1 = firstTime;
                    Vector2 firstPos = firstPostion;
                    float t2 = Time.time;

                    arcParams = Lawnf.CalculateProjectileParameters(
                        v,
                        t1,
                        firstPos,
                        t2,
                        firstPostion,
                        flightTime
                    );
                }
            }

            Bullet b = PlantMgr.SetBullet(
                _plant,
                GetBulletType2(),
                BulletMoveWay.Throw,
                _plant.attackDamage * 2
            );

            if (b == null || arcParams == null || arcParams.Length < 4)
                return null;

            // apply arc
            b.Vx = arcParams[1];
            b.Vy = arcParams[2];
            b.detaVy = -arcParams[3];

            // umbrella target (if any)
            b.targetPlant = targetPlant;

            // NOTE: damage = base attackDamage + 0x14 (20)
            b.Damage = _plant.attackDamage + 0x14;
            b.fromType = _plant.thePlantType;

            _plant.targetZombie = null;

            // sound
            int soundId = Random.RandomRangeInt(3, 5);
            GameAPP.PlaySound(soundId, 0.5f, 1f);

            // melon chain
            if (_plant.PumpkinType == PlantType.MelonPumpkin)
                MelonShoot();

            return b;
        }
        protected virtual void UniqueEffect(Bullet b) { }
        public override BulletType GetBulletType() => BulletType.Bullet_cabbage;
        public override BulletType GetBulletType2() => GetBulletType();
        protected virtual Vector2 GetZombiePosition(Zombie zombie)
        {
            if (zombie == null || zombie.col == null)
                return default;

            var bounds = zombie.col.bounds;

            // NORMAL ZOMBIES
            if (zombie.theZombieType != ZombieType.ZombieBoss && zombie.theZombieType != ZombieType.ZombieBoss2) //the 2 zombie types
            {
                // aim at the TOP of the collider
                return new Vector2(
                    bounds.center.x,
                    bounds.center.y + bounds.extents.y
                );
            }

            // SPECIAL ZOMBIES (0x2C, 0x2E) — use land height instead
            var mouse = Mouse.Instance;
            if (mouse != null)
            {
                float landY = mouse.GetLandY(8.5f, _plant.thePlantRow);
                return new Vector2(8.5f, landY + 0.3f);
            }

            return default;
        }
    }
}