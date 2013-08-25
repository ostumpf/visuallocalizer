using System;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Collections.Generic;

namespace VisualLocalizer {

    /// <summary>
    /// Contains types declared only to hold GUIDs
    /// </summary>
    public static class Guids {

        /// <summary>
        /// GUID of the "Visual Localizer" window pane
        /// </summary>
        [Guid("E839DED2-9BB7-4f84-9453-C22CBD0C46E9")]
        public static class VisualLocalizerWindowPane { }

        /// <summary>
        /// GUID of the command set of the commands located in context menus of VS
        /// </summary>
        [Guid("42b49eb8-7690-46f2-8267-52939c5e642f")]
        public static class VLCommandSet { }

        /// <summary>
        /// GUID of the command set of the commands located in Batch move toolwindow
        /// </summary>
        [Guid("41896b92-0335-4522-b75f-35dc0a64d5a3")]
        public static class VLBatchMoveToolbarCommandSet { }

        /// <summary>
        /// GUID of the command set of the commands located in Batch inline toolwindow
        /// </summary>
        [Guid("F2983E9C-E545-4d2f-A5F2-D04683356AD0")]
        public static class VLBatchInlineToolbarCommandSet { }
    }

    /// <summary>
    /// Holds IDs for VLCommandSet, VLBatchMoveToolbarCommandSet and VLBatchInlineToolbarCommandSet
    /// </summary> 
    public static class PackageCommandIDs {
        /// <summary>
        /// "Visual Localizer" submenu of code context menu
        /// </summary>
        public const int CodeMenu= 0x0009; 

        /// <summary>
        /// "Visual Localizer" submenu of Solution Explorer context menu
        /// </summary>
        public const int SolExpMenu = 0x0005; 

        /// <summary>
        /// "Move to resources..." menu item in the code context menu
        /// </summary>
        public const int MoveCodeMenuItem = 0x0007;

        /// <summary>
        /// "Inline" menu item in the code context menu
        /// </summary>
        public const int InlineCodeMenuItem = 0x0008;

        /// <summary>
        /// "Batch move to resources (document)..." menu item in the code context menu
        /// </summary>
        public const int BatchMoveCodeMenuItem = 0x0014;

        /// <summary>
        /// "Batch inline (document)..." menu item in the code context menu
        /// </summary>
        public const int BatchInlineCodeMenuItem = 0x0015;

        /// <summary>
        /// "Batch move to resources (selection)..." menu item in the code context menu
        /// </summary>
        public const int BatchMoveSelectionCodeMenuItem = 0x0018;

        /// <summary>
        /// "Batch inline (selection)..." menu item in the code context menu
        /// </summary>
        public const int BatchInlineSelectionCodeMenuItem = 0x0017;

        /// <summary>
        /// "Batch move to resources..." menu item in the Solution Explorer context menu
        /// </summary>
        public const int BatchMoveSolExpMenuItem = 0x0003;

        /// <summary>
        /// "Batch inline..." menu item in the Solution Explorer context menu
        /// </summary>
        public const int BatchInlineSolExpMenuItem = 0x0016;

        /// <summary>
        /// "Global translate..." menu item in the Solution Explorer context menu
        /// </summary>
        public const int TranslateSolExpMenuItem = 0x0019;


        /// <summary>
        /// ID of the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarID = 0x1001; 
       
        /// <summary>
        /// The "Execute" button of the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarRunID = 0x1003;

        /// <summary>
        /// The "Namespace policy" items list in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarModesListID = 0x1004;

        /// <summary>
        /// The "Namespace policy" combo box in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarModeID = 0x1005;

        /// <summary>
        /// The "Show/Hide filter" button of the "batch move" tool window toolbar 
        /// </summary>
        public const int BatchMoveToolbarShowFilterID = 0x1006;

        /// <summary>
        /// The "Remember unchecked" items list in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarRememberUncheckedListID = 0x1012;

        /// <summary>
        /// The "Remember unchecked" combo box in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarRememberUncheckedID = 0x1013;

        /// <summary>
        /// The "Remove unchecked" button in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarRemoveUncheckedID = 0x1014;

        /// <summary>
        /// The "Restore unchecked" button in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarRestoreUncheckedID = 0x1015;

        /// <summary>
        /// The "Remove/restore unchecked" menu-button in the "batch move" tool window toolbar
        /// </summary>
        public const int BatchMoveToolbarUncheckedMenuID = 0x1016;

        /// <summary>
        /// ID of the "batch inline" tool window toolbar
        /// </summary>
        public const int BatchInlineToolbarID = 0x2001;

        /// <summary>
        /// The "Execute" button of the "batch inline" tool window toolbar
        /// </summary>
        public const int BatchInlineToolbarRunID = 0x2002;

        /// <summary>
        /// The "Remove unchecked" button in the "batch inline" tool window toolbar
        /// </summary>
        public const int BatchInlineToolbarRemoveUncheckedID = 0x2004;

        /// <summary>
        /// The "Restore unchecked" button in the "batch inline" tool window toolbar
        /// </summary>
        public const int BatchInlineToolbarPutBackUncheckedID = 0x2005;
    }

    /// <summary>
    /// Filetype based on file extension
    /// </summary>
    public enum FILETYPE { 
        /// <summary>
        /// Unknown file type
        /// </summary>
        UNKNOWN, 

        /// <summary>
        /// *.cs file
        /// </summary>
        CSHARP, 

        /// <summary>
        /// *.aspx, *.master or *.ascx file
        /// </summary>
        ASPX, 

        /// <summary>
        /// *.vb file
        /// </summary>
        VB }

    /// <summary>
    /// String constants used in VL
    /// </summary>
    public static class StringConstants {
        /// <summary>
        /// Extension of the ResX resource files
        /// </summary>
        public const string ResXExtension = ".resx";

        /// <summary>
        /// Custom tool generating public designer classes
        /// </summary>
        public const string PublicResXTool = "PublicResXFileCodeGenerator";

        /// <summary>
        /// Custom tool generating internal designer classes
        /// </summary>
        public const string InternalResXTool = "ResXFileCodeGenerator";

        /// <summary>
        /// Comment inserted before string literals that should be marked for future batch move command - C#
        /// </summary>
        public const string CSharpLocalizationComment = "/*" + LocalizationComment + "*/";

        /// <summary>
        /// Comment inserted before string literals that should be marked for future batch move command - ASP .NET
        /// </summary>
        public const string AspNetLocalizationComment = "<%--" + LocalizationComment + "--%>";

        /// <summary>
        /// Text of the comment marking string literals for future reference
        /// </summary>
        public const string LocalizationComment = "VL_NO_LOC";

        /// <summary>
        /// List of extensions of accepted C# files
        /// </summary>
        public static readonly string[] CsExtensions = { ".cs" };

        /// <summary>
        /// List of extensions of accepted VB .NET files
        /// </summary>
        public static readonly string[] VBExtensions = { ".vb" };

        /// <summary>
        /// List of extensions of accepted ASP .NET files
        /// </summary>
        public static readonly string[] AspxExtensions = { ".aspx", ".master", ".ascx" };

        /// <summary>
        /// List of extensions of possible code-behind ASP .NET files
        /// </summary>
        public static readonly string[] CodeExtensions = { ".vb", ".cs" };

        /// <summary>
        /// List of extensions of accepted images
        /// </summary>
        public static readonly string[] IMAGE_FILE_EXT = { ".png", ".gif", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };

        /// <summary>
        /// List of extensions of accepted icons
        /// </summary>
        public static readonly string[] ICON_FILE_EXT = { ".ico" };

        /// <summary>
        /// List of extensions of accepted sounds
        /// </summary>
        public static readonly string[] SOUND_FILE_EXT = { ".wav" };

