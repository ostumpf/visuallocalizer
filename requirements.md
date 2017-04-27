# Requirements

## Code editor

Following features are provided in code editor:

* strings can be moved to resources ad-hoc. A dialog window will be shown to specify the resource file, resource key, comment. Similar strings will be searched and offered to replace as well. 
* move all strings from file (and related files) to resources. (Batch move to resources). Action will respect Localizable attribute. User may select to localize setters of specified properties/parameters only (e.g. all calls to Console.WriteLine) 
* when a resource is used and the resource is a format string, VL will produce warning when the resource is not used where format string is expected (e.g. Console.Writeline, String.Format etc.) or when incorrect amount of parameters is provided. 
* copy & paste is resource-aware. When copying source code containing references to resource keys not available in the target source file, the user is notified and can choose one of the following solutions: 
	* inline strings
	* add reference to the source project
	* duplicate resource string in local resource file
* ASP.NET page/master page/control localization is supported 
	* editor offers similar features as C# code editor
	* Localizable attribute is respected 
	* Batch localization - the user can batch-process all setters of some property (e.g. move all initializations of Label.Text to resources)
* XAML support and localization of Silverlight/WPF projects ?? 

## Solution explorer

Following features are provided in solution explorer:

* batch move all strings to resources (from project, selection of projects, entire solution) 

## Resource editor 

Following features are provided in resource editor:

* Find all references of a resource key
* Resource refactoring: rename, duplicate, inline, merge resources

## Localization support 

Localization support can be invoked both from Visual Studio or as a standalone application. It should ease the process of localization and management of resource dictionaries. 
Following features are provided as localization support:

* Creation and management of several resource dictionaries for different languages. 
* Compare resource dictionaries (diff)
* The tool may utilize available spellcheck and translation tools and services 


