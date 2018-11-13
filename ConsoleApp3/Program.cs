using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {

            string cDir = args[0];
            string vText = args[1];
            string repo = args[2];
            string opFolder = args[3];
            string repoDir = "";

            var versionList = ExtractVersionList(vText);


            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript($"Remove-Item -Recurse -Force {cDir}*");
                ps.AddScript($"Remove-Item -Recurse -Force {opFolder}*");
                ps.AddScript($"cd {cDir}");                
                ps.AddScript($"git clone {repo}");
                ps.AddScript($"Get-ChildItem -Name");                

                var res = ps.Invoke();

                repoDir = (string)res[0].BaseObject;         
            }

            LoopThroughVersions();

            ConsolidateVersions();

            void ConsolidateVersions()
            {
                
                    string pathToScan = opFolder;

                    string destinationFile = $"{opFolder}OutputOrderedDistinct.txt";

                    File.Delete(destinationFile);

                    var filesList = Directory.EnumerateFiles(pathToScan, "*", SearchOption.AllDirectories);

                    List<string> dependencies = new List<string>();
                    foreach (var file in filesList)
                    {
                        using (StreamReader sr = File.OpenText(file))
                        {
                            string s = "";
                            while ((s = sr.ReadLine()) != null)
                            {
                                dependencies.Add(s);
                            }
                        }
                    }

                    using (StreamWriter sw = new StreamWriter(destinationFile))
                    {
                        foreach (var line in dependencies.Distinct().OrderBy(m => m).ToList())
                        {
                            sw.WriteLine(line);
                        }
                    }
                
            }

            void LoopThroughVersions()
            {
                void GenerateDependenciesForIndividualVersion(string ver)
                {
                    
                        string pathToScan = cDir+repoDir;
                        string destinationFile = $"{opFolder}OutPut.txt";
                        string destinationFileOrdered = $"{opFolder}OutPutOrdered{ver}.txt";

                        File.Delete(destinationFileOrdered);
                        File.Delete(destinationFile);

                        var filesList = Directory.EnumerateFiles(pathToScan, "packages.config", SearchOption.AllDirectories);

                        StringBuilder sb = new StringBuilder();
                        foreach (var file in filesList)
                        {
                            sb.AppendLine(File.ReadAllText(file));
                        }

                        using (StreamWriter sw = File.CreateText(destinationFile))
                        {
                            sw.Write(sb);
                        }

                        List<string> list = new List<string>();

                        using (StreamReader sr = File.OpenText(destinationFile))
                        {
                            string s = "";
                            while ((s = sr.ReadLine()) != null)
                            {
                                list.Add(s);
                            }
                        }


                        using (StreamWriter sw = new StreamWriter(destinationFileOrdered))
                        {
                            foreach (var line in list.Where(m => m.Contains("package id=")).Distinct().OrderBy(m => m).ToList())
                            {
                                sw.WriteLine(line);
                            }
                        }
                    
                }

                


                foreach (var version in versionList)
                {
                    using (PowerShell ps = PowerShell.Create())
                    {
                        ps.AddScript($"cd {cDir+repoDir}");
                        ps.AddScript($"git checkout {version}");
                        ps.Invoke();
                    }

                    GenerateDependenciesForIndividualVersion(version);
                }
            }

            List<string> ExtractVersionList(string vText1)
            {
                List<string> vList = new List<string>();

                using (StreamReader sr = File.OpenText(vText1))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        vList.Add(s);
                    }
                }

                return vList;
            }
        }
    }
}
