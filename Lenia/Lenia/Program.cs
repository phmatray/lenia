using Lenia.Modules;
using TheAppManager.Startup;

AppManager.Start(args, modules =>
{
    modules
        .Add<BlazorModule>()
        .Add<InfrastructureModule>();
});
