using UTMPro.RedirectEngine.Models;

namespace UTMPro.RedirectEngine.Services;

public class WeightedUrlSelector
{
    public string? Pick(List<DestinationModel> destinations)
    {
        if (destinations == null || destinations.Count == 0)
            return null;

        if (destinations.Count == 1) return destinations[0].Url;

        int totalWeight = destinations.Sum(d => d.Weight);
        if (totalWeight <= 0) return destinations[0].Url;

        int roll = Random.Shared.Next(1, totalWeight + 1);
        int cumulative = 0;

        foreach (var dest in destinations)
        {
            cumulative += dest.Weight;
            if (roll <= cumulative)
                return dest.Url;
        }

        return destinations.Last().Url;
    }
}
