using Xunit;

namespace ClinicBooking.Tests.Infrastructure;

/// <summary>
/// Shares a single <see cref="TestDatabase"/> across every test class in the
/// "Database" collection so the (relatively expensive) schema creation happens
/// once. Tests stay isolated by always creating their own doctor/patient rows.
/// </summary>
[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<TestDatabase>
{
}
