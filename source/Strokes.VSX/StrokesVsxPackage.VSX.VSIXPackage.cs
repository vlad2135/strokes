﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CSharpAchiever.VSX;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Strokes.BasicAchievements.NRefactory;
using Strokes.Core;
using Strokes.Core.Integration;
using Strokes.Core.Service;
using Strokes.Core.Service.Model;
using Strokes.Data;
using Strokes.GUI;
using Strokes.GUI.ViewModels;
using Strokes.Service;
using Strokes.Service.Data;
using Strokes.VSX.Trackers;
using StructureMap;

namespace Strokes.VSX
{
    /// <summary>
    /// This is UICONTEXT_NoSolution package - meaning Strokes will start together 
    /// with Visual Studio regardless of which type of project is loaded.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("{adfc4e64-0397-11d1-9f4e-00a0c911004f}")]
    [ProvideToolWindow(typeof(AchievementStatisticsToolWindow), Style = VsDockStyle.MDI)]
    [ProvideService(typeof(IAchievementLibraryService))]
    [ProvideService(typeof(IAchievementService))]
    [Guid(GuidList.guidCSharpAchiever_Achiever_VSIXPkgString)]
    public sealed class StrokesVsxPackage : PackageEx, IAchievementLibraryService
    {
        /// <summary>
        /// Cookie that allows the package to stop listening for build events.
        /// </summary>
        private uint updateSolutionEventsCookie = 0;

        private readonly IAchievementService _achievementService;
        private readonly ISettingsRepository _settingsRepository;
        /// <summary>
        /// Initializes a new instance of the <see cref="StrokesVsxPackage"/> class.
        /// </summary>
        public StrokesVsxPackage()
        {
            ObjectFactory.Configure(a =>
            {
                a.For<IAchievementRepository>().Singleton().Use<AppDataXmlCompletedAchievementsRepository>().Ctor<string>("storageFile").Is("AchievementStorage.xml");
                a.For<ISettingsRepository>().Singleton().Use<AppDataXmlSettingsRepository>().Ctor<string>("storageFile").Is("SettingsStorage.xml");
                a.For<IAchievementService>().Singleton().Use<ParallelStrokesAchievementService>();
            });

            _achievementService = ObjectFactory.GetInstance<IAchievementService>();
            _settingsRepository = ObjectFactory.GetInstance<ISettingsRepository>();
        }

        /// <summary>
        /// Registers a achievement assembly.
        /// </summary>
        /// <param name="assembly">The assembly to register.</param>
        public void RegisterAchievementAssembly(Assembly assembly)
        {
            _achievementService.LoadAchievementsFrom(assembly);
        }

        /// <summary>
        /// Gets the environment's status bar.
        /// </summary>
        private IVsStatusbar StatusBar
        {
            get
            {
                return GetService<SVsStatusbar>() as IVsStatusbar;
            }
        }

        /// <summary>
        /// Gets the solution's build manager.
        /// </summary>
        private IVsSolutionBuildManager2 SolutionBuildManager
        {
            get
            {
                return ServiceProvider.GlobalProvider.GetService<SVsSolutionBuildManager>() as IVsSolutionBuildManager2;
            }
        }

        /// <summary>
        /// Gets the top-level object in the Visual Studio automation object model.
        /// </summary>
        private DTE DTE
        {
            get
            {
                return GetService<DTE>();
            }
        }

        /// <summary>
        /// Gets the service used to add handlers for menu commands and to define verbs.
        /// </summary>
        private OleMenuCommandService MenuService
        {
            get
            {
                return GetService<IMenuCommandService>() as OleMenuCommandService;
            }
        }

        /// <summary>
        /// Called when the VSPackage is loaded by Visual Studio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            var preferredLocale = _settingsRepository.GetSettings().PreferredLocale;
            try
            {
                var cInfo = preferredLocale == string.Empty ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(preferredLocale);
                System.Threading.Thread.CurrentThread.CurrentUICulture = cInfo;
            }
            catch
            {
                //Ignored. This will throw if the culture in the settings repo isn't a valid culture.
            }

            if (MenuService != null)
            {
                var menuCommandID = new CommandID(
                    GuidList.guidCSharpAchiever_Achiever_VSIXCmdSet,
                    (int)PkgCmdIDList.showAchievementIndex);

                var menuItem = new MenuCommand(MenuItemCallback, menuCommandID);

                MenuService.AddCommand(menuItem);
            }

            if (SolutionBuildManager != null)
            {
                var buildTracker = new BuildTracker(DTE, _achievementService);

                SolutionBuildManager.AdviseUpdateSolutionEvents(buildTracker, out updateSolutionEventsCookie);
            }

            AddService<IAchievementLibraryService>(this, true);
            AddService(_achievementService, true);

            RegisterAchievementAssembly(typeof(NRefactoryAchievement).Assembly);

            AchievementUIContext.AchievementClicked += AchievementContext_AchievementClicked;
            _achievementService.StaticAnalysisCompleted += DetectionDispatcher_DetectionCompleted;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; 
        ///     <c>false</c> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (SolutionBuildManager != null && updateSolutionEventsCookie != 0)
                SolutionBuildManager.UnadviseUpdateSolutionEvents(updateSolutionEventsCookie);
        }

        /// <summary>
        /// Handles the DetectionCompleted event of the DetectionDispatcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">
        ///     The <see cref="DetectionCompletedEventArgs"/> instance containing the event data.
        /// </param>
        private void DetectionDispatcher_DetectionCompleted(object sender, StaticAnalysisEventArgs e)
        {
            StatusBar.SetText(string.Format("{0} achievements tested in {1} milliseconds",
                e.AchievementsTested, e.ElapsedMilliseconds));
        }

        /// <summary>
        /// Handles the AchievementClicked event of the AchievementContext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">
        ///     The <see cref="AchievementClickedEventArgs"/> instance containing the event data.
        /// </param>
        private void AchievementContext_AchievementClicked(object sender, AchievementClickedEventArgs args)
        {
            DTE.ItemOperations.OpenFile(args.AchievementDescriptor.CodeOrigin.FileName, EnvDTE.Constants.vsViewKindCode);
        }

        /// <summary>
        /// Menus the item callback.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            ShowAchievementPane(true);
        }

        /// <summary>
        /// Shows the achievement pane.
        /// </summary>
        /// <param name="activate">
        ///     <c>true</c> to show and makes the achievement pane the active window.
        ///     <c>true</c> to show without making the achievement pane the active window.
        /// </param>
        private void ShowAchievementPane(bool activate)
        {
            var window = FindToolWindow<AchievementStatisticsToolWindow>(0, true);
            var windowFrame = window.Frame as IVsWindowFrame;

            if (activate)
                windowFrame.Show();
            else
                windowFrame.ShowNoActivate();
        }
    }
}
