using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Welcome
/// +=1==+=1============+=1=================+
/// |0x00| player(byte) | max players(byte) |
/// +====+==============+===================+
/// The server will welcome the new client. Contains
/// the new id for the client, and the maximum allowed
/// players
/// 
/// ----------------------------------------------
/// Receive Maze
/// +=1==+=1====+=1=====+=1======+=size - 2=~~~=====+
/// |0x01| size | width | height |    maze data     |
/// +====+======+=======+========+==========~~~=====+
/// Message telling us there is a new maze. First byte
/// is the size of the whole payload. Next two bytes are 
/// the width and height of the maze. The remaining size - 2
/// bytes are the map of the maze.
/// 
/// Maze tiles are using the lower 4 bits to denote whether or not
/// a side is open. A bit of 1 means that side is open
/// 1000 => Bottom side open
/// 0100 => Left side open
/// 0010 => Top side open
/// 0001 => Right side open
/// 
/// ----------------------------------------------
/// Illegal move
/// +=1==+
/// |0x05|
/// +====+
/// The last move we made was illegal
/// 
/// ----------------------------------------------
/// Out of turn
/// +=1==+
/// |0x06|
/// +====+
/// 
/// ----------------------------------------------
/// Player moved
/// +=1==+=1======+=1======+=1====+
/// |0x07| player | column |  row |
/// +====+========+========+======+
/// Informs us that a player has been moved. First
/// byte is which player, followed by their new 
/// column and row
/// 
/// ----------------------------------------------
/// Player turn
/// +=1==+=1======+
/// |0x08| player |
/// +====+========+
/// Informs us which player's turn it is.
/// 
/// ----------------------------------------------
/// Too many players
/// +=1==+
/// |0x09|
/// +====+
/// 
/// ----------------------------------------------
/// Player joined
/// +=1==+=1======+
/// |0x0a| player |
/// +====+========+
/// Informs us that a player joined, and their new id
/// 
/// ----------------------------------------------
/// Player left
/// +=1==+=1======+
/// |0x0b| player |
/// +====+========+
/// Informs us that a player left, and their id
/// 
/// ----------------------------------------------
/// Player win
/// +=1==+=1======+
/// |0x0c| player |
/// +====+========+
/// Informs us that a player has won
/// 
/// ----------------------------------------------
/// New game
/// +=1==+
/// |0x0d|
/// +====+
/// A new game is about to start
/// 
/// +---+---+---+---+
/// | 2 | 1 | 3 |   |
/// 
/// ----------------------------------------------
/// Server terminated
/// +=1==+
/// |0x0e|
/// +====+
/// The server has terminated the session
/// ----------------------------------------------
/// </summary>
namespace MazeBot {
    public struct Player {
        public bool InGame;
        public sbyte Column;
        public sbyte Row;
    }

    public enum Move : byte {
        Right = 0x0f,
        Up = 0x10,
        Left = 0x11,
        Down = 0x12,
        JumpRight = 0x13,
        JumpUp = 0x14,
        JumpLeft = 0x15,
        JumpDown = 0x16,
    }

    class Program {
        static Random r = new Random();
        static Socket socket;
        static byte[] buffer;
        static bool connected = false;
        static bool shouldPlay = false;


        static Player[] players = null;
        static Maze theMaze = null;


        static int MyPlayer;
        static int MaxPlayers;

