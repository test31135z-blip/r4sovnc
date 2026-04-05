using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace R4SoVNC.Server.Builder
{
    /// <summary>
    /// Compiles the embedded client source files into a standalone .exe
    /// using the Roslyn C# compiler bundled as a NuGet package.
    /// No .NET SDK or Visual Studio required on the build machine.
    /// </summary>
    public static class ClientCompiler
    {
        // Apphost placeholder — .NET SDK patches this to the assembly name
        private const string AppHostPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2db15579a8b498bbf8b24dc9";

        private static readonly string ServerDir =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        private static readonly string ClientSourceDir =
            Path.Combine(ServerDir, "ClientSource");

        public class CompileResult
        {
            public bool   Success      { get; init; }
            public string ExePath      { get; init; } = "";
            public string ErrorMessage { get; init; } = "";
        }

        public static CompileResult Compile(string host, int port,
            string outputDir, Action<string> progress,
            CancellationToken cancel = default)
        {
            try
            {
                // ── 1. Verify ClientSource is present ─────────────────────────
                if (!Directory.Exists(ClientSourceDir))
                    return Fail($"ClientSource directory not found:\n{ClientSourceDir}");

                progress("Reading client source files...");
                var syntaxTrees = BuildSyntaxTrees(host, port, cancel);
                if (syntaxTrees.Count == 0)
                    return Fail("No .cs files found in ClientSource.");

                // ── 2. Collect reference assemblies ───────────────────────────
                progress("Resolving references...");
                var refs = CollectReferences(progress);
                if (refs.Count == 0)
                    return Fail("Could not locate .NET runtime reference assemblies.");

                // ── 3. Roslyn compile ─────────────────────────────────────────
                progress("Compiling with Roslyn (no SDK required)...");
                cancel.ThrowIfCancellationRequested();

                string dllName = "R4SoVNC.Client";
                var compilation = CSharpCompilation.Create(
                    assemblyName: dllName,
                    syntaxTrees:  syntaxTrees,
                    references:   refs,
                    options: new CSharpCompilationOptions(
                        outputKind:            OutputKind.ConsoleApplication,
                        optimizationLevel:     OptimizationLevel.Release,
                        platform:              Platform.AnyCpu,
                        allowUnsafe:           true,
                        nullableContextOptions: NullableContextOptions.Enable));

                Directory.CreateDirectory(outputDir);
                string dllPath = Path.Combine(outputDir, dllName + ".dll");
                string pdbPath = Path.Combine(outputDir, dllName + ".pdb");

                EmitResult emitResult = compilation.Emit(dllPath, pdbPath);

                if (!emitResult.Success)
                {
                    var errors = emitResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString());
                    return Fail("Compilation errors:\n" + string.Join("\n", errors));
                }

                // ── 4. Copy dependency DLLs ───────────────────────────────────
                progress("Copying dependencies...");
                CopyDependencies(outputDir);

                // ── 5. Write runtimeconfig.json ───────────────────────────────
                progress("Writing runtime config...");
                WriteRuntimeConfig(outputDir, dllName);

                // ── 6. Find & patch apphost to make a .exe ────────────────────
                progress("Creating launcher executable...");
                string exePath = Path.Combine(outputDir, dllName + ".exe");
                bool   gotExe  = TryCreateAppHost(dllName, exePath);

                if (!gotExe)
                {
                    // Fallback: write a .bat launcher
                    File.WriteAllText(
                        Path.Combine(outputDir, dllName + ".bat"),
                        $"@echo off\r\ndotnet \"%~dp0{dllName}.dll\"\r\n");

                    exePath = dllPath; // point to dll
                    progress("Note: apphost not found — created .bat launcher instead.");
                }

                progress("Done!");
                return new CompileResult { Success = true, ExePath = exePath };
            }
            catch (OperationCanceledException)
            {
                return Fail("Build cancelled.");
            }
            catch (Exception ex)
            {
                return Fail(ex.ToString());
            }
        }

        // ── Syntax trees ───────────────────────────────────────────────────────

        private static List<SyntaxTree> BuildSyntaxTrees(string host, int port, CancellationToken cancel)
        {
            var trees = new List<SyntaxTree>();
            foreach (string file in Directory.GetFiles(ClientSourceDir, "*.cs", SearchOption.AllDirectories))
            {
                cancel.ThrowIfCancellationRequested();
                string src = File.ReadAllText(file, Encoding.UTF8);

                // Patch ClientConfig.cs with real HOST and PORT
                if (Path.GetFileName(file).Equals("ClientConfig.cs", StringComparison.OrdinalIgnoreCase))
                {
                    src = "namespace R4SoVNC.ClientEmbed\n{\n" +
                          "    internal static class ClientConfig\n    {\n" +
                          $"        public const string HOST = \"{EscapeString(host)}\";\n" +
                          $"        public const int    PORT = {port};\n" +
                          "    }\n}\n";
                }

                trees.Add(CSharpSyntaxTree.ParseText(src, path: file));
            }
            return trees;
        }

        // ── Reference assembly discovery ───────────────────────────────────────

        private static List<MetadataReference> CollectReferences(Action<string> progress)
        {
            var refs = new List<MetadataReference>();

            // a) .NET 8 shared framework DLLs (runtime, no SDK needed)
            string[] runtimeRoots =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             @"dotnet\shared\Microsoft.NETCore.App"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                             @"dotnet\shared\Microsoft.NETCore.App"),
                @"C:\Program Files\dotnet\shared\Microsoft.NETCore.App",
            };

            string[] desktopRoots =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             @"dotnet\shared\Microsoft.WindowsDesktop.App"),
                @"C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App",
            };

            AddFrameworkRefs(refs, runtimeRoots,  "8.", progress);
            AddFrameworkRefs(refs, desktopRoots,  "8.", progress);

            // b) Dependency DLLs from the server's own output directory
            string[] clientDeps = { "NAudio.dll", "Newtonsoft.Json.dll",
                                     "AForge.dll", "AForge.Video.dll",
                                     "AForge.Video.DirectShow.dll",
                                     "AForge.Math.dll" };
            foreach (string dep in clientDeps)
            {
                string p = Path.Combine(ServerDir, dep);
                if (File.Exists(p))
                    refs.Add(MetadataReference.CreateFromFile(p));
            }

            // c) The currently-loaded mscorlib / System.Runtime as fallback
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) continue;
                try { refs.Add(MetadataReference.CreateFromFile(asm.Location)); } catch { }
            }

            return refs;
        }

        private static void AddFrameworkRefs(List<MetadataReference> refs,
            string[] roots, string versionPrefix, Action<string> progress)
        {
            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;
                var versions = Directory.GetDirectories(root)
                    .Where(d => Path.GetFileName(d).StartsWith(versionPrefix))
                    .OrderByDescending(d => d)
                    .ToArray();

                if (versions.Length == 0) continue;
                string latest = versions[0];
                progress($"Found runtime: {Path.GetFileName(latest)}");

                foreach (string dll in Directory.GetFiles(latest, "*.dll"))
                {
                    try { refs.Add(MetadataReference.CreateFromFile(dll)); } catch { }
                }
                return; // use first (latest) found root
            }
        }

        // ── Dependency DLL copy ───────────────────────────────────────────────

        private static void CopyDependencies(string outputDir)
        {
            string[] deps = {
                "NAudio.dll", "NAudio.Core.dll", "NAudio.WinForms.dll",
                "NAudio.WinMM.dll", "NAudio.Asio.dll", "NAudio.Wasapi.dll",
                "Newtonsoft.Json.dll",
                "AForge.dll", "AForge.Math.dll",
                "AForge.Video.dll", "AForge.Video.DirectShow.dll",
            };
            foreach (string dep in deps)
            {
                string src = Path.Combine(ServerDir, dep);
                string dst = Path.Combine(outputDir, dep);
                if (File.Exists(src) && src != dst)
                    File.Copy(src, dst, overwrite: true);
            }
        }

        // ── runtimeconfig.json ────────────────────────────────────────────────

        private static void WriteRuntimeConfig(string outputDir, string dllName)
        {
            string json =
                "{\n" +
                "  \"runtimeOptions\": {\n" +
                "    \"tfm\": \"net8.0\",\n" +
                "    \"frameworks\": [\n" +
                "      { \"name\": \"Microsoft.NETCore.App\",        \"version\": \"8.0.0\" },\n" +
                "      { \"name\": \"Microsoft.WindowsDesktop.App\", \"version\": \"8.0.0\" }\n" +
                "    ]\n" +
                "  }\n" +
                "}\n";
            File.WriteAllText(Path.Combine(outputDir, dllName + ".runtimeconfig.json"), json, Encoding.UTF8);
        }

        // ── Apphost creation ──────────────────────────────────────────────────

        private static bool TryCreateAppHost(string assemblyName, string exeOutputPath)
        {
            string? template = FindAppHostTemplate();
            if (template == null) return false;

            try
            {
                byte[] exe   = File.ReadAllBytes(template);
                byte[] find  = Encoding.UTF8.GetBytes(AppHostPlaceholder);
                byte[] replace = new byte[find.Length]; // zero-padded
                byte[] nameBytes = Encoding.UTF8.GetBytes(assemblyName + ".dll");
                Array.Copy(nameBytes, replace, Math.Min(nameBytes.Length, replace.Length));

                int idx = IndexOf(exe, find);
                if (idx < 0) return false;

                Array.Copy(replace, 0, exe, idx, replace.Length);
                File.WriteAllBytes(exeOutputPath, exe);
                return true;
            }
            catch { return false; }
        }

        private static string? FindAppHostTemplate()
        {
            // SDK locations (if SDK is installed)
            var sdkRoots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             @"dotnet\sdk"),
                @"C:\Program Files\dotnet\sdk",
            };
            foreach (string root in sdkRoots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string ver in Directory.GetDirectories(root)
                             .Where(d => Path.GetFileName(d).StartsWith("8."))
                             .OrderByDescending(d => d))
                {
                    string p = Path.Combine(ver, "AppHostTemplate", "apphost.exe");
                    if (File.Exists(p)) return p;
                }
            }

            // NuGet packs location (installed with runtime packs)
            var packRoots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             @"dotnet\packs\Microsoft.NETCore.App.Host.win-x64"),
                @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Host.win-x64",
            };
            foreach (string root in packRoots)
            {
                if (!Directory.Exists(root)) continue;
                foreach (string ver in Directory.GetDirectories(root)
                             .Where(d => Path.GetFileName(d).StartsWith("8."))
                             .OrderByDescending(d => d))
                {
                    string p = Path.Combine(ver, "runtimes", "win-x64", "native", "apphost.exe");
                    if (File.Exists(p)) return p;
                }
            }

            return null;
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                    if (haystack[i + j] != needle[j]) { found = false; break; }
                if (found) return i;
            }
            return -1;
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private static CompileResult Fail(string msg) =>
            new CompileResult { Success = false, ErrorMessage = msg };

        private static string EscapeString(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
