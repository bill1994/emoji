using UnityEngine;
using System.Collections;

namespace Kyub
{
    [System.Serializable]
    public struct IntVector2
    {
        [SerializeField]
        int m_x;
        [SerializeField]
        int m_y;

        public int x { get { return m_x; } set { m_x = value; } }
        public int y { get { return m_y; } set { m_y = value; } }

        public IntVector2(int p_x, int p_y)
        {
            m_x = p_x;
            m_y = p_y;
        }

        public IntVector2(Vector2 p_arg)
        {
            m_x = (int)p_arg.x;
            m_y = (int)p_arg.y;
        }

        public override bool Equals(object obj)
        {
            return obj is IntVector2 && Equals((IntVector2)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(IntVector2 other)
        {
            return x == other.x && y == other.y;
        }

        public static bool operator ==(IntVector2 lhs, IntVector2 rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator ==(IntVector2 lhs, Vector2 rhs)
        {
            return lhs.Equals(new IntVector2(rhs));
        }

        public static bool operator !=(IntVector2 lhs, IntVector2 rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator !=(IntVector2 lhs, Vector2 rhs)
        {
            return !lhs.Equals(new IntVector2(rhs));
        }

        public static IntVector2 operator /(IntVector2 p_intVector, float p_number)
        {
            return new IntVector2((int)(p_intVector.x / p_number), (int)(p_intVector.y / p_number));
        }

        public static IntVector2 operator *(IntVector2 p_intVector, float p_number)
        {
            return new IntVector2((int)(p_intVector.x * p_number), (int)(p_intVector.y * p_number));
        }

        public static IntVector2 operator +(IntVector2 p_intVector, IntVector2 p_intVector2)
        {
            return new IntVector2((int)(p_intVector.x + p_intVector2.x), (int)(p_intVector.y + p_intVector2.y));
        }

        public static IntVector2 operator -(IntVector2 p_intVector, IntVector2 p_intVector2)
        {
            return new IntVector2((int)(p_intVector.x - p_intVector2.x), (int)(p_intVector.y - p_intVector2.y));
        }

        public static implicit operator Vector2(IntVector2 p_intVector)
        {
            return new Vector2(p_intVector.x, p_intVector.y);
        }

        public static implicit operator IntVector2(Vector2 p_vector)
        {
            return new IntVector2(p_vector);
        }
    }
}
