﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Emzi0767.Ada.Plugins
{
    internal class AdaPluginManager
    {
        public int PluginCount { get { return this.RegisteredPlugins.Count; } }
        internal IEnumerable<Assembly> PluginAssemblies { get { return this.LoadedAssemblies.Select(xkvp => xkvp.Value); } }
        internal Assembly MainAssembly { get; private set; }
        private Dictionary<string, AdaPlugin> RegisteredPlugins { get; set; }
        private Dictionary<string, Assembly> LoadedAssemblies { get; set; }

        public AdaPluginManager()
        {
            L.W("ADA PLG", "Initializing Plugin manager");
            this.RegisteredPlugins = new Dictionary<string, AdaPlugin>();
            L.W("ADA PLG", "Initializer");
        }

        public void LoadAssemblies()
        {
            L.W("ADA PLG", "Loading all plugin assemblies");
            this.LoadedAssemblies = new Dictionary<string, Assembly>();
            var a = typeof(AdaPlugin).GetTypeInfo().Assembly;
            this.LoadedAssemblies.Add(a.FullName, a);
            this.MainAssembly = a;
            var l = a.Location;
            l = Path.GetDirectoryName(l);
            l = Path.Combine(l, "plugins");
            if (Directory.Exists(l))
            {
                var x = Directory.GetFiles(l, "*.dll", SearchOption.TopDirectoryOnly);
                foreach (var xx in x)
                {
                    L.W("ADA PLG", "Loaded file '{0}'", xx);
                    var xa = AssemblyLoadContext.Default.LoadFromAssemblyPath(xx);
                    this.LoadedAssemblies.Add(xa.FullName, xa);
                }
            }
            L.W("ADA PLG", "Registering dependency resolver");
            AssemblyLoadContext.Default.Resolving += Default_Resolving;
            L.W("ADA PLG", "Done");
        }

        public void Initialize()
        {
            L.W("ADA PLG", "Registering and initializing plugins");
            var @as = this.PluginAssemblies;
            var ts = @as.SelectMany(xa => xa.DefinedTypes);
            var pt = typeof(IAdaPlugin);
            foreach (var t in ts)
            {
                if (!pt.IsAssignableFrom(t.AsType()) || !t.IsClass || t.IsAbstract)
                    continue;

                L.W("ADA PLG", "Type {0} is a plugin", t.ToString());
                var iplg = (IAdaPlugin)Activator.CreateInstance(t.AsType());
                var plg = new AdaPlugin { Plugin = iplg };
                this.RegisteredPlugins.Add(plg.Name, plg);
                L.W("ADA PLG", "Registered plugin '{0}'", plg.Name);
                plg.Plugin.Initialize();
                plg.Plugin.LoadConfig(AdaBotCore.ConfigManager.GetConfig(iplg));
                L.W("ADA PLG", "Plugin '{0}' initialized", plg.Name);
            }
            this.UpdateAllConfigs();
            L.W("ADA PLG", "Registered and initialized {0:#,##0} plugins", this.RegisteredPlugins.Count);
        }

        internal void UpdateAllConfigs()
        {
            foreach (var plg in this.RegisteredPlugins)
            {
                AdaBotCore.ConfigManager.UpdateConfig(plg.Value.Plugin);
            }
        }

        private Assembly Default_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            if (this.LoadedAssemblies.ContainsKey(arg2.FullName))
                return this.LoadedAssemblies[arg2.FullName];
            return null;
        }
    }
}