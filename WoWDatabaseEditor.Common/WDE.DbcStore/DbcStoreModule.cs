﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.DbcStore
{
    [AutoRegister]
    public class DbcStoreModule : ModuleBase
    {
        private IContainerProvider containerProvider = null!;
        
        // this could be moved to somewhere else. ItemModule?
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() =>
                    {
                        var factory = containerProvider.Resolve<IParameterFactory>();
                        factory.RegisterCombined("ItemParameter", "ItemDbcParameter", "ItemDatabaseParameter", (dbc, db) => new ItemParameter(dbc, db), QuickAccessMode.Limited);
                    },
                    ThreadOption.PublisherThread,
                    true);

            containerProvider.Resolve<ILoadingEventAggregator>()
                .OnEvent<DatabaseLoadedEvent>()
                .SubscribeAction(_ =>
                {
                    LoadDatabaseItemsAsync().ListenErrors();
                    LoadSpawnGroupTemplates().ListenErrors();
                });
        }

        private async Task LoadDatabaseItemsAsync()
        {
            var factory = containerProvider.Resolve<IParameterFactory>();
            var database = containerProvider.Resolve<IDatabaseProvider>();
            factory.Register("ItemDatabaseParameter", new DatabaseItemParameter(await database.GetItemTemplatesAsync()));
        }
        
        private async Task LoadSpawnGroupTemplates()
        {
            var factory = containerProvider.Resolve<IParameterFactory>();
            var database = containerProvider.Resolve<IDatabaseProvider>();
            factory.Register("SpawnGroupTemplateParameter", new SpawnGroupTemplateParameter(await database.GetSpawnGroupTemplatesAsync()));
        }

        internal class SpawnGroupTemplateParameter : ParameterNumbered
        {
            public SpawnGroupTemplateParameter(IList<ISpawnGroupTemplate>? items)
            {
                if (items == null)
                    return;
                Items = new();
                foreach (var i in items)
                    Items.Add(i.Id, new SelectOption(i.Name));
            }
        }

        internal class DatabaseItemParameter : ParameterNumbered
        {
            public DatabaseItemParameter(IList<IItem>? items)
            {
                if (items == null)
                    return;
                Items = new();
                foreach (var i in items)
                    Items.Add(i.Entry, new SelectOption(i.Name));
            }
        }
        
        internal class ItemParameter : ParameterNumbered, IItemParameter
        {
            public ItemParameter(IParameter<long> dbc, IParameter<long> db)
            {
                Items = new();
                if (dbc.Items != null)
                {
                    foreach (var i in dbc.Items)
                        Items[i.Key] = i.Value;
                }
            
                if (db.Items != null)
                {
                    foreach (var i in db.Items)
                        Items[i.Key] = i.Value;
                }
            }
        }
    }
}