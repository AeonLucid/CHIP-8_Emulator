using System;
using System.Collections.Generic;
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
        ///     Used to generate random values.
        /// </summary>
        private Random _random;

        /// <summary>
        ///     A flag which is set if the screen needs to be re-drawn.
        /// </summary>
        public bool DrawFlag { get; set; }

        public Dictionary<ushort, bool> Keys;

        public byte[] Pixels => _gfx;

        public void Initialize()
        {
            _pc = 0x200;                // Program counter starts at 0x200 because the system expects the application to be loaded at memory location 0x200.
            _opcode = 0;                // Reset current opcode
            _i = 0;                     // Reset index register
            _sp = 0;                    // Reset stack pointer

            _gfx = new byte[64 * 32];   // Clear display
            _stack = new ushort[16];    // Clear stack
            _v = new byte[16];          // Clear registers V0-VF
            _memory = new byte[4096];   // Clear memory

            // Load fontset
            for (var i = 0; i < 80; i++)
            {
                _memory[i] = ChipConstants.Fontset[i];
            }

            _delayTimer = 0;            // Reset timers (?)
            _soundTimer = 0;

            _random = new Random();

            DrawFlag = true;            // Clear screen

            Keys = new Dictionary<ushort, bool>(ChipKeyMapping.Map.Count); // Clear keys

            foreach (var keymap in ChipKeyMapping.Map)
            {
                Keys.Add(keymap.Value, false);
            }
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
        private void EmulateCycle()
        {
            // Fetch the next opcode.
            _opcode = (ushort) (_memory[_pc] << 8 | _memory[_pc + 1]);

            // Decode opcode
            switch (_opcode & 0xF000)
            {
                case 0x0000:
                    switch (_opcode & 0x000F)
                    {
                        case 0x0000:
                            for (var i = 0; i < _gfx.Length; i++)
                            {
                                _gfx[i] = 0;
                            }

                            DrawFlag = true;
                            _pc += 2;
                            break;

                        case 0x000E:
                            _pc = _stack[--_sp];
                            _pc += 2;
                            break;
                            
                        default:
                            Console.WriteLine($">> Unknown opcode: 0x{_opcode:X}");
                            break;
                    }
                    break;

                // 1NNN: Jumps to address NNN.
                case 0x1000:
                    _pc = (ushort) (_opcode & 0x0FFF);
                    break;

                // 2NNN: Calls subroutine at NNN.
                case 0x2000: 
                    _stack[_sp] = _pc;
                    _sp++;
                    _pc = (ushort) (_opcode & 0x0FFF);
                    break;

                // 3XNN: Skips the next instruction if VX equals NN. (Usually the next instruction is a jump to skip a code block)
                case 0x3000:
                    if (_v[(_opcode & 0x0F00) >> 8] == (_opcode & 0x00FF))
                    {
                        _pc += 4;
                    }
                    else
                    {
                        _pc += 2;
                    }
                    break;

                // 4XNN: Skips the next instruction if VX doesn't equal NN. (Usually the next instruction is a jump to skip a code block)
                case 0x4000:
                    if (_v[(_opcode & 0x0F00) >> 8] != (_opcode & 0x00FF))
                    {
                        _pc += 4;
                    }
                    else
                    {
                        _pc += 2;
                    }
                    break;

                // 6XNN: Sets VX to NN.
                case 0x6000: 
                    _v[(_opcode & 0x0F00) >> 8] = (byte) (_opcode & 0x00FF);
                    _pc += 2;
                    break;

                // 7XNN: Adds NN to VX.
                case 0x7000:
                    _v[(_opcode & 0x0F00) >> 8] += (byte) (_opcode & 0x00FF);
                    _pc += 2;
                    break;

                case 0x8000:
                    switch (_opcode & 0x000F)
                    {
                        // 8XY0: Sets VX to the value of VY.
                        case 0x0000:
                            _v[(_opcode & 0x0F00) >> 8] = _v[(_opcode & 0x00F0) >> 4];
                            _pc += 2;
                            break;

                        // 8XY1: Sets VX to VX or VY. (Bitwise OR operation) VF is reset to 0.
                        case 0x0001:
                            _v[(_opcode & 0x0F00) >> 8] |= _v[(_opcode & 0x00F0) >> 4];
                            _v[0xF] = 0;
                            _pc += 2;
                            break;

                        // 8XY2: Sets VX to VX and VY. (Bitwise AND operation) VF is reset to 0.
                        case 0x0002:
                            _v[(_opcode & 0x0F00) >> 8] &= _v[(_opcode & 0x00F0) >> 4];
                            _v[0xF] = 0;
                            _pc += 2;
                            break;

                        // 8XY3: Sets VX to VX xor VY. VF is reset to 0.
                        case 0x0003:
                            _v[(_opcode & 0x0F00) >> 8] ^= _v[(_opcode & 0x00F0) >> 4];
                            _v[0xF] = 0;
                            _pc += 2;
                            break;

                        // 8XY4: Adds VY to VX. VF is set to 1 when there's a carry, and to 0 when there isn't.
                        case 0x0004:
                            if (_v[(_opcode & 0x00F0) >> 4] > 0xFF - _v[(_opcode & 0x0F00) >> 8])
                            {
                                _v[0xF] = 1;
                            }
                            else
                            {
                                _v[0xF] = 0;
                            }

                            _v[(_opcode & 0x0F00) >> 8] += _v[(_opcode & 0x00F0) >> 4];
                            _pc += 2;
                            break;

                        // 8XY5: VY is subtracted from VX. VF is set to 0 when there's a borrow, and 1 when there isn't.
                        case 0x0005:
                            if (_v[(_opcode & 0x00F0) >> 4] > _v[(_opcode & 0x0F00) >> 8])
                            {
                                _v[0xF] = 0;
                            }
                            else
                            {
                                _v[0xF] = 1;
                            }

                            _v[(_opcode & 0x0F00) >> 8] -= _v[(_opcode & 0x00F0) >> 4];
                            _pc += 2;
                            break;

                        // 8XY6: Shifts VX right by one. VF is set to the value of the least significant bit of VX before the shift.
                        case 0x0006:
                            _v[0xF] = (byte) (_v[(_opcode & 0x0F00) >> 8] & 1);
                            _v[(_opcode & 0x0F00) >> 8] >>= 1;
                            _pc += 2;
                            break;

                        // 8XYE: Shifts VX left by one. VF is set to the value of the most significant bit of VX before the shift.
                        case 0x000E:
                            _v[0xF] = (byte)(_v[(_opcode & 0x0F00) >> 8] >> 7);
                            _v[(_opcode & 0x0F00) >> 8] <<= 1;
                            _pc += 2;
                            break;


                        default:
                            Console.WriteLine($">> Unknown opcode: 0x{_opcode:X}");
                            break;
                    }
                    break;

                // 9XY0: Skips the next instruction if VX doesn't equal VY. (Usually the next instruction is a jump to skip a code block)
                case 0x9000:
                    if (_v[(_opcode & 0x0F00) >> 8] != _v[(_opcode & 0x00F0) >> 4])
                    {
                        _pc += 4;
                    }
                    else
                    {
                        _pc += 2;
                    }
                    break;

                // ANNN: Sets I to the address NNN.
                case 0xA000: 
                    _i = (ushort) (_opcode & 0x0FFF);
                    _pc += 2;
                    break;

                // CXNN: Sets VX to the result of a bitwise and operation on a random number (Typically: 0 to 255) and NN.
                case 0xC000:
                    _v[(_opcode & 0x0F00) >> 8] = (byte) ((byte)(_random.Next(0, 255) & 0x00FF) & (byte)(_opcode & 0x00FF));
                    _pc += 2;
                    break;
                
                // DXYN: Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels.
                case 0xD000: 
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

                            var targetY = x + xLine + (y + yLine) * 64;
                            if (targetY > _gfx.Length) continue;
                            
                            if (_gfx[targetY] == 1) // Check if byte is already set to 1.
                            {
                                _v[0xF] = 1; // Collision occured.
                            }

                            _gfx[x + xLine + (y + yLine) * 64] ^= 1;
                        }
                    }

                    DrawFlag = true;
                    _pc += 2;
                    break;

                // Keys
                case 0xE000:
                    switch (_opcode & 0x00FF)
                    {
                        // EX9E: Skips the next instruction if the key stored in VX is pressed. (Usually the next instruction is a jump to skip a code block)
                        case 0x009E:
                            if (Keys[_v[(_opcode & 0x0F00) >> 8]])
                            {
                                _pc += 4;
                            }
                            else
                            {
                                _pc += 2;
                            }
                            break;

                        // EXA1: Skips the next instruction if the key stored in VX isn't pressed. (Usually the next instruction is a jump to skip a code block)
                        case 0x00A1:
                            if (Keys[_v[(_opcode & 0x0F00) >> 8]])
                            {
                                _pc += 2;
                            }
                            else
                            {
                                _pc += 4;
                            }
                            break;
                            
                        default:
                            Console.WriteLine($">> Unknown opcode: 0x{_opcode:X}");
                            break;
                    }
                    break;

                case 0xF000:
                    var fx = (byte)((_opcode & 0x0F00) >> 8);
                    switch (_opcode & 0x00FF)
                    {
                        // FX07: Sets VX to the value of the delay timer.
                        case 0x0007:
                            _v[(_opcode & 0x0F00) >> 8] = _delayTimer;
                            _pc += 2;
                            break;

                        // FX15: Sets the delay timer to VX.
                        case 0x0015:
                            _delayTimer = _v[(_opcode & 0x0F00) >> 8];
                            _pc += 2;
                            break;

                        // FX18: Sets the sound timer to VX.
                        case 0x0018:
                            _soundTimer = _v[(_opcode & 0x0F00) >> 8];
                            _pc += 2;
                            break;

                        // FX1E: Adds VX to I
                        case 0x001E:
                            if (_i + _v[fx] > 0xFFF)
                            {
                                _v[0xF] = 1;
                            }
                            else
                            {
                                _v[0xF] = 1;
                            }

                            _i += _v[fx];
                            _pc += 2;
                            break;

                        // FX29: Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
                        case 0x0029:
                            _i = (ushort) (_v[fx] * 0x5);
                            _pc += 2;
                            break;

                        // FX33: Stores the binary-coded decimal representation of VX.
                        case 0x0033:
                            _memory[_i] = (byte) (_v[fx] / 100);
                            _memory[_i + 1] = (byte) (_v[fx] / 10 % 10);
                            _memory[_i + 2] = (byte) (_v[fx] % 100 % 10);
                            _pc += 2;
                            break;

                        // FX55: Stores V0 to VX (including VX) in memory starting at address I.
                        case 0x0055:
                            for (var i = 0; i <= fx; i++)
                            {
                                _memory[_i + i] = _v[i];
                            }

                            _i += fx;
                            _i++;
                            _pc += 2;
                            break;

                        // FX65: Fills V0 to VX (including VX) with values from memory starting at address I.
                        case 0x0065:
                            for (var i = 0; i <= fx; i++)
                            {
                                _v[i] = _memory[_i + i];
                            }
                            
                            _i += fx;
                            _i++;
                            _pc += 2;
                            break;

                        default:
                            Console.WriteLine($">> Unknown opcode: 0x{_opcode:X}");
                            break;
                    }
                    break;

                default:
                    Console.WriteLine($">> Unknown opcode: 0x{_opcode:X}");
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
