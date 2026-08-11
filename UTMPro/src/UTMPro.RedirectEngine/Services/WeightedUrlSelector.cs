using UTMPro.RedirectEngine.Models;

namespace UTMPro.RedirectEngine.Services;

public class WeightedUrlSelector
{
    public string? Pick(IReadOnlyList<DestinationModel> destinations) =>
        PickDestination(destinations)?.Url;

    public DestinationModel? PickDestination(IReadOnlyList<DestinationModel>? destinations)
    {
        if (destinations == null || destinations.Count == 0)
            return null;

        if (destinations.Count == 1)
            return destinations[0];

        // Use long so a malformed/large set of integer weights cannot overflow.
        var totalWeight = destinations.Sum(destination => (long)Math.Max(0, destination.Weight));
        if (totalWeight <= 0)
            return destinations[0];

        var roll = Random.Shared.NextInt64(1, totalWeight + 1);
        long cumulative = 0;

        foreach (var destination in destinations)
        {
            cumulative += Math.Max(0, destination.Weight);
            if (roll <= cumulative)
                return destination;
        }

        return destinations[^1];
    }
}
