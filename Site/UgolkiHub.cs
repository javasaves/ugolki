using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Runtime.CompilerServices;
using Site.Classes;

namespace Site
{
    public class UgolkiHub : Hub
    {
        private static List<User> Users = new List<User>();

        private static Dictionary<string, User> Connections = new Dictionary<string, User>();

        public static List<Room> Rooms = new List<Room>();

        public static Dictionary<string, GameView> Games = new Dictionary<string, GameView>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override Task OnConnected()
        {
            Connections.Add(Context.ConnectionId, null);
            return base.OnConnected();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override Task OnDisconnected(bool stopCalled)
        {
            try
            {
                User user = Connections[Context.ConnectionId];
                if (user != null)
                {
                    user.Connections.Remove(Context.ConnectionId);
                }
                Connections.Remove(Context.ConnectionId);
            }
            catch (Exception ex) { }
            return base.OnDisconnected(stopCalled);
        }

        public void SendMessage(string groupId, string message)
        {
            User user = Connections[Context.ConnectionId];
            Clients.Group(groupId).sendMessage(new Message() { UserName = user.Name, Text = message }, groupId);
        }

        public string Login(User user)
        {
            if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Password) || user.Password.Length < 2 || user.Name == "AI")
            {
                return string.Empty;
            }
            bool nameExists = Users.Count(x => x.Name == user.Name) > 0;
            bool passwordIsValid = Users.Count(x => x.Name == user.Name && x.Password == user.Password) > 0;
            user.Hash = "";
            if (nameExists && passwordIsValid || !nameExists)
            {
                if (nameExists)
                {
                    user = Users.First(x => x.Name == user.Name && x.Password == user.Password);
                }
                else
                {
                    user.Hash = (user.Name + user.Password).GetHashCode().ToString();
                    user.Connections = new List<string>();
                    Users.Add(user);
                }
                user.Connections.Add(Context.ConnectionId);
                Connections[Context.ConnectionId] = user;
            }
            if (!string.IsNullOrEmpty(user.Hash))
            {
                Clients.Caller.setData(Users, Rooms);
            }
            return user.Hash;
        }

        public void Logout(User user)
        {
            User user2 = Connections[Context.ConnectionId];

            if (user2.Name == user.Name)
            {
                user2.Connections.Remove(Context.ConnectionId);
                Connections[Context.ConnectionId] = null;
            }
        }

        public void CreateRoom()
        {
            User user = Connections[Context.ConnectionId];

            if (user == null) return;

            GameView gameView = new GameView();


            Room room = new Room()
            {
                RoomId = Guid.NewGuid().ToString(),
                Player1 = user,
                State = GameState.Wait,
                Game = gameView.Game,
                Ready1 = false,
                Ready2 = false
            };
            Rooms.Add(room);
            Groups.Add(Context.ConnectionId, room.RoomId);
            Clients.All.addRoom(room);
        }

        public void SelectCell(string roomId, int x, int y)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(r => r.RoomId == roomId);

            if (room.State != GameState.Move1 && room.State != GameState.Move2)
            {
                return;
            }
            if ((room.State == GameState.Move1 && room.Player1 != user) || (room.State == GameState.Move2 && room.Player2 != user))
            {
                return;
            }

