SET NOCOUNT ON;
USE [RefalinaLaundry];

SELECT
    [Id],
    [UserName],
    [NormalizedUserName],
    [FullName],
    [IsActive],
    [MustChangePassword],
    [AccessFailedCount],
    [LockoutEnabled],
    [LockoutEnd],
    CAST(CASE
        WHEN [LockoutEnd] IS NOT NULL AND [LockoutEnd] > SYSDATETIMEOFFSET() THEN 1
        ELSE 0
    END AS bit) AS [IsCurrentlyLocked]
FROM [dbo].[AspNetUsers]
ORDER BY [UserName];
