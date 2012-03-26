// Guids.cs
// MUST match guids.h
using System;

namespace OndrejStumpf.VisualLocalizer
{
    static class GuidList
    {
        public const string guidVisualLocalizerPkgString = "74b8212c-0d66-4fee-99e3-ab64a2d30e50";
        public const string guidVisualLocalizerCmdSetString = "7b8f6fd3-f366-4fdb-8278-0a3c4395b13e";

        public static readonly Guid guidVisualLocalizerCmdSet = new Guid(guidVisualLocalizerCmdSetString);
    };
}