# Documentation

[Manual Installation](Manual-Installation)
[Project Requirements ](-requirements)

**Note**
If you're using Team Foundation Server for version control, make sure you "check out for edit" the ResX files that you want to edit with Visual Localizer.

## Installation

The Visual Localizer extension is shipped as MSI (Microsoft Windows Installer) package. Since the installer modifies Windows registry in the HKLM  hive, it must be run with administrator privileges. Also, .NET version at least 3.5 is required. All instances of Visual Studio should be first terminated before running the Visual Localizer installer.
In the first step, the installer lets user pick the versions of Visual Studio in which the package should be registered. Visual Studio 2008, Visual Studio 2010, Visual Studio 2012, Visual Studio 2013 and Visual Studio 2015 are supported; because of the Microsoft policy, it is not possible to install any extension to the Express editions of Visual Studio.
Finally, installation directory is specified. No other actions are required; the installer handles the registration process by itself. If the installation is completed successfully, Visual Localizer appears in the list of Visual Studio installed products, available in Help/About.

## Move to Resources Command
The “move to resources” command is invoked from the code window context menu. It assumes user clicked on a string literal in code – if this assumption fails or the string literal cannot be moved for other reasons, an error is displayed.
Otherwise a dialog pops up, allowing user to specify details of the operation. The resource key and value can be modified as well as destination resource file. Inputted data are validated after every change – presence of the resource key in the selected resource file is checked and depending on their resource values, either an error is displayed or an option to reference or overwrite the resource entry is offered.
Certain resource files in the list may have “(internal)” written next to them – this indicates that the resource file is located in another project and its designer class has the internal (or Friend) modifier. Referencing such resource file is possible, but will result in a “protection level” error of the compiler. 
The dialog also offers selection of the namespace policy – a choice between using a qualified reference name (includes namespace) or using an unqualified name and adding an import statement if necessary. However, user’s choice may be overridden by Visual Localizer if producing such reference would cause an error – for example, if the new unqualified reference would in the context of the current code block actually point to something else. If the string literal being moved is decorated with the 'const' modifier, a simple transfer to resources would cause the code to be uncompileable, since constants must be initialized with compile-time evaluable expressions. Therefore, a confirmation dialog is displayed to alert the user that the const modifier will be removed. This behavior applies only for constant class/struct fields, not variables - Visual Localizer is not able to detect such situations.
While in C# and VB .NET the newly created reference has just one possible form, in AspX code it must be decided between several options. If the referenced string literal is located in a website project, a resource expression (“<%$”) is used. Otherwise, standard output element (“<%=”) is produced. Moreover, if the string literal is actually a plain text between AspX elements, the reference is wrapped in the <asp:Literal> element. 
Pressing “OK” in the dialog replaces the string literal with the reference and adds this operation as an atomic unit to the undo stack of the document.

## Batch Move to Resources Command
When invoked from the code context menu, the command processes the active document. If the Solution Explorer’s menu was used, all selected project items are recursively processed.
The command examines the specified code files, looking for localizable string literals. It ignores the code that is commented out, skips empty strings and also those string literals that cannot be referenced are ignored. A list of result items is created from the strings that were found. Each result item contains data such as position, whether the string literal is a verbatim string, whether it comes from a designer file etc. These data are utilized for calculating the “localization probability” of the string literal and also in the filter panel of the “batch move” tool window.
The list of result items is then displayed to the user in a tool window, docked to the bottom of the Visual Studio window by default. In the following paragraphs we will describe features of the “batch move” tool window and how it can be used to make the batch-processing quick and effective.
Selecting only relevant string literals to be moved to resource files is achieved using checkboxes in the first column. To make this easier, the grid can be sorted by any of its columns and also, the grid’s context menu contains option to check/uncheck all currently selected rows. 
The destination resource file, the key and value can be specified (data are validated whenever changed and potential name conflicts are displayed). Again, the grid’s context menu can be used to choose the destination resource file for all currently selected rows. The options available in the menu are intersection of possible destination files of all selected rows.
When a grid’s row is double-clicked, corresponding string literal is highlighted in its (newly opened) code window.
The namespace policy can be customized. Either fully qualified references are created or unqualified references are used and import statements are added if necessary, using exactly the same behavior as the “move” command.
Also, the unchecked string literals policy can be selected. By default, the unchecked rows of the grid are ignored – however they can also be marked in the source code so that future “batch move” command would ignore them. A special comment is used for this purpose, called “VL_NO_LOC”. If this option is used, the comment will be inserted before every unchecked string literal in the grid. When the “batch move” command processes the document in the future, thusly marked string literals will appear in the grid, but can be easily removed using a filter. However, since neither VB .NET nor ASP .NET support multiline comments necessary for this operation, this option is effective only in C# source code.
The unchecked rows from the grid can be whenever removed for better clarity (and possibly returned back). Corresponding buttons in the toolbar are used to achieve this.
The filter can be used to select only relevant string literals for localization. This will be discussed in the following section, as well as the “localization probability” displayed in the second column.
When the “Execute” button is pressed, all checked string literals are replaced with references in their respective source code files and corresponding resources are added to the resource files. For each such replacement a new undo unit is placed up the undo stack of the document . If the “VL_NO_LOC” policy was selected, the “/**VL_NO_LOC**/” comment is inserted before every unchecked C# string literal; also this action creates a new undo unit.
All the files used in the tool window (either as source files of the string literals or resource files) are locked while the window is displayed. This is necessary to ensure data integrity – the result items contain absolute positions of the strings in their source files, so modifying the file may cause the operation to fail.
After clicking the "Execute" button, string literals are checked for presence of the 'const' modifier. If any is found, confirmation dialog is displayed letting user know that it will be removed in order to keep the code valid.

