using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Character;

namespace NexusForever.Game.Character
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameCharacter(this IServiceCollection sc)
        {
            sc.AddSingleton<ICharacterManager, CharacterManager>();
        }
    }
}
