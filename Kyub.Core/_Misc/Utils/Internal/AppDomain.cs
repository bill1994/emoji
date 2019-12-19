#if UNITY_WINRT && !UNITY_EDITOR && !UNITY_WP8
#define RT_ENABLED
#endif

#if RT_ENABLED

using System;
using System.Reflection;
using System.Collections.Generic;
using WindowsSpace = Windows;

namespace System
{
    public sealed class AppDomain
    {
	    public static AppDomain CurrentDomain { get; private set; }
	
	    static AppDomain()
	    {
		    CurrentDomain = new AppDomain();
	    }

	    public Assembly[] GetAssemblies()
	    {
            return new List<System.Reflection.Assembly>(GetAssemblyListAsync().Result).ToArray();
	    }

	    private async System.Threading.Tasks.Task<IEnumerable<Assembly>> GetAssemblyListAsync()
	    {
		    var folder = WindowsSpace.ApplicationModel.Package.Current.InstalledLocation;
		
		    List<Assembly> assemblies = new List<Assembly>();
		    foreach (WindowsSpace.Storage.StorageFile file in await folder.GetFilesAsync())
		    {
			    if (file.FileType == ".dll" || file.FileType == ".exe")
			    {
				    AssemblyName name = new AssemblyName() { Name = file.Name };
				    Assembly asm = Assembly.Load(name);
				    assemblies.Add(asm);
			    }
		    }
		
		    return assemblies;
	    }
    }
}
#endif