using PulsarModLoader;
using PulsarModLoader.Keybinds;

namespace Extended_Planning
{
    public class Mod : PulsarMod, IKeybind
    {
        public override string Version => "0.0.0";

        public override string Author => "18107";

        public override string ShortDescription => "Provides more options for route planning";

        public override string LongDescription => "Removes the limit of 5 waypoints.\nLeft click adds a waypoint to the start of the list,\nmiddle click removes all waypoints,\nright click still adds a waypoint to the end of the list.";

        public override string Name => "Extended planning";

        public override string ModID => "extendedplanning";

        public override string HarmonyIdentifier()
        {
            return "id107.extendedplanning";
        }

        public void RegisterBinds(KeybindManager manager)
        {
            manager.NewBind("Middle Click", "middle_click", "Basics", "MOUSE2");
        }
    }
}
