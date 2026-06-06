using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Shared;

namespace NexusForever.Game.Spell
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameSpell(this IServiceCollection sc)
        {
            sc.AddTransientFactory<ISpellParameters, SpellParameters>();
            sc.AddSingleton<ISpellEffectDependencyResolver, SpellEffectDependencyResolver>();
            sc.AddSingleton(sp => new GlobalSpellManager(sp.GetRequiredService<ISpellEffectDependencyResolver>()));
            sc.AddSingleton<IGlobalSpellManager>(sp => sp.GetRequiredService<GlobalSpellManager>());
        }
    }
}
