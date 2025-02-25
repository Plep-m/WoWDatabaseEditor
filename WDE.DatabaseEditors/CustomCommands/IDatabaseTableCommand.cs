using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.CustomCommands
{
    [NonUniqueProvider]
    public interface IDatabaseTableCommand
    {
        ImageUri Icon { get; }
        string Name { get; }
        string CommandId { get; }
        Task Process(DatabaseCommandDefinitionJson definition, IDatabaseTableData tableData, IAddRowKey addRow);
    }
    
    [NonUniqueProvider]
    public interface IDatabaseTablePerKeyCommand
    {
        ImageUri Icon { get; }
        string Name { get; }
        string CommandId { get; }
        Task Process(DatabaseCommandDefinitionJson definition, IDatabaseTableData tableData, ICollection<DatabaseKey> keys, IAddRowKey addRow);
    }
}