using System;
using System.Linq;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;
using System.Text.RegularExpressions;

namespace TypeScript4WebMatrix
{
    /// <summary>
    /// A sample WebMatrix extension.
    /// </summary>
    [Export(typeof(Extension))]
    public class TypeScript4WebMatrix : Extension
    {
        /// <summary>
        /// Stores a reference to the small TypeScript image.
        /// </summary>
        private readonly BitmapImage _typescriptCompileImageSmall = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptCompile_16x16.png", UriKind.Absolute));

        /// <summary>
        /// Stores a reference to the large TypeScript image.
        /// </summary>
        private readonly BitmapImage _typescriptCompileImageLarge = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptCompile_32x32.png", UriKind.Absolute));
        /// <summary>
        /// Stores a reference to the small TypeScript image.
        /// </summary>
        private readonly BitmapImage _typescriptUpdateImageSmall = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptUpdate_16x16.png", UriKind.Absolute));

        /// <summary>
        /// Stores a reference to the large TypeScript image.
        /// </summary>
        private readonly BitmapImage _typescriptUpdateImageLarge = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptUpdate_32x32.png", UriKind.Absolute));

        /// <summary>
        /// Stores a reference to the WebMatrix host interface.
        /// </summary>
        private IWebMatrixHost _webMatrixHost;

        /// <summary>
        /// Reference to the EditorTaskPanelService.
        /// </summary>
        private IEditorTaskPanelService _editorTaskPanel;

        [Import(typeof(IEditorTaskPanelService))]
        private IEditorTaskPanelService EditorTaskPanelService
        {
            get
            {
                return _editorTaskPanel;
            }
            set
            {
                _editorTaskPanel = value;
            }
        }

        DesignFactory.WebMatrix.IExecuter.IExecuter _executer;

        /// <summary>
        /// Initializes a new instance of the TypeScript4WebMatrix class.
        /// </summary>
        public TypeScript4WebMatrix()
            : base("TypeScript4WebMatrix")
        {
        }

        /// <summary>
        /// Called to initialize the extension.
        /// </summary>
        /// <param name="host">WebMatrix host interface.</param>
        /// <param name="initData">Extension initialization data.</param>
        protected override void Initialize(IWebMatrixHost host, ExtensionInitData initData)
        {
            _webMatrixHost = host;

            // Add a simple button to the Ribbon
            initData.RibbonItems.Add(
                new RibbonGroup(
                    "TypeScript",
                    new RibbonItem[]
                    {
                        new RibbonButton(
                            "Compile TypeScript",
                            new DelegateCommand(HandleRibbonButtonCompileTypeScriptInvoke),
                            null,
                            _typescriptCompileImageSmall,
                            _typescriptCompileImageLarge),
                        new RibbonButton(
                            "Update software",
                            new DelegateCommand(HandleRibbonButtonUpdateSoftwareInvoke),
                            null,
                            _typescriptUpdateImageSmall,
                            _typescriptUpdateImageLarge)
                    }));

            _executer = DesignFactory.WebMatrix.ExecuterFactory.GetExecuter(
                "TypeScript", _webMatrixHost, _editorTaskPanel);

            _webMatrixHost.WebSiteChanged += (sender, e) => { Reinitialize(); };
            Reinitialize();
        }

        private void Reinitialize()
        {
            _executer.InitializeTabs();
            // Register handler for right-click context menu
            _webMatrixHost.ContextMenuOpening += new EventHandler<ContextMenuOpeningEventArgs>(webMatrixHost_ContextMenuOpening);
        }

        /// <summary>
        /// Called when the Ribbon button "Check for update" is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        private async void HandleRibbonButtonUpdateSoftwareInvoke(object parameter)
        {
            await UpdateSoftwareAsync();
        }

        /// <summary>
        /// Called when the Ribbon button "Compile TypeScript" is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        private async void HandleRibbonButtonCompileTypeScriptInvoke(object parameter)
        {
            // Find all .ts files in project folder
            var tsfiles = Directory.GetFiles(_webMatrixHost.WebSite.Path, "*.ts", SearchOption.AllDirectories);
            await CompileTypeScriptFilesAsync(tsfiles);
        }


