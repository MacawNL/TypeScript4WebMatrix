using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TypeScript4WebMatrix
{
    [Export(typeof (Extension))]
    public class TypeScript4WebMatrix : Extension
    {
        private readonly BitmapImage _typescriptCompileImageSmall = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptCompile_16x16.png", UriKind.Absolute));
        private readonly BitmapImage _typescriptCompileImageLarge = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptCompile_32x32.png", UriKind.Absolute));
        private readonly BitmapImage _typescriptUpdateImageSmall = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptUpdate_16x16.png", UriKind.Absolute));
        private readonly BitmapImage _typescriptUpdateImageLarge = new BitmapImage(new Uri("pack://application:,,,/TypeScript4WebMatrix;component/TypeScriptUpdate_32x32.png", UriKind.Absolute));
        
        private IWebMatrixHost _webMatrixHost;              // Keep reference to the WebMatrix host interface.
        private IEditorTaskPanelService _editorTaskPanel;   // Keep reference to the EditorTaskPanelService.

        [Import(typeof (IEditorTaskPanelService))]
        private IEditorTaskPanelService EditorTaskPanelService
        {
            get { return _editorTaskPanel; }
            set { _editorTaskPanel = value; }
        }

        private DesignFactory.WebMatrix.IExecuter.IExecuter _executer;

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

            initData.RibbonItems.Add(
                new RibbonGroup(
                    "TypeScript",
                    new RibbonItem[]
                        {
                            new RibbonButton(
                        "Compile TypeScript",
                        new DelegateCommand(CanExecute, HandleRibbonButtonCompileTypeScriptInvoke),
                        null,
                        _typescriptCompileImageSmall,
                        _typescriptCompileImageLarge),
                            new RibbonButton(
                        "Update software",
                        new DelegateCommand(CanExecute, HandleRibbonButtonUpdateSoftwareInvoke),
                        null,
                        _typescriptUpdateImageSmall,
                        _typescriptUpdateImageLarge)
                        }));

            _executer = DesignFactory.WebMatrix.ExecuterFactory.GetExecuter(
                "TypeScript", _webMatrixHost, _editorTaskPanel);

            _webMatrixHost.WebSiteChanged += (sender, e) => _executer.InitializeTabs();

            // Register handler for right-click context menu
            _webMatrixHost.ContextMenuOpening += new EventHandler<ContextMenuOpeningEventArgs>(webMatrixHost_ContextMenuOpening);
        }

        private bool CanExecute(object parmeter)
        {
            return (_executer != null && !_executer.IsRunning());
        }

        /// <summary>
        /// Called when the Ribbon button "Check for update" is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        private void HandleRibbonButtonUpdateSoftwareInvoke(object parameter)
        {
            try
            {
                if (!_executer.Start()) { return; }
                UpdateSoftwareAsync().ContinueWith((antecedent) => _executer.End());
            }
            catch(Exception ex)
            {
                _webMatrixHost.ShowExceptionMessage("TypeScript software update", "The following error occured. Please report at https://github.com/MacawNL/TypeScript4WebMatrix/issues.", ex);
            }
        }

        /// <summary>
        /// Called when the Ribbon button "Compile TypeScript" is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        private void HandleRibbonButtonCompileTypeScriptInvoke(object parameter)
        {
            try
            {
                if (!_executer.Start()) { return; }
                var tsfiles = Directory.GetFiles(_webMatrixHost.WebSite.Path, "*.ts", SearchOption.AllDirectories);
                CompileTypeScriptFilesAsync(tsfiles).ContinueWith((antecedent) => _executer.End());
                }
            catch(Exception ex)
            {
                _webMatrixHost.ShowExceptionMessage("Compile TypeScript", "The following error occured. Please report at https://github.com/MacawNL/TypeScript4WebMatrix/issues.", ex);
            }
 
        }

        void webMatrixHost_ContextMenuOpening(object sender, ContextMenuOpeningEventArgs e)
        {
            try
            {
                ISiteItem siteItem = e.Items.FirstOrDefault<ISiteItem>();
                if (siteItem != null)
                {
                    ISiteFolder siteFolder = siteItem as ISiteFolder;
                    if (siteFolder != null) // folders must end with '\'
                    {
                        e.AddMenuItem(new ContextMenuItem("Compile TypeScript files in folder", _typescriptCompileImageSmall,
                                                          new DelegateCommand(CanExecute, (parameter) =>
                                                              {
                                                                  if (!_executer.Start())
                                                                  {
                                                                      return;
                                                                  }
                                                                  var tsfiles = Directory.GetFiles(siteFolder.Path, "*.ts", SearchOption.AllDirectories);
                                                                  CompileTypeScriptFilesAsync(tsfiles).ContinueWith((antecedent) => _executer.End());
                                                              }), null));
                    }
                    else
                    {
                        var siteFile = siteItem as ISiteFile;
                        if (siteFile != null && Path.GetExtension(siteFile.Path) == ".ts")
                        {
                            e.AddMenuItem(new ContextMenuItem("Compile TypeScript file", _typescriptCompileImageSmall,
                                                              new DelegateCommand(CanExecute, (parameter) =>
                                                                  {
                                                                      if (!_executer.Start())
                                                                      {
                                                                          return;
                                                                      }
                                                                      var tsfiles = new string[] {siteFile.Path};
                                                                      CompileTypeScriptFilesAsync(tsfiles).ContinueWith((antecedent) => _executer.End());
                                                                  }), null));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _webMatrixHost.ShowExceptionMessage("Compile TypeScript (context menu)", "The following error occured. Please report at https://github.com/MacawNL/TypeScript4WebMatrix/issues.", ex);
            }
        }

        private Task UpdateSoftwareAsync()
        {
            return Task.Factory.StartNew(UpdateSoftware, _executer.GetCancellationToken());
        }

        private void UpdateSoftware()
        {
            // When we want to update node.exe itself as well, latest distro at http://nodejs.org/dist/latest/node.exe 
            var extensionFolder = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var nodeExe = Path.Combine(extensionFolder, @"node.exe");
            var npmFile = Path.Combine(extensionFolder, @"node_modules\npm\cli.js");
            string typeScriptModule = Path.Combine(extensionFolder, @"node_modules\typescript");

            _executer.RunAsync(nodeExe, String.Format("\"{0}\" update -g npm", npmFile)).Wait();
            if (Directory.Exists(typeScriptModule))
            {
                _executer.RunAsync(nodeExe, String.Format("\"{0}\" update -g typescript", npmFile)).Wait();
            }
            else
            {
                _executer.RunAsync(nodeExe, String.Format("\"{0}\" install -g typescript", npmFile)).Wait();
            }
            _executer.WriteLine("All software updated.");
        }


        private Task CompileTypeScriptFilesAsync(string[] tsfiles)
        {
            Task compilationTask = null;
            var extensionFolder = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            string typeScriptModule = Path.Combine(extensionFolder, @"node_modules\typescript");
            if (!Directory.Exists(typeScriptModule))
            {
                _executer.WriteLine("TypeScript not installed yet. Updating software first.");
                compilationTask = UpdateSoftwareAsync().ContinueWith((antecedent) => CompileTypeScriptFiles(tsfiles), _executer.GetCancellationToken());
            }
            else
            {
                compilationTask = Task.Factory.StartNew(() => CompileTypeScriptFiles(tsfiles), _executer.GetCancellationToken());
            }
            return compilationTask;
        }

        private void CompileTypeScriptFiles(string[] tsfiles)
        {
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
                    Compile(tsfile);
                }
            }
        }

        private bool Compile(string tsfile)
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
                // 3. There is a space between filename and (line,column), in above example: Raytracer.ts (6,27): but beware for Raytrace (1).ts (6,27):
                // 4. In path slashes are forwards, not backwards
                // Our implementation for now is a bit "rude", but seems to do the job in most cases.
                string errorFormatFilename = tsfile.Replace(@"\", "/");
                int errorFormatFilenameLength = errorFormatFilename.Length;
                if (output.StartsWith(errorFormatFilename)) // looks like a reported error, remove space between filename and (, like in ..../Raytracer.ts (6,27)
                {
                    if (output.Length > errorFormatFilenameLength + 2 && output[errorFormatFilenameLength] == ' ' && output[errorFormatFilenameLength + 1] == '(')
                    {
                        output = tsfile + output.Substring(errorFormatFilenameLength + 1).Replace("):", "): error :");
                    }
                }
                return output;
            });

            // Delete the resulting javascript file, otherwise javascript file exists if compilation fails
            string jsfile = Path.ChangeExtension(tsfile, ".js");
            if (File.Exists(jsfile))
            {
                File.Delete(jsfile);
            }

            Task<bool> task = _executer.RunAsync(nodeExe, String.Format("\"{0}\" \"{1}\"", tscNode, tsfile));
            task.Wait();
            _executer.ConfigureParsing(null, null); // reset to no special processing
            return task.Result;
        }
    }
}
