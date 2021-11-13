using SadConsole;
using Console = SadConsole.Console;
using Microsoft.Xna.Framework;

namespace GameJamSadConsoleSample
{
    // Source and inspiration: "The SadConsole Roguelike Tutorial Series", www.ansiware.com 
    // Tutorial pt 1: https://ansiware.com/tutorial-part-1-preface-and-hello-world-sadconsole-v8/
    // Tutorial pt 2: https://ansiware.com/tutorial-part-2-player-creation-and-display-v8/
    // Tutorial pt 3: https://ansiware.com/tutorial-part-3-movement-and-keyboard-input-v8/
    // Tutorial pt 4: https://ansiware.com/tutorial-part-4-create-a-room-v8/
    // Tutorial pt 5: https://ansiware.com/tutorial-part-5-actors-and-tiles-v8/
    public class GameLoop
    {
        public const int Width = 30;
        public const int Height = 30;
        private static Player player;

        private static TileBase[] _tiles;
        private const int roomStartY = 1;
        private const int roomStartX = 2;
        private const int _roomWidth = 10; 
        private const int _roomHeight = 10;
        
     

        static void Main(string[] args)
        {
            SadConsole.Game.Create(Width, Height);
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }
      
        private static void Init()
        {
            CreateWalls();
            CreateFloors();
            Console startingConsole = new ScrollingConsole(Width, Height, Global.FontDefault, new Rectangle(0, 0, Width, Height), _tiles);
            startingConsole.Print(1, 12, "En m0rk og stormfull aften", ColorAnsi.CyanBright);
            SadConsole.Global.CurrentScreen = startingConsole;
            CreatePlayer();
            startingConsole.Children.Add(player);
        }
        
        private static void Update(GameTime time)
        {
            CheckKeyboard();
        }
        
        private static void CheckKeyboard()
        {
            if (Global.KeyboardState.IsKeyReleased(Microsoft.Xna.Framework.Input.Keys.F5))
            {
                Settings.ToggleFullScreen();
            }

            if (Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Up))
            {
                player.MoveBy(new Point(0, -1));
            }

            if (Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Down))
            {
                player.MoveBy(new Point(0, 1));
            }

            if (Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                player.MoveBy(new Point(-1, 0));
            }

            if (Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Right))
            {
                player.MoveBy(new Point(1, 0));
            }
        }

        private static void CreatePlayer()
        {
            player = new Player(Color.Yellow, Color.Transparent);
            player.Position = new Point(5, 5);
        }

        private static void CreateFloors()
        {
            for (int x = roomStartX; x < _roomWidth; x++)
            {
                for (int y = roomStartY; y < _roomHeight; y++)
                {
                    _tiles[y * Width + x] = new TileFloor();
                }
            }
        }

        
        
        private static void CreateWalls()
        {
            int wallAlternator = 0;
            string wallCharacters = "$#$%";
            _tiles = new TileBase[Width * Height];

            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i] = new TileWall(wallCharacters[wallAlternator]);
                wallAlternator++;
                if (wallAlternator == wallCharacters.Length)
                {
                    wallAlternator = 0;
                }
            }
        }
        public static bool IsTileWalkable(Point location)
        {
            if (location.X < 0 || location.Y < 0 || location.X >= Width || location.Y >= Height)
                return false;
            return !_tiles[location.Y * Width + location.X].IsBlockingMove;
        }
    }
    
}