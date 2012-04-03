using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.IO;


namespace VLTestSetupCustomActionInst {
    [RunInstaller(true)]
    public partial class Run : Installer {
       
        public override void Install(IDictionary stateSaver) {
            base.Install(stateSaver);
           
            foreach (DictionaryEntry param in this.Context.Parameters) {
                string key = (param.Key == null ? "0" : param.Key.ToString());
                string value = (param.Value == null ? "0" : param.Value.ToString());

                if (key.StartsWith("checkbox") && value=="1") {
                    register(key);
                }
            }                       
       
        }

        private void register(string param) {
            string key;
            string subpath;
            getInstallKey(param,out key,out subpath);
            
            using (RegistryKey setupKey = Registry.LocalMachine.OpenSubKey(key)) {
                if (setupKey != null) {
                    object registryPath=setupKey.GetValue("ProductDir");
                    if (registryPath != null) {
                        string devenv = Path.Combine(registryPath.ToString(),subpath);
                        if (!string.IsNullOrEmpty(devenv)) {                            
                            Process.Start(devenv, "/setup /nosetupvstemplates").WaitForExit();
                        }
                    }
                }
            }           
        }

        private void getInstallKey(string param,out string key,out string subpath) {
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
                case "checkbox2008exp":
                    key = @"SOFTWARE\Microsoft\VCSExpress\9.0\Setup\VS";
                    subpath = @"Common7\IDE\vcsexpress.exe";
                    break;
                case "checkbox2010exp":
                    key = @"SOFTWARE\Microsoft\VCSExpress\10.0\Setup\VS";
                    subpath = @"Common7\IDE\vcsexpress.exe";
                    break;
                case "checkbox2011exp":
                    key = @"SOFTWARE\Microsoft\VCSExpress\11.0\Setup\VS";
                    subpath = @"Common7\IDE\vcsexpress.exe";
                    break;
                default: throw new ArgumentException("Error during installation - unknown version of Visual Studio.");                   
            }
           
        }
    }
}
