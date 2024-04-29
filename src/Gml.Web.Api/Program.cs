using Gml.Web.Api.Core.Extensions;

WebApplication.CreateBuilder(args)
    .RegisterServices()
    .Build()
    .RegisterServices()
    .Run();
