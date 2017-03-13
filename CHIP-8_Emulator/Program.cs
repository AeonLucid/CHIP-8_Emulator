using CHIP_8_Emulator.Chip;

namespace CHIP_8_Emulator
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            using (var game = new ChipWindow())
            {
                game.Run(60); // 30 Hz
            }
        }
    }
}