**Filter**
	The purpose of the filter is to remove irrelevant string literals from the grid and leave only those that are really a subject to localization. The filter works with a set of “localization criteria”. In this section, we will focus on how they can be edited and how they are utilized by the filter.
	Localization criteria can be customized in the Visual Localizer settings page, accessible through Tools/Options. The “common criteria” are displayed in the grid, which enables user to edit the action part of the criterion. The “custom criteria” can be added using the “Add” button at the bottom of the settings page – a full set of properties (i.e. target, condition and action) must be specified to safely save the settings.
Using these options, user can create a very specific set of criteria to make batch localization much easier process. As it was mentioned above, the criteria are also used to calculate “localization probability”. More specifically, when a new result item is added to the “batch move” tool window, its LP is calculated and if it is at least 50%, the row is by default checked. User can then use LP as an indicator whether the string literal should be localized (for example sort the grid according to this column and check only rows with certain LP).
Moreover, the set of criteria specified in the settings page is copied and displayed in the filter panel above the grid. This panel contains all the criteria as well as option to modify their actions just for this instance of “batch move” tool window. Any changes made in this panel are instantly reflected in the LP of the grid’s rows. To make this even more effective, more actions are available in the filter panel combo boxes:
* Check the rows matching the criterion
* Uncheck the rows matching the criterion
* Check the rows matching the criterion and remove the other (unchecked) rows
* Remove the rows matching the criterion

Any rows removed during these operations can be returned back to the grid using the “Restore unchecked” button in the tool window’s toolbar. Changing the action of a criterion in filter panel will not be saved – it is only valid for this instance of the tool window.
Even when the tool window is already displayed, user can open the Visual Localizer settings page and edit (add, remove) the criteria. More precisely, changing the action of the criteria will not have any immediate effect – the criteria used in the filter panel are copy of the ones in the settings. However, one can add new custom criteria and those will be immediately displayed in the filter panel, ready to be used.
	Visual Localizer contains several custom criteria by default. These criteria were created to fit most developer’s needs and project conventions. 

## Inline Command
As well as the “move to resources” command, the inline command is invoked from the code context menu. It assumes user clicked on a reference to resource that should be replaced with a hard-coded string literal taken from the resource file. If such assumption fails, i.e. there is no result item on the place user clicked, an error is displayed.
Otherwise, the reference is replaced and corresponding undo unit is added to the document’s undo stack. The resource entry itself stays unmodified. Even if there are culture-specific versions of the resource file, the inline value is always taken from the culture-neutral one.
Since the value of the resource stored in the resource file may contain special characters, such as double quotes or newlines, Visual Localizer escapes these characters with respect to the current programming language syntax before inserting the string into the code.
 
## Batch Inline Command
The “batch inline” command can be invoked either from the code context menu (the active document is processed), or from the Solution Explorer’s context menu, in which case all selected project items are recursively processed.
The command explores the code, looking for references to resources that can be replaced with hard-coded string literals. The same rules as in the “inline” command are applied. All found results are displayed on the “batch inline” tool window, which is similar to the “batch move” version, but offers fewer options since it is likely to be used less often. No value in the grid can be edited; it is only possible to check/uncheck the references that will be replaced with hard-coded strings.
When the “Execute” button is pressed, all checked rows are replaced in their respective source code files, creating a new undo unit if possible (the file is opened). The “Remove unchecked rows” and “Restore unchecked rows” buttons work the same as in the “batch move” tool window.

