using System;
using System.Runtime.InteropServices;

namespace VisualLocalizer
{
    public static class Guids {

        [Guid("E839DED2-9BB7-4f84-9453-C22CBD0C46E9")]
        public static class VisualLocalizerWindowPane { }

        [Guid("42b49eb8-7690-46f2-8267-52939c5e642f")]
        public static class VLCommandSet { }
    
    }

    public static class PackageCommandIDs {
       public const int CodeMenu= 0x0009;
       public const int SolExpMenu = 0x0005;

       public const int MoveCodeMenuItem = 0x0007;
    }
}