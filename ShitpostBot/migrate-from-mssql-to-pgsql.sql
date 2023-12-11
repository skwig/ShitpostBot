-- This script assumes the existing MSSQL Post table is exported into "Post_export" via DBeaver.

-- Image posts (Type=0)
SELECT p."Type"                                                     AS "Type",
       p."postedon"                                                 AS "PostedOn",
       p."chatguildid"                                              AS "ChatGuildId",
       p."chatchannelid"                                            AS "ChatChannelId",
       p."chatmessageid"                                            AS "ChatMessageId",
       p."posterid"                                                 AS "PosterId",
       p."trackedon"                                                AS "TrackedOn",
       p."evaluatedon"                                              AS "EvaluatedOn",
       ((p."Content"::json) -> 'Image' -> 'ImageId')::text::numeric AS "Image_ImageId",
        (p."Content"::json) -> 'Image' -> 'ImageUri'                 AS "Image_ImageUri",
       sq."Image.ImageFeatures.FeatureVector"                       AS "Image_ImageFeatures_FeatureVector"
FROM "Post_export" p
         JOIN (SELECT "id",
                      array_agg(value::text::numeric) AS "Image.ImageFeatures.FeatureVector"
               FROM "Post_export",
                    json_array_elements(("Content"::json) -> 'Image' -> 'ImageFeatures' -> 'FeatureVector') AS arr(value)
               where "Type" = 0
               GROUP BY "id"
               ORDER BY "id") sq ON sq."id" = p."id"
WHERE p."Type" = 0
ORDER BY p."postedon";

-- Link posts (Type=1)
INSERT INTO "Post" ("Type",
                    "PostedOn",
                    "ChatGuildId",
                    "ChatChannelId",
                    "ChatMessageId",
                    "PosterId",
                    "TrackedOn",
                    "EvaluatedOn",
                    "Link_LinkId",
                    "Link_LinkUri",
                    "Link_LinkProvider")
SELECT p."Type"                                                         AS "Type",
       p."postedon"                                                     AS "PostedOn",
       p."chatguildid"                                                  AS "ChatGuildId",
       p."chatchannelid"                                                AS "ChatChannelId",
       p."chatmessageid"                                                AS "ChatMessageId",
       p."posterid"                                                     AS "PosterId",
       p."trackedon"                                                    AS "TrackedOn",
       p."evaluatedon"                                                  AS "EvaluatedOn",
       ((p."Content"::json) -> 'Link' -> 'LinkId')::text::numeric       AS "Link_LinkId",
        (p."Content"::json) -> 'Link' -> 'LinkUri'                       AS "Link_LinkUri",
       ((p."Content"::json) -> 'Link' -> 'LinkProvider')::text::numeric AS "Link_LinkProvider"
FROM "Post_export" p
WHERE p."Type" = 1
ORDER BY p."postedon";