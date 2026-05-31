namespace CustomPlantClass
{
    public class BaseCustomPlant : MonoBehaviour
    {
        public bool isPF = false;
        public Plant _plant => GetComponent<Plant>();
        public TextMeshPro extraText;
        public TextMeshPro extraTextShadow;
        public virtual void Start()
        {
            _plant.shoot = GetShoot();
            OnSpawn();
        }

        public virtual void OnSpawn() { }
        public virtual void OnDie(DieReason reason) { }
        public virtual void Update()
        {
            OnUpdate();
            try
            {
                UpdateText();
            }
            catch { }
        }
        public virtual void OnUpdate() { }
        public virtual void SetTextPosition()
        {
            if (extraText != null)
            {
                extraText.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            }
        }
        public virtual void UpdateText()
        {
            if (GameAPP.theGameStatus == GameStatus.InGame && extraText != null)
            {
                SetTextPosition();
                if (_plant != null)
                {
                    string text = GetTextString();
                    extraText.text = text;
                }
            }
        }
        public virtual void SetTextStyle(TextMeshPro text)
        {
            text.fontSize = 2.1f;
        }
        public virtual Color SetTextColor() =>Color.green;
        public virtual string GetTextString() => "";
        public virtual List<KeyValuePair<string, string>> GetLiveInfo()
        {
            return new List<KeyValuePair<string, string>>();
        }
        public virtual void InitText(GameObject text)
        {
            if (_plant != null && _plant.healthSlider != null && extraText == null)
            {
                extraText = _plant.SetPlantText(GetDamage().ToString() ?? "", Color.cyan, new Vector2(0f, 0.7f), text.transform, "0", 500);
                SetTextStyle(extraText);
            }
        }

        public virtual Transform FindShoot()
            => _plant.transform.GetChild(0).Find(GetShootPath());
        
        public virtual string GetShootPath() => "Shoot";

        public Transform GetShoot()
        {
            var s = FindShoot();
            return s != null ? s : _plant.transform;
        }

        public virtual BulletType GetBulletType()
        {
            if(DataMgr.plantBulletTypes.TryGetValue(_plant.thePlantType,out var a))
            {
                return a;
            }
            else
            {
                return BulletType.Bullet_pea;
            }
        }
        public virtual BulletType GetBulletType2() => GetBulletType();

        public virtual BulletMoveWay GetBulletMoveWay() => BulletMoveWay.MoveRight;
        public virtual BulletMoveWay GetBulletMoveWay2() => GetBulletMoveWay();
        public virtual BulletMoveWay GetBulletMoveWayPF_SuperGatling()
        {
            if(Lawnf.TravelUltimate(UltiBuff.EnumValue51)) return BulletMoveWay.MoveRight;
            return BulletMoveWay.Right_free;
        }

        public virtual int GetDamage() => _plant.attackDamage;
        public virtual int GetDamage2() => GetDamage();

        public virtual Bullet AnimShoot_Custom()
        {
            Bullet b=Shoot_Custom();
            EventManager.TriggerEvent(GameEvent.OnPlantShoot,_plant);
            return b;
        }
        public virtual Bullet AnimShoot2_Custom() => Shoot2_Custom();

        public virtual Bullet Shoot_Custom()
        {
            Bullet b = PlantMgr.SetBullet(_plant, GetBulletType(), GetBulletMoveWay(), GetDamage()); //applies fields automatically
            int soundId = Random.Range(3, 5);
            GameAPP.PlaySound(soundId, 0.5f, 1.0f);
            return b;
        }
        
        public virtual Bullet Shoot2_Custom()
        {
            Bullet b = PlantMgr.SetBullet(_plant, GetBulletType2(), GetBulletMoveWay2(), GetDamage2()); //applies fields automatically
            int soundId = Random.Range(3, 5);
            GameAPP.PlaySound(soundId, 0.5f, 1.0f);
            return b;
        }


        public virtual void FixedUpdate()
        {
            if (isPF)
            {
                _plant.thePlantHealth = _plant.thePlantMaxHealth;
                _plant.flashCountDown=10f;
                _plant.UpdateText();
            } 
            OnFixedUpdate();
        }
        public virtual void OnFixedUpdate() { }

        public virtual void StartPF()
        {
            _plant.invincible = true;
            _plant.uncrashable = true;
            isPF = true;
            _plant.isFlashing = true;

            // Wrap the coroutine so we can detect when it finishes
            _plant.StartCoroutine(PFWrapper());
        }

        private IEnumerator PFWrapper()
        {
            // Run the user‑overridable supershoot
            yield return SuperShoot();

            // When it finishes, call the overridable end hook
            SuperEnd();
        }

        // Overridable supershoot logic
        public virtual IEnumerator SuperShoot()
        {
            yield return null;
        }

        // Overridable end hook
        public virtual void SuperEnd()
        {
            _plant.invincible = false;
            _plant.uncrashable = false;
            isPF = false;
            _plant.flashCountDown = 0f;
            _plant.isFlashing = false;
        }
    }
}