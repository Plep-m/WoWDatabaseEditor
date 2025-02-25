﻿namespace TheEngine.Handles
{
    public struct MeshHandle
    {
        internal int Handle { get; }

        internal MeshHandle(int id)
        {
            Handle = id;
        }

        public bool Equals(MeshHandle other)
        {
            return Handle == other.Handle;
        }

        public override bool Equals(object? obj)
        {
            return obj is MeshHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Handle;
        }

        public static bool operator ==(MeshHandle left, MeshHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MeshHandle left, MeshHandle right)
        {
            return !left.Equals(right);
        }
    }
    
}
