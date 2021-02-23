#tool nuget:?package=xunit.runner.console
#tool nuget:?package=OctopusTools

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildCounter = Argument<int>("buildCounter", 0);
var version = EnvironmentVariable<string>("Version", "2.0.0");
var isPreRelease = EnvironmentVariable<bool>("IsPreRelease", true);


var octopusURL =  EnvironmentVariable("OctopusURL"); 
var octopusApiKey = EnvironmentVariable("OctopusApiKey"); 
var octopusProject = EnvironmentVariable("OctopusProject");
var octoPackDir = Directory("./octopacked/");

var ciSuffix = string.Format("CI{0:00000}", buildCounter);
var ciVersion =  version + "-" + ciSuffix;	

var author = "VertexDrMobileServer";
var copyRight = string.Format("Copyright © VertexDrMobileServer 2019-{0}",  DateTime.Now.Year);


var packages = new List<string>()
{
	"VertexDR",
	
};

Func<IFileSystemInfo, bool> included_packages = fileSystemInfo =>
{	
	//Information(fileSystemInfo.Path.FullPath);
	var fp = new FilePath(fileSystemInfo.Path.FullPath);
	var name =  fp.GetFilename().FullPath;
	var parts = name.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
	if (parts.Length > 2)
	{
		var packageName = parts[0] + "." + parts[1];
		Information(packageName);
		return packages.Contains(packageName);
	}
	return false;
};
//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("Set-Version")				
		.Does(() =>
{
		var file = "./src/VersionInfo.cs";
		var semVersion = string.Concat(version + "-" + buildCounter);
		CreateAssemblyInfo(file, new AssemblyInfoSettings {
			Version = version,
			FileVersion = version,
			InformationalVersion = semVersion,		
		});

		if (!isPreRelease)	
		{
			ciVersion = version;
			ciSuffix = "";
		}
});

Task("Info")
		.IsDependentOn("Set-Version")
		.Does(() =>
{
		Information("Version: {0}", version);
		Information("CI Suffix: {0}", ciSuffix);
		Information("CI Version: {0}", ciVersion);
});




Task("Clean")
		.IsDependentOn("Info")
		.Does(() =>
{
		if (!DirectoryExists(octoPackDir))
		{
			CreateDirectory(octoPackDir);
		}
		else
		{
			CleanDirectory(octoPackDir, included_packages);
		}

		CleanDirectory("src/VertexDrMobileServer/bin");
		

});

Task("Restore")
		.IsDependentOn("Clean")
		.Does(() =>
{	
		NuGetRestore("src/VertexDrMobileServer.sln");
		
});

Task("Build")
		.IsDependentOn("Restore")
		.Does(() =>
{
		if(IsRunningOnWindows())
		{
			// Use MSBuild
			MSBuild("src/VertexDrMobileServer.sln", settings =>
				settings.SetConfiguration(configuration)
			);
					
		}
		else
		{
			// Use XBuild
			Information("Not supported!");
			// XBuild("./src/x.sln", settings => settings.SetConfiguration(configuration));
		}
});

Task("Pack")			
		.IsDependentOn("Build")
		.Does(() =>
{	
		//
		// Infrastructure
		//
		NuGetPack(new NuGetPackSettings {
			Id = "VertexDrMobileServer",
			Version = version,
			Suffix = ciSuffix,
			Authors = new[] { author },
			Copyright = copyRight,
			Description = "VertexDrMobileServer",
			NoPackageAnalysis = true,
			Files = new [] {	
				new NuSpecContent { Source = "App_Data/**" },
				new NuSpecContent { Source = "Areas/**" },
				new NuSpecContent { Source = "bin/**" },
		 		new NuSpecContent { Source = "Content/**" },
				new NuSpecContent { Source = "fonts/**"},
				new NuSpecContent { Source = "Scripts/**" },
				new NuSpecContent { Source = "Views/**" },
				new NuSpecContent { Source = "*.ico" },
				new NuSpecContent { Source = "ApplicationInsights.config" },
				new NuSpecContent { Source = "Global.asax" },
				new NuSpecContent { Source = "Log4Net.config" },
						
			},
			BasePath = "./src/VertexDrMobileServer",			
			OutputDirectory = octoPackDir
		});	
						 
});

Task("Push")
  .IsDependentOn("Pack")
  .Does(() => 
{				
						

	
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/VertexDrMobileServer." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
								 		
});


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////


RunTarget(target);