using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;

/// Contains actions performed during installation of Visual Localizer.
namespace VLSetupFinalizer {

    /// <summary>
    /// Implements actions performed after installation and uninstallation. In both cases, command "devenv /setup /nosetupvstemplates"
    /// gets executed, with "devenv" being located in Common7/IDE folder of respective VS installation.
    /// </summary>
    [RunInstaller(true)]
    public partial class Register : Installer {        

        public override void Rollback(IDictionary savedState) {
            string[] tokens = null;
            if (savedState.Contains("uninstPaths")) {
                // get paths of "devenv" files, where VL is registered
                string paths = (string)savedState["uninstPaths"];

                if (paths != null)
                    tokens = paths.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            }

            base.Rollback(savedState);

            if (tokens != null) {
                // execute command on each devenv
                foreach (string path in tokens)
                    Process.Start(path, "/setup /nosetupvstemplates").WaitForExit();
            }
        }

        public override void Install(IDictionary stateSaver) {
            base.Install(stateSaver);
            List<string> devenvPaths = new List<string>();

            // parameters contain data sent from GUI - key can be either "checkbox2008", "checkbox2010", "checkbox2012" and "checkbox2013"
            foreach (DictionaryEntry param in this.Context.Parameters) {
                string key = (param.Key == null ? "0" : param.Key.ToString());
                string value = (param.Value == null ? "0" : param.Value.ToString());
                
                // if user checked the option in installer GUI
                if (key.ToLower().StartsWith("checkbox") && value == "1")
                    RegisterToVS(key, devenvPaths);                
            }

            string paths = string.Join("|", devenvPaths.ToArray());            
            stateSaver.Add("uninstPaths", paths); // devenvPaths contains all paths to "devenv" files, where VL was registered
        }

        public override void Uninstall(IDictionary savedState) {
            string[] tokens = null;
            if (savedState.Contains("uninstPaths")) {
                // get paths of "devenv" files, where VL is registered
                string paths = (string)savedState["uninstPaths"];                

                if (paths != null)
                    tokens = paths.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            }

            base.Uninstall(savedState);

            if (tokens != null) {
                // execute command on each devenv
                foreach (string path in tokens)                     
                    Process.Start(path, "/setup /nosetupvstemplates").WaitForExit();                
            }
        }      

        /// <summary>
        /// Registers VL in VS specified by param - executes "devenv /setup /nosetupvstemplates"
        /// </summary>
        /// <param name="param">checkbox2008, checkbox2010, checkbox2012 or checkbox2013</param>
        private void RegisterToVS(string param, List<string> devenvPaths) {
            string key;
            string subpath;            
            GetInstallKey(param, out key, out subpath);
            
            using (RegistryKey setupKey = Registry.LocalMachine.OpenSubKey(key)) {
                if (setupKey != null) {
                    object registryPath = setupKey.GetValue("ProductDir");                    
                    if (registryPath != null) {
                        string devenv = Path.Combine(registryPath.ToString(), subpath);
                        if (!string.IsNullOrEmpty(devenv)) {
                            devenvPaths.Add(devenv);
                            Process.Start(devenv, "/setup /nosetupvstemplates").WaitForExit();
                        }
                    }
                }
            }            
        }

        /// <summary>
        /// Gets well-known information about VS installations.
        /// </summary>
        /// <param name="param">Passed from installer GUI - either checkbox2008, checkbox2010, checkbox2012 or checkbox2013</param>
        /// <param name="key">Output param - registry key path</param>
        /// <param name="subpath">Output param - subfolder, where devenv is located</param>
        private void GetInstallKey(string param, out string key, out string subpath) {
            switch (param) {
                case "checkbox2008":
                    key = @"SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;
                case "checkbox2010":
                    key = @"SOFTWARE\Microsoft\VisualStudio\10.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;
                case "checkbox2012":
                    key = @"SOFTWARE\Microsoft\VisualStudio\11.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;
                case "checkbox2013":
                    key = @"SOFTWARE\Microsoft\VisualStudio\12.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;                
                default: throw new ArgumentException("Error during installation - unknown version of Visual Studio.");
            }

        }
    }
}
