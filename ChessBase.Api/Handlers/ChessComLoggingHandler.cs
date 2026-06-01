using System.Diagnostics;

namespace ChessBase.Api.Handlers;

public class ChessComLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ChessComLoggingHandler> _logger;

    public ChessComLoggingHandler(ILogger<ChessComLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Логируем сам запрос
        _logger.LogDebug("Sending HTTP {Method} to {Url}", request.Method, request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        stopwatch.Stop();

        // Логируем результат и время выполнения
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("HTTP {Method} {Url} completed in {Elapsed}ms with {StatusCode}",
                request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds, response.StatusCode);
        }
        else
        {
            _logger.LogWarning("HTTP {Method} {Url} failed after {Elapsed}ms with {StatusCode}",
                request.Method, request.RequestUri, stopwatch.ElapsedMilliseconds, response.StatusCode);
        }

        // Тут можно централизованно обработать 429 Too Many Requests (Rate Limit Chess.com)
        if (response.StatusCode == (System.Net.HttpStatusCode)429)
        {
            _logger.LogCritical("Chess.com API Rate Limit reached!");
        }

        return response;
    }
}
//
// 2. Зарегистрируйте его в `Program.cs`
// Теперь нужно сказать DI-контейнеру использовать этот обработчик для вашего клиента:
//
// // 1. Сначала регистрируем сам хендлер
// builder.Services.AddTransient<ChessComLoggingHandler>();
//
// // 2. Добавляем его в цепочку HttpClient
// builder.Services.AddHttpClient<IChessComClient, ChessComClient>(client => {
// client.BaseAddress = new Uri("https://api.chess.com/pub/");
// client.DefaultRequestHeaders.Add("User-Agent", "ChessBase-App");
// })
// .AddHttpMessageHandler<ChessComLoggingHandler>(); // <-- Вот тут магия
//
// 3. Очистите `ChessComClient.cs`
// Теперь из самого клиента можно удалить лишний код.
//
//     Что можно убрать из `ChessComClient`:
// 1.  Логирование URL и статус-кодов: Теперь это делает DelegatingHandler автоматически для всех методов.
// 2.  Проверку User-Agent в конструкторе: Если вы добавили его в Program.cs, он будет там всегда.
//
//     Зачем это нужно Junior-у:
// 1.  DRY (Don't Repeat Yourself): Тебе не нужно писать _logger.LogInformation в каждом методе GetArchives, GetGames и т.д. Один хендле
// логирует всё сразу.
// 2.  Чистота кода: В ChessComClient остается только логика парсинга данных (бизнес-логика), а не детали HTTP-протокола.
// 3.  Единая точка входа: Если завтра Chess.com попросит добавить какой-то специальный заголовок или API-ключ ко всем запросам, ты
// изменишь это в одном месте — в DelegatingHandler.
// 4.  Обработка Rate Limits: Chess.com очень строг к лимитам. В хендлере можно легко реализовать «паузу» (Wait and Retry), если пришел
// статус 429.