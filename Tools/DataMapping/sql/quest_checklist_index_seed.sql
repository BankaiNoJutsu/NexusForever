-- Quest checklist indices for the tutorial cryopod NPCs (creatures 73422 and 73662, world 3460).
-- Hand-written, not generated.
--
-- Quests 10525 and 10526 ask the player to talk to two NPCs of each target group (objectives 21319, 21332, 21378, 21379; TalkToTargetGroup, count 2).
-- The client treats TalkToTargetGroup as a checklist: progress is one bit per creature checklist index, complete when two bits are set
-- (Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md). The imported spawns of both creatures all carry index 0, so the objective could never complete.
-- Other retail spawn groups of a single creature id carry distinct indices (creature 23379: 0 to 3).
--
-- GUESSED: the retail indices of these spawns are not known. Indices are assigned in entity id order. Any distinct assignment satisfies the objective.
-- Idempotent: a spawn is only touched while it still belongs to the creature and the creature's spawns are not already distinct.

USE `nexus_forever_world`;
START TRANSACTION;

SET @nf_fix_73422 := (SELECT COUNT(DISTINCT `questChecklistIdx`) = 1 AND COUNT(*) > 1 FROM `entity` WHERE `creature` = 73422 AND `world` = 3460);
SET @nf_fix_73662 := (SELECT COUNT(DISTINCT `questChecklistIdx`) = 1 AND COUNT(*) > 1 FROM `entity` WHERE `creature` = 73662 AND `world` = 3460);

UPDATE `entity` e
JOIN (SELECT `id`, ROW_NUMBER() OVER (ORDER BY `id`) - 1 AS idx FROM `entity` WHERE `creature` = 73422 AND `world` = 3460) n ON n.`id` = e.`id`
SET e.`questChecklistIdx` = n.idx
WHERE @nf_fix_73422 = 1 AND n.idx < 32;

UPDATE `entity` e
JOIN (SELECT `id`, ROW_NUMBER() OVER (ORDER BY `id`) - 1 AS idx FROM `entity` WHERE `creature` = 73662 AND `world` = 3460) n ON n.`id` = e.`id`
SET e.`questChecklistIdx` = n.idx
WHERE @nf_fix_73662 = 1 AND n.idx < 32;

COMMIT;
