using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Configurations;

namespace GymApp.Tests.Functional.Abstractions;

/// <summary>
/// Disables the Testcontainers Ryuk resource-reaper before any container is built.
/// The Ryuk reaper bind-mounts the Docker socket, which fails on macOS Docker
/// Desktop installs that expose only the user-scoped socket
/// (~/.docker/run/docker.sock — "operation not supported" for bind mounts).
/// FunctionalTestWebAppFactory disposes its own containers in DisposeAsync, so the
/// reaper's stray-container cleanup is not needed.
///
/// A [ModuleInitializer] runs at assembly load — guaranteed before any container
/// field initializer or container build executes. We disable the reaper two ways:
///  1. <see cref="TestcontainersSettings.ResourceReaperEnabled"/> — the in-process
///     setting the library actually checks when starting a container. Setting it
///     directly does not depend on env-var read timing.
///  2. The TESTCONTAINERS_RYUK_DISABLED env var — belt-and-suspenders for any code
///     path (or child process) that re-reads configuration from the environment.
/// The functional.runsettings file sets the same env var at the test-host process
/// level as a third, earliest-possible safety net.
/// </summary>
internal static class TestcontainersBootstrap
{
    [ModuleInitializer]
    internal static void Init()
    {
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        TestcontainersSettings.ResourceReaperEnabled = false;
    }
}
