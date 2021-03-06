﻿// Copyright (c) 2012 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.

namespace NativeClientVSAddIn
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Windows.Forms;

  using EnvDTE;
  using EnvDTE80;
  using Microsoft.VisualStudio.VCProjectEngine;

  /// <summary>
  /// This class handles reading and writing properties on property pages.
  /// </summary>
  public class PropertyManager
  {
    /// <summary>
    /// The target project to read from.
    /// </summary>
    private VCProject project_;

    /// <summary>
    /// The target configuration and platform to read from.
    /// </summary>
    private VCConfiguration configuration_;

    /// <summary>
    /// Constructs the property manager. Sets the platform type to Other which invalidates use
    /// of the property manager until SetTarget is called with a valid target.
    /// </summary>
    public PropertyManager()
    {
      PlatformType = ProjectPlatformType.Other;
    }

    /// <summary>
    /// Specifies the type of plug-in being run in this debug session.
    /// </summary>
    public enum ProjectPlatformType
    {
      /// <summary>
      /// Represents all non-pepper/non-nacl platform types.
      /// </summary>
      Other,

      /// <summary>
      /// Indicates project platform is a trusted plug-in (nexe).
      /// </summary>
      NaCl,

      /// <summary>
      /// Indicates project platform is an untrusted plug-in.
      /// </summary>
      Pepper
    }

    /// <summary>
    /// Gets or sets the current project platform type. This indicates Pepper, NaCl, or Other type
    /// of project. If this is set to Other then it is invalid to read the properties.
    /// </summary>
    public ProjectPlatformType PlatformType { get; protected set; }

    /// <summary>
    /// Gets or sets the current project platform name.
    /// </summary>
    public string PlatformName { get; protected set; }

    /// <summary>
    /// Gets or sets the full path to the output assembly.
    /// </summary>
    public virtual string PluginAssembly
    {
      get
      {
        AssertValidPlatform();
        VCLinkerTool linker = configuration_.Tools.Item("VCLinkerTool");
        return configuration_.Evaluate(linker.OutputFile);
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the main project directory.
    /// </summary>
    public virtual string ProjectDirectory
    {
      get
      {
        AssertValidPlatform();
        return project_.ProjectDirectory;
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the directory where the output assembly is placed.
    /// </summary>
    public virtual string OutputDirectory
    {
      get
      {
        AssertValidPlatform();
        return configuration_.Evaluate(configuration_.OutputDirectory);
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the Native Client VS Add-in version.
    /// </summary>
    public string NaClAddInVersion
    {
      get
      {
        AssertValidPlatform();
        return GetProperty("ConfigurationGeneral", "NaClAddInVersion");
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets this project's setting of where the NaCl SDK Root is.
    /// </summary>
    public string SDKRootDirectory
    {
      get
      {
        AssertValidPlatform();
        string value = GetProperty("ConfigurationGeneral", "VSNaClSDKRoot");

        if (string.IsNullOrEmpty(value))
        {
          MessageBox.Show(Strings.SDKPathNotSetError);
          return null;
        }

        return value.TrimEnd("/\\".ToArray<char>());
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the port to use for the web server launched during debugging.
    /// </summary>
    public string WebServerPort
    {
      get
      {
        AssertValidPlatform();
        return GetProperty("ConfigurationGeneral", "NaClWebServerPort");
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the command to debug (assumed to be chrome.exe)
    /// </summary>
    public string LocalDebuggerCommand
    {
      get
      {
        AssertValidPlatform();
        return GetProperty("WindowsLocalDebugger", "LocalDebuggerCommand");
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the toolchain name. Ex: newlib.
    /// </summary>
    public string ToolchainName
    {
      get
      {
        AssertNaCl();
        if (IsPNaCl())
            return "newlib";
        return GetProperty("ConfigurationGeneral", "ToolchainName");
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the target archirecture name. Ex: x86_64.
    /// </summary>
    public string TargetArchitecture
    {
      get
      {
        AssertNaCl();
        return GetProperty("ConfigurationGeneral", "TargetArchitecture");
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Gets or sets the path to the NaCl manifest file (NaCl platform only).
    /// </summary>
    public string ManifestPath
    {
      get
      {
        AssertNaCl();
        return GetProperty("ConfigurationGeneral", "NaClManifestPath");
      }

      protected set
      {
      }
    }

    /// <summary>
    /// Return true if the given platform is a NaCl platform.
    /// </summary>
    public static bool IsNaClPlatform(string platformName)
    {
        return platformName.Equals(Strings.NaCl32PlatformName, StringComparison.OrdinalIgnoreCase) ||
               platformName.Equals(Strings.NaCl64PlatformName, StringComparison.OrdinalIgnoreCase) ||
               platformName.Equals(Strings.NaClARMPlatformName, StringComparison.OrdinalIgnoreCase) ||
               platformName.Equals(Strings.PNaClPlatformName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Return true if the given platform is the PNaCl platform.
    /// </summary>
    public bool IsPNaCl()
    {
        return PlatformName.Equals(Strings.PNaClPlatformName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Return true if the given platform is a PPAPI platform.
    /// </summary>
    public static bool IsPepperPlatform(string platformName)
    {
        return platformName.Equals(Strings.PepperPlatformName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets the target project, platform, and configuration to get settings from.
    /// </summary>
    /// <param name="proj">Project to read settings from.</param>
    /// <param name="targetPlatformName">Platform type to read settings from.</param>
    /// <param name="targetConfigName">Configuration to read from (Debug or Release).</param>
    public void SetTarget(Project proj, string targetPlatformName, string targetConfigName)
    {
      // Set the project platform.  If it is set to Other then no settings are valid to be read.
      SetPlatform(targetPlatformName);
      if (!IsPepperPlatform(targetPlatformName) && !IsNaClPlatform(targetPlatformName))
        return;

      // We don't support non-visual C/C++ projects.
      if (!Utility.IsVisualCProject(proj))
      {
        PlatformType = ProjectPlatformType.Other;
        return;
      }

      // Set the member variables for configuration and project to the target.
      project_ = (VCProject)proj.Object;
      foreach (VCConfiguration config in project_.Configurations)
      {
        if (config.ConfigurationName == targetConfigName &&
            config.Platform.Name == targetPlatformName)
        {
          configuration_ = config;
          break;
        }
      }
    }

    /// <summary>
    /// Overload of SetTarget if the VCConfiguration is already known.
    /// </summary>
    /// <param name="config">Configuration to read settings from.</param>
    public void SetTarget(VCConfiguration config)
    {
      if (config == null)
      {
        throw new ArgumentNullException("Config");
      }

      configuration_ = config;
      project_ = config.project;

      SetPlatform(config.Platform.Name);
    }

    private void SetPlatform(string platform)
    {
      PlatformName = platform;

      if (IsPepperPlatform(PlatformName))
      {
        PlatformType = ProjectPlatformType.Pepper;
      }
      else if (IsNaClPlatform(PlatformName))
      {
        PlatformType = ProjectPlatformType.NaCl;
      }
      else
      {
        PlatformType = ProjectPlatformType.Other;
      }
    }

    /// <summary>
    /// Sets the target project, platform, and configuration to the active start-up project and
    /// selected platform and configuration so that settings are read from them.
    /// </summary>
    /// <param name="dte">The main Visual Studio object.</param>
    public void SetTargetToActive(DTE2 dte)
    {
      // We require that there is only a single start-up project.
      // If multiple start-up projects are specified then we use the first and display a warning.
      Array startupProjects = dte.Solution.SolutionBuild.StartupProjects as Array;
      if (startupProjects == null || startupProjects.Length == 0)
      {
        throw new ArgumentOutOfRangeException("startupProjects.Length");
      }
      else if (startupProjects.Length > 1)
      {
        // Display a warning if multiple start-up projects and one is nacl/pepper.
        foreach (Project proj in startupProjects)
        {
          VCConfiguration config = Utility.GetActiveVCConfiguration(proj);
          if (IsPepperPlatform(config.Platform.Name) || IsNaClPlatform(config.Platform.Name))
          {
            System.Windows.Forms.MessageBox.Show(Strings.MultiStartProjectWarning);
            break;
          }
        }
      }

      // Get the first start-up project object.
      List<Project> projList = dte.Solution.Projects.OfType<Project>().ToList();
      string startProjectName = startupProjects.GetValue(0) as string;
      Project startProject = projList.Find(proj => proj.UniqueName == startProjectName);

      VCConfiguration activeConfig = Utility.GetActiveVCConfiguration(startProject);

      // GetActiveVCConfiguration will return null if not a VC project.
      if (activeConfig == null)
      {
        PlatformType = ProjectPlatformType.Other;
        return;
      }

      SetTarget(activeConfig);
    }

    /// <summary>
    /// Reads any generic property from the current target properties.
    /// </summary>
    /// <param name="page">Name of the page where the property is located.</param>
    /// <param name="name">Name of the property.</param>
    /// <returns>The property requested.</returns>
    public virtual string GetProperty(string page, string name)
    {
      IVCRulePropertyStorage pageStorage = configuration_.Rules.Item(page);
      return pageStorage.GetEvaluatedPropertyValue(name);
    }

    /// <summary>
    /// Sets any generic property to the current target properties.
    /// </summary>
    /// <param name="page">Page where property is located.</param>
    /// <param name="name">Name of the property.</param>
    /// <param name="value">Unevaluated string value to set.</param>
    public virtual void SetProperty(string page, string name, string value)
    {
      IVCRulePropertyStorage pageStorage = configuration_.Rules.Item(page);
      pageStorage.SetPropertyValue(name, value);
    }

    /// <summary>
    /// Ensures that the current target has the NaCl platform and throws if not.
    /// </summary>
    private void AssertNaCl()
    {
      if (PlatformType != ProjectPlatformType.NaCl)
      {
        throw new Exception(string.Format(
          "Cannot read NaCl only property on {0} platform", configuration_.Platform.Name));
      }
    }

    /// <summary>
    /// Ensures that the current target has the Pepper platform and throws if not.
    /// </summary>
    private void AssertPepper()
    {
      if (PlatformType != ProjectPlatformType.Pepper)
      {
        throw new Exception(string.Format(
          "Cannot read Pepper only property on {0} platform", configuration_.Platform.Name));
      }
    }

    /// <summary>
    /// Ensures the current target is either the NaCl or Pepper platform. Throws if not.
    /// </summary>
    private void AssertValidPlatform()
    {
      if (PlatformType == ProjectPlatformType.Other)
      {
        throw new Exception(string.Format(
          "Unsupported platform type: {0} platform", configuration_.Platform.Name));
      }
    }
  }
}
