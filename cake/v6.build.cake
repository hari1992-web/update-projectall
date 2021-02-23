#tool nuget:?package=xunit.runner.console
#tool nuget:?package=OctopusTools
#r "./extensions/PBI.Cake.Extensions.dll"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define constants
//var version = GetAssemblyVersion("./PBI.AssemblyInfo.cs");

// Define directories
//var octoPackDir = Directory("./octopacked/");

var ssoFormsDir = Directory("./src/SsoIdentityProviders/FormsAuthIdentityProvider/bin");
var ssoWinDir = Directory("./src/SsoIdentityProviders/WindowsAuthIdentityProvider/bin");

var adhocDir = Directory("./src/SilverlightServer/PBI.Server/bin");
var secMgr = Directory("./src/SilverlightServer/PBI.Server.SecurityManagement/bin");

var designServicesDir = Directory("./src/Services/PBI.Services/bin/");
var dashDir = Directory("./src/Dashboards/Web/bin");

//var pageRptDir = Directory("./src/PBI.PageReports.Web/bin");

var dashDataProviderDir = Directory("./src/Dashboards/Services.DataProvider.WcfService/bin");
var dashExportDir = Directory("./src/Dashboards/Services.Export.WcfService/bin");
var jobServiceDir = Directory("./src/JobService/Service/bin");

var designDir = Directory("./src/PresentationCenter/Designer/PrecisionBI/bin");
//var keyMgrDir = Directory("./src/PresentationCenter/DesignerKeyInstaller/bin");
var dbMgDir = Directory("./src/PBI.Database.Migration/bin");
//var dbInstallDir = Directory("./src/Util/PBICmd/PBIDatabaseSetup/bin");

//var metaConversionDir = Directory("./src/Util/IdahoConversion/ConversionUtility/PBI_CENTRAL/PBIV6.Conversion.Client/bin");
//var objConversionDir = Directory("./src/Util/IdahoConversion/ObjectConversionUntility/bin");

/* var packages = new List<string>()
{
"PBI.Adhoc",
"PBI.Conversion",
"PBI.Conversion",
"PBI.Dashboards",
"PBI.DataProviderService",
"PBI.DbMigrations",
"PBI.Designer",
"PBI.DesignerServices",
"PBI.ExportService",
"PBI.FAIDP",
"PBI.JobService",
"PBI.KeyManager",
"PBI.SecurityManagement",
"PBI.WAIDP",
"PBI.PageReports"
};

Func<IFileSystemInfo, bool> included_packages = fileSystemInfo=>
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
 */

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
{
	/* if (!DirectoryExists(octoPackDir))
	{
		CreateDirectory(octoPackDir);
	}
	else
	{
		CleanDirectory(octoPackDir, included_packages);
	} */
		
	CleanDirectory(ssoFormsDir);
	CleanDirectory(ssoWinDir);

	CleanDirectory(adhocDir);	
	CleanDirectory(secMgr);	
	
	CleanDirectory(designServicesDir);
	CleanDirectory(dashDir);

	//CleanDirectory(pageRptDir);

	CleanDirectory(dashDataProviderDir);
	CleanDirectory(dashExportDir);
	CleanDirectory(jobServiceDir);

	CleanDirectory(designDir);
	//CleanDirectory(keyMgrDir);
	
	CleanDirectory(dbMgDir);
	//CleanDirectory(dbInstallDir);
		
	//CleanDirectory(metaConversionDir);
	//CleanDirectory(objConversionDir);
});

Task("Restore")
	.IsDependentOn("Clean")
	.Does(() =>
{
	NuGetRestore("./src/Idaho.sln");
	
	NuGetRestore("./src/SsoIdentityProviders.sln");
	NuGetRestore("./src/Dashboards.sln");
	//NuGetRestore("./src/PageReports.sln");
	NuGetRestore("./src/JobService.sln");

	NuGetRestore("./src/PresentationCenter/PBIStudioDesignerAll.sln");
	//NuGetRestore("./src/PresentationCenter/DesignerKeyInstaller/DesignerKeyInstaller.sln");
	
	NuGetRestore("./src/DatabaseMigrations.sln");
	//NuGetRestore("./src/Util/PBICmd/PBI.Util.CommandLine.sln");		
	
	//NuGetRestore("./src/Util/IdahoConversion/ConversionUtility/PBI.Conversion.sln");
	//NuGetRestore("./src/Util/IdahoConversion/ObjectConversionUntility/AnalyticsObjectConversion.sln");
});