        static void Main(string[] args) {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            buffer = new byte[4096];

            socket.Connect("127.0.0.1", 8008);
            connected = true;

            while (connected) {

                socket.Receive(buffer, 0, sizeof(byte), SocketFlags.None);

                PacketType type = (PacketType)buffer[0];

                switch (type) {
                    case PacketType.WELCOME: {
                        socket.Receive(buffer, 1, 2 * sizeof(byte), SocketFlags.None);
                        MyPlayer = buffer[1];
                        MaxPlayers = buffer[2];
                        players = new Player[MaxPlayers];
                        shouldPlay = true;

                        // We set all the player states up to an including ours explicitly to in game
                        for (int i = 0; i <= MyPlayer; i++) {
                            players[i].InGame = true;
                        }
                        Console.WriteLine($"Welcome player {MyPlayer + 1} of {MaxPlayers}");
                        break;
                    }
                    case PacketType.RECEIVE_MAZE: {
                        socket.Receive(buffer, 1, sizeof(short), SocketFlags.None);
                        // Packet size includes 2 bytes for the dimensions, in addition to the 
                        // raw data
                        short packetSize = BitConverter.ToInt16(buffer, 1);

                        socket.Receive(buffer, 3, packetSize, SocketFlags.None);
                        theMaze = ParseMaze(buffer);
                        break;
                    }
                    case PacketType.ILLEGAL_MOVE: {
                        Console.WriteLine("Illegal move!");
                        break;
                    }
                    case PacketType.OUT_OF_TURN: {
                        Console.WriteLine("It was not our turn. Oops!");
                        break;
                    }
                    case PacketType.PLAYER_MOVED: {
                        socket.Receive(buffer, 1, 3 * sizeof(byte), SocketFlags.None);
                        var index = buffer[1];
                        players[index].Column = (sbyte)buffer[2];
                        players[index].Row = (sbyte)buffer[3];
                        break;
                    }
                    case PacketType.PLAYER_TURN: {
                        // Whose turn is it anyway?
                        socket.Receive(buffer, 1, sizeof(byte), SocketFlags.None);
                        var player = buffer[1];
                        Console.Write($"It is player {player + 1}'s turn.");

                        if (player == MyPlayer && shouldPlay) {
                            Console.Write("(This is us!)\n");
                            byte[] bytes = new byte[1];
                            Move move = CalculateMove();
                            bytes[0] = (byte)move;
                            Console.WriteLine($"Calculated move: {move}");
                            socket.Send(bytes, 1, SocketFlags.None);
                        }
                        else {
                            Console.Write("\n");
                        }

                        break;
                    }
                    case PacketType.TOO_MANY_PLAYERS:
                        Console.WriteLine("Too many players. Bot cannot play!");
                        return;
                    case PacketType.PLAYER_JOINED: {
                        socket.Receive(buffer, 1, sizeof(byte), SocketFlags.None);
                        var index = buffer[1];
                        players[index].InGame = true;
                        Console.WriteLine($"Player {index + 1} has joined");
                        break;
                    }
                    case PacketType.PLAYER_LEFT: {
                        socket.Receive(buffer, 1, sizeof(byte), SocketFlags.None);
                        var index = buffer[1];
                        players[index].InGame = false;
                        Console.WriteLine($"Player {index + 1} has left");
                        break;
                    }
                    case PacketType.PLAYER_WIN: {
                        socket.Receive(buffer, 1, sizeof(byte), SocketFlags.None);

                        if (buffer[1] == MyPlayer) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("We won!");
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine($"Player {buffer[1] + 1} won");
                        }
                        Console.ResetColor();
                        shouldPlay = false;
                        break;
                    }
                    case PacketType.NEW_GAME: {
                        Console.WriteLine("Starting a new game");
                        shouldPlay = true;
                        break;
                    }

                    default:
                        Console.WriteLine($"Unrecognized server message: {type}");
                        socket.Close();
                        return;
                } // switch (type)

            } //  while (connected)
        }


        static Move CalculateMove() {
            Player me = players[MyPlayer];

            // First priority is to go right, IF there are no players there.
            if (me.Column < theMaze.Width - 1 && !IsPlayer(me.Column + 1, me.Row)) {
                if (theMaze.IsWall(WallType.Right, me.Column, me.Row))
                    return Move.JumpRight;
                else
                    return Move.Right;
            }

            // Second priority is to go down
            if (me.Row < theMaze.Height - 1 && !IsPlayer(me.Column, me.Row + 1)) {
                if (theMaze.IsWall(WallType.Bottom, me.Column, me.Row))
                    return Move.JumpDown;
                else
                    return Move.Down;
            }

            // Things are getting weird. Is some player trying to block us from moving?
            // Let's go left
            if (me.Column > 0 && !IsPlayer(me.Column - 1, me.Row)) {
                if (theMaze.IsWall(WallType.Left, me.Column, me.Row))
                    return Move.JumpLeft;
                else
                    return Move.Left;
            }

            if (me.Row > 0 && !IsPlayer(me.Column, me.Row - 1)) {
                if (theMaze.IsWall(WallType.Top, me.Column, me.Row))
                    return Move.JumpUp;
                else
                    return Move.Up;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Have we been boxed? Attempting an invalid move to wait in place");
            Console.ResetColor();

            var v = Enum.GetValues(typeof(Move));
            return (Move)v.GetValue(r.Next(v.Length));
        }

        static bool IsPlayer(int x, int y) {
            for (int i = 0; i < players.Length; i++) {
                if (players[i].Column == x && players[i].Row == y && players[i].InGame)
                    return true;
            }
            return false;
        }

        static Maze ParseMaze(byte[] buffer) {
            byte width = buffer[3];
            byte height = buffer[4];
            var bufferCopy = new byte[width * height];

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    var index = y * width + x;
                    bufferCopy[index] = buffer[5 + index];
                }
            }

            var maze = new Maze(bufferCopy, width, height);
            return maze;
        }
    }
}
