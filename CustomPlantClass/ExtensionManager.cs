using Il2CppInterop.Runtime;

namespace CustomPlantClass
{
    public static class ExtensionManager
    {
        public static GridItemType ToGridItemType(this ID self)
        {
            return (GridItemType)self.id;
        }
        public static ID ToID(this GridItemType self)
        {
            return (int)self;
        }
        public static int AddAndGetIndex<T>(this List<T> self, T item)
        {
            int counter=self.Count;
            self.Add(item);
            return counter;
        }
        public static int AddAndGetIndex<T>(this Il2CppSystem.Collections.Generic.List<T> self, T item)
        {
            int counter=self.Count;
            self.Add(item);
            return counter;
        }
        public static Component AddComponent(this MonoBehaviour self, Il2CppSystem.Type type)
        {
            return self.gameObject.AddComponent(type);
        }
        public static Component GetOrAddComponent(this MonoBehaviour self, Il2CppSystem.Type type)
        {
            if(self.TryGetComponent(type,out var component)) return component;
            return self.gameObject.AddComponent(type);
        }
    }
}
