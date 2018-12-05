using Onion.SolutionParser.Parser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace Centipede
{ 
    public enum VsProjectFileType
    {
        CompileFile,
        Project,
        Solution,
    }

    public class VsProjectFile
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public VsProjectFileType Type { get; set; }
        public ConcurrentDictionary<string, VsProjectFile> IncludedIn { get; set; } = new ConcurrentDictionary<string, VsProjectFile>();
    }


    public class Centipede
    {
        private readonly ILogger _logger;
        private readonly ConcurrentBag<string> _slnFiles = new ConcurrentBag<string>();
        private readonly ConcurrentBag<string> _projFiles = new ConcurrentBag<string>();

        private readonly ConcurrentDictionary<string, VsProjectFile> _projectFilesIndex = new ConcurrentDictionary<string, VsProjectFile>();

        public Centipede(ILogger logger, string path = "./")
        {
            try
            {
                _logger = logger ?? new NullLogger();
                ProcessVsFiles(path);
                BuildProjectFilesReference();
            }
            catch (Exception e)
            {
                _logger.Log(e.Message);
            }
        }

        private void BuildProjectFilesReference()
        {
            BuildProjectFiles();
            BuildSlnFiles();
        }

        private void BuildProjectFiles()
        {
            //set up proj files first as it acts as a joint between sln and code files.
            Parallel.ForEach(_projFiles, (projFilePath) =>
            {
                try
                {
                    var projectFile = _projectFilesIndex.GetOrAdd(projFilePath.ToUpper(), (key) =>
                    {
                        return new VsProjectFile()
                        {
                            Path = projFilePath,
                            Name = Path.GetFileName(projFilePath),
                            Type = VsProjectFileType.Project,
                        };
                    });

                    var project = new Project(projFilePath);
                    var compileItems = project.Items.Where(i => i.ItemType.Equals("Compile"));

                    //add comile files into index and build IncludedIn
                    foreach (var compileItem in compileItems)
                    {
                        var filePath = Path.GetFullPath(Path.Combine(compileItem.Project.DirectoryPath, compileItem.EvaluatedInclude));
                        var compileFile = _projectFilesIndex.GetOrAdd(filePath.ToUpper(), (key) =>
                        {
                            return new VsProjectFile()
                            {
                                Path = filePath,
                                Name = Path.GetFileName(filePath),
                                Type = VsProjectFileType.CompileFile,
                            };
                        });
                        compileFile.IncludedIn.TryAdd(projFilePath.ToUpper(), projectFile);
                    }
                }
                catch (Exception e)
                {
                    _logger.Log(e.Message);
                }
            });
        }

        public IEnumerable<string> GetProjectFileNames(string file)
        {
            return _projectFilesIndex.Values.Where(pf => pf.Path.IndexOf(file, StringComparison.CurrentCultureIgnoreCase) >= 0).Select(pf => pf.Path).OrderBy(pf => pf);
        }

        private void BuildSlnFiles()
        {
            Parallel.ForEach(_slnFiles, (slnFilePath) =>
            {
                try
                {
                    var slnFile = _projectFilesIndex.GetOrAdd(slnFilePath, (key) =>
                    {
                        return new VsProjectFile()
                        {
                            Path = slnFilePath,
                            Name = Path.GetFileName(slnFilePath),
                            Type = VsProjectFileType.Solution,
                        };
                    });

                    var solution = SolutionParser.Parse(slnFilePath);

                    foreach (var project in solution.Projects)
                    {
                        var projectPath = project.Path;
                        if (!Path.IsPathRooted(projectPath))
                        {
                            projectPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(slnFilePath), projectPath));
                        }
                        VsProjectFile projectFile = null;
                        if (_projectFilesIndex.TryGetValue(projectPath.ToUpper(), out projectFile))
                        {
                            projectFile.IncludedIn.TryAdd(slnFilePath, slnFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Log(e.Message);
                }
            });
        }

        private void ProcessVsFiles(string path)
        {
            _slnFiles.AddRange(Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly));
            _projFiles.AddRange(Directory.GetFiles(path, "*.*proj", SearchOption.TopDirectoryOnly));

            Parallel.ForEach(GetValidDirectories(path), (directory) =>
            {
                ProcessVsFiles(directory);
            });
        }

        private IEnumerable<string> GetValidDirectories(string path)
        {
            return Directory.GetDirectories(path).Where(x => !Path.GetFileName(x).StartsWith("."));
        }

        public IEnumerable<VsProjectFile> GetHostSolutions (string fileName)
        {
            var result = new List<VsProjectFile>();
            VsProjectFile file = null;
            if(_projectFilesIndex.TryGetValue(fileName.ToUpper(), out file))
            {
                ProcessHostSolutions(file, result);
            }
            return result;
        }

        private void ProcessHostSolutions(VsProjectFile file, List<VsProjectFile> result)
        {
            if (file.Type == VsProjectFileType.Solution)
            {
                result.Add(file);
                return;
            }
            else
            {
                foreach(var refFiles in file.IncludedIn.Values)
                {
                    ProcessHostSolutions(refFiles, result);
                }
            }
        }
    }

    public static class ConcurrentBagExt
    {
        public static void AddRange<T>(this ConcurrentBag<T> @this, IEnumerable<T> toAdd)
        {
            foreach (var element in toAdd)
            {
                @this.Add(element);
            }
        }
    }
}
