namespace CustomPlantClass
{
    public class CustomShooter : BaseCustomPlant
    {
        public virtual float thePlantAttackInterval => _plant.thePlantAttackInterval;
        public float thePlantAttackContDown = 0f;
        public override void Update()
        {
            if (_plant.Active)
            {
                if (_plant.board != null)
                {
                    if (!_plant.board.boardTag.isScaredyDream || _plant.thePlantType == PlantType.ScaredyShroom)
                    {
                        PlantShootUpdate();
                    }
                }
            }
            base.Update();
        }
        public virtual void UpdateAttackCountDown() => _plant.UpdateAttackCountDown();
        public virtual GameObject SearchZombie() => _plant.SearchZombie();
        public virtual bool Shootable()
        {
            if (SearchZombie() != null)
            {
                return true;
            }
            else if (_plant.SearchBoss() != null)
            {
                return true;
            }
            else if (_plant.SearchGoldMagnet())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual void PlantShootUpdate()
        {
            UpdateAttackCountDown();
            if (_plant.thePlantAttackCountDown <= 0f)
            {
                _plant.thePlantAttackCountDown = thePlantAttackInterval * Random.Range(0.95f, 1.05f);
                if (Shootable())
                {
                    _plant.anim.SetTriggerString("shoot");
                }
            }
        }
    }
}