Task("Build")
	.IsDependentOn("Restore")
	.Does(() =>
{
	if(IsRunningOnWindows())
	{
		// Use MSBuild
		
		// NOTE: Silverlight applications must be compiled to x86 platform - https://github.com/cake-build/cake/issues/585
		MSBuild("./src/Idaho.sln", settings =>
			settings.SetConfiguration(configuration)
				.SetMSBuildPlatform(MSBuildPlatform.x86)
		);
		MSBuild("./src/SsoIdentityProviders.sln", settings =>
			settings.SetConfiguration(configuration)
		);											
		MSBuild("./src/Dashboards.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		//MSBuild("./src/PageReports.sln", settings =>
		//	settings.SetConfiguration(configuration)
		//);

		MSBuild("./src/JobService.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		
		MSBuild("./src/PresentationCenter/PBIStudioDesignerAll.sln", settings =>
			settings.SetConfiguration(configuration)
				.SetMSBuildPlatform(MSBuildPlatform.x86)
		);
		/* MSBuild("./src/PresentationCenter/DesignerKeyInstaller/DesignerKeyInstaller.sln", settings =>
			settings.SetConfiguration(configuration)
				.SetMSBuildPlatform(MSBuildPlatform.x86)
		); */			

		MSBuild("./src/DatabaseMigrations.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		/* MSBuild("./src/Util/PBICmd/PBI.Util.CommandLine.sln", settings =>
			settings.SetConfiguration(configuration)
				.SetMSBuildPlatform(MSBuildPlatform.x86)
		);
		
		
		MSBuild("./src/Util/IdahoConversion/ConversionUtility/PBI.Conversion.sln", settings =>
			settings.SetConfiguration(configuration)
		);
		MSBuild("./src/Util/IdahoConversion/ObjectConversionUntility/AnalyticsObjectConversion.sln", settings =>
			settings.SetConfiguration(configuration)
		); */
	}
	else
	{
		// Use XBuild
		Information("Not supported!");
		// XBuild("./Idaho.sln", settings => settings.SetConfiguration(configuration));
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
/* 
Task("Pack")			
		.IsDependentOn("Test")
		.Does(() =>
{
		Information("Packing Version: " + version);
		
		//SSO
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.FAIDP",
			Version = version,
			Authors = new[] {"PrecisionBI"},
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
			BasePath = "./SsoIdentityProviders/FormsAuthIdentityProvider",			
			OutputDirectory = octoPackDir
		});		
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.WAIDP",
			Version = version,
			Authors = new[] {"PrecisionBI"},
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
			BasePath = "./SsoIdentityProviders/WindowsAuthIdentityProvider",			
			OutputDirectory = octoPackDir
		});		
		
		//Security Manager
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.SecurityManagement",
			Version = version,
			Authors = new[] {"PrecisionBI"},
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
			BasePath = "./SilverlightServer/PBI.Server.SecurityManagement",			
			OutputDirectory = octoPackDir
		});
		
		//Ad hoc
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.Adhoc",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Ad hoc Web Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "bin/**.dll" },
				new NuSpecContent { Source = "ClientBin/**.xap" },
				new NuSpecContent { Source = "Images/**.aspx" },
				new NuSpecContent { Source = "Images/**.jpg" },
				new NuSpecContent { Source = "*.ico" },
				new NuSpecContent { Source = "*.asax" },
				new NuSpecContent { Source = "*.cer" },
				new NuSpecContent { Source = "*.pfx" },
				new NuSpecContent { Source = "*.xml" },
				new NuSpecContent { Source = "*.js" },
				new NuSpecContent { Source = "*.aspx" },
				new NuSpecContent { Source = "saml.config" },
				new NuSpecContent { Source = "saml.Release.config" },
				new NuSpecContent { Source = "Web.config" },
				new NuSpecContent { Source = "Web.Release.config" },
			},
			BasePath = "./SilverlightServer/PBI.Server",			
			OutputDirectory = octoPackDir
		});	
		
		//DesignerServices
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.DesignerServices",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Designer Web Services",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "bin/**.dll" },
				new NuSpecContent { Source = "**.svc" },
				new NuSpecContent { Source = "Web.config" },
				new NuSpecContent { Source = "Web.Release.config" },
			},
			BasePath = "./Services/PBI.Services",			
			OutputDirectory = octoPackDir
		});	
		
		//Dashboards
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.Dashboards",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Dashboard Web Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "bin/**" },
				new NuSpecContent { Source = "Content/**" },
				new NuSpecContent { Source = "Images/**" },
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
			BasePath = "./Dashboards/Web",			
			OutputDirectory = octoPackDir
		});		

		//Page Reports
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.PageReports",
			Version = version,
			Authors = new[] {"PrecisionBI"},
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
			BasePath = "./PBI.PageReports.Web",			
			OutputDirectory = octoPackDir
		});		
				
		//Export Service
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.ExportService",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI WCF Export Windows Service",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
			},
			BasePath = "./Dashboards/Services.Export.WcfService/bin/" + Directory(configuration),			
			OutputDirectory = octoPackDir,
			ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)  //http://stackoverflow.com/a/37814762
		});		
		
		//DataProvider Service
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.DataProviderService",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Dashboards WCF DataProvider Windows Service",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
			},
			BasePath = "./Dashboards/Services.DataProvider.WcfService/bin/" + Directory(configuration),			
			OutputDirectory = octoPackDir,
			ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)  
		});		
		
		//Job Service
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.JobService",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Scheduled Jobs Windows Service",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
			},
			BasePath = "./JobService/Service/bin/" + Directory(configuration),			
			OutputDirectory = octoPackDir,
			ArgumentCustomization = args => args.Append("-Prop Configuration=" + configuration)  //http://stackoverflow.com/a/37814762
		});		
		
		
	  //Designer
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.Designer",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Dashboard Desginer Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
			},
			BasePath = "./PresentationCenter/Designer/PrecisionBI/bin",			
			OutputDirectory = octoPackDir,
		});		
		
		//KeyManager
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.KeyManager",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Encryption Key Manager Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config;*.exe.manifest;*.application" }
			},
			BasePath = "./PresentationCenter/DesignerKeyInstaller/bin/Release",			
			OutputDirectory = octoPackDir,
		});		
				

		//Database Migrations
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.DbMigrations",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Database Migration Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.Debug.config" }
			},
			BasePath = "./PBI.Database.Migration/bin/Release",			
			OutputDirectory = octoPackDir,
		});		
						
		//NOTE: not packaged -> Database Installer		
		
		//Conversion Utilities
		
		//Metadata Conversion (Anwar's tool)
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.Conversion.Metadata",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Metadata Conversion Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.config" }
			},
			BasePath = "./Util/IdahoConversion/ConversionUtility/PBI_CENTRAL/PBIV6.Conversion.Client/bin/Release",			
			OutputDirectory = octoPackDir,
		});
		
		//Object Conversion (Dan's tool)
		NuGetPack(new NuGetPackSettings {
			Id = "PBI.Conversion.Objects",
			Version = version,
			Authors = new[] {"PrecisionBI"},
			Description = "PrecisionBI Object Conversion Application",
			NoPackageAnalysis = true,
			Files = new [] {			
				new NuSpecContent { Source = "*.*", Exclude = "App.config" }
			},
			BasePath = "./Util/IdahoConversion/ObjectConversionUntility/bin/Release",			
			OutputDirectory = octoPackDir,
		});
});


Task("Push")
  .IsDependentOn("Pack")
  .Does(() => {					
		
	//SSO
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.FAIDP." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.WAIDP." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });

	//Ad hoc
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.Adhoc." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.SecurityManagement." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
		
	//DesignerServices
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.DesignerServices." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
		
	//Dashboards
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.Dashboards." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
			
	//Page Reports
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.PageReports." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });
			
	//Export Service
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.ExportService." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
			
	//DataProvider Service
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.DataProviderService." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
			
	//Job Service
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.JobService." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
			

  	//Designer
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.Designer." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });			
	//Key Manager
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.KeyManager." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });		

			
	//Database Migrations 
    OctoPush(deployserverURI, octopusApiKey, new FilePath("./octopacked/PBI.DbMigrations." + version + ".nupkg"),
      new OctopusPushSettings {
        ReplaceExisting = true
      });

	//No push for conversion tools yet	
});

Task("Copy")
	.IsDependentOn("Push")
	.Does(() => {
		//HACK: for 6.8 installer to get packages - should eventually use Deploy from OctopusDeploy
		var sharedFolder = Directory("C:\\LatestPackages");
		CleanDirectory(sharedFolder, included_packages);
		CopyFiles("./octopacked/*.nupkg", sharedFolder);
	});

Task("OctoRelease")
  .IsDependentOn("Copy")
  .Does(() => {
    OctoCreateRelease("PrecisionBI 6 - Latest", new CreateReleaseSettings {
        Server = deployserverURI,
        ApiKey = octopusApiKey,
        ReleaseNumber = version,
				DefaultPackageVersion = version,
				IgnoreExisting = true
      });
  });
	 */
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
