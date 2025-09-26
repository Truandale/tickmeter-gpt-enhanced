using System;
using System.Drawing;

namespace tickMeter.Classes
{
    public static class ColorZoneEvaluator
    {
        // Zone evaluation results
        public enum Zone
        {
            Green,
            Yellow,
            Red
        }

        // Evaluate ping zone based on current profile
        public static Zone EvaluatePingZone(float pingMs)
        {
            var profile = App.settingsManager.GetColorZoneProfile();
            
            if (pingMs <= profile.PingGreenMs)
                return Zone.Green;
            if (pingMs <= profile.PingYellowMs)
                return Zone.Yellow;
            return Zone.Red;
        }

        // Evaluate tickrate zone based on current profile and target
        public static Zone EvaluateTickrateZone(float currentHz, float targetHz)
        {
            if (targetHz <= 0) return Zone.Green; // Avoid division by zero
            
            var profile = App.settingsManager.GetColorZoneProfile();
            float ratio = currentHz / targetHz;
            
            if (ratio >= profile.TickrateGreenRatio)
                return Zone.Green;
            if (ratio >= profile.TickrateYellowRatio)
                return Zone.Yellow;
            return Zone.Red;
        }

        // Evaluate ticktime zone based on current profile and target
        public static Zone EvaluateTicktimeZone(float ticktimeMs, float targetHz)
        {
            if (targetHz <= 0) return Zone.Green; // Avoid division by zero
            
            var profile = App.settingsManager.GetColorZoneProfile();
            float targetIntervalMs = 1000.0f / targetHz;
            float ratio = ticktimeMs / targetIntervalMs;
            
            if (ratio <= profile.TicktimeGreenRatio)
                return Zone.Green;
            if (ratio <= profile.TicktimeYellowRatio)
                return Zone.Yellow;
            return Zone.Red;
        }

        // Convert zone to color based on settings form colors
        public static Color ZoneToColor(Zone zone)
        {
            switch (zone)
            {
                case Zone.Green:
                    return App.settingsForm.ColorGood.ForeColor;
                case Zone.Yellow:
                    return App.settingsForm.ColorMid.ForeColor;
                case Zone.Red:
                    return App.settingsForm.ColorBad.ForeColor;
                default:
                    return App.settingsForm.ColorGood.ForeColor;
            }
        }

        // Legacy method for tickrate (maintains compatibility)
        public static Color GetTickRateColor(float tickrate)
        {
            // Use original thresholds for now to maintain compatibility
            if (tickrate < 30)
                return App.settingsForm.ColorBad.ForeColor;
            if (tickrate < 50)
                return App.settingsForm.ColorMid.ForeColor;
            return App.settingsForm.ColorGood.ForeColor;
        }

        // New method for ping using profile-based zones
        public static Color GetPingColor(float pingMs)
        {
            var zone = EvaluatePingZone(pingMs);
            return ZoneToColor(zone);
        }
    }
}