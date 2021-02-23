#tool nuget:?package=xunit.runner.console
#tool nuget:?package=OctopusTools

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var buildCounter = Argument<int>("buildCounter", 0);
var version = EnvironmentVariable<string>("Version", "7.0.0");
var isPreRelease = EnvironmentVariable<bool>("IsPreRelease", true); //powershell has hard time with bool args
var packageVersion = EnvironmentVariable<string>("PackageVersion", null);
//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define constants

var octopusURL =  EnvironmentVariable("OctopusURL"); 
var octopusApiKey = EnvironmentVariable("OctopusApiKey"); 
var octopusProject = EnvironmentVariable("OctopusProject");
var octoPackDir = Directory("./octopacked/");

// see: https://www.xavierdecoster.com/post/2013/04/29/semantic-versioning-auto-incremented-nuget-package-versions.html
var ciSuffix = string.Format("CI{0:00000}", buildCounter);

// If package version supplied, we need to get the suffix from it (if there is one)
if (!string.IsNullOrEmpty(packageVersion))
{
	var index = packageVersion.IndexOf("-");
	if (index != -1)
		ciSuffix = packageVersion.Remove(0, index + 1); // + 1 to remove the '-' char
	else
		ciSuffix = "";
}

// If package version is supplied always use that; otherwiwse, attempt to do proper semantic versioning on our own
var ciVersion =  packageVersion ?? version;
if (isPreRelease && string.IsNullOrEmpty(packageVersion))
	ciVersion = version + "-" + ciSuffix;	

var author = "PrecisionBI";
var copyRight = string.Format("Copyright © PrecisionBI 2001-{0}",  DateTime.Now.Year);

var packages = new List<string>()
{
	"PBI.FAIDP",
	"PBI.WAIDP",
	"PBI.SecurityManagement",
	"PBI.DbMigrations",
	"PBI.DesignerServices",
	"PBI.JobService",
	"PBI.ExportService",
	"PBI.Admin.WebApi",
	"PBI.Worksheets.WebApi",
	"PBI.Crosstabs.WebApi",
	"PBI.Designer.WebApi",
	"PBI.Dashboards.WebApi",
	"PBI.PivotStreamGenerator",
	"PBI.PageReports",
	"PBI.OAuth2"
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
Task("build-security-only")
	.Does(() =>
{
	//WARN: This Task is NOT actually used in the normal build process!

	//Eseentially this is a quick test build to make sure CodeContracts and its ccrewriter are working properly.		
	Information(configuration);

	CleanDirectory(string.Format("./src/SilverlightServer/PBI.Security/obj/", configuration));
	CleanDirectory(string.Format("./src/SilverlightServer/PBI.Security/bin/", configuration));

	CleanDirectory(string.Format("./src/SilverlightServer/PBI.Security.Cryptography/obj/", configuration));
	CleanDirectory(string.Format("./src/SilverlightServer/PBI.Security.Cryptography/bin/", configuration));

	CleanDirectory(string.Format("./src/Services/PBI.Services.Common/obj/", configuration));
	CleanDirectory(string.Format("./src/Services/PBI.Services.Common/bin/", configuration));

	NuGetRestore("./src/Security.sln");

	MSBuild("./src/Security.sln",  settings => settings.SetConfiguration(configuration));
});

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
});

Task("Info")
	.IsDependentOn("Set-Version")
	.Does(() =>
{
	Information("Version: {0}", version);
	Information("CI Suffix: {0}", ciSuffix);
	Information("CI Version: {0}", ciVersion);
});

Task("Build-v6")
	.IsDependentOn("Info")
	.Does(() =>
{
	CakeExecuteScript("./v6.build.cake");
});

Task("Clean")
	.IsDependentOn("Build-v6")		
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

	CleanDirectory("./src/Pbi.Web.Api/bin");
	CleanDirectory("./src/Pbi.Web.Administration.Api/bin");
	CleanDirectory("./src/Pbi.Web.Worksheets.Api/bin");
	CleanDirectory("./src/Pbi.Web.Crosstabs.Api/bin");
	CleanDirectory("./src/Pbi.Web.Dashboards.Api/bin");
	CleanDirectory("./src/Pbi.Web.Dashboards.Mvc.PivotStreamGenerator/bin");		
	CleanDirectory("./src/PBI.PageReports.Web/bin");
});

