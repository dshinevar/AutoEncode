using AutoEncodeServer.Data;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoEncodeServer
{
    internal static class ExtensionMethods
    {
        public static void UpdateSourceFiles(this List<SourceFileData> sourceFiles, IEnumerable<SourceFile> newSourceFiles)
        {
            IEnumerable<SourceFileData> sourceFilesToRemove = sourceFiles.Except(newSourceFiles, (s, n) => string.Equals(s.FullPath, n.FullPath, StringComparison.OrdinalIgnoreCase));
            sourceFiles.RemoveRange(sourceFilesToRemove);

            IEnumerable<SourceFileData> sourceFilesToAdd = newSourceFiles.Except(sourceFiles, (n, s) => string.Equals(n.FullPath, s.FullPath, StringComparison.OrdinalIgnoreCase))
                .Select(x => new SourceFileData(x));
            sourceFiles.AddRange(sourceFilesToAdd);

            Debug.Assert(sourceFiles.Count == newSourceFiles.Count(), "Number of incoming source files should match outgoing number of source files.");

            sourceFiles.Sort(SourceFileData.CompareByFileName);
        }

        public static void UpdateShowSourceFiles(this List<ShowSourceFileData> showSourceFiles, IEnumerable<SourceFile> newSourceFiles)
        {
            IEnumerable<ShowSourceFileData> sourceFilesToRemove = showSourceFiles.Except(newSourceFiles, (s, n) => string.Equals(s.FullPath, n.FullPath, StringComparison.OrdinalIgnoreCase));
            showSourceFiles.RemoveRange(sourceFilesToRemove);

            IEnumerable<ShowSourceFileData> sourceFilesToAdd = newSourceFiles.Except(showSourceFiles, (n, s) => string.Equals(n.FullPath, s.FullPath, StringComparison.OrdinalIgnoreCase))
                .Select(x => new ShowSourceFileData(x));
            showSourceFiles.AddRange(sourceFilesToAdd);

            Debug.Assert(showSourceFiles.Count == newSourceFiles.Count(), "Number of incoming source files should match outgoing number of source files.");

            showSourceFiles.Sort(SourceFileData.CompareByFileName);
        }
    }
}