        /// <summary>
        /// List of extensions of accepted text files
        /// </summary>
        public static readonly string[] TEXT_FILE_EXT = { ".txt" };

        /// <summary>
        /// GUID of the Windows C# project in the Solution Explorer
        /// </summary>
        public const string WindowsCSharpProject = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

        /// <summary>
        /// GUID of the Windows VB .NET project in the Solution Explorer
        /// </summary>
        public const string WindowsVBProject = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";

        /// <summary>
        /// GUID of the ASP .NET web application project in the Solution Explorer
        /// </summary>
        public const string WebApplicationProject = "{349C5851-65DF-11DA-9384-00065B846F21}";

        /// <summary>
        /// GUID of the ASP .NET web site project in the Solution Explorer
        /// </summary>
        public const string WebSiteProject = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";

        /// <summary>
        /// GUID of the physical file project item in the Solution Explorer
        /// </summary>
        public const string PhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";

        /// <summary>
        /// GUID of the physical folder project item in the Solution Explorer
        /// </summary>
        public const string PhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";

        /// <summary>
        /// GUID of the virtual folder (References...) project item in the Solution Explorer
        /// </summary>
        public const string VirtualFolder = "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}";

        /// <summary>
        /// GUID of a sub project in the Solution Explorer
        /// </summary>
        public const string Subproject = "{EA6618E8-6E24-4528-94BE-6889FE16485C}";

        /// <summary>
        /// Format string for ASP .NET output element
        /// </summary>
        public const string AspElementReferenceFormat = "<%= {0} %>";

        /// <summary>
        /// Format string for ASP .NET resource expression element
        /// </summary>
        public const string AspElementExpressionFormat = "<%$ {0}:{1},{2} %>";

        /// <summary>
        /// Format string for ASP .NET Literal element
        /// </summary>
        public const string AspLiteralFormat = "<asp:Literal runat=\"server\" Text=\"{0}\"/>";

        /// <summary>
        /// Format string for the import statement in ASP .NET
        /// </summary>
        public const string AspImportDirectiveFormat = "<%@ Import Namespace=\"{0}\" %>\r\n";

        /// <summary>
        /// Format string for the import statement in C#
        /// </summary>
        public const string CSharpUsingBlockFormat = "using {0};\r\n";

        /// <summary>
        /// Format string for the import statement in VB .NET
        /// </summary>
        public const string VBUsingBlockFormat = "Imports {0}\r\n";

        /// <summary>
        /// Attributes ignored by ASP .NET code explorer, in the format [elementName]:[attributeName]. Symbol * is metacharacter, meaning "any string"
        /// </summary>
        public static readonly string[] AspNetIgnoredAttributes = { "*:ID", "*:Name", "*:runat" };

        /// <summary>
        /// Text replacing the string literal in the context string
        /// </summary>
        public const string ContextSubstituteText = "**RESOURCE REFERENCE**";

        /// <summary>
        /// Name of the ASP .NET resources folder
        /// </summary>
        public const string GlobalWebSiteResourcesFolder = "App_GlobalResources";

        /// <summary>
        /// Default namespace for ASP .NET website resource files
        /// </summary>
        public const string GlobalWebSiteResourcesNamespace = "Resources";

        /// <summary>
        /// Clipboard data format name - Solution Explorer file list
        /// </summary>
        public const string SOLUTION_EXPLORER_FILE_LIST = "CF_VSSTGPROJECTITEMS";

        /// <summary>
        /// Clipboard data format name - Windows Explorer file list
        /// </summary>
        public const string FILE_LIST = "FileDrop";
    }

    /// <summary>
    /// Numeric constants used in VL
    /// </summary>
    public static class NumericConstants {

        /// <summary>
        /// Number of lines of code that are added to a result item as a context
        /// </summary>
        public const int ContextLineRadius = 2;
    }
}