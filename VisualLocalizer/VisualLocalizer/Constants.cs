using System;
using System.Runtime.InteropServices;

namespace VisualLocalizer
{
    public static class Guids {

        [Guid("E839DED2-9BB7-4f84-9453-C22CBD0C46E9")]
        public static class VisualLocalizerWindowPane { }

        [Guid("42b49eb8-7690-46f2-8267-52939c5e642f")]
        public static class VLCommandSet { }

        [Guid("41896b92-0335-4522-b75f-35dc0a64d5a3")]
        public static class VLBatchMoveToolbarCommandSet { }

        [Guid("F2983E9C-E545-4d2f-A5F2-D04683356AD0")]
        public static class VLBatchInlineToolbarCommandSet { }
    }

    public static class PackageCommandIDs {
        public const int CodeMenu= 0x0009;
        public const int SolExpMenu = 0x0005;

        public const int MoveCodeMenuItem = 0x0007;
        public const int InlineCodeMenuItem = 0x0008;
        public const int BatchMoveCodeMenuItem = 0x0014;
        public const int BatchInlineCodeMenuItem = 0x0015;
        public const int BatchMoveSelectionCodeMenuItem = 0x0018;
        public const int BatchInlineSelectionCodeMenuItem = 0x0017;

        public const int BatchMoveSolExpMenuItem = 0x0003;
        public const int BatchInlineSolExpMenuItem = 0x0016;

        public const int BatchMoveToolbarID = 0x1001;        
        public const int BatchMoveToolbarRunID = 0x1003;
        public const int BatchMoveToolbarModesListID = 0x1004;
        public const int BatchMoveToolbarModeID = 0x1005;
        public const int BatchMoveToolbarShowFilterID = 0x1006;
        public const int BatchMoveToolbarRunVerbatimizeCheckedID = 0x1011;
        public const int BatchMoveToolbarRunVerbatimizeUncheckedID = 0x1007;
        public const int BatchMoveToolbarRememberUncheckedListID = 0x1012;
        public const int BatchMoveToolbarRememberUncheckedID = 0x1013;

        public const int BatchInlineToolbarID = 0x2001;
        public const int BatchInlineToolbarRunID = 0x2002;
    }

    public static class StringConstants {
        public const string ResXExtension = ".resx";
        public const string PublicResXTool = "PublicResXFileCodeGenerator";
        public const string InternalResXTool = "ResXFileCodeGenerator";
        public const string CsExtension = ".cs";
        public const string NoLocalizationComment = "/*VL_NO_LOC*/";
    }
}