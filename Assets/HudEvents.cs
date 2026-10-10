using System;

namespace SundarbanGuardian.Core
{
    /// Minimal event bus. Keeps HazardZoneTrigger decoupled from whatever
    /// UI system (uGUI/UI Toolkit) you end up wiring the HUD with.
    public static class HudEvents
    {
        public static event Action<string, float, string> EnteredHazardZone;
        public static event Action ExitedHazardZone;
        public static event Action<string> EnvironmentalRestoration;

        public static event Action<float, float> HazardZoneCleared;

        public static void RaiseEnteredHazardZone(string riskTier, float rawValue, string unit)
            => EnteredHazardZone?.Invoke(riskTier, rawValue, unit);

        public static void RaiseExitedHazardZone()
            => ExitedHazardZone?.Invoke();

        public static void RaiseEnvironmentalRestoration(string message)
            => EnvironmentalRestoration?.Invoke(message);

        public static void RaiseHazardZoneCleared(float x, float z)
            => HazardZoneCleared?.Invoke(x, z);
    }
}
