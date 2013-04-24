using System;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Collections.Generic;

/// Contains all types associated with the Visual Localizer project, including VLlib.
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
        public const int CodeMenu= 0x0009; // "Visual Localizer" submenu of code context menu
        public const int SolExpMenu = 0x0005; // "Visual Localizer" submenu of Solution Explorer context menu

        public const int MoveCodeMenuItem = 0x0007;
        public const int InlineCodeMenuItem = 0x0008;        
        public const int BatchMoveCodeMenuItem = 0x0014;
        public const int BatchInlineCodeMenuItem = 0x0015;
        public const int BatchMoveSelectionCodeMenuItem = 0x0018;
        public const int BatchInlineSelectionCodeMenuItem = 0x0017;

        public const int BatchMoveSolExpMenuItem = 0x0003;
        public const int BatchInlineSolExpMenuItem = 0x0016;
        public const int TranslateSolExpMenuItem = 0x0019;

        public const int BatchMoveToolbarID = 0x1001;        
        public const int BatchMoveToolbarRunID = 0x1003;
        public const int BatchMoveToolbarModesListID = 0x1004;
        public const int BatchMoveToolbarModeID = 0x1005;
        public const int BatchMoveToolbarShowFilterID = 0x1006;                
        public const int BatchMoveToolbarRememberUncheckedListID = 0x1012;
        public const int BatchMoveToolbarRememberUncheckedID = 0x1013;
        public const int BatchMoveToolbarRemoveUncheckedID = 0x1014;
        public const int BatchMoveToolbarRestoreUncheckedID = 0x1015;
        public const int BatchMoveToolbarUncheckedMenuID = 0x1016;

        public const int BatchInlineToolbarID = 0x2001;
        public const int BatchInlineToolbarRunID = 0x2002;
        public const int BatchInlineToolbarRemoveUncheckedID = 0x2004;
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
        public const string ResXExtension = ".resx";
        public const string PublicResXTool = "PublicResXFileCodeGenerator";
        public const string InternalResXTool = "ResXFileCodeGenerator";
        public const string CSharpLocalizationComment = "/*" + LocalizationComment + "*/";
        public const string AspNetLocalizationComment = "<%--" + LocalizationComment + "--%>";
        public const string LocalizationComment = "VL_NO_LOC";

        public static readonly string[] CsExtensions = { ".cs" };
        public static readonly string[] VBExtensions = { ".vb" };
        public static readonly string[] AspxExtensions = { ".aspx", ".master", ".ascx" };
        public static readonly string[] CodeExtensions = { ".vb", ".cs" };

        public static readonly string[] IMAGE_FILE_EXT = { ".png", ".gif", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };
        public static readonly string[] ICON_FILE_EXT = { ".ico" };
        public static readonly string[] SOUND_FILE_EXT = { ".wav" };
        public static readonly string[] TEXT_FILE_EXT = { ".txt" };

        public const string WindowsCSharpProject = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        public const string WindowsVBProject = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        public const string WebApplicationProject = "{349C5851-65DF-11DA-9384-00065B846F21}";
        public const string WebSiteProject = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        public const string PhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";
        public const string PhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
        public const string VirtualFolder = "{6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C}";
        public const string Subproject = "{EA6618E8-6E24-4528-94BE-6889FE16485C}";

        public const string AspElementReferenceFormat = "<%= {0} %>";
        public const string AspElementExpressionFormat = "<%$ {0}:{1},{2} %>";        
        public const string AspLiteralFormat = "<asp:Literal runat=\"server\" Text=\"{0}\"/>";

        public const string AspImportDirectiveFormat = "<%@ Import Namespace=\"{0}\" %>\r\n";
        public const string CSharpUsingBlockFormat = "using {0};\r\n";
        public const string VBUsingBlockFormat = "Imports {0}\r\n";

        /// <summary>
        /// Attributes ignored by ASP .NET code explorer, in the format [elementName]:[attributeName]. Symbol * is metacharacter, meaning "any string"
        /// </summary>
        public static readonly string[] AspNetIgnoredAttributes = { "*:ID", "*:Name", "*:runat" };

        public const string ContextSubstituteText = "**RESOURCE REFERENCE**";
        public const string GlobalWebSiteResourcesFolder = "App_GlobalResources";
        public const string GlobalWebSiteResourcesNamespace = "Resources";

        public const string SOLUTION_EXPLORER_FILE_LIST = "CF_VSSTGPROJECTITEMS";
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