﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NodeDocumentationMarkdownGenerator.Verbs;

namespace NodeDocumentationMarkdownGenerator.Commands
{
    internal static class FromDirectoryCommand
    {
        /// <summary>
        /// Creates markdown files using the fromdirectory verb
        /// </summary>
        /// <param name="opts"></param>
        internal static void HandleDocumentationFromDirectory(FromDirectoryOptions opts)
        {
            Program.VerboseMode = opts.Verbose;
            var searchOption = opts.RecursiveScan ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var fileInfos = new List<MdFileInfo>();
            fileInfos.AddRange(ScanFolderForAssemblies(opts.InputFolderPath, opts.Filter, opts.ReferencePaths, searchOption));
            if (opts.IncludeCustomNodes)
            {
                fileInfos.AddRange(ScanFolderForCustomNodes(opts.InputFolderPath, searchOption));
            }

            MarkdownHandler.CreateMdFilesFromFileNames(fileInfos, opts.OutputFolderPath, opts.Overwrite, opts.CompressImages, opts.CompressGifs, opts.DictionaryDirectory, opts.LayoutSpecPath);
        }

        private static List<MdFileInfo> ScanFolderForCustomNodes(string inputFolderPath, SearchOption searchOption)
        {
            var allDyfs = Directory.GetFiles(inputFolderPath, "*.dyf", searchOption).Select(x => new FileInfo(x)).ToList();
            var fileInfos = new List<MdFileInfo>();
            foreach (var cn in allDyfs)
            {
                var fileInfo = MdFileInfo.FromCustomNode(cn.FullName);
                if (fileInfo is null) continue;

                fileInfos.Add(fileInfo);
            }
            return fileInfos;
        }

        private static List<MdFileInfo> ScanFolderForAssemblies(string inputFolderPath, IEnumerable<string> filter, IEnumerable<string> referencePaths, SearchOption searchOption)
        {
            var allAssembliesFromInputFolder = Directory.GetFiles(inputFolderPath, "*.*", searchOption)
                .Where(x => x.EndsWith(".dll") || x.EndsWith(".ds"))
                .Select(x => new FileInfo(x))
                .GroupBy(x => x.Name)
                .Select(x => x.FirstOrDefault());


            var referenceDllPaths = new List<string>();
            if(referencePaths != null)
            {
                referenceDllPaths = referencePaths
                .SelectMany(p => new DirectoryInfo(p)
                    .EnumerateFiles("*.dll", SearchOption.AllDirectories)
                    .Select(d => d.FullName)
                    .Distinct()).ToList();
            }

            if (filter.Any())
            {
                // Filters the assemblies specified in the filter from allAssembliesFromInputFolder,
                // the assembly paths left after this filter are the ones that will be scanned for nodes.
                var assemblyPathsToScan = allAssembliesFromInputFolder
                    .Where(x => filter.Contains(x.Name) || filter.Contains(x.FullName))
                    .Select(x => x.FullName);


                // We still need all assemblies in the inputFolderPath for the PathAssemblyResolver
                // so here we separate the assemblyPathsToScan from allAssembliesFromInputFolder
                // which gives us the additional assemblies that need to be added to the AssemblyResolveHandler
                var addtionalPathsToLoad = allAssembliesFromInputFolder
                    .Select(x => x.FullName)
                    .Except(assemblyPathsToScan)
                    .ToList();
                   

                // If there are any paths specified in the referencePaths we need to add them
                // to addtionalPathsToLoad as the AssemblyResolveHandler will need them to resolve types.
                if (referenceDllPaths.Any())
                {
                    addtionalPathsToLoad.AddRange(referenceDllPaths);
                }

                return AssemblyHandler.ScanAssemblies(assemblyPathsToScan, addtionalPathsToLoad);
            }

            // If there are no filters specified we want to scan all assemblies in the inputFolderPath
            // and still add any referencePaths to the AssemblyResolveHandler
            return AssemblyHandler.
                ScanAssemblies(allAssembliesFromInputFolder.Select(x => x.FullName), referenceDllPaths);
        }
    }
}
