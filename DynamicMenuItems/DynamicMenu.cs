using DynamicMenuItems.Classes;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;

namespace DynamicMenuItems
{
    internal sealed class DynamicMenu
    {
        private List<Tuple<int, string, string>> _menuItems = new List<Tuple<int, string, string>>();
        private DynamicItemMenuCommand _rootMenuItem;

        private int _idCount = 0;
        private DTE2 _dte2;

        public static readonly Guid CommandSet = new Guid("acac0ca9-d496-4208-9d28-07e6c887f79b");

        private readonly Package _package;

        private DynamicMenu(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            _menuItems.Add(Tuple.Create(this._idCount++, "Classes", "Do Class 1"));
            _menuItems.Add(Tuple.Create(this._idCount++, "Classes", "Do Class 2"));
            _menuItems.Add(Tuple.Create(this._idCount++, "Classes", "Do Class 3"));
            _menuItems.Add(Tuple.Create(this._idCount++, "Resources", "Do Resource 1"));
            _menuItems.Add(Tuple.Create(this._idCount++, "Resources", "Do Resource 2"));
            _menuItems.Add(Tuple.Create(this._idCount++, "Resources", "Do Resource 3"));

            _dte2 = (DTE2)this.ServiceProvider.GetService(typeof(DTE));

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                // Add the DynamicItemMenuCommand for the expansion of the root item into N items at run time.
                var dynamicItemRootId = new CommandID(new Guid(DynamicMenuPackageGuids.guidDynamicMenuPackageCmdSet), (int)DynamicMenuPackageGuids.cmdidMyCommand);
                this._rootMenuItem = new DynamicItemMenuCommand(dynamicItemRootId,
                    IsValidDynamicItem,
                    OnInvokedDynamicItem,
                    OnBeforeQueryStatusDynamicItem);
                commandService.AddCommand(this._rootMenuItem);

                var menuContainer = new DynamicItemMenuContainer(commandService, this._dte2, this._menuItems);
            }
        }

        private void OnInvokedDynamicItem(object sender, EventArgs args)
        {
            var invokedCommand = (DynamicItemMenuCommand)sender;
            var uih = _dte2.ToolWindows.SolutionExplorer;
            var selectedItems = (Array)uih.SelectedItems;
            var selectedItem = selectedItems.GetValue(0) as UIHierarchyItem;
            var testName = "";
            var prjItem = selectedItem.Object as ProjectItem;
            if (prjItem == null)
            {
                var prj = selectedItem.Object as Project;
                if (prj == null) return;
                else testName = prj.Name;
            }
            else
            {
                testName = prjItem.Name;
            }

            var matches = this._menuItems.Where(c => c.Item2 == testName).OrderBy(c => c.Item1).ToList();

            if (matches.Count == 0) return;

            var isRootItem = (invokedCommand.MatchedCommandId == 0);
            // The index is set to 1 rather than 0 because the Solution.Projects collection is 1-based.
            var indexForDisplay = (isRootItem ? 0 : (invokedCommand.MatchedCommandId - (int)DynamicMenuPackageGuids.cmdidMyCommand));
            var match = matches[indexForDisplay];

            if (match == null) return;

            var newId = this._idCount++;

            _menuItems.Add(Tuple.Create(newId, "ConsoleApplication4", string.Concat("Text From Code: ", newId)));

            var message = string.Format(CultureInfo.CurrentCulture, "Woo Hoo, you clicked item {0} - {1}", match.Item1, match.Item3);
            var title = "DynamicMenu";

            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private bool IsValidDynamicItem(int commandId)
        {
            var uih = _dte2.ToolWindows.SolutionExplorer;
            var selectedItems = (Array)uih.SelectedItems;
            var selectedItem = selectedItems.GetValue(0) as UIHierarchyItem;
            var testName = "";
            var prjItem = selectedItem.Object as ProjectItem;

            if (prjItem == null)
            {
                var prj = selectedItem.Object as Project;
                if (prj == null) return false;
                else testName = prj.Name;
            }
            else
            {
                testName = prjItem.Name;
            }

            var matchCount = this._menuItems.Where(c => c.Item2 == testName).OrderBy(c => c.Item1).Count();

            if (matchCount == 0) return false;

            //System.Diagnostics.Debug.WriteLine("Is Valid Dynamic Item -- Command Id: {0}, {1}, {2}", commandId, (int)DynamicMenuPackageGuids.cmdidMyCommand, commandId - (int)DynamicMenuPackageGuids.cmdidMyCommand);

            // The match is valid if the command ID is >= the id of our root dynamic start item
            // and the command ID minus the ID of our root dynamic start item
            // is less than or equal to the number of projects in the solution.
            return (commandId >= (int)DynamicMenuPackageGuids.cmdidMyCommand) && ((commandId - (int)DynamicMenuPackageGuids.cmdidMyCommand) < matchCount);
        }

        private void OnBeforeQueryStatusDynamicItem(object sender, EventArgs args)
        {
            var matchedCommand = (DynamicItemMenuCommand)sender;
            var uih = _dte2.ToolWindows.SolutionExplorer;
            var selectedItems = (Array)uih.SelectedItems;
            var selectedItem = selectedItems.GetValue(0) as UIHierarchyItem;
            var testName = "";
            var prjItem = selectedItem.Object as ProjectItem;

            if (prjItem == null)
            {
                var prj = selectedItem.Object as Project;
                if (prj == null)
                {
                    matchedCommand.Enabled = false;
                    matchedCommand.Visible = false;
                    matchedCommand.MatchedCommandId = 0;
                    return;
                }
                else
                {
                    testName = prj.Name;
                }
            }
            else
            {
                testName = prjItem.Name;
            }

            var matches = this._menuItems.Where(c => c.Item2 == testName).OrderBy(c => c.Item1).ToList();

            if (matches.Count == 0)
            {
                matchedCommand.Enabled = false;
                matchedCommand.Visible = false;
                matchedCommand.MatchedCommandId = 0;
                return;
            }

            matchedCommand.Enabled = true;
            matchedCommand.Visible = true;

            // Find out whether the command ID is 0, which is the ID of the root item.
            // If it is the root item, it matches the constructed DynamicItemMenuCommand,
            // and IsValidDynamicItem won't be called.
            var isRootItem = (matchedCommand.MatchedCommandId == 0);

            // The index is set to 1 rather than 0 because the Solution.Projects collection is 1-based.
            var indexForDisplay = (isRootItem ? 0 : (matchedCommand.MatchedCommandId - (int)DynamicMenuPackageGuids.cmdidMyCommand));

            matchedCommand.Text = matches[indexForDisplay].Item3;
            matchedCommand.MatchedCommandId = 0;

            System.Diagnostics.Debug.WriteLine("On Before Query Status Dynamic Item -- {0}, {1}, {2}", indexForDisplay, matchedCommand.MatchedCommandId, matchedCommand.Text);
        }

        public static DynamicMenu Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this._package;
            }
        }

        public static void Initialize(Package package)
        {
            Instance = new DynamicMenu(package);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "DynamicMenu";
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}