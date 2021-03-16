using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bb.Plugins
{

    public class EdiEnvironment
    {

        public EdiEnvironment()
        {

        }

        public DirectoryInfo BaseDirectory { get; set; }

        public DirectoryInfo EdiPath { get; } = new FileInfo(System.Reflection.Assembly.GetAssembly(typeof(EdiEnvironment)).Location).Directory;

        public string Name { get; set; }

        public List<Plugin> Plugins { get; set; }

        public string[] GetDirectories()
        {

            List<string> _dirs = new List<string>();

            foreach (var item in Plugins)
            {
                var p = Path.Combine(BaseDirectory.FullName, item.Path);
                _dirs.Add(p);
            }

            return _dirs.ToArray();

        }
    }

}
