using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityMasterDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateMaster> CreatureTemplate => GetTable<MySqlCreatureTemplateMaster>();
    public ITable<MySqlCreatureCata> Creature => GetTable<MySqlCreatureCata>();
    public ITable<MySqlBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => GetTable<TrinityString>();
    public ITable<TrinityMasterMySqlServersideSpell> SpellDbc => GetTable<TrinityMasterMySqlServersideSpell>();
    public ITable<MySqlGameObjectCata> GameObject => GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCreatureModelInfoShadowlands> CreatureModelInfo => GetTable<MySqlCreatureModelInfoShadowlands>();
}