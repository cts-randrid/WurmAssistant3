﻿using AldursLab.WurmAssistant3.Areas.Core.Contracts;

namespace AldursLab.WurmAssistant3.Areas.Core.Services
{
    [KernelBind(BindingHint.Singleton)]
    public class AppManager : IAppManager
    {
        public AppManager()
        {
        }

        public void RunMigrations()
        {
            
        }
    }
}
