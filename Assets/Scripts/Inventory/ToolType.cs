using System;
using UnityEngine;

namespace SundarbanGuardian.Inventory
{
    /// <summary>
    /// The four universal environmental restoration tools usable by all species in the Sundarbans.
    /// </summary>
    public enum ToolType
    {
        Seed = 0,             // বীজ (Restores deforested/high-risk NDVI zones)
        NetCutter = 1,        // জাল কাটার ছুরি (Cuts illegal fishing monofilament nets)
        CleaningLiquid = 2,   // পরিষ্কারের তরল (Cleans toxic pollution/oil spill zones)
        TrapDestroyer = 3     // ফাঁদ ধ্বংসকারী (Dismantles poacher wire snares/traps)
    }

    /// <summary>
    /// Metadata and display helper for the four universal tools.
    /// </summary>
    public static class ToolInfo
    {
        public static string GetNameBn(ToolType tool) => tool switch
        {
            ToolType.Seed => "বীজ",
            ToolType.NetCutter => "জাল কাটার ছুরি",
            ToolType.CleaningLiquid => "পরিষ্কারের তরল",
            ToolType.TrapDestroyer => "ফাঁদ ধ্বংসকারী",
            _ => "টুল"
        };

        public static string GetNameEn(ToolType tool) => tool switch
        {
            ToolType.Seed => "Seed",
            ToolType.NetCutter => "Net-Cutter Knife",
            ToolType.CleaningLiquid => "Cleaning Liquid",
            ToolType.TrapDestroyer => "Trap Destroyer",
            _ => "Tool"
        };

        public static string GetShortLabel(ToolType tool) => tool switch
        {
            ToolType.Seed => "SEED",
            ToolType.NetCutter => "KNIFE",
            ToolType.CleaningLiquid => "CLEAN",
            ToolType.TrapDestroyer => "TRAP",
            _ => "TOOL"
        };

        public static string GetSymbol(ToolType tool) => tool switch
        {
            ToolType.Seed => "🌱",
            ToolType.NetCutter => "🔪",
            ToolType.CleaningLiquid => "🧪",
            ToolType.TrapDestroyer => "🪤",
            _ => "🧰"
        };

        public static Color GetThemeColor(ToolType tool) => tool switch
        {
            ToolType.Seed => new Color(0.22f, 0.85f, 0.38f, 1.0f),           // Emerald Green
            ToolType.NetCutter => new Color(0.35f, 0.75f, 0.95f, 1.0f),       // Sky / Steel Blue
            ToolType.CleaningLiquid => new Color(0.18f, 0.92f, 0.88f, 1.0f),  // Aquamarine
            ToolType.TrapDestroyer => new Color(0.95f, 0.65f, 0.18f, 1.0f),   // Amber / Bronze
            _ => Color.white
        };

        public static string GetActionDescriptionBn(ToolType tool) => tool switch
        {
            ToolType.Seed => "বনায়নের বীজ ছিটিয়ে দিন (Plant Mangrove Vegetation)",
            ToolType.NetCutter => "নিষিদ্ধ জাল কেটে উদ্ধার করুন (Cut Illegal Net)",
            ToolType.CleaningLiquid => "দূষিত স্থান পরিষ্কার করুন (Neutralize Pollution)",
            ToolType.TrapDestroyer => "শিকারির ফাঁদ ভেঙে ফেলুন (Dismantle Poacher Snare)",
            _ => "টুল ব্যবহার করুন (Use Tool)"
        };
    }
}
