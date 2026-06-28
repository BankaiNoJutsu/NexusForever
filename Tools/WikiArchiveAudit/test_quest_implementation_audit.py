import shutil
import tempfile
import unittest
from pathlib import Path

import quest_implementation_audit


class QuestImplementationAuditTests(unittest.TestCase):
    def test_script_quality_is_scoped_to_each_script_filter_owner_block(self) -> None:
        script_root = Path(tempfile.mkdtemp(prefix="nf-quest-audit-"))
        try:
            quest_dir = script_root / "Quests"
            quest_dir.mkdir(parents=True)
            (quest_dir / "MixedQuestScripts.cs").write_text(
                """
using NexusForever.Script;

namespace NexusForever.Script.Main.Quests;

[ScriptFilterOwnerId(100u)]
public class StubOnlyQuestScript
{
    public void OnState()
    {
        logger.Info("observed only");
    }
}

[ScriptFilterOwnerId(101u)]
public class FollowUpQuestScriptImpl : FollowUpQuestScript<FollowUpQuestScriptImpl>
{
    protected override ushort NextQuestId => 102;
}

[ScriptFilterOwnerId(5593u)]
public class SharedBranchQuestScript
{
    public void Complete(IQuest owner)
    {
        CrimsonIsleQuestChain.GrantQuestsIfMissing(owner, globalQuestManager, log, 5573, 8855);
    }
}

[ScriptFilterOwnerId(10546u)]
public class TerminalPathObserverQuestScript
{
    public void OnQuestStateChange(QuestState next, QuestState previous)
    {
        log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, previous, next);
    }
}

[ScriptFilterOwnerId(200u, 201u)]
public class SharedObjectiveQuestScript
{
    public void Complete(IQuest owner)
    {
        owner.ObjectiveUpdate(300u, 1u);
    }
}
""".lstrip(),
                encoding="utf-8",
            )

            self.assertEqual(
                {100, 101, 200, 201, 5593, 10546},
                quest_implementation_audit.load_script_ids(script_root),
            )
            self.assertEqual(
                {100},
                quest_implementation_audit.load_script_quality(script_root),
            )
            self.assertEqual(
                "curated_full",
                quest_implementation_audit.classify(
                    {"id": 5593, "objective_ids": (8247,)},
                    {8247: {"type": 5}},
                    {5593},
                    set(),
                ),
            )
            self.assertEqual(
                "generic_with_script",
                quest_implementation_audit.classify(
                    {"id": 10546, "objective_ids": (21389,)},
                    {21389: {"type": 5}},
                    {10546},
                    set(),
                ),
            )
        finally:
            shutil.rmtree(script_root, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
