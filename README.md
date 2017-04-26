Visual Localizer
===============

# Project Description

Visual Localizer is an open source plugin for Microsoft Visual Studio. It focuses on advanced manipulation with ResX files and localization of (possibly completed) projects. Basic functions of Visual Localizer include:
* moving string literals from code to selected resource file, either ad-hoc or batch - all string literals in document (project, solution) 
* inlining of references to resource files back to string literals 
* smart editor of resource files (tracks number of references to a resource, enables inlining etc.) 
* using Google Translate, Bing or MyMemory to translate string resources in ResX files 
* localization of C#, VB .NET and ASP .NET projects

## Downloads
Download [installation binary](https://github.com/ostumpf/visuallocalizer/blob/master/install/Setup.msi).

### [Donate - PayPal or credit card](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=YY7U267D6DRA2)

## Used Technologies
* implemented as VSPackage, distributed as MSI
* works in Microsoft Visual Studio 2008, 2010, 2012 and 2013 - NOT the Express editions (as any Visual Studio extension)
* requires .NET 3.5 or higher

## Source Code Window
Visual Localizer provides new menu items in the context menu of code windows - user can invoke these commands:
**Move to resources** command: this command expects that the user right-clicked on a string literal in a code window and in the context menu picked this command. A dialog with information about the string literal  is displayed, enabling user to select destination resource file and to modify key and value. If valid data are inputted, the string literal is moved to selected resource file and a reference is created on its place.

![move to resources dialog](https://github.com/ostumpf/visuallocalizer/blob/master/images/movetoresources-dialog.png)

**Inline** command is basically a reverse operation to the *move to resources* command. It expects the context menu was invoked on a reference to a resource; the reference is resolved and replaced by actual string literal value.

**Batch move to resources (document)** command is a generalization of the *move to resources* command. If invoked from code context menu, it parses whole file looking for localizable string literals, displaying the results in a tool  window. This tool window enables user to select only relevant results, also to modify key and value and to select destination resource file. However, this command gets more effective when invoked from Solution Explorer's context menu - all selected  project items (files, folder, projects...) will be included in the search, making it possible to localize whole project(s) within a few clicks.

![batch move tool window](https://github.com/ostumpf/visuallocalizer/blob/master/images/batchMoveToolWindow.png)

**Batch move to resources (selection)** is a variant of previous command that again works with the code window - it expects user highlighted any piece of code and invoked the command on this selection. Only string literals that have non-empty  intersection with the selected block of code are displayed as results in the tool window.

**Batch inline (document)** and **Batch inline (selection)** do what their names suggest - they parse given code files looking for references to resources and display results in a tool window, letting user select which references  will be replaced with string literals. The tool window for these commands is a lot simpler than the tool window for *batch move* commands, because there is not much to change or set up. The *batch inline* command can also be invoked  from Solution Explorer's context menu with the same effect as in *batch move*.

![batch inline tool window](https://github.com/ostumpf/visuallocalizer/blob/master/images/batchInlineToolWindow.png)

## Solution Explorer
New menu item is added to the context menu of projects and project items. It enables user to perform the *Batch move* and *Batch inline* operations on selected item(s).

## ResX Editor
Visual Localizer provides its own editor of ResX files. It preserves the functionality of the old one, while offering much friendlier user interface and many new functions, such as:
* keeping track of references to resources (number of references is shown and possibility to view them is available in the context menu)
* import of another resource file (merge)
* synchronization of multilingual resource files
* using online translation services to translate resource values

![editor gui](https://github.com/ostumpf/visuallocalizer/blob/master/images/editorGUI.png)

## Batch Move Window
After invoking the Batch move operation (either from the Solution Explorer or the source code window), all string literals found in the file are placed in the Batch Move window, ordered by probability of localization. User can easily decide, which strings  will be moved to which resource files and which will stay hard-coded. After finishing the operation, Batch Move window is closed.

## Communicating with the User
Standard Output Window with custom window pane is used to display common messages. If the /log option was used when starting Visual Studio, warnings and errors can be found in standard log file (ActivityLog.xml).

## Ideas (possible future implementations)
* option to mark ResX file as "dependant" on another ResX file. These "dependant" files could not be edited directly, however every change made to their parent is reflected in them. This could be useful in situations where each ResX file represents localization  for one language - in code-development phase, project would contain several (possibly half-translated) ResX files, which could be later given to translator. 
* standalone application - tool for translators (non-programmers without Visual Studio). It would provide extremely simple and intuitive user interface for editing ResX files - translating into several languages. 
* support for Razor syntax in ASP .NET 
* Localization of WPF projects, including XAML. 
* Generate assignments from the ResX editor