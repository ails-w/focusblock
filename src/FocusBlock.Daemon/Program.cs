using FocusBlock.Contracts;
using FocusBlock.Core;
using FocusBlock.Daemon;
using FocusBlock.Daemon.Services;

var builder = Host.CreateApplicationBuilder(args);

string configPath =
    builder.Configuration["FocusBlock:ConfigPath"] ?? "/etc/focusblock/focusblock.json";
string socketPath =
    builder.Configuration["FocusBlock:SocketPath"] ?? "/run/focusblock/focusblock.sock";

AppConfig config = await new ConfigService(configPath).LoadAsync();

builder.Services.AddSingleton(config);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProcessSource, ProcProcessSource>();
builder.Services.AddSingleton<ISignalSender, LibcSignalSender>();
builder.Services.AddSingleton<IPasswordVerifier, AuthService>();
builder.Services.AddSingleton<BlockEngine>();
builder.Services.AddSingleton<CooldownManager>();
builder.Services.AddSingleton<IIpcRequestHandler, DaemonRequestHandler>();
builder.Services.AddSingleton(sp =>
    new IpcServer(socketPath, sp.GetRequiredService<IIpcRequestHandler>()));

builder.Services.AddHostedService<IpcServerHostedService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();