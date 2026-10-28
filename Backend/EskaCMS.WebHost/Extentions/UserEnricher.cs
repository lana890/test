// Removed: UserEnricher was unused (never wired into the Serilog pipeline) and buggy — it
// dereferenced a possibly-null NameIdentifier claim (NullReferenceException on anonymous
// requests) and always overwrote "UserName" with "". Emptied for clarity; safe to delete this file.
