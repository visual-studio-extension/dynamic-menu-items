using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace DynamicMenuItems
{
    class Guids
    {
        public static readonly Guid GuidDynamicMenuPackageCmdSet = new Guid("acac0ca9-d496-4208-9d28-07e6c887f79b");  // get the GUID from the .vsct file
        public const int DynamicStartButton = 0x0104;
    }

    internal sealed class DynamicMenu
    {
        private static List<OleMenuCommand> _commands;
        private readonly Package _package;

        private DynamicMenu(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var commandId = new CommandID(Guids.GuidDynamicMenuPackageCmdSet, Guids.DynamicStartButton);
                var command = new OleMenuCommand(DynamicStartCommandCallback, commandId);
                command.Visible = false;
                command.BeforeQueryStatus += DynamicStartBeforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private void DynamicStartBeforeQueryStatus(object sender, EventArgs e)
        {
            var currentCommand = sender as OleMenuCommand;
            currentCommand.Visible = true;
            currentCommand.Text = "Init";
            currentCommand.Enabled = true;

            CreateCommands();
        }

        private void CreateCommands()
        {
            var mcs = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (_commands == null)
                _commands = new List<OleMenuCommand>();

            foreach (var cmd in _commands)
            {
                mcs.RemoveCommand(cmd);
            }

            var list = new List<string>
                {
                    "Hello A",
                    "Hello B",
                    "Hello C"
                };

            var j = 1;
            foreach (var ele in list)
            {
                var menuCommandID = new CommandID(Guids.GuidDynamicMenuPackageCmdSet, Guids.DynamicStartButton + j++);
                var command = new OleMenuCommand(this.DynamicStartCommandCallback, menuCommandID);
                command.Text = "Cake: " + ele;
                command.BeforeQueryStatus += (x, y) => { (x as OleMenuCommand).Visible = true; };
                _commands.Add(command);
                mcs.AddCommand(command);
            }
        }

        private void DynamicStartCommandCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var text = cmd.Text;
            ShowDialog("Hello", text);
        }

        private void ShowDialog(string title, string message)
        {
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
    }
}