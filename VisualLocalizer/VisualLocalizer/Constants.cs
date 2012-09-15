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
        public static class VLBatchToolbarCommandSet { }
    }

    public static class PackageCommandIDs {
        public const int CodeMenu= 0x0009;
        public const int SolExpMenu = 0x0005;

        public const int MoveCodeMenuItem = 0x0007;
        public const int InlineCodeMenuItem = 0x0008;
        public const int BatchMoveCodeMenuItem = 0x0014;
        public const int BatchInlineCodeMenuItem = 0x0015;

        public const int BatchMoveSolExpMenuItem = 0x0003;
        public const int BatchInlineSolExpMenuItem = 0x0016;

        public const int ShowToolWindowItem = 0x0013;

        public const int BatchToolbarID = 0x1001;        
        public const int BatchToolbarRunID = 0x1003;
        public const int BatchToolbarModesListID = 0x1004;
        public const int BatchToolbarModeID = 0x1005;
    }

    public static class StringConstants {
        public const string ResXExtension = ".resx";
        public const string PublicResXTool = "PublicResXFileCodeGenerator";
        public const string InternalResXTool = "ResXFileCodeGenerator";
        public const string UsingStatement = "using";
        public const string CsExtension = ".cs";
    }
}