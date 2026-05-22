using System.Runtime.CompilerServices;

namespace GymApp.Tests.Integration.Abstractions;

/// <summary>
/// Assembly-load bootstrap for Testcontainers.
/// </summary>
/// <remarks>
/// Runs via <see cref="ModuleInitializerAttribute"/> — guaranteed to execute at
/// assembly load, before any type's static/field initializers (including
/// <c>IntegrationTestWebAppFactory</c>'s <c>MsSqlContainer</c> field).
///
/// Disables the Testcontainers Ryuk resource-reaper. On macOS Docker Desktop the
/// user-scoped Docker socket (<c>~/.docker/run/docker.sock</c>) cannot be
/// bind-mounted ("operation not supported"), which makes the Ryuk container fail
/// to start and blocks every integration test before any SQL container launches.
///
/// Ryuk is only a stray-container janitor. <c>IntegrationTestWebAppFactory</c>
/// already stops its own containers in <c>DisposeAsync</c>, so disabling Ryuk is
/// safe for this suite.
/// </remarks>
internal static class TestcontainersBootstrap
{
    [ModuleInitializer]
    internal static void Init()
        => Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
}