Task("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{	
	NuGetRestore("./src/Voyage.Designer.sln");
	NuGetRestore("./src/Voyage.Administration.sln");
	NuGetRestore("./src/Voyage.Worksheets.sln");
	NuGetRestore("./src/Voyage.Crosstabs.sln");
	NuGetRestore("./src/Voyage.Dashboards.sln");
	NuGetRestore("./src/PivotStreamGenerator.sln");
	NuGetRestore("./src/PageReports.sln");
});

Task("Build")
	.IsDependentOn("Restore")
	.Does(() =>
{
	if(IsRunningOnWindows())
	{
		// Use MSBuild
		MSBuild("./src/Voyage.Designer.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/Voyage.Administration.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/Voyage.Worksheets.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/Voyage.Crosstabs.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/Voyage.Dashboards.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/PivotStreamGenerator.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/PageReports.sln", settings =>
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

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    XUnit("./src/**/bin/*/*/*.tests.dll", new XUnitSettings {
		HtmlReport = true,
		OutputDirectory = "./tests"
	});
});

Task("Pack")			
	.IsDependentOn("Build")
	.Does(() =>
{	
	//
	// Infrastructure
	//
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.FAIDP",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI FormsAuthIdentityProvider",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**.dll" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Scripts/**js" },
			new NuSpecContent { Source = "Views/**" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "saml.config" },				
			new NuSpecContent { Source = "saml.Release.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/SsoIdentityProviders/FormsAuthIdentityProvider",			
		OutputDirectory = octoPackDir
	});	
	
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.WAIDP",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI WindowsAuthIdentityProvider",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**.dll" },
			new NuSpecContent { Source = "Views/**" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "saml.config" },				
			new NuSpecContent { Source = "saml.Release.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/SsoIdentityProviders/WindowsAuthIdentityProvider",			
		OutputDirectory = octoPackDir
	});		
	
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.SecurityManagement",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Security Management Web Application",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**.dll" },
			new NuSpecContent { Source = "Controls/**.ascx" },
			new NuSpecContent { Source = "Scripts/**.js" },
			new NuSpecContent { Source = "*.aspx" },
			new NuSpecContent { Source = "*.ashx" },
			new NuSpecContent { Source = "*.css" },
			new NuSpecContent { Source = "SecurityManagement.Master" },
			new NuSpecContent { Source = "Web.config" },
		},
		BasePath = "./src/SilverlightServer/PBI.Server.SecurityManagement",			
		OutputDirectory = octoPackDir
	});
	
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.DbMigrations",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Database Migration Application",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
		},
		BasePath = "./src/PBI.Database.Migration/bin/" + Directory(configuration),			
		OutputDirectory = octoPackDir,
	});		

	//
	// Services
	// 
	
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.DesignerServices",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Designer Web Services",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**.dll" },
			new NuSpecContent { Source = "**.svc" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Services/PBI.Services",			
		OutputDirectory = octoPackDir
	});			
	
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.ExportService",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI WCF Export Windows Service",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
		},
		BasePath = "./src/Dashboards/Services.Export.WcfService/bin/" + Directory(configuration),			
		OutputDirectory = octoPackDir,
		ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)  //http://stackoverflow.com/a/37814762
	});		
			
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.JobService",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Scheduled Jobs Windows Service",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
		},
		BasePath = "./src/JobService/Service/bin/" + Directory(configuration),			
		OutputDirectory = octoPackDir,
		ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)  //http://stackoverflow.com/a/37814762
	});		
	
	//
	// Web Sites
	//
	
	// Administration
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.Admin.WebApi",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Administration Web API",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Images/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Service References/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "*.txt" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "*packages.config" },			
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Administration.Api",			
		OutputDirectory = octoPackDir
	});
	
	//Worksheets
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.Worksheets.WebApi",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Worksheets Web API",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Images/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Service References/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "*.txt" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "*packages.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Worksheets.Api",			
		OutputDirectory = octoPackDir
	});	
	
	//Crosstabs
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.Crosstabs.WebApi",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Crosstabs Web API",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Images/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Service References/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "*.txt" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "*packages.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Crosstabs.Api",			
		OutputDirectory = octoPackDir
	});	
	
	// Designer
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.Designer.WebApi",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Designer Web API",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Images/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Service References/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "*.txt" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "*packages.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Api",			
		OutputDirectory = octoPackDir
	});	

		//Dashboards
		NuGetPack(new NuGetPackSettings {
		Id = "PBI.Dashboards.WebApi",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Dashboard Web API",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Images/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Service References/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "*.txt" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "*packages.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Dashboards.Api",			
		OutputDirectory = octoPackDir
		});				

	//PivotStreamGenerator MVC
		NuGetPack(new NuGetPackSettings {
		Id = "PBI.PivotStreamGenerator",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Dashboard Web API",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "Views/**.config" },
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*packages.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Dashboards.Mvc.PivotStreamGenerator",			
		OutputDirectory = octoPackDir
		});				
		
	//PageReports MVC
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.PageReports",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI Page Reports Web Application",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "fonts/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "Views/Web.config", Target = "Views" },				
			new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },
			new NuSpecContent { Source = "*.xml" },
			new NuSpecContent { Source = "saml.config" },
			new NuSpecContent { Source = "saml.Release.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/PBI.PageReports.Web",			
		OutputDirectory = octoPackDir
	});		

	//OAuth2 MVC
	NuGetPack(new NuGetPackSettings {
		Id = "PBI.OAuth2",
		Version = version,
		Suffix = ciSuffix,
		Authors = new[] { author },
		Copyright = copyRight,
		Description = "PrecisionBI OAuth2 Web Application",
		NoPackageAnalysis = true,
		Files = new [] {			
			new NuSpecContent { Source = "bin/**" },
			new NuSpecContent { Source = "Content/**" },
			new NuSpecContent { Source = "Scripts/**" },
			new NuSpecContent { Source = "Views/**.cshtml" },
			new NuSpecContent { Source = "Views/Web.config", Target = "Views" },				
			//new NuSpecContent { Source = "*.ico" },
			new NuSpecContent { Source = "*.asax" },
			new NuSpecContent { Source = "*.cer" },
			new NuSpecContent { Source = "*.pfx" },				
			new NuSpecContent { Source = "saml.config" },
			new NuSpecContent { Source = "saml.Release.config" },
			new NuSpecContent { Source = "Web.config" },
			new NuSpecContent { Source = "Web.Release.config" },
		},
		BasePath = "./src/Pbi.Web.Security.OAuth2.Web",			
		OutputDirectory = octoPackDir
	});					 
});


Task("Push")
	.IsDependentOn("Pack")
	.Does(() => 
{				
	//Infrastructure

    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.FAIDP." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.WAIDP." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.SecurityManagement." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.DbMigrations." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
		
	//Services

    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.DesignerServices." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });						
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.JobService." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.ExportService." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
			
	//Web Sites
	
    OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.Admin.WebApi." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.Worksheets.WebApi." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.Crosstabs.WebApi." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.Designer.WebApi." + ciVersion + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });		
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.Dashboards.WebApi." + ciVersion + ".nupkg"),
       new OctopusPushSettings {
         ReplaceExisting = true
       });	
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.PivotStreamGenerator." + ciVersion + ".nupkg"),
       new OctopusPushSettings {
         ReplaceExisting = true
       });			 
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.PageReports." + ciVersion + ".nupkg"),
       new OctopusPushSettings {
         ReplaceExisting = true
       });	
	OctoPush(octopusURL, octopusApiKey, new FilePath("./octopacked/PBI.OAuth2." + ciVersion + ".nupkg"),
       new OctopusPushSettings {
         ReplaceExisting = true
       });						 		
});

Task("Tag")
	.IsDependentOn("Push")
	.Does(() => 
{
    OctoCreateRelease(octopusProject, new CreateReleaseSettings {
        Server = octopusURL,
        ApiKey = octopusApiKey,
        ReleaseNumber = ciVersion,
		DefaultPackageVersion = ciVersion,
		IgnoreExisting = true
    });
});
	
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