## Global Translate Command
Unlike the commands discussed above, the “global translate” command does not work with source code files. Its purpose is to enable user to perform batch translation of selected resource files. Invoked from the Solution Explorer’s context menu, the command scans the selected project items, looking for ResX files.
All found resource files are displayed in a dialog, in which other details of the operation can be specified. It is necessary to select the source and target language for the translation – either a saved language pair (created either in the settings page or the ResX editor) can be used, or a new one can be created and optionally saved along with the others. Also the translation service must be specified, keeping in mind that using Microsoft Translator requires valid AppID to be inputted in the Visual Localizer settings page (if the AppID field is empty, the Bing service will not even appear in the list of available translation services).
When the “Translate” button is clicked, selected translation service is used to translate all string resources in the checked resource files. If the resource file is opened in the moment of performing this operation, new undo units are added to its undo stack as appropriate.
 
## ResX Editor
Visual Localizer provides custom editor of ResX files, which is more comfortable to work with and offers more functions than the default Visual Studio editor. The editor is set as default for opening “.resx” files during the installation of Visual Localizer.	

**Overview**
	The editor’s window consists of a toolbar and six tabs, each corresponding to a possible resource value content type. Each resource entry is identified by the resource key, which must be unique within the file and is used when referencing the resource entry from code. If the value of the resource entry is string, it is placed in the “Strings” tab, which contains a grid similar to the one used in the “batch move” tool window. Images (i.e. bitmaps), icons and sounds are placed in corresponding tabs; the rest of the file types is placed in the “Files” tab. The last mentioned resources will be commonly referenced as “media resources”. Since the ResX file format enables to store strongly typed data (integers, structures etc.), the ResX editor uses the “Others” tab to store such content.
The editor enables user to perform all kinds of operations with the resource entries. Some of the operations can be invoked from the editor’s toolbar; all of them are available in the context menu. Almost all of these operations can be undone using Visual Studio undo stack for the edited file. When performing an irreversible operation, a confirmation dialog is always displayed. We will describe available functions in the following sections.

**Adding Resources**
	There are several ways of adding a new resource entry to the editor. First, the “Add Resource” button on the toolbar can be used to select files that will be linked to the resource file. The button also enables user to create a new string (by adding a row to the strings grid) or a new image file. 
	Second, if the clipboard currently contains list of file paths (e.g. from Windows file explorer or the Solution Explorer), the list can be pasted into the editor – the files will be sorted into appropriate tabs as necessary. The same effect can be achieved via the “drag’n’drop” operation – again, list of file paths is the desirable content. Finally, when working with string or strongly typed resources, the tab-separated text format is used to place data to the clipboard. Thus, it is for example possible to copy data to and from Microsoft Excel.
	When adding a new file resource to the resource file which is not a part of the solution, the new resource is embedded rather than linked.

**Removing Resources**
	When removing a string or embedded resource, the corresponding entry is just deleted from the resource file. However, there are three versions of the remove process for linked media resource entries. 
	First, the entry is removed from the resource file, but the referenced file stays untouched as a part of the project.
	Second, the entry is removed and the referenced file is excluded from the project. The file remains on disk.
	And finally, the entry is removed and the referenced file deleted from disk. This operation cannot be undone for obvious reasons.

 
**Modification of Existing Resources**
	When editing string or “other” resources, the value and the comment can be modified directly in the grid. To edit the comment of a media resource, a context menu on the resource item must be invoked and corresponding dialog selected.
When editing a resource from the “Others” tab, not only a resource value but also a resource type can be specified. Clearly, these two values must match – it is not possible to save a resource with invalid value. Unlike the resource value, the resource type is not edited directly in the grid, but using a special dialog which gets invoked whenever user attempts to edit the respective cell. In this dialog, the type is specified by its assembly and its full name. The list of available assemblies consists of assemblies currently loaded in the current application domain. This feature is another big advantage of the Visual Localizer ResX editor – the default VS editor does not enable to add this kind of resources and not even change their types.
If the resource key is changed, the editor performs “pseudo-refactoring” of the source code – every reference to the resource is updated to point to the new key. Since the editor runs the reference-searching thread in periods, it would be easy to create an error this way – if two successive key renames would be performed, the thread would not have enough time to catch up and the references would be improperly renamed. Therefore, when renaming the references, their new position and text is updated right away.

