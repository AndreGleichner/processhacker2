﻿/*
 * Process Hacker Toolchain - 
 *   Build script
 * 
 * Copyright (C) 2017 dmex
 * 
 * This file is part of Process Hacker.
 * 
 * Process Hacker is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Process Hacker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Process Hacker.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace CustomBuildTool
{
    public static class Program
    {
        public static Dictionary<string, string> ProgramArgs;

        private static bool BuildSdk(BuildFlags Flags)
        {
            PrintColorMessage("Copying Plugin SDK...", ConsoleColor.Cyan);

            if (!Build.CopyTextFiles())
                return false;
            if (!Build.CopyPluginSdkHeaders())
                return false;
            if (!Build.CopyVersionHeader())
                return false;
            if (!Build.FixupResourceHeader())
                return false;
            if (!Build.CopyLibFiles(Flags))
                return false;

            return true;
        }

        public static void Main(string[] args)
        {
            ProgramArgs = ParseArgs(args);

            if (ProgramArgs.ContainsKey("-cleanup"))
            {
                if (Restart("-cleanup"))
                    return;

                if (!Build.InitializeBuildEnvironment(true))
                    return;

                Build.CleanupAppxSignature();
                Build.CleanupBuildEnvironment();

                Build.ShowBuildStats();
            }
            else if (ProgramArgs.ContainsKey("-phapppub_gen"))
            {
                if (!Build.InitializeBuildEnvironment(false))
                    return;

                Build.BuildPublicHeaderFiles();
            }
            else if (ProgramArgs.ContainsKey("-graph"))
            {
                if (!Build.InitializeBuildEnvironment(true))
                    return;

                Build.ShowBuildEnvironment("changelog", true, false);

                if (Win32.GetConsoleMode(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), out ConsoleMode mode))
                    Win32.SetConsoleMode(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), mode | ConsoleMode.ENABLE_VIRTUAL_TERMINAL_PROCESSING);

                Console.WriteLine(Build.GetBuildLogString());

                Build.ShowBuildStats();
            }
            else if (ProgramArgs.ContainsKey("-sdk"))
            {
                if (!Build.InitializeBuildEnvironment(false))
                    return;

                Build.CopyKProcessHacker(
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildDebug
                    );

                BuildSdk(
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildDebug | BuildFlags.BuildVerbose
                    );
            }
            else if (ProgramArgs.ContainsKey("-cleansdk"))
            {
                if (!Build.InitializeBuildEnvironment(false))
                    return;

                if (!Build.BuildSolution("ProcessHacker.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildVerbose
                    ))
                {
                    return;
                }

                BuildSdk(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose);

                Build.ShowBuildStats();
            }
            else if (ProgramArgs.ContainsKey("-bin"))
            {
                if (!Build.InitializeBuildEnvironment(false))
                    return;

                Build.ShowBuildEnvironment("bin", false, true);
                Build.CopyKeyFiles();

                if (!Build.BuildSolution("ProcessHacker.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyKProcessHacker(
                    BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!BuildSdk(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.BuildSolution("plugins\\Plugins.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyWow64Files(BuildFlags.None))
                    return;

                if (!Build.BuildBinZip())
                    return;

                Build.ShowBuildStats();
            }
            else if (ProgramArgs.ContainsKey("-debug"))
            {
                if (!Build.InitializeBuildEnvironment(true))
                    return;

                Build.ShowBuildEnvironment("debug", true, true);
                Build.CopyKeyFiles();

                if (!Build.BuildSolution("ProcessHacker.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildDebug | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyKProcessHacker(
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildDebug | BuildFlags.BuildVerbose))
                    return;

                if (!BuildSdk(
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildDebug | BuildFlags.BuildVerbose
                    ))
                {
                    return;
                }

                if (!Build.BuildSolution("plugins\\Plugins.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit |
                    BuildFlags.BuildDebug | BuildFlags.BuildVerbose
                    ))
                {
                    return;
                }

                if (!Build.CopyWow64Files(BuildFlags.BuildDebug))
                    return;

                Build.ShowBuildStats();
            }
            else if (ProgramArgs.ContainsKey("-appveyor"))
            {
                if (!Build.InitializeBuildEnvironment(true))
                    return;

                Build.ShowBuildEnvironment("nightly", true, true);
                Build.CopyKeyFiles();

                if (!Build.BuildSolution("ProcessHacker.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyKProcessHacker(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!BuildSdk(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.BuildSolution("plugins\\Plugins.sln", BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyWow64Files(BuildFlags.None))
                    return;

                if (!Build.BuildBinZip())
                    return;
                if (!Build.BuildSetupExe())
                    return;
                if (!Build.BuildChecksumsFile())
                    return;
                if (!Build.BuildUpdateSignature())
                    return;

                if (Build.AppveyorUploadBuildFiles())
                    Build.WebServiceUpdateConfig();
            }
            else if (ProgramArgs.ContainsKey("-appxbuild"))
            {
                if (!Build.InitializeBuildEnvironment(true))
                    return;

                Build.ShowBuildEnvironment("appx", true, true);
                Build.CopyKeyFiles();

                if (!Build.BuildSolution("ProcessHacker.sln", BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!BuildSdk(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.BuildSolution("plugins\\Plugins.sln", BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyWow64Files(BuildFlags.None))
                    return;

                Build.BuildAppxPackage(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose);

                Build.ShowBuildStats();
            }
            else if (ProgramArgs.ContainsKey("-appxmakecert"))
            {
                if (Restart("-appxmakecert"))
                    return;

                if (!Build.InitializeBuildEnvironment(false))
                    return;

                Build.ShowBuildEnvironment("appxcert", true, true);

                Build.BuildAppxSignature();

                Build.ShowBuildStats();
            }
            else
            {
                if (!Build.InitializeBuildEnvironment(true))
                    return;

                Build.ShowBuildEnvironment("release", true, true);
                Build.CopyKeyFiles();
     
                if (!Build.BuildSolution("ProcessHacker.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyKProcessHacker(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!BuildSdk(BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.BuildSolution("plugins\\Plugins.sln",
                    BuildFlags.Build32bit | BuildFlags.Build64bit | BuildFlags.BuildVerbose))
                    return;

                if (!Build.CopyWow64Files(BuildFlags.None))
                    return;

                if (!Build.BuildBinZip())
                    return;
                if (!Build.BuildSetupExe())
                    return;
                Build.BuildPdbZip();
                Build.BuildSdkZip();
                Build.BuildSrcZip();

                Build.ShowBuildStats();
            }
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string argPending = null;

            foreach (string s in args)
            {
                if (s.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                {
                    if (!dict.ContainsKey(s))
                        dict.Add(s, string.Empty);

                    argPending = s;
                }
                else
                {
                    if (argPending != null)
                    {
                        dict[argPending] = s;
                        argPending = null;
                    }
                    else
                    {
                        if (!dict.ContainsKey(string.Empty))
                            dict.Add(string.Empty, s);
                    }
                }
            }

            return dict;
        }

        public static void PrintColorMessage(string Message, ConsoleColor Color, bool Newline = true, BuildFlags Flags = BuildFlags.BuildVerbose)
        {
            if ((Flags & BuildFlags.BuildVerbose) != BuildFlags.BuildVerbose)
                return;

            Console.ForegroundColor = Color;
            if (Newline)
                Console.WriteLine(Message);
            else
                Console.Write(Message);
            Console.ResetColor();
        }

        private static bool Restart(string Arguments)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                return false;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                    Arguments = Arguments,
                    Verb = "runas"
                });
            }
            catch (Exception) { }

            Environment.Exit(Environment.ExitCode);

            return true;
        }
    }

    [Flags]
    public enum BuildFlags
    {
        None,
        Build32bit = 0x1,
        Build64bit = 0x2,
        BuildDebug = 0x4,
        BuildVerbose = 0x8
    }
}