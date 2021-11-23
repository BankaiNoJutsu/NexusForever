﻿using NexusForever.WorldServer.Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusForever.WorldServer.Script.Map
{
    [Script(870)]
    public class CrimsonIsle : MapScript
    {
        static uint Q5596_QOBJ_CRASH_SITE_ZONEID = 1611;

        public override void OnEnterZone(Player player, uint zoneId)
        {
            if (zoneId == Q5596_QOBJ_CRASH_SITE_ZONEID)
                if (player.QuestManager.GetQuestState(5596) == Game.Quest.Static.QuestState.Accepted)
                    player.QuestManager.ObjectiveUpdate(8255, 1u);
        }
    }
}
