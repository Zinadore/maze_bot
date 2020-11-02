using System;
using System.Collections.Generic;
using System.Text;

namespace MazeBot
{
    [Flags]
    public enum WallType: byte
    {
        Right   = 0b00000001,
        Top     = 0b00000010,
        Left    = 0b00000100,
        Bottom  = 0b00001000
    }


    public class Maze
    {
        public int Width => m_Width;
        public int Height => m_Height;

        public Maze(byte[] buffer, int width, int height)
        {
            m_Board = buffer;
            m_Width = width;
            m_Height = height;
        }

        public bool IsWall(WallType type, int x, int y)
        {
            if (x < 0 || x > m_Width)
                return true;
            if (y < 0 || y > m_Height)
                return true;

            var index = y * m_Width + x;
            var cellType = (WallType)m_Board[index];
            return  !((cellType & type) == type);
        }

        public bool InBounds(int x, int y) {
            return x >= 0
                && x < m_Width
                && y >= 0
                && y < m_Height;
        }

        private byte[] m_Board;
        private int m_Width;
        private int m_Height;

    }
}
