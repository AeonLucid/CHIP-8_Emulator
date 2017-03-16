using System.Collections.Generic;
using OpenTK.Input;

namespace CHIP_8_Emulator.Chip
{
    internal static class ChipKeyMapping
    {
        public static Dictionary<Key, ushort> Map = new Dictionary<Key, ushort>
        {
            { Key.Number1, 0x1 }, { Key.Number2, 0x2 }, { Key.Number3, 0x3 }, { Key.Number4, 0xC }, 
            { Key.Q, 0x4 }, { Key.W, 0x5 }, { Key.E, 0x6 }, { Key.R, 0xD }, 
            { Key.A, 0x7 }, { Key.S, 0x8 }, { Key.D, 0x9 }, { Key.F, 0xE }, 
            { Key.Z, 0xA }, { Key.X, 0x0 }, { Key.C, 0xB }, { Key.V, 0xF }, 
        };
    }
}
