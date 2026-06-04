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
                {100, 101, 200, 201},
                quest_implementation_audit.load_script_ids(script_root),
            )
            self.assertEqual(
                {100},
                quest_implementation_audit.load_script_quality(script_root),
            )
        finally:
            shutil.rmtree(script_root, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
