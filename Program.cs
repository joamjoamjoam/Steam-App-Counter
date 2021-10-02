using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace steamAppFinder
{
    class Program
    {
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        static void Main(string[] args)
        {
            int exitCode = 0;
            List<String> file = new List<string>();
            try
            {
                var handle = GetConsoleWindow();

                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    // Hide
                    ShowWindow(handle, SW_HIDE);
                }



                String appName = (args.Length > 0) ? args[0].ToLower() : "";
                String mode = (args.Length > 1) ? args[1].ToLower() : "count";
                List<Process> procs = Process.GetProcesses().ToList();
                Process steam = procs.Find(x => x.ProcessName.ToLower() == "steam");

                file.Add($"Mode: {mode}");
                file.Add($"App Name: {appName}");


                List<Process> procIdList = new List<Process>();
                if (steam != null)
                {
                    file.Add($"Steam ID: {steam.Id}");
                    int steamId = steam.Id;

                    foreach (var proc in procs)
                    {
                        using (ManagementObject mo = new ManagementObject($"win32_process.handle='{proc.Id}'"))
                        {
                            if (mo != null)
                            {
                                try
                                {
                                    mo.Get();
                                    int parentPid = Convert.ToInt32(mo["ParentProcessId"]);
                                    if (parentPid == steamId)
                                    {
                                        Console.Out.WriteLine($"{proc.ProcessName} is running as a child to {mo["ParentProcessId"]}");
                                        if (proc.ProcessName.ToLower() != "steamwebhelper")
                                        {
                                            if (proc.ProcessName.ToLower() == "gameoverlayui")
                                            {
                                                procIdList.Add(proc);
                                                Console.Out.WriteLine($"{proc.ProcessName} was added to the running game count.");
                                                // Attach onto Process and wait for it to exit
                                                

                                            }

                                        }
                                    }

                                }
                                catch (Exception ex)
                                {
                                    // the process ended between polling all of the procs and requesting the management object
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.Out.WriteLine($"Steam is not Running");
                }
                Console.Out.WriteLine($"Games Running: {procIdList.Count}");
                
                switch (mode)
                {
                    case "attach":
                        // Hold Until Process Terminate
                        Console.Out.WriteLine($"Attaching to Games Running:\n{String.Join("\n", procIdList.Select(p => $"{p.Id} - {p.ProcessName}"))}");
                        file.Add($"Attaching to Games Running:\n{String.Join("\n", procIdList.Select(p => $"{p.Id} - {p.ProcessName}"))}");
                        while (procIdList.Count > 0)
                        {
                            if (!procIdList[0].HasExited)
                            {
                                procIdList[0].WaitForExit();
                            }
                            procIdList.RemoveAt(0);
                        }
                        file.Add("Done Waiting on Attached Processes. Exiting Now.");
                        exitCode = 0;
                        break;
                    case "count":
                    default:
                        file.Add($"Counted {procIdList.Count} Steam Processes Running");
                        exitCode = procIdList.Count;
                        break;
                }
            }
            catch(Exception ex)
            {
                file.Add(ex.Message);
            }
            
            File.WriteAllLines(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\steamAppCounter.log", file);
            Environment.Exit(exitCode);
        }
    }
}
