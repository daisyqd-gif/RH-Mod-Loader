

namespace CustomPlantClass
{
    public class ModPlugin : BasePlugin
    {
        public ManualLogSource Logger;
        public override void Load()
        {
            try
            {
                Logger= Log;
                try{Tools.InitMod();}catch(Exception e){
                    ModLogger.LogError("Exception occured during initmod\n" +e.ToString()+"\nPlease refer to the common errors pdf.");
                }

                try { InitializeMod(); }
                catch (Exception e)
                {
                    ModLogger.LogError("Exception occured during mod initialization\n" + e.ToString() + "\nPlease refer to the common errors pdf.");
                }

                try { OnStart(); }
                catch (Exception e)
                {
                    ModLogger.LogError("Exception occured in OnStart\n" + e.ToString() + "\nPlease refer to the common errors pdf.");
                }
                try { InitializePlants(); }
                catch (Exception e)
                {
                    ModLogger.LogError("Exception occured during plant initialization\n" + e.ToString() + "\nPlease refer to the common errors pdf.");
                }
                try { InitializeZombies(); }
                catch (Exception e)
                {
                    ModLogger.LogError("Exception occured during zombie initialization\n" + e.ToString() + "\nPlease refer to the common errors pdf.");
                }
                try { InitializeBuffs(); }
                catch (Exception e)
                {
                    ModLogger.LogError("Exception occured during buff initialization\n" + e.ToString() + "\nPlease refer to the common errors pdf.");
                }

                DataMgr.AddGameStartAction(OnGameStart);
                DataMgr.AddGameStartAction(OnGameInit);
                DataMgr.AddGameStartAction(InitializeConditions);
            }
            catch (Exception e)
            {
                ModLogger.LogError(e.ToString() + "\nPlease refer to the common errors pdf.");
            }
        }
        public virtual void InitializeMod() { }
        public virtual void OnStart() { }
        public virtual void InitializeBuffs() { }
        public virtual void InitializePlants() { }
        public virtual void InitializeZombies() { }
        public virtual void InitializeConditions() { }
        public virtual void OnGameStart() { }
        public virtual void OnGameInit() { }
    }
}