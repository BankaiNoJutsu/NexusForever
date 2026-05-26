using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Spell;

[Collection(LegacyServiceProviderCollection.Name)]
public class SpellTargetValidationTests
{
    [Theory]
    [InlineData(22u, 22u, 180f, false)]
    [InlineData(22u, 170u, 180f, true)]
    [InlineData(22u, 170u, 0f, false)]
    [InlineData(22u, 170u, 360f, false)]
    public void ShouldApplyPrimaryTargetAngle_SkipsSelfAnchoredTargets(uint casterGuid, uint targetGuid, float targetAngle, bool expected)
    {
        bool result = NexusForever.Game.Spell.Spell.ShouldApplyPrimaryTargetAngle(casterGuid, targetGuid, targetAngle);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SendSpellStart_AttachesCreatureTelegraphToCaster_WhenPrimaryTargetExists()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            IUnitEntity caster = CreateUnit(1001u, new Vector3(10f, 20f, 30f), out RecordingDispatchProxy<IUnitEntity> casterProxy);
            IPlayer target = CreatePlayer(2002u, new Vector3(10f, 20f, 20f));
            casterProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == target.Guid ? target : null);

            var parameters = new NexusForever.Game.Spell.SpellParameters
            {
                PrimaryTargetId = target.Guid,
                SpellInfo       = CreateSpellInfoWithConeTelegraph()
            };

            var spell = new NexusForever.Game.Spell.Spell(caster, parameters);
            InvokePrivate(spell, "InitialiseTelegraphs");
            InvokePrivate(spell, "SendSpellStart");

            RecordingDispatchProxy<IUnitEntity>.Invocation invocation = Assert.Single(casterProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)));
            ServerSpellStart spellStart = Assert.IsType<ServerSpellStart>(invocation.Arguments[0]);

            ServerSpellStart.InitialPosition initialPosition = Assert.Single(spellStart.InitialPositionData);
            ServerSpellStart.TelegraphPosition telegraphPosition = Assert.Single(spellStart.TelegraphPositionData);

            Assert.Equal(caster.Guid, initialPosition.UnitId);
            Assert.Equal(caster.Guid, telegraphPosition.AttachedUnitId);
            Assert.Equal(caster.Position, telegraphPosition.Position.Vector);
            Assert.Equal(target.Guid, spellStart.PrimaryTargetId);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static IServiceProvider BuildProvider()
    {
        IScriptCollection scriptCollection = RecordingDispatchProxy<IScriptCollection>.Create(out _);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out RecordingDispatchProxy<IScriptManager> scriptManagerProxy);
        scriptManagerProxy.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), scriptCollection);

        return new ServiceCollection()
            .AddSingleton<NexusForever.Game.Spell.GlobalSpellManager>()
            .AddSingleton(scriptManager)
            .BuildServiceProvider();
    }

    private static IUnitEntity CreateUnit(uint guid, Vector3 position, out RecordingDispatchProxy<IUnitEntity> proxy)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), guid);
        proxy.SetProperty(nameof(IUnitEntity.Position), position);
        proxy.SetProperty(nameof(IUnitEntity.Rotation), Vector3.Zero);
        proxy.SetProperty(nameof(IUnitEntity.HitRadius), 1f);
        return unit;
    }

    private static IPlayer CreatePlayer(uint guid, Vector3 position)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.Position), position);
        proxy.SetProperty(nameof(IPlayer.Rotation), Vector3.Zero);
        proxy.SetProperty(nameof(IPlayer.HitRadius), 1f);
        return player;
    }

    private static ISpellInfo CreateSpellInfoWithConeTelegraph()
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 99u });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 123u, Spell4BaseIdBaseSpell = 99u });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>
        {
            new()
            {
                Id              = 456u,
                DamageShapeEnum = (uint)DamageShape.Cone,
                Param01         = 10f,
                Param02         = 60f
            }
        });

        return spellInfo;
    }

    private static void InvokePrivate(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(instance, null);
    }
}
