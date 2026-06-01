using System.Text.Json;
using ChessBase.Data;
using ChessBase.Kafka;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChessBase.BackgroundService;

public class AnalysisConsumeWorker(
    IServiceProvider serviceProvider,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<AnalysisConsumeWorker> logger) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); //не блокируем старт приложения.

        var options = kafkaOptions.Value;
        
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Consumer.BootstrapServers ?? options.BootstrapServers,
            GroupId = options.Consumer.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, error) => logger.LogError("Kafka error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(options.TopicGamesToAnalyze);
        logger.LogInformation("Kafka Consumer started listening topic: {Topic}", options.TopicGamesToAnalyze);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Блокирующий вызов, но он прервется при остановке приложения (stoppingToken) есть ли смысл его еще дополнительно отпускать по тайммеру наверно нет?
                    var consumeResult = consumer.Consume(stoppingToken);
                    
                    if (string.IsNullOrWhiteSpace(consumeResult?.Message?.Value))
                    {
                        logger.LogWarning("Received empty payload.");
                        continue;
                    }

                    var gameEvent = JsonSerializer.Deserialize<GameFetchedEvent>(consumeResult.Message.Value);
                    
                    if (string.IsNullOrEmpty(gameEvent?.Uuid))
                    {
                        logger.LogWarning("Deserialized event is null or missing Uuid! Raw JSON: {Raw}", consumeResult.Message.Value);
                        consumer.Commit(consumeResult); 
                        continue;
                    }

                    logger.LogInformation("Received game {ExternalId}. Starting processing...", gameEvent.Uuid);

                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ChessDbContext>();
                    var analyzer = scope.ServiceProvider.GetRequiredService<GameAnalyzer>();

                    if (await dbContext.Games.AnyAsync(g => g.ExternalId == gameEvent.Uuid, stoppingToken))
                    {
                        logger.LogWarning("Game {ExternalId} already exists in DB. Skipping.", gameEvent.Uuid);
                        consumer.Commit(consumeResult);
                        continue;
                    }
                    //todo add inner chanel prevent fall connection to kafka on timeout
                    var report = await analyzer.AnalyzePgnAsync(gameEvent.Pgn, gameEvent.TargetUsername);
                    
                    var game = new GameReport 
                    { 
                        Url = gameEvent.Url,
                        ExternalId = gameEvent.Uuid,
                        PgnText = gameEvent.Pgn,
                        TotalAccuracy = report.TotalAccuracy,
                        OpeningName = report.OpeningName,
                        EcoCode = report.EcoCode,
                        UserIsWhite = report.UserIsWhite,
                        Result = report.ResultForUser,
                        PlayedAt = DateTimeOffset.FromUnixTimeSeconds(gameEvent.EndTimeSeconds).UtcDateTime, 
                        Moves = report.Moves 
                    };
                
                    dbContext.Games.Add(game);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    
                    consumer.Commit(consumeResult);
                    logger.LogInformation("Game {ExternalId} analyzed and saved successfully.", game.ExternalId);
                }
                catch (ConsumeException e)
                {
                    logger.LogError("Consume error: {Reason}", e.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Kafka Consumer gracefully stopping...");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error in Kafka Consumer loop.");
            throw; 
        }
        finally
        {
            consumer.Close();
        }
    }
}