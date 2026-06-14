using System.Threading.Channels;
using Reflekt.Domain.Cards;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.OpenGraph;

public sealed class OpenGraphBackgroundService(
    ChannelReader<FetchJob> reader,
    OpenGraphFetcher fetcher,
    IServiceScopeFactory scopeFactory,
    ILogger<OpenGraphBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cardRepository = scope.ServiceProvider.GetRequiredService<CardRepository>();

                var card = await cardRepository.GetByIdAsync(job.CardId, stoppingToken);
                if (card is null)
                    continue;

                var result = await fetcher.FetchAsync(job.Url, stoppingToken);

                if (result.Ok)
                {
                    var title = result.Title?[..Math.Min(result.Title.Length, 500)];
                    var description = result.Description?[..Math.Min(result.Description.Length, 1000)];
                    card.UpdateEvidenceLinkMetadata(job.EvidenceLinkId, title, description, result.IconUrl, result.ImageUrl, FetchStatus.Ok);
                }
                else
                    card.UpdateEvidenceLinkMetadata(job.EvidenceLinkId, null, null, null, null, FetchStatus.Failed);

                await cardRepository.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process OpenGraph fetch for EvidenceLink {Id}", job.EvidenceLinkId);
            }
        }
    }
}
