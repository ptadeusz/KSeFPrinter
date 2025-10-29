
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
///wymagany jest proces wprowadzenia opóźnienia dla wołania 'auth/challenge'
[assembly: TestCollectionOrderer(
    "KSeF.Client.Tests.DelayedTestCollectionOrderer", // pełna nazwa typu z namespace
    "KSeF.Client.Tests")]




namespace KSeF.Client.Tests;

// Klasa wprowadzająca opóźnienia pomiędzy wykonaniem testów
public class DelayedTestCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        foreach (var collection in testCollections)
        {
            Task.Delay(2000).GetAwaiter().GetResult();
            yield return collection;
        }
    }
}

