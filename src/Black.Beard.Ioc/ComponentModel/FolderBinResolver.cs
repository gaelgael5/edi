using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Bb.ComponentModel
{


    /// <summary>
    /// Ressolve the folder's list of binaries files
    /// </summary>
    public class FolderBinResolver
    {


        /// <summary>
        ///     return value indicate if the current
        ///     <see cref="AppDomain.GetAssemblies()" contains items from Microsoft technology stack Web libraries />
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is system web assembly loaded; otherwise, <c>false</c>.
        /// </value>
        public static bool HasSystemWebAssemblyLoaded
        {
            get
            {
                if (!_isSystemWebAssemblyLoaded.HasValue)
                    _isSystemWebAssemblyLoaded = _hasSystemWebAssemblyLoaded_Impl();
                return _isSystemWebAssemblyLoaded.Value;
            }
        }

        private static bool _hasSystemWebAssemblyLoaded_Impl()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.FullName.StartsWith("System.Web,", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        ///     Gets bin path for the case the running application is a console application.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DirectoryInfo> GetConsoleBinPath()
        {

            var _h = new HashSet<string>();
            var appDomain = AppDomain.CurrentDomain;

            if (!string.IsNullOrEmpty(appDomain.RelativeSearchPath))
                if (_h.Add(appDomain.RelativeSearchPath))
                    yield return new DirectoryInfo(appDomain.RelativeSearchPath);

            if (_h.Add(appDomain.BaseDirectory))
                yield return new DirectoryInfo(appDomain.BaseDirectory);
        }

        /// <summary>
        /// Gets bin's paths for the case the running application is a web application
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<DirectoryInfo> GetWebBinPath()
        {

            var ass = GetWebAssembly();
            if (ass != null)
            {
                var type = ass.GetType(_typeBuildManager);
                if (type != null)
                {

                    var method = type.GetMethod(_GetReferencedAssembliesMethodName, BindingFlags.Static | BindingFlags.Public);
                    if (method != null)
                    {
                        var list = method.Invoke(null, new object[] { }) as IEnumerable;
                        if (list != null)
                        {
                            var _h = new HashSet<string>();
                            var items = list.Cast<Assembly>()
                                .Where(c => !c.CodeBase.ToLower().Contains(@"microsoft.net"))
                                .Where(c => !c.CodeBase.ToLower().Contains(@"/temp/"))
                                .ToList();

                            foreach (var item in items)
                            {
                                var uri = new Uri(item.CodeBase);
                                var file = new FileInfo(uri.AbsolutePath);
                                if (file.Exists && _h.Add(file.Directory.FullName) && file.Directory.Exists)
                                    yield return file.Directory;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Gets bin path for the case the running application is a web application
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetWebReferencedAssemblies()
        {

            var items = new List<Assembly>();
            var ass = GetWebAssembly();
            if (ass != null)
            {
                var type = ass.GetType(_typeBuildManager);

                if (type != null)
                {
                    var method = type.GetMethod(_GetReferencedAssembliesMethodName, BindingFlags.Static | BindingFlags.Public);
                    if (method != null)
                    {
                        var list = method.Invoke(null, new object[] { }) as IEnumerable;
                        if (list != null)
                            items.AddRange(list.OfType<Assembly>());
                    }
                }
            }
            return items;
        }

        /// <summary>
        ///     Gets loaded assemblies.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetLoadedAssemblies()
        {
            var items = AppDomain.CurrentDomain.GetAssemblies().ToList();
            return items;
        }

        /// <summary>
        /// Determines whether [is a web application].
        /// </summary>
        /// <returns></returns>
        public static bool IsWebApplication()
        {

            return GetWebAssembly() != null;

        }


        private static Assembly GetWebAssembly() => Assembly.LoadWithPartialName("System.Web"); // Microsoft.AspNetCore.App
        private static bool? _isSystemWebAssemblyLoaded;
        private const string _typeBuildManager = "System.Web.Compilation.BuildManager";
        private const string _GetReferencedAssembliesMethodName = "GetReferencedAssemblies";

    }
}