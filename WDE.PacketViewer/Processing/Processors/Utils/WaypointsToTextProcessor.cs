using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Utils;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils
{
    public class WaypointsToTextProcessor : IPacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IWaypointProcessor waypointProcessor;

        public WaypointsToTextProcessor(IWaypointProcessor waypointProcessor)
        {
            this.waypointProcessor = waypointProcessor;
        }

        public bool Process(PacketHolder packet) => waypointProcessor.Process(packet);

        public async Task<string> Generate()
        {
            StringBuilder sb = new();
            Dictionary<UniversalGuid, float> randomnessMap = new();
            foreach (var unit in waypointProcessor.State)
                randomnessMap[unit.Key] = waypointProcessor.RandomMovementPacketRatio(unit.Key);

            foreach (var unit in waypointProcessor
                         .State
                         .OrderBy(pair => randomnessMap[pair.Key]))
            {
                if (unit.Key.Type == UniversalHighGuid.Player)
                    continue;
                
                if (unit.Value.Paths.Count == 0)
                    continue;

                var randomness = waypointProcessor.RandomMovementPacketRatio(unit.Key);
                sb.AppendLine("Creature " + unit.Key.ToWowParserString() + $" (entry: {unit.Key.Entry})  randomness: {(randomness) * 100:0.00}%");
                int pathId = 0;
                int segmentId = 0;
                DateTime? prevPathFinishedTime = null;
                foreach (var path in unit.Value.Paths)
                {
                    if (path.IsFirstPathAfterSpawn)
                        sb.AppendLine("    (object created, so it's his first path ever)");
                    if (!path.IsContinuationAfterPause)
                        sb.AppendLine("    (despawn) ");

                    if (prevPathFinishedTime != null && path.IsContinuationAfterPause)
                    {
                        var wait = (path.PathStartTime - prevPathFinishedTime.Value);
                        if (wait.TotalSeconds < 10)
                            sb.AppendLine($"    (after {(ulong)wait.TotalMilliseconds} ms)");
                        else
                            sb.AppendLine($"    (after {wait.ToNiceString()} [{(ulong)wait.TotalMilliseconds} ms])");
                    }

                    sb.AppendLine("* Path " + pathId++);
                    segmentId = 0;

                    foreach (var segment in path.Segments)
                    {
                        if (segment.Wait.HasValue)
                            sb.AppendLine($"    wait {(ulong)segment.Wait.Value.TotalMilliseconds} ms");
                        
                        sb.AppendLine("   Segment " + segmentId++);
                        if (segment.JumpGravity.HasValue)
                        {
                            var p = segment.Waypoints[^1];
                            sb.AppendLine($"    jump to ({p.X}, {p.Y}, {p.Z}) gravity: {segment.JumpGravity.Value} move time: {segment.MoveTime}");
                        }
                        else
                        {
                            foreach (var p in segment.Waypoints)
                            {
                                sb.AppendLine($"    ({p.X}, {p.Y}, {p.Z})");
                            }   
                        }
                    }

                    if (path.DestroysAfterPath)
                        sb.AppendLine("    (object destroyed, so this is the last point for sure)");
                    sb.AppendLine("");

                    prevPathFinishedTime = path.PathStartTime + TimeSpan.FromMilliseconds(path.TotalMoveTime);
                }
            }

            return sb.ToString();
        }
    }
}