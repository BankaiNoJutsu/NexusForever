namespace NexusForever.Game.Static.Pet
{
    public enum PetStance
    {
        Aggressive = 1,
        Defensive  = 2,
        Passive    = 3,
        Assist     = 4,
        Stay       = 5
    }

    public static class PetStanceEncoding
    {
        public const uint AssistMask     = 0x01;
        public const uint StayMask       = 0x02;
        public const uint PassiveMask    = 0x04;
        public const uint DefensiveMask  = 0x08;
        public const uint AggressiveMask = 0x10;

        public static uint ToWireMask(PetStance stance)
        {
            return stance switch
            {
                PetStance.Aggressive => AggressiveMask,
                PetStance.Defensive  => DefensiveMask,
                PetStance.Passive    => PassiveMask,
                PetStance.Assist     => AssistMask,
                PetStance.Stay       => StayMask,
                _                    => 0u
            };
        }

        public static PetStance FromWireMask(uint stanceMask)
        {
            return stanceMask switch
            {
                AggressiveMask => PetStance.Aggressive,
                DefensiveMask  => PetStance.Defensive,
                PassiveMask    => PetStance.Passive,
                AssistMask     => PetStance.Assist,
                StayMask       => PetStance.Stay,
                _              => default
            };
        }
    }
}
