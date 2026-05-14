namespace GymApp.Tests.Functional.Abstractions;

[CollectionDefinition(nameof(FunctionalTestCollection))]
public sealed class FunctionalTestCollection
    : ICollectionFixture<FunctionalTestWebAppFactory>;
