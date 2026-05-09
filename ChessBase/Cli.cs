// var builder = WebApplication.CreateBuilder(args); 
// builder.Services.AddControllers(); 
// builder.Services.AddEndpointsApiExplorer();
//
// var app = builder.Build();
//
// builder.Services.AddHttpClient("ChessClient", client =>
// {
//     client.DefaultRequestHeaders.Add(
//         "User-Agent",
//         "ChessAnalyzerApp/1.0 (dim4ik121313@gmail.com)"
//     );
// });
//
// app.MapControllers();
// app.Run();

// DI: Мы будем регистрировать HttpClient через AddHttpClient<IChessComClient, ChessComClient>() в основном проекте.