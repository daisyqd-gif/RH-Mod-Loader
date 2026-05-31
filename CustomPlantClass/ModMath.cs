namespace CustomPlantClass
{
    public static class MathHelper
    {
        // ============================================================
        //  ROTATION / QUATERNION HELPERS
        // ============================================================
        public static Quaternion DirectionToRotation(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }

        public static Vector2 RotationToDirection(Quaternion rotation)
        {
            float angle = rotation.eulerAngles.z * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        }

        public static Quaternion LookAt2D(Vector2 from, Vector2 to)
        {
            Vector2 dir = (to - from).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, 0, angle);
        }

        public static Quaternion RotateTowards2D(
            Quaternion current,
            float targetAngle,
            float maxDegreesPerSecond)
        {
            Quaternion target = Quaternion.Euler(0f, 0f, targetAngle);
            return Quaternion.RotateTowards(
                current,
                target,
                maxDegreesPerSecond * Time.deltaTime
            );
        }

        public static Quaternion RandomRotation2D()
            => Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // ============================================================
        //  VECTOR HELPERS
        // ============================================================
        public static float DistanceSq(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        public static Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        public static Vector2 ClampMagnitude(Vector2 v, float max)
        {
            float mag = v.magnitude;
            return mag > max ? v * (max / mag) : v;
        }

        // ============================================================
        //  RANDOM HELPERS
        // ============================================================
        public static T GetRandomValue<T>(List<T> values, Func<T, bool> selector = null)
        {
            selector ??= (_ => true);

            int count = 0;
            for (int i = 0; i < values.Count; i++)
                if (selector(values[i]))
                    count++;

            if (count == 0)
                return default;

            int target = Random.Range(0, count);

            for (int i = 0; i < values.Count; i++)
                if (selector(values[i]) && target-- == 0)
                    return values[i];

            return default;
        }

        public static float GetRandomWithMean(float min, float max, float targetMean)
        {
            if (!(min < targetMean && targetMean < max))
                return min;

            float p = Random.value;
            float leftWeight = (max - targetMean) / (max - min);

            if (p < leftWeight)
            {
                if (min <= 0f)
                    return Random.Range(min, targetMean);

                float logMin = Mathf.Log(min);
                float logMean = Mathf.Log(targetMean);
                float r = Random.Range(logMin, logMean);
                return Mathf.Exp(r);
            }
            else
            {
                float logMean = Mathf.Log(targetMean);
                float logMax = Mathf.Log(max);
                float r = Random.Range(logMean, logMax);
                return Mathf.Exp(r);
            }
        }

        public static float GetRandomLogSymmetric(float minRatio, float maxRatio)
        {
            if (minRatio < 0f) minRatio = 0.01f;
            if (maxRatio < 0f) maxRatio = 0.01f;

            float logMin = Mathf.Log(minRatio);
            float logMax = Mathf.Log(maxRatio);

            float r = Random.Range(logMin, logMax);
            return Mathf.Exp(r) - 1f;
        }

        // ============================================================
        //  SCALAR HELPERS
        // ============================================================
        public static float Remap(float v, float a, float b, float c, float d)
            => c + (v - a) * (d - c) / (b - a);

        public static float RemapClamped(float v, float a, float b, float c, float d)
        {
            float t = Mathf.InverseLerp(a, b, v);
            return Mathf.Lerp(c, d, t);
        }

        public static bool ApproximatelyZero(float v, float eps = 0.0001f)
            => Mathf.Abs(v) < eps;

        // ============================================================
        //  BALLISTIC HELPERS
        // ============================================================
        public static Vector2 CalculateProjectileWithGravity(
            Vector2 projectilePos,
            Vector2 targetVelocity,
            Vector2 targetPos,
            float flightTime,
            float gravity)
        {
            float vx = (targetVelocity.x * flightTime + targetPos.x - projectilePos.x) / flightTime;

            float vy = (
                targetVelocity.y * flightTime + targetPos.y
                - projectilePos.y
                - gravity * 0.5f * flightTime * flightTime
            ) / flightTime;

            return new Vector2(vx, vy);
        }

        public static float[] CalculateProjectileWithSpeed(
            Vector2 projectilePos,
            Vector2 targetVelocity,
            Vector2 targetPos,
            float flightTime)
        {
            float dx = targetVelocity.x * flightTime + targetPos.x - projectilePos.x;
            float dy = targetVelocity.y * flightTime + targetPos.y - projectilePos.y;

            float g = Physics2D.gravity.y;

            dy = (dy - g * 1.5f * 0.5f * flightTime * flightTime) / flightTime;
            float vx = dx / flightTime;

            float angleDeg = Mathf.Atan2(dy, vx) * Mathf.Rad2Deg;

            return new float[]
            {
                angleDeg,
                vx,
                dy,
                g * 1.5f
            };
        }

        public static float[] CalculateProjectileParameters(
            Vector2 startPos,
            float t1,
            Vector2 firstPlace,
            float t2,
            Vector2 secondPlace,
            float flightTime)
        {
            float dt = t2 - t1;
            float minDt = Time.deltaTime;
            if (dt < minDt)
                dt = minDt;

            Vector2 targetVelocity = (secondPlace - firstPlace) / dt;

            return CalculateProjectileWithSpeed(
                startPos,
                targetVelocity,
                secondPlace,
                flightTime
            );
        }

        // ============================================================
        //  COROUTINE HELPERS
        // ============================================================
        public static IEnumerator SmoothRotate(
            Transform t,
            Quaternion target,
            float smoothTime = 0.5f,
            float rotationSpeed = 0.6f)
        {
            if (t == null)
                yield break;

            Quaternion start = t.rotation;
            float elapsed = 0f;

            while (elapsed < smoothTime)
            {
                float tNorm = elapsed / smoothTime;
                t.rotation = Quaternion.Slerp(start, target, tNorm);

                elapsed += Time.fixedDeltaTime * rotationSpeed;
                yield return new WaitForFixedUpdate();
            }

            t.rotation = target;
        }
        public static int NextEmptyIndex<T>(object source, Func<T, bool> isEmpty=null)
        {
            if (isEmpty == null)
            {
                isEmpty=(T entry)=>{
                    if (entry == null)
                        return true;

                    if (entry.Equals(null)) // UnityEngine.Object null
                        return true;

                    // Optional: support objects with IsDeleted/IsDestroyed flags
                    var type = entry.GetType();
                    var deletedField = type.GetField("IsDeleted");
                    if (deletedField != null && (bool)deletedField.GetValue(entry))
                        return true;

                    return false;
                };
            }
            // CASE 1 — Managed List<T>
            if (source is List<T> managedList)
            {
                for (int i = 0; i < managedList.Count; i++)
                    if (isEmpty(managedList[i]))
                        return i;

                return managedList.Count;
            }

            // CASE 2 — Il2Cpp List<T>
            if (source is Il2CppSystem.Collections.Generic.List<T> il2cppList)
            {
                int count = il2cppList.Count;
                for (int i = 0; i < count; i++)
                    if (isEmpty(il2cppList[i]))
                        return i;

                return count;
            }

            // CASE 4 — ANY IEnumerable<T> (managed or IL2CPP)
            if (source is IEnumerable<T> enumerable)
            {
                int index = 0;
                foreach (var entry in enumerable)
                {
                    if (isEmpty(entry))
                        return index;
                    index++;
                }
                return index;
            }

            throw new NotSupportedException("Unsupported list type for NextEmptyIndex");
        }
        public static Vector3 GetLevelButtonPosition(int col, int row)
        {
            return new Vector3(
                -300f + col * 150f,
                160f - row * 130f
            );
        }
    }
}