        private async Task UpdateSoftwareAsync()
        {
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = tokenSource.Token;
                bool isCancelled = false;
                _executer.Start(() =>
                {
                    isCancelled = true;
                    tokenSource.Cancel();
                });
                await Task.Run(async () => await UpdateSoftware(), cancellationToken);
                _executer.End(isCancelled);
            }
            catch (Exception ex)
            {
                _webMatrixHost.ShowExceptionMessage("TypeScript software update", "The following error occured:", ex);
            }
        }

        private async Task UpdateSoftware()
        {
            // When we want to update node.exe itself as well, latest distro at http://nodejs.org/dist/latest/node.exe 
            var extensionFolder = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var nodeExe = Path.Combine(extensionFolder, @"node.exe");
            var npmFile = Path.Combine(extensionFolder, @"node_modules\npm\cli.js");
            string typeScriptModule = Path.Combine(extensionFolder, @"node_modules\typescript");

            await _executer.RunAsync(nodeExe, String.Format("\"{0}\" update -g npm", npmFile));
            if (Directory.Exists(typeScriptModule))
            {
                await _executer.RunAsync(nodeExe, String.Format("\"{0}\" update -g typescript", npmFile));
            }
            else
            {
                await _executer.RunAsync(nodeExe, String.Format("\"{0}\" install -g typescript", npmFile));
            }
            _executer.WriteLine("All software updated.");
        }

        void webMatrixHost_ContextMenuOpening(object sender, ContextMenuOpeningEventArgs e)
        {
            ISiteItem siteItem = e.Items.FirstOrDefault<ISiteItem>();
            if (siteItem != null)
            {
                ISiteFolder siteFolder = siteItem as ISiteFolder;
                if (siteFolder != null) // folders must end with '\'
                {
                    e.AddMenuItem(new ContextMenuItem("Compile TypeScript files in folder", _typescriptCompileImageSmall,
                        new DelegateCommand(async (parameter) =>
                        {
                            var tsfiles = Directory.GetFiles(siteFolder.Path, "*.ts", SearchOption.AllDirectories);
                            await CompileTypeScriptFilesAsync(tsfiles);
                        }), null));
                }
                else
                {
                    ISiteFile siteFile = siteItem as ISiteFile;
                    if (siteFile != null && Path.GetExtension(siteFile.Path) == ".ts")
                    {
                        e.AddMenuItem(new ContextMenuItem("Compile TypeScript file", _typescriptCompileImageSmall,
                            new DelegateCommand(async (parameter) => 
                            {
                                var tsfiles = new string[] { siteFile.Path };
                                await CompileTypeScriptFilesAsync(tsfiles);
                            }), null));
                    }
                }
            }
        }

