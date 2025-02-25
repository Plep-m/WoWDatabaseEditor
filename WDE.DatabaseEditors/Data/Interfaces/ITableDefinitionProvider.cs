﻿using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data.Interfaces
{
    [UniqueProvider]
    public interface ITableDefinitionProvider
    {
        DatabaseTableDefinitionJson? GetDefinition(string? definitionId);
        DatabaseTableDefinitionJson? GetDefinitionByTableName(string? tableName);
        DatabaseTableDefinitionJson? GetDefinitionByForeignTableName(string? tableName);
        IEnumerable<string>? CoreCompatibility(string definitionId);
        IEnumerable<DatabaseTableDefinitionJson> Definitions { get; }
        IEnumerable<DatabaseTableDefinitionJson> IncompatibleDefinitions { get; }
        
        IEnumerable<DatabaseTableDefinitionJson> AllDefinitions { get; }
    }
}