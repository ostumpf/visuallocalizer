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


namespace VLSetupFinalizer {
    [RunInstaller(true)]
    public partial class Register : Installer {

        private List<string> devenvPaths = new List<string>();

        public override void Install(IDictionary stateSaver) {
            base.Install(stateSaver);
  
            foreach (DictionaryEntry param in this.Context.Parameters) {
                string key = (param.Key == null ? "0" : param.Key.ToString());
                string value = (param.Value == null ? "0" : param.Value.ToString());

                if (key.StartsWith("checkbox") && value == "1")                     
                    register(key);                
            }

            string paths = toString(devenvPaths, '|');            
            stateSaver.Add("uninstPaths", paths);
        }

        public override void Uninstall(IDictionary savedState) {
            string[] tokens = null;
            if (savedState.Contains("uninstPaths")) {
                string paths = (string)savedState["uninstPaths"];                

                if (paths != null)
                    tokens = paths.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            }

            base.Uninstall(savedState);

            if (tokens != null) {
                foreach (string path in tokens)                     
                    Process.Start(path, "/setup /nosetupvstemplates").WaitForExit();
                
            }
        }

        private string toString(List<string> list,char separator) {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < list.Count; i++) {
                b.Append(list[i]);
                if (i != list.Count - 1) b.Append(separator);
            }
            return b.ToString();
        }

        private void register(string param) {
            string key;
            string subpath;
            getInstallKey(param, out key, out subpath);
            
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

        private void getInstallKey(string param, out string key, out string subpath) {
            switch (param) {
                case "checkbox2008":
                    key = @"SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;
                case "checkbox2010":
                    key = @"SOFTWARE\Microsoft\VisualStudio\10.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;
                case "checkbox2011":
                    key = @"SOFTWARE\Microsoft\VisualStudio\11.0\Setup\VS";
                    subpath = @"Common7\IDE\devenv.exe";
                    break;                
                default: throw new ArgumentException("Error during installation - unknown version of Visual Studio.");
            }

        }
    }
}
