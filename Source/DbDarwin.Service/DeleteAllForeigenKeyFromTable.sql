DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'
ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(f.parent_object_id))
    + '.' + QUOTENAME(OBJECT_NAME(f.parent_object_id)) + 
    ' DROP CONSTRAINT ' + QUOTENAME(f.name) + ';'
  FROM sys.key_constraints k
join sys.foreign_keys f on k.parent_object_id = f.referenced_object_id
where OBJECT_SCHEMA_NAME (k.parent_object_id) = N'{0}' AND OBJECT_NAME(k.parent_object_id) = N'{1}'
PRINT @sql
EXECUTE sp_executesql @sql