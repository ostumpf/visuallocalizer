// Guids.cs
// MUST match guids.h
using System;

namespace OndrejStumpf.VLTestingPackage
{
    static class GuidList
    {
        public const string guidVLTestingPackageCmdSetString = "051269c8-5bf8-4c5c-8ecd-ccbd3bbe1c65";
        public const string guidVLTestingPackageOutputWindow = "3C0449B5-A3E7-4c3b-9B34-FC51E133B830";
        public const string guidVLTestingPackageMarker = "0CE620DE-FA36-4baa-8BC3-0C9A12F97F11";              
        

        public static readonly Guid guidVLTestingPackageCmdSet = new Guid(guidVLTestingPackageCmdSetString);
    };
}