using System;
using System.Numerics;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Game.Crafting
{
    public readonly struct CraftingDiscoveryEvaluation
    {
        public CraftingDiscoveryEvaluation(CraftingDiscovery result, CraftingDirection direction, float distance)
        {
            Result    = result;
            Direction = direction;
            Distance  = distance;
        }

        public CraftingDiscovery Result { get; }
        public CraftingDirection Direction { get; }
        public float Distance { get; }
    }

    /// <summary>
    /// Coordinate-discovery UI thresholds derived from <c>Lua_Crafting_AddCoordinateDiscoveryInfo</c> (0x14059e9a0)
    /// and <c>Crafting_GetProfessionModifierValue</c> (0x1405e6140). Attempt coordinate parsing is diagnostic
    /// until native producer or live-capture evidence maps the authoritative server bridge.
    /// </summary>
    public static class CraftingDiscoveryEvaluator
    {
        public const uint DiscoverableSchematicFlag = 2u;

        public static bool IsCoordinateDiscoverySchematic(TradeskillSchematic2Entry schematic)
        {
            return schematic != null && (schematic.Flags & DiscoverableSchematicFlag) != 0u;
        }

        public static bool TryParseDiagnosticAttemptCoordinates(CraftStats craftStats, uint apSpSplitDelta, out Vector2 attempt)
        {
            if (craftStats == null)
            {
                attempt = default;
                return false;
            }

            if (apSpSplitDelta != 0u)
            {
                attempt = new Vector2(
                    BitConverter.Int32BitsToSingle((int)apSpSplitDelta),
                    BitConverter.Int32BitsToSingle((int)craftStats.CircuitComplete));

                if (attempt != default)
                    return true;
            }

            if (craftStats.CircuitComplete != 0u)
            {
                attempt = new Vector2(
                    craftStats.CircuitComplete & 0xFFFFu,
                    craftStats.CircuitComplete >> 16);

                return true;
            }

            attempt = default;
            return false;
        }

        public static CraftingDiscoveryEvaluation Evaluate(
            TradeskillSchematic2Entry schematic,
            Vector2 attempt,
            float discoveryRadius1Multiplier = 1f,
            float discoveryRadius2Multiplier = 1f)
        {
            Vector2 target = new(schematic.VectorX, schematic.VectorY);
            float distance = Vector2.Distance(attempt, target);

            float hotRadius = discoveryRadius1Multiplier * schematic.Radius;
            if (hotRadius <= 0f)
                hotRadius = schematic.DiscoverableRadius;

            float warmRadius = hotRadius + (discoveryRadius2Multiplier * schematic.DiscoverableRadius);
            if (warmRadius < hotRadius)
                warmRadius = hotRadius;

            CraftingDiscovery result;
            if (distance <= schematic.CritRadius)
                result = CraftingDiscovery.Success;
            else if (distance <= hotRadius)
                result = CraftingDiscovery.Hot;
            else if (distance <= warmRadius)
                result = CraftingDiscovery.Warm;
            else
                result = CraftingDiscovery.Cold;

            return new CraftingDiscoveryEvaluation(result, ResolveDirection(attempt, target), distance);
        }

        public static CraftingDirection ResolveDirection(Vector2 attempt, Vector2 target)
        {
            Vector2 delta = attempt - target;
            if (delta.LengthSquared() <= float.Epsilon)
                return CraftingDirection.None;

            // Client finish packet carries a 4-bit direction hint for the discovery UI.
            float angle = MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI);
            if (angle < 0f)
                angle += 360f;

            int octant = (int)MathF.Floor((angle + 22.5f) / 45f) % 8;
            return octant switch
            {
                0 => CraftingDirection.E,
                1 => CraftingDirection.SE,
                2 => CraftingDirection.S,
                3 => CraftingDirection.SW,
                4 => CraftingDirection.W,
                5 => CraftingDirection.NW,
                6 => CraftingDirection.N,
                _ => CraftingDirection.NE,
            };
        }
    }
}
