using System;
using System.Collections.Generic;
using System.Text;

namespace MazeBot
{
    public enum PacketType : byte
    { 
        WELCOME                 = 0x00,
        RECEIVE_MAZE            = 0x01,
        RECEIVE_MAZE_COMRESSED  = 0x02,
        ILLEGAL_MOVE            = 0x05,
        OUT_OF_TURN             = 0x06,
        PLAYER_MOVED            = 0x07,
        PLAYER_TURN             = 0x08,
        TOO_MANY_PLAYERS        = 0x09,
        PLAYER_JOINED           = 0x0a,
        PLAYER_LEFT             = 0x0b,
        PLAYER_WIN              = 0x0c,
        NEW_GAME                = 0x0d,
        SERVER_TERMINATED       = 0x0e

    };
}