        private async Task CompileTypeScriptFilesAsync(string[] tsfiles)
        {
            var extensionFolder = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            string typeScriptModule = Path.Combine(extensionFolder, @"node_modules\typescript");
            if (!Directory.Exists(typeScriptModule))
            {
                await UpdateSoftwareAsync();
            }

            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = tokenSource.Token;
                bool isCancelled = false;
                _executer.Start(() =>
                {
                    isCancelled = true;
                    tokenSource.Cancel();
                });
                await Task.Run(async () => await CompileTypeScriptFiles(tsfiles), cancellationToken);
                _executer.End(isCancelled);
            }
            catch (Exception ex)
            {
                _webMatrixHost.ShowExceptionMessage("TypeScript compilation", "The following error occured:", ex);
            }
        }

        private async Task CompileTypeScriptFiles(string[] tsfiles)
        {
            bool refreshRequired = false; // assume we don't have to refresh the tree, if new js files are created do refresh

            _executer.WriteLine("Number of TypeScript files found: {0}", tsfiles.Length);
            foreach (var tsfile in tsfiles)
            {
                var tsfileRelative = tsfile.Substring(_webMatrixHost.WebSite.Path.Length); 

                 // Skip any .ts files that are in a node module
                if (tsfile.Contains(@"\node_modules\"))
                {
                    _executer.WriteLine(@"TypeScript file '~{0}' is located in a Node module and will not be compiled", tsfileRelative);
                    continue;
                }
                
                var jsfile = Path.Combine(Path.GetDirectoryName(tsfile), Path.GetFileNameWithoutExtension(tsfile) + ".js");
                var compile = false; // assume we don't compile
                if (!File.Exists(jsfile))
                {
                    compile = true;
                    refreshRequired = true;
                }
                else
                {
                    // if newer than correspondsing .js file then compile
                    if (File.GetLastWriteTime(tsfile) > File.GetLastWriteTime(jsfile))
                    {
                        compile = true;
                    }
                    else
                    {
                        _executer.WriteLine(@"Compilation of TypeScript file '~{0}' is up to date", tsfileRelative);
                    }
                }

                if (compile)
                {
                    // Save the file on the UI thread
                    _executer.UIThreadDispatcher.Invoke(new Action(() =>
                        {
                            var save = _webMatrixHost.HostCommands.GetCommand(Microsoft.WebMatrix.Extensibility.CommonCommandIds.GroupId, (int)Microsoft.WebMatrix.Extensibility.CommonCommandIds.Ids.Save);
                            if (save.CanExecute(tsfile))
                            {
                                save.Execute(tsfile);
                            }
                        }));
                    _executer.WriteLine(@"Compiling TypeScript file '~{0}'...", tsfileRelative);
                    await Compile(tsfile);
                }
            }

            if (refreshRequired)
            {
                // Save the tree on the UI thread
                _executer.UIThreadDispatcher.Invoke(new Action(() =>
                {
                    var save = _webMatrixHost.HostCommands.GetCommand(Microsoft.WebMatrix.Extensibility.CommonCommandIds.GroupId, (int)Microsoft.WebMatrix.Extensibility.CommonCommandIds.Ids.Refresh);
                    if (save.CanExecute(null))
                    {
                        save.Execute(null);
                    }
                }));

            }
        }

        private async Task Compile(string tsfile)
        {
            var extensionFolder = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var nodeExe = Path.Combine(extensionFolder, @"node.exe");

            // If there is a node module in the site folder containing the TypeScript compiler, use it.
            string tscNode;
            string typescriptNodeModuleTscNode = Path.Combine(_webMatrixHost.WebSite.Path, @"node_modules\typescript\bin\tsc");
            if (File.Exists(typescriptNodeModuleTscNode))
            {
                tscNode = typescriptNodeModuleTscNode;
            }
            else
            {
                tscNode = Path.Combine(extensionFolder, @"node_modules\typescript\bin\tsc");
            }

            _executer.ConfigureParsing(null, (output) =>
            {
                // Need to do some replacements on output by tsc, the TypeScript compiler.
                // There is no text "error" or "warning" in the reported errors. Errors
                // are in the following format: 
                // C:/Users/serge/Documents/My Web Sites/NodeStarterSite/Raytracer.ts (6,27): Expected ';'
                // Issues encountered: (reported as http://typescript.codeplex.com/workitem/208)
                // 1. I can't determine if this line is an error, warning or just an informational message
                // 2. The error format is not in the MSBuild error format (described in http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx)
                // 3. There is there a space between filename and (line,column), in above example: Raytracer.ts (6,27): 
                // 4. In path slashes are forwards, not backwards
                // Our implementation for now is a bit "rude", but seems to do the job in most cases.
                output = output.Replace(" (", "(").Replace("):", "): error :").Replace('/', '\\');
                return output;
            });
            bool success = await _executer.RunAsync(nodeExe, String.Format("\"{0}\" \"{1}\"", tscNode, tsfile));
            _executer.ConfigureParsing(null, null); // reset to no special processing
        }
    }
}
