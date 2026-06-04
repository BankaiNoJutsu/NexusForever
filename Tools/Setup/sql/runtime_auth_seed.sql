-- Runtime auth seed cleanup for local NexusForever setup.
--
-- Fortune sessions are transient account state. Builds before the 2026-06-04
-- Fortune card-display guard could persist entitlement-only AccountItem ids in
-- account_fortune_session; the 16042 client then dereferenced a null card
-- display object when opening Madame Fay's Fortune page.
--
-- Keep fresh and reinitialized local auth databases out of that stale state.
START TRANSACTION;

DELETE FROM `account_fortune_session`;

COMMIT;