            Game game = room.Game;
            int player = 0;
            if (room.Player1 == user)
            {
                player = 1;
            }
            else if (room.Player2 == user)
            {
                player = -1;
            }
            int value = game.Cells[x, y];
            if (value == 0)
            {
                if (game.MovesFromSelectedCell != null &&
                    game.MovesFromSelectedCell.Count(m => m.X == x && m.Y == y) > 0 &&
                    game.SelectedCell.X >= 0 &&
                    game.SelectedCell.Y >= 0)
                {
                    bool win1 = Board.CheckWin(game.Cells, 1);
                    Classes.Game.ApplyMove(game.Cells, new Service.Move() { Path = new[] { new Service.Vector2() { X = game.SelectedCell.X, Y = game.SelectedCell.Y }, new Service.Vector2() { X = x, Y = y } }, Player = game.Cells[game.SelectedCell.X, game.SelectedCell.Y] });

                    game.SelectedCell = new Vector2(-1, -1);
                    game.MovesFromSelectedCell = null;
                    if (room.State == GameState.Move1)
                    {
                        if (Board.CheckWin(game.Cells, player) && win1)
                        {
                            room.State = GameState.Win1;
                            room.Unready();
                        }
                        else
                        {
                            room.State = GameState.Move2;
                        }
                    }
                    else
                    {
                        if (Board.CheckWin(game.Cells, player) && win1)
                        {
                            room.State = GameState.Draw;
                            room.Unready();
                        }
                        else if(win1)
                        {
                            room.State = GameState.Win1;
                            room.Unready();
                        }
                        else if (Board.CheckWin(game.Cells, player))
                        {
                            room.State = GameState.Win2;
                            room.Unready();
                        }
                        else
                        {
                            room.State = GameState.Move1;
                        }
                    }

                    if (room.State == GameState.Move1)
                    {
                        if (room.Player1.Name == "AI")
                        {
                            Clients.Group(room.RoomId).updateRoom(room);
                            MoveAi(roomId, -player);
                        }
                    }
                    else if (room.State == GameState.Move2)
                    {
                        if (room.Player2.Name == "AI")
                        {
                            Clients.Group(room.RoomId).updateRoom(room);
                            MoveAi(roomId, -player);
                        }
                    }
                }
            }
            else if (player == value)
            {
                var selected = new Vector2(x, y);
                game.SelectedCell = selected;
                game.MovesFromSelectedCell = Board.FindMoves(game.Cells, new List<Vector2>() { selected }, value).Select(m => m.To).ToList();
            }
            Clients.Group(room.RoomId).updateRoom(room);
        }

        private void MoveAi(string roomId, int player)
        {
            var room = Rooms.First(r => r.RoomId == roomId);
            var moves = UgolkiCalculator.GetMovesLocal(room.Game.Cells, player, 4, Classes.Game.GetPlayerPoint(player), -player, true);
            //var moves = UgolkiCalculator.GetMovesMsmq(room.Game.Cells, player, 4, Classes.Game.GetPlayerPoint(player), -player, true);
            //var moves = UgolkiCalculator.GetMovesMsmq(room.Game.Cells, player, 4, Classes.Game.GetPlayerPoint(player), -player, true);
            Service.Move move;
            bool win1 = Board.CheckWin(room.Game.Cells, 1);
            if (moves.Count > 0)
            {
                double min = moves.Min(x => x.Value);
                
                move = moves.First(m => m.Value == min).Key;
                
                Classes.Game.ApplyMove(room.Game.Cells, move);
            }
            if (room.State == GameState.Move1)
            {
                if (Board.CheckWin(room.Game.Cells, player) && win1)
                {
                    room.State = GameState.Win1;
                    room.Unready();
                }
                else
                {
                    room.State = GameState.Move2;
                }
            }
            else
            {
                if (Board.CheckWin(room.Game.Cells, player) && win1)
                {
                    room.State = GameState.Draw;
                    room.Unready();
                }
                else if (win1)
                {
                    room.State = GameState.Win1;
                    room.Unready();
                }
                else if (Board.CheckWin(room.Game.Cells, player))
                {
                    room.State = GameState.Win2;
                    room.Unready();
                }
                else
                {
                    room.State = GameState.Move1;
                }
            }
            Clients.Group(room.RoomId).updateRoom(room);
        }

        public void Join(string roomId)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);

            Groups.Add(Context.ConnectionId, roomId);
            if (room.Player1 == user || room.Player2 == user || room.Observers.Contains(user))
            {
                return;
            }

            if (room.Player1 == null)
            {
                room.Player1 = user;
                room.Unready();
                room.SetWait();
            }
            else if (room.Player2 == null)
            {
                room.Player2 = user;
                room.Unready();
                room.SetWait();
            }
            else
            {
                room.Observers.Add(user);
            }

            Clients.All.updateRoom(room);
        }

        public void AddAi(string roomId)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);

            if ((room.Player1 == null && room.Player2 == user) || (room.Player1 == user && room.Player2 == null))
            {
                if (room.Player1 == null && room.Player2 == user)
                {
                    room.Player1 = User.AI;
                }
                else if (room.Player1 == user && room.Player2 == null)
                {
                    room.Player2 = User.AI;
                }
                room.Unready();
                room.SetWait();
                Clients.All.updateRoom(room);

                Service.UgolkiServiceClient client = new Service.UgolkiServiceClient(new System.ServiceModel.InstanceContext(UgolkiServiceCallback.Callback));
                int count = client.GetClientsCount();
                Clients.Group(room.RoomId).setClientsCount(room.RoomId, count);

            }

        }

        public void RefreshClients(string roomId)
        {
            var room = Rooms.First(x => x.RoomId == roomId);
            Service.UgolkiServiceClient client = new Service.UgolkiServiceClient(new System.ServiceModel.InstanceContext(UgolkiServiceCallback.Callback));
            int count = client.GetClientsCount();
            Clients.Group(room.RoomId).setClientsCount(room.RoomId, count);
        }

        public void RemoveAi(string roomId)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);

            if (((room.Player1.Name == "AI" && room.Player2 == user) || (room.Player1 == user && room.Player2.Name == "AI")) && room.State != GameState.Move1 && room.State != GameState.Move2)
            {
                if (room.Player1.Name == "AI" && room.Player2 == user)
                {
                    room.Player1 = null;
                }
                else if (room.Player1 == user && room.Player2.Name == "AI")
                {
                    room.Player2 = null;
                }
                room.Unready();
                room.SetWait();
                Clients.All.updateRoom(room);
            }

        }

        public void Exit(string roomId)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);

            if (room.Player1 == user)
            {
                room.Player1 = null;
                room.Unready();
                if (room.State == GameState.Move1 || room.State == GameState.Move2)
                {
                    room.State = GameState.Win2;
                }
            }
            else if (room.Player2 == user)
            {
                room.Player2 = null;
                room.Unready();
                if (room.State == GameState.Move1 || room.State == GameState.Move2)
                {
                    room.State = GameState.Win1;
                }
            }
            else
            {
                room.Observers.Remove(user);
            }

            Groups.Remove(Context.ConnectionId, roomId);
            if ((room.Player1 == null || room.Player1.Name == "AI") && (room.Player2 == null || room.Player2.Name == "AI") && (room.Observers == null || room.Observers.Count == 0))
            {
                Clients.All.removeRoom(room);
            }
            else
            {
                Clients.All.updateRoom(room);
            }
        }

        public void SetObserver(string roomId)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);

            if (room.State != GameState.Move1 && room.State != GameState.Move2)
            {
                if (room.Player1 == user)
                {
                    room.Player1 = null;
                    room.Observers.Add(user);
                    room.Unready();
                }
                else if (room.Player2 == user)
                {
                    room.Player2 = null;
                    room.Observers.Add(user);
                    room.Unready();
                }

                Clients.All.updateRoom(room);
            }
        }

        public void SetPlayer(string roomId)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);

            if (room.State != GameState.Move1 && room.State != GameState.Move2)
            {
                if (room.Player1 == null)
                {
                    room.Observers.Remove(user);
                    room.Player1 = user;
                    room.Unready();
                    room.SetWait();
                }
                else if (room.Player2 == null)
                {
                    room.Observers.Remove(user);
                    room.Player2 = user;
                    room.Unready();
                    room.SetWait();
                }

                Clients.All.updateRoom(room);
            }
        }

        public void Ready(string roomId, bool value = true)
        {
            User user = Connections[Context.ConnectionId];
            var room = Rooms.First(x => x.RoomId == roomId);
            if (room.Player1 == user || room.Player2 == user)
            {
                if (room.Player1 == user)
                {
                    room.Ready1 = value;
                    if (room.Player2 != null && room.Player2.Name == "AI")
                    {
                        room.Ready2 = value;
                    }
                }
                else if (room.Player2 == user)
                {
                    room.Ready2 = value;
                    if (room.Player1 != null && room.Player1.Name == "AI")
                    {
                        room.Ready1 = value;
                    }
                }
                if (room.Ready1 && room.Ready2)
                {
                    room.Start();
                }
                if (room.State == GameState.Move1 && room.Player1.Name == "AI")
                {
                    MoveAi(roomId, 1);
                }
                else
                {
                    Clients.All.updateRoom(room);
                }
            }
        }

        public List<Room> GetRooms()
        {
            return Rooms;
        }

        public List<User> GetUsers()
        {
            return Users;
        }
    }

    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string Hash { get; set; }
        public List<string> Connections { get; set; } = new List<string>();

        public static User AI { get; private set; }

        static User()
        {
            AI = new User() { Name = "AI" };
        }
    }

    public class Room
    {
        public string RoomId { get; set; }
        public List<User> Observers { get; private set; } = new List<User>();
        public User Player1 { get; set; }
        public User Player2 { get; set; }
        public GameState State { get; set; }
        public Game Game { get; set; }
        public bool Ready1 { get; set; }
        public bool Ready2 { get; set; }

        public void CheckWin(int player1, int player2)
        {

        }

        public void Unready()
        {
            Ready1 = false;
            Ready2 = false;
        }

        public void SetWait()
        {
            if (State == GameState.Win1 || State == GameState.Win2)
            {
                State = GameState.Wait;
                Game.Cells = Board.StartCells();
            }
        }

        public void Start()
        {
            State = GameState.Move1;
        }
    }

    public class Game
    {
        public int[,] Cells { get; set; }
        public Vector2 SelectedCell { get; set; }
        public List<Vector2> MovesFromSelectedCell { get; set; }
    }

    public class GameView
    {
        public Board Board { get; set; }

        public Game Game { get; set; }

        public GameView()
        {
            Board = new Board();
            Game = new Game()
            {
                Cells = Board.Cells,
                SelectedCell = new Vector2(-1, -1)
            };
        }
    }

    public enum GameState { Wait, Move1, Move2, Win1, Win2, Draw }

    public class Message
    {
        public string UserName { get; set; }
        public string Text { get; set; }
    }
}