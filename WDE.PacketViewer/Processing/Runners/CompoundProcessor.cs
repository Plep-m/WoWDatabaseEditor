using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Runners
{
    public abstract class CompoundProcessor<T, R1> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T>
    {
        private readonly R1 r1;

        protected CompoundProcessor(R1 r1)
        {
            this.r1 = r1;
        }

        public bool PreProcess(PacketHolder packet)
        {
            r1.Process(packet);
            return true;
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> where R2 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;

        protected CompoundProcessor(R1 r1, R2 r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }

        public bool PreProcess(PacketHolder packet)
        {
            r1.Process(packet);
            r2.Process(packet);
            return true;
        }
    }
    public abstract class CompoundProcessor<T, R1, R2, R3> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
        }

        public bool PreProcess(PacketHolder packet)
        {
            r1.Process(packet);
            r2.Process(packet);
            r3.Process(packet);
            return true;
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2, R3, R4> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
        where R4 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;
        private readonly R4 r4;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3, R4 r4)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
            this.r4 = r4;
        }

        public bool PreProcess(PacketHolder packet)
        {
            r1.Process(packet);
            r2.Process(packet);
            r3.Process(packet);
            r4.Process(packet);
            return true;
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2, R3, R4, R5> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
        where R4 : IPacketProcessor<T>
        where R5 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;
        private readonly R4 r4;
        private readonly R5 r5;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3, R4 r4, R5 r5)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
            this.r4 = r4;
            this.r5 = r5;
        }

        public bool PreProcess(PacketHolder packet)
        {
            r1.Process(packet);
            r2.Process(packet);
            r3.Process(packet);
            r4.Process(packet);
            r5.Process(packet);
            return true;
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2, R3, R4, R5, R6> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
        where R4 : IPacketProcessor<T>
        where R5 : IPacketProcessor<T>
        where R6 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;
        private readonly R4 r4;
        private readonly R5 r5;
        private readonly R6 r6;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3, R4 r4, R5 r5, R6 r6)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
            this.r4 = r4;
            this.r5 = r5;
            this.r6 = r6;
        }

        public bool PreProcess(PacketHolder packet)
        {
            r1.Process(packet);
            r2.Process(packet);
            r3.Process(packet);
            r4.Process(packet);
            r5.Process(packet);
            r6.Process(packet);
            return true;
        }
    }
}