-- Runtime auth seed cleanup for local NexusForever setup.
--
-- Fortune sessions are transient account state. Builds before the 2026-06-04
-- Fortune card-display guard could persist entitlement-only AccountItem ids in
-- account_fortune_session; the 16042 client then dereferenced a null card
-- display object when opening Madame Fay's Fortune page.
--
-- Keep fresh and reinitialized local auth databases out of that stale state.
--
-- Baseline local accounts should not be treated as retail trial accounts by
-- the client. Seed the non-trial economy/social entitlements while preserving
-- any higher existing amounts.
START TRANSACTION;

INSERT INTO `account_entitlement` (`id`, `entitlementId`, `amount`)
SELECT `account`.`id`, 15, 1
FROM `account`
WHERE NOT EXISTS (
  SELECT 1
  FROM `account_entitlement`
  WHERE `account_entitlement`.`id` = `account`.`id`
    AND `account_entitlement`.`entitlementId` = 15
);

INSERT INTO `account_entitlement` (`id`, `entitlementId`, `amount`)
SELECT `account`.`id`, 17, 1
FROM `account`
WHERE NOT EXISTS (
  SELECT 1
  FROM `account_entitlement`
  WHERE `account_entitlement`.`id` = `account`.`id`
    AND `account_entitlement`.`entitlementId` = 17
);

UPDATE `account_entitlement`
SET `amount` = 1
WHERE `entitlementId` IN (15, 17)
  AND `amount` = 0;

DELETE FROM `account_fortune_session`;

COMMIT;
