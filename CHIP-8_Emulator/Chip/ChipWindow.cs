using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace CHIP_8_Emulator.Chip
{
    internal class ChipWindow : GameWindow
    {
        private const int GameWidth = 64, GameHeight = 32;

        private const int GameSizeScale = 10;

        private readonly ChipSystem _chipSystem;

        public ChipWindow() : base(GameWidth * GameSizeScale, GameHeight * GameSizeScale)
        {
            _chipSystem = new ChipSystem();
            _chipSystem.Initialize();
            _chipSystem.LoadGame(ChipGame.Pong2);

            WindowBorder = WindowBorder.Fixed;
            UpdateFrame += OnUpdateFrame;
            RenderFrame += OnRenderFrame;
            KeyUp += OnKeyUp;
            KeyDown += OnKeyDown;
        }

        // Called at 60 Hz
        private void OnUpdateFrame(object sender, FrameEventArgs frameEventArgs)
        {
            _chipSystem.EmulateCycles((int) (ChipSystem.TargetClockSpeed / TargetUpdateFrequency));
            _chipSystem.EmulateSoundCycle();
        }

        // Called as fast as possible
        private void OnRenderFrame(object sender, FrameEventArgs frameEventArgs)
        {
            if (!_chipSystem.DrawFlag) return;

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, 640, 320, 0, -1, 1);

            GL.Begin(PrimitiveType.Quads);
            for (var x = 0; x < 64; x++)
            {
                for (var y = 0; y < 32; y++)
                {
                    if (_chipSystem.Pixels[x + y * 64] <= 0) continue;

                    GL.Vertex2(x * GameSizeScale, y * GameSizeScale);
                    GL.Vertex2(x * GameSizeScale, y * GameSizeScale + GameSizeScale);
                    GL.Vertex2((x + 1) * GameSizeScale, y * GameSizeScale + GameSizeScale);
                    GL.Vertex2((x + 1) * GameSizeScale, y * GameSizeScale);
                }
            }
            GL.End();

            SwapBuffers();

            _chipSystem.DrawFlag = false;
        }

        #region Keys
        private void OnKeyUp(object sender, KeyboardKeyEventArgs keyboardKeyEventArgs)
        {

        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs keyboardKeyEventArgs)
        {
            if (keyboardKeyEventArgs.Key == Key.Escape)
            {
                Close();
                return;
            }

            var chipKey = ChipKeyMapping.Map.FirstOrDefault(x => x.Key == keyboardKeyEventArgs.Key);
            if (chipKey.Key == Key.Unknown) return;

            
        }
        #endregion
    }
}
