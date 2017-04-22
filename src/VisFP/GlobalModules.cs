using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisFP.BusinessObjects;
using VisFP.Data;

namespace VisFP
{
    public static class ModulesRepository
    {
        static List<ITaskModule> modules;
        static Dictionary<Type, Guid> modulesId;
        static Dictionary<string, ITaskModule> moduleNames;

        static ModulesRepository()
        {
            modules = new List<ITaskModule>();
            modulesId = new Dictionary<Type, Guid>();
            moduleNames = new Dictionary<string, ITaskModule>();
        }

        public static T GetModule<T>() where T : ITaskModule
        {
            return (T)modules.SingleOrDefault(x => x.GetType() == typeof(T));
        }

        public static Guid RegisterModule(ITaskModule module, ApplicationDbContext context)
        {
            var moduleName = module.GetModuleName();
            if (modules.Any(x => x.GetType() == module.GetType()))
                throw new Exception($"Модуль {moduleName} ({module.GetType()}) уже добавлен!");

            var dbModule = context.TaskTypes.SingleOrDefault(x => x.TaskTypeName == moduleName);
            if (dbModule == null)
            {
                dbModule = new Data.DBModels.DbTaskType
                {
                    TaskTypeName = module.GetModuleName(),
                    TaskTypeNameToView = module.GetModuleNameToView()
                };
                context.TaskTypes.Add(dbModule);
            }

            modules.Add(module);
            moduleNames.Add(module.GetModuleName(), module);
            modulesId.Add(module.GetType(), dbModule.TaskTypeId);
            return dbModule.TaskTypeId;
        }

        public static Guid GetModuleId<T>() where T : ITaskModule
        {
            return modulesId[typeof(T)];
        }
        public static Guid GetModuleId(Type T)
        {
            return modulesId[T];
        }

        public static ITaskModule GetTaskModuleByName(string name)
        {
            ITaskModule module;
            if (moduleNames.TryGetValue(name, out module))
                return module;
            return null;
        }
    }
}