**Translation**
	Values of the resource entries in the strings grid can be translated using Microsoft Translator (requires AppID to be specified in the settings), Google Translate or MyMemory. Necessary argument for this operation is the language pair – a combination of source and target language. These can be added either on the settings page or using the “New language pair” button available in the translation providers menu.
	All currently selected rows of the strings grid are translated; for each modifying action there is a new undo unit added to the undo stack.
	
**Inlining**
	The “Inline” action invoked from the editor works the same as the “batch inline” command. However, once a reference is inlined, it cannot be re-found, therefore it is not possible to undo this action. Another reason for this is that the source code files may have been edited after the inline operation, so any existing result items are rendered invalid.

**Embedded vs. Linked Resources**
	There are two ways of adding a media resource entry to a ResX file. The first way is called the embedded resource – the ResX file actually contains the media resource, in a text representation of its binary data. The second way, called a linked resource, stores only a reference to the media resource in the ResX file. The Visual Localizer-provided ResX editor can work with both representations of data and is also able to transfer between them – the respective commands can be found in the context menu.
	Also, while it is possible to open a linked media resource (via double-click), an embedded resource cannot be opened since there is no file for the editor.
	By default, when adding a media resource to the ResX file which is a part of the solution, the resource entry is created as linked. If the ResX file stands alone, the resource is embedded.

**Synchronization**
	The goal of the synchronization feature is to help developers keep multiple versions of resource files for various cultures synchronized, i.e. the resource files should contain the same resource entries (same keys) and differ only in values.
	When editing a culture-neutral resource file, the “Proffer” button is available in the toolbar. Clicking this button causes Visual Localizer to search the file’s directory and collect all culture-specific versions of this file. These files are then updated, i.e. presence of each resource key from the culture-neutral file is checked and added if not present.
	When editing a culture-specific resource file, the button says “Update” and does almost the same, but only for the currently edited file. That is, its culture-neutral version is found (if exists) and missing resource entries are added.
	The culture-specific resource files usually contain only string resources (the media resources are usually culture-independent and placed only in the culture-neutral file). Therefore the synchronization feature only works with string resources and completely ignores all media resources.
	While all commands and editor functions so far ignored the letter case of the resource keys, the synchronization commands are case-sensitive. This is because if a key in a culture-specific file and its equivalent in the culture-neutral file differed (even only in case), .NET would not be able to match them and use them in translations.

**Merging**
The merging feature is a simpler version of the synchronization feature; another resource file is selected and its data are copied to the current file, even if already present. Unlike synchronization, this feature works with whole resource files, i.e. string resources and strongly typed resources as well as media resources.

## Settings
Visual Localizer settings are completely integrated in the Visual Studio settings model. Thus, it is possible to edit the Visual Localizer settings among all others in the “Options” page. Also, when importing or exporting settings from Visual Studio, Visual Localizer settings are included in the process, making it possible to backup or transfer the settings as required.
There are two categories of settings; each of them has its own subpage in the settings window. First, the editor settings:
* “Reference update interval” – specifies interval in which the ReferenceLookuperThread is periodically run.
* “Invalid key name policy” – this affects both editor and the “move to resources” commands. It specifies the action to take when user inputs an invalid identifier for a resource key. 
* “Bing AppID” – it is not necessary to specify this value, however without it, using Microsoft Translator service is not possible.
* “Optimize format strings for translation” – determines whether the format placeholders in strings are temporarily replaced with numbers when using a translation service. 
* “Language pairs” – the list of translation language pairs, used whenever working with translation services. 

The second category of settings concerns only the “batch move to resources” and the “batch inline” tool window, since it specifies settings for the filter panel and the result items grid. 
* “Show context column” – the tool window grids may contain additional column providing the context for the result item, i.e. a few lines of code around the string literal or the reference to resource. Displaying this column may render the grid slightly confusing; therefore the column can be hidden.
* “Determine types of attributes in ASP .NET elements” – use .NET Reflection and FileCodeModel to filter out those string literals which are located within an attribute with other data type than string. 

All changes made in this settings page and confirmed are effective immediately, i.e. all open editors, tool windows etc. are revalidated and updated with the new settings. 	

