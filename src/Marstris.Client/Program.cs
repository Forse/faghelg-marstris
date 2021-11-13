using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Marstris.Core.Communication;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using Console = SadConsole.Console;

namespace Marstris.Core
{
    public class GameLoop
    {
        private const string WaitingText = "Waiting...";
        private static int playerId;

        private static int Width;
        private static int Height;
        private static Color PlayerColor;

        private static ConcurrentDictionary<int, Player> playerMap = new();
        private static ConcurrentDictionary<int, Bus> busMap = new();
        private static ConcurrentDictionary<int, Bullet> bulletMap = new();
        private static ConcurrentDictionary<Guid, Player> passengerMap = new();

        private static GameClient gameClient = null!;
        private static GameState gameState = null!;
        private static Console console = null!;

        static int Main(string[] args)
        {
            try
            {
                using var source = new CancellationTokenSource();

                System.Console.CancelKeyPress += (o, e) =>
                {
                    e.Cancel = true;
                    source.Cancel();
                    gameClient.Dispose();
                };

                var process = Process.GetProcessById(Environment.ProcessId);
                process.Exited += (e, r) =>
                {
                    gameClient.Dispose();
                    source.Cancel();
                };

                //var host = args.Length == 0 ? "manzana.local" : args[0];
                var host = args.Length == 0 ? "localhost" : args[0];
                gameClient = new GameClient();

                gameClient.StateUpdated = (state) => { gameState = state; };

                var result = gameClient.Connect(host, CommunicationConstants.TcpPort, source.Token);

                playerId = result.PlayerData.Id;
                Width = result.Layout.Width;
                Height = result.Layout.Height;
                PlayerColor = result.PlayerData.Color;

                SadConsole.Game.Create(Width, Height);

                SadConsole.Game.OnInitialize = Init;
                SadConsole.Game.OnUpdate = Update;

                SadConsole.Game.Instance.Run();
                SadConsole.Game.Instance.Dispose();

                return 0;
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
                return -1;
            }
        }

        private static void WriteCenterPosition(string text, Color foreColor, Color? backColor = default)
        {
            var width = (Width / 2) - text.Length / 2;
            var position = new Point(width, Height / 2);

            console.Print(position.X, position.Y, text, foreColor, backColor ?? Color.Transparent);
        }

        private static void Init()
        {
            console = new ScrollingConsole(Width, Height, Global.FontDefault, new Rectangle(0, 0, Width, Height));
            Global.CurrentScreen = console;
        }

        private static void UpdateTitle(string value, Color color)
        {
            console.Print(25, Height - 1, value, color);
        }

        private static async void Update(GameTime time)
        {
            if (gameState == null)
            {
                WriteCenterPosition(WaitingText, Color.White);
            }
            else
            {
                if (gameState.Status == GameStatus.Crashed)
                {
                    console.FillWithRandomGarbage();
                }
                else if (gameState.Status == GameStatus.Finished)
                {
                    console.Clear();
                    WriteCenterPosition(gameState.FinishText, Color.White);
                }
                else if (gameState.Status == GameStatus.Running)
                {
                    WriteCenterPosition(WaitingText, Color.Transparent);

                    console.Print(0, 0, "Use arrow keys and A / D / Q / E for bus control & shooting", Color.White,
                        Color.Transparent);
                    UpdateTitle("Player " + playerId, PlayerColor);

                    gameState.Sounds.ForEach(sound => { System.Console.Beep(); });

                    var playerIndex = 0;
                    gameState.Players.ToList().ForEach(playerPosition =>
                    {
                        var position = new Point(playerPosition.Value.X, playerPosition.Value.Y);
                        var color = playerPosition.Value.Color;

                        var player = playerMap.GetOrAdd(playerPosition.Key, (id) =>
                        {
                            var newPlayer = CreatePlayer(color, position);
                            console.Children.Add(newPlayer);
                            return newPlayer;
                        });
                        var scoreText = $"Player {playerPosition.Key}: {gameState.Scores[playerPosition.Key]}";
                        var scoreX = 0 + Width * playerIndex - scoreText.Length;
                        if (scoreX < 0)
                        {
                            scoreX = 0;
                        }

                        console.Print(scoreX, Height - 1, scoreText, playerPosition.Value.Color, Color.Transparent);

                        if (playerPosition.Value.Splat)
                        {
                            player.SetSplatted();
                        }
                        else
                        {
                            player.SetDefault();
                        }

                        player.SetForeground(player.Width, player.Height, color);
                        player.MoveTo(position);
                        playerIndex++;
                    });

                    gameState.Bullets.ToList().ForEach(bulletPosition =>
                    {
                        var position = new Point(bulletPosition.Value.X, bulletPosition.Value.Y);
                        var color = bulletPosition.Value.Color;

                        var bullet = bulletMap.GetOrAdd(bulletPosition.Key, (id) =>
                        {
                            var newBullet = CreateBullet(color, position);
                            console.Children.Add(newBullet);
                            return newBullet;
                        });

                        bullet.SetForeground(bullet.Width, bullet.Height, color);
                        bullet.MoveTo(position);
                    });

                    gameState.Buses.ToList().ForEach(busPosition =>
                    {
                        var position = new Point(busPosition.Value.X, busPosition.Value.Y);
                        var color = busPosition.Value.Color;

                        var bus = busMap.GetOrAdd(busPosition.Key, (id) =>
                        {
                            var newBus = CreateBus(color, position);
                            console.Children.Add(newBus);
                            return newBus;
                        });

                        bus.SetForeground(bus.Width, bus.Height, color);
                        bus.MoveTo(position);
                    });

                    gameState.Passengers.ForEach(passengersPosition =>
                    {
                        var position = new Point(passengersPosition.X, passengersPosition.Y);
                        var color = passengersPosition.Color;

                        var passenger = passengerMap.GetOrAdd(passengersPosition.Id, (id) =>
                        {
                            var newPassenger = CreatePassenger(color, position);
                            console.Children.Add(newPassenger);
                            return newPassenger;
                        });

                        passenger.SetForeground(passenger.Width, passenger.Height, color);
                        passenger.MoveTo(position);
                    });
                }

                await CheckKeyboard();
            }
        }

        private static async Task CheckKeyboard()
        {
            if (Global.KeyboardState.IsKeyReleased(Keys.F11))
            {
                Settings.ToggleFullScreen();
            }

            var tasks = Global.KeyboardState.KeysPressed.Select(input =>
            {
                return gameClient.SendAsync(new CommandMessage
                {
                    Keys = input.Key
                });
            });
            await Task.WhenAll(tasks);
        }

        private static Bus CreateBus(Color color, Point start)
        {
            var bus = new Bus(color, Color.Transparent);
            bus.Position = start;
            return bus;
        }

        private static Player CreatePlayer(Color color, Point start)
        {
            var player = new Player(color, Color.Transparent);
            player.Position = start;
            return player;
        }

        private static Player CreatePassenger(Color color, Point start)
        {
            var player = new Player(color, Color.Transparent);
            player.Position = start;
            return player;
        }

        private static Bullet CreateBullet(Color color, Point start)
        {
            var bullet = new Bullet(color, Color.Transparent);
            bullet.Position = start;
            return bullet;
        }
    }
}