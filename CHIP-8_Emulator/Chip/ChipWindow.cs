using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace CHIP_8_Emulator.Chip
{
    internal class ChipWindow : GameWindow
    {
        private const int GameWidth = 64, GameHeight = 32;

        private const int GameSizeScale = 10;

        private const int TargetClockSpeed = 540;

        private readonly ChipSystem _chipSystem;

        public ChipWindow() : base(GameWidth * GameSizeScale, GameHeight * GameSizeScale)
        {
            _chipSystem = new ChipSystem();
            _chipSystem.Initialize();
            _chipSystem.LoadGame(ChipGame.Pong);

            UpdateFrame += OnUpdateFrame;
            RenderFrame += OnRenderFrame;
        }

        private void OnUpdateFrame(object sender, FrameEventArgs frameEventArgs)
        {
            _chipSystem.EmulateCycles((int) (TargetClockSpeed / TargetUpdateFrequency));
        }

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
    }
}
