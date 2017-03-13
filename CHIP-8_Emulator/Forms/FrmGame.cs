using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using CHIP_8_Emulator.Chip;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace CHIP_8_Emulator.Forms
{
    public partial class FrmGame : Form
    {
        private const int GameWidth = 64, GameHeight = 32;

        private const int GameSizeScale = 10;

        private readonly GLControl _renderControl;

        private Thread _chipThread;

        private ChipSystem _chipSystem;
        
        private bool _chipEmulate;

        public FrmGame()
        {
            Toolkit.Init();
            InitializeComponent();
            
            // Setup OpenGL
            _renderControl = new GLControl
            {
                Width = GameWidth * GameSizeScale,
                Height = GameHeight * GameSizeScale,
                Location = new Point(0, 0),
                Padding = new Padding(0, 0, 0, 0)
            };
            
            _renderControl.Paint += RenderControl_Paint;

            Controls.Add(_renderControl);

            Width = GameWidth * GameSizeScale + 16;
            Height = GameHeight * GameSizeScale + 39;
        }

        private void FrmGame_Load(object sender, EventArgs e)
        {
            _chipThread = new Thread(EmulateGame);
            _chipThread.Start();
        }

        private void FrmGame_FormClosing(object sender, FormClosingEventArgs e)
        {
            _chipEmulate = false;
        }

        #region OpenGL
        private void RenderControl_Paint(object sender, PaintEventArgs paintEventArgs)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, 640, 320, 0, -1, 1);
            
            for (var x = 0; x < 64; x++)
            {
                for (var y = 0; y < 32; y++)
                {
                    if (_chipSystem.Pixels[x + y * 64] <= 0) continue;
                    
                    GL.Begin(PrimitiveType.Quads);
                    GL.Vertex2(x * GameSizeScale, y * GameSizeScale);
                    GL.Vertex2(x * GameSizeScale, y * GameSizeScale + GameSizeScale);
                    GL.Vertex2((x + 1) * GameSizeScale, y * GameSizeScale + GameSizeScale);
                    GL.Vertex2((x + 1) * GameSizeScale, y * GameSizeScale);
                    GL.End();
                }
            }
            
            _renderControl.SwapBuffers();
        }
        #endregion
        
        private void EmulateGame()
        {
            // Initialize chip8 system
            _chipEmulate = true;
            _chipSystem = new ChipSystem();
            _chipSystem.Initialize();
            _chipSystem.LoadGame(ChipGame.Pong);

            // Emulation loop
            while (_chipEmulate)
            {
                _chipSystem.EmulateCycle();

                if (_chipSystem.DrawFlag)
                {
                    Console.WriteLine("## DRAW!");

                    _renderControl.Invalidate();
                    _chipSystem.DrawFlag = false;
                }

                // _chipSystem.SetKeys();
            }
        }
    }
}
