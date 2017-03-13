using System;
using System.IO;

namespace CHIP_8_Emulator.Chip
{
    /// <summary>
    ///     Main class of the Chip-8 emulator.
    /// </summary>
    internal class ChipSystem
    {
        public const int TargetClockSpeed = 540;

        public const int TargetClockSpeedSound = 60;

        /// <summary>
        ///     The current opcode.
        /// </summary>
        private ushort _opcode;

        /// <summary>
        ///     The game memory.
        /// </summary>
        private byte[] _memory;

        /// <summary>
        ///     The game graphics. (64 x 32, black and white)
        /// </summary>
        private byte[] _gfx;

        /// <summary>
        ///     The CPU registers. (Chip 8 has 15 8-bit general purpose registers
        ///     named V0, V1 up to VE. The 16th register is used for the 'carry flag')
        /// </summary>
        private byte[] _v;

        /// <summary>
        ///     An index register 'I' which can have a value from 0x000 to 0xFFF.
        /// </summary>
        private ushort _i;
        
        /// <summary>
        ///     An program counter 'PC' which can have a value from 0x000 to 0xFFF.
        /// </summary>
        private ushort _pc;

        /// <summary>
        ///     Time register that counts at 60 Hz, when above zero it will count to zero.
        /// </summary>
        private byte _delayTimer;

        /// <summary>
        ///     Time register that counts at 60 Hz, when above zero it will count to zero.
        /// </summary>
        private byte _soundTimer;

        /// <summary>
        ///     The stack is used to remember the current location before a jump is performed.
        ///     Everytime you perform a jump or call a subroutine, store the program counter in
        ///     the stack before proceeding.
        /// </summary>
        private ushort[] _stack;

        /// <summary>
        ///     The stack pointer is used to remember which level of the stack is used.
        /// </summary>
        private ushort _sp;

        /// <summary>
        ///     An hex based keypad (0x0-0xF). Used to store the current state of a key.
        /// </summary>
        private byte[] _key;

        /// <summary>
        ///     A flag which is set if the screen needs to be re-drawn.
        /// </summary>
        public bool DrawFlag { get; set; }

        public byte[] Pixels => _gfx;

        public void Initialize()
        {
            _pc = 0x200;                // Program counter starts at 0x200 because the system expects the application to be loaded at memory location 0x200.
            _opcode = 0;                // Reset current opcode
            _i = 0;                     // Reset index register
            _sp = 0;                    // Reset stack pointer

            _gfx = new byte[64 * 32];   // Clear display
            _stack = new ushort[16];      // Clear stack
            _v = new byte[16];          // Clear registers V0-VF
            _memory = new byte[4096];   // Clear memory

            // Load fontset
            for (var i = 0; i < 80; i++)
            {
                _memory[i] = ChipConstants.Fontset[i];
            }

            _delayTimer = 0;            // Reset timers (?)
            _soundTimer = 0;

            DrawFlag = true;            // Clear screen
        }

        public void LoadGame(ChipGame game)
        {
            var gameName = game.ToString().ToUpper();
            var gamePath = Path.Combine("Games", gameName);

            // Check if the file exists
            if (!File.Exists(gamePath))
            {
                throw new Exception($"Game not found: {gameName}");
            }

            // Load game in memory
            var gameBytes = File.ReadAllBytes(gamePath);

            for (var i = 0; i < gameBytes.Length; i++)
            {
                _memory[i + 512] = gameBytes[i];
            }
        }

        /// <summary>
        ///     Emulates the next CPU cycle.
        ///     Opcodes: https://en.wikipedia.org/wiki/CHIP-8#Opcode_table
        /// </summary>
        public void EmulateCycle()
        {
            // Fetch the next opcode.
            _opcode = (ushort) (_memory[_pc] << 8 | _memory[_pc + 1]);

            // Decode opcode
            switch (_opcode & 0xF000)
            {
                case 0x2000: // 2NNN: Calls subroutine at NNN.
                    _stack[_sp] = _pc;
                    _sp++;
                    _pc = (ushort) (_opcode & 0x0FFF);
                    break;

                case 0x6000: // 6XNN: Sets VX to NN.
                    _v[(_opcode & 0x0F00) >> 8] = (byte) (_opcode & 0x00FF);
                    _pc += 2;
                    break;

                case 0xA000: // ANNN: Sets I to the address NNN.
                    _i = (ushort) (_opcode & 0x0FFF);
                    _pc += 2;
                    break;

                case 0xD000: // DXYN: Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels.
                    var x = _v[(_opcode & 0x0F00) >> 8];
                    var y = _v[(_opcode & 0x00F0) >> 4];
                    var height = _opcode & 0x000F;

                    _v[0xF] = 0;
                    for (var yLine = 0; yLine < height; yLine++)
                    {
                        var pixel = _memory[_i + yLine];
                        for (var xLine = 0; xLine < 8; xLine++) // Loop over 8 bits
                        {
                            if ((pixel & (0x80 >> xLine)) == 0) continue; // Shift to the current bit, continue if that bit is set in the current pixel of the sprite.

                            if (_gfx[x + xLine + (y + yLine) * 64] == 1) // Check if byte is already set to 1.
                            {
                                _v[0xF] = 1; // Collision occured.
                            }

                            _gfx[x + xLine + (y + yLine) * 64] ^= 1;
                        }
                    }

                    DrawFlag = true;
                    _pc += 2;
                    break;

                default:
//                    Console.WriteLine($">> Unknown opcode: 0x{_opcode:X}");
                    break;
            }
        }

        /// <summary>
        ///     Emulates multiple CPU cycles.
        ///     Thanks to: 
        ///      - http://stackoverflow.com/a/1393529
        ///      - http://stackoverflow.com/a/827720
        /// </summary>
        /// <param name="cycles"></param>
        public void EmulateCycles(int cycles)
        {
            var n = 0;
            while (n < cycles)
            {
                EmulateCycle();
                n += 1;
            }
        }

        public void EmulateSoundCycle()
        {
            // Update timers
            if (_delayTimer > 0)
            {
                _delayTimer--;
            }

            if (_soundTimer > 0)
            {
                if (_soundTimer == 1)
                {
                    Console.WriteLine("## BEEP!");
                }

                _soundTimer--;
            }
        }
    }
}