## Localization Probability
Hardly all strings in source code are subjects to localization. Sometimes, something else than user output is saved in the variable with the string data type, for example files paths, SQL queries, identifiers or IP addresses. Ideally, the “batch move” command would be able to differentiate between strings that get eventually displayed to a user (and therefore should be localized) and strings that are only used to store values (and should be omitted). The perfect behavior cannot be achieved simply because there is no characteristic that deterministically distinguishes the two groups of string literals in code. Therefore Visual Localizer contains only heuristic approximation of such algorithm, called “localization probability”.
 “Localization probability” (LP) is displayed as a percentage value in the second column of the “batch move” tool window grid. It represents a suitability of the string literal for localization and was designed to quickly eliminate all string literals which seem not to contain culture-dependent value. By default, all string literals have 50% LP and certain criteria are used to decrease or increase the value.

**Custom Criteria**
To calculate LP, concept of “localization criteria ” is used. In this section, we will describe the more general variant of localization criteria, called “custom criteria”. In the following section, we will discuss “common criteria”, which are simpler and are based on “custom criteria”. Since Visual Localizer enables customization of both “custom” and “common” criteria, using them may rapidly increase effectiveness of the “batch move” command.
A custom localization criterion consists of three parts and takes the form:
_If <<predicate>> (<<target>>) then <<action>>_
To explain them, we will use following example:
_If the name of the class in which the string literal belongs matches “^.**Abstract.**$”, the LP should be lowered by 20._

The first part of the criterion is the target, i.e. what is tested. In the example, “class name” is the target. Visual Localizer provides the following set of predefined targets:
* String literal value
* The name of the namespace the string literal was found in (if any)
* The name of the class the string literal was found in (if any)
* The name of the method the string literal was found in (if any)
* The name of the variable the string literal was found in (if any)
* The element name, if the string literal comes from AspX element or AspX plain text
* The element prefix, if the string literal comes from AspX element or AspX plain text
* The attribute, if the string literal comes from AspX element
* The line of code on which the string literal was found

The second part of localization criterion is the predicate, i.e. condition that the target must satisfy in order to pass the criterion. In the examples above “matches (regular expression)” is the predicate. These are the predefined predicates supported by Visual Localizer:
* Be null 
* Contain no letters (e.g. “127.0.0.1”)
* Contain no whitespace 
* Contain only capital letters and symbols (e.g. “COLUMN_ID”)
* Match specified regular expression
* Not match the specified regular expression

The last part of the criterion is the action, i.e. how should LP be modified, if the predicate is satisfied. In the example above, “lower the LP by 20” is the action. The list of available actions is as follows:
* Force localize – string literals matching the criterion are given a 100% LP
* Force NOT localize – string literals matching the criterion are given a 0% LP
* Set value – an additional value from -100 to +100 must be supplied, representing the likeliness of localizability of the string literal matching the criterion. Positive values raise LP, negative values make it smaller.
* Ignore – is equal to setting the value to 0

Clearly there may be situations in which various criteria are in conflict; we will discuss this as well as the exact calculation of the LP. When Visual Localizer is installed, it contains several predefined custom criteria that were designed to cover most cases and fit most developers’ needs; therefore it is possible to take advantage of the “localization probability” right away without creating the criteria by hand. 

**Common Criteria**
While the “custom criteria” consist of the target, the predicate and the action, in “common criteria” the target and the predicate are merged together and cannot be customized. That is, there are several predefined common criteria – they cannot be deleted, no common criteria can be added and only the action part can be edited. For example, these are some the common criteria available:
* String literal is a verbatim string
* String literal comes from [Localizable(false)](Localizable(false)) block
* String comes from ASP .NET plain text
The complete list can be viewed and customized in the Visual Localizer settings page.
 
**Calculation**
As it was already mentioned, one of the two purposes of the “localization criteria” is to calculate the “localization probability”. Their second purpose (filter in the “batch move” tool window) is discussed above. In this section we will discuss the concrete algorithm for calculating LP.
 If two localization criteria affecting one string literal are in conflict (i.e. one says the string must be localized while the other claims the opposite), following rules are successively applied:
# If at least one criterion has the “Force localize” action, the string literal is given 100% LP.
# Otherwise, if at least one criterion has the “Force NOT localize” action, the string literal is given 0% LP.
# Otherwise the values are combined using specific formula. If no criterion affects the string literal, it is given 50% LP.