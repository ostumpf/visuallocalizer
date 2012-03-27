// Guids.cs
// MUST match guids.h
using System;

namespace OndrejStumpf.VLTestingPackage
{
    static class GuidList
    {
        public const string guidVLTestingPackagePkgString = "4d84a08f-4147-4224-8a05-96b47e9d5f6a";
        public const string guidVLTestingPackageCmdSetString = "051269c8-5bf8-4c5c-8ecd-ccbd3bbe1c65";

        public static readonly Guid guidVLTestingPackageCmdSet = new Guid(guidVLTestingPackageCmdSetString);
    };
}