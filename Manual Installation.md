When facing problems with the installer (security issues etc.), here are instructions for manual installation.

## Installation

1. Download and extract this archive: [http://download-codeplex.sec.s-msft.com/Download?ProjectName=visuallocalizer&DownloadId=831000](http://download-codeplex.sec.s-msft.com/Download?ProjectName=visuallocalizer&DownloadId=831000)

2. Copy the DLL's to a path on a disk

3. Edit the .reg file - replace the '???' (line 11) with a path to a directory from Step 2 

4. If you're running 64bit Windows, replace all {"HKEY_LOCAL_MACHINE\Software\Microsoft\VisualStudio\11.0"} with {"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Microsoft\VisualStudio\11.0"}

5. Also, replace all 'VisualStudio\11.0' with appropriate version:
 * 9.0 for Visual Studio 2008
 * 10.0 for Visual Studio 2010
 * 11.0 for Visual Studio 2012
 * 12.0 for Visual Studio 2013

6. save the changes and import the registry settings

7. run CMD prompt in Administrator mode (C:\Windows\System32\cmd.exe -> Run as Administrator) and execute following command:
<VS-install-path>\Common7\IDE\devenv.exe /setup

8. Done.

## Uninstallation

1. Delete the files from Step 2 of the installation

2. Delete the registry entries added from the .reg file

3. Run Step 7 again
  