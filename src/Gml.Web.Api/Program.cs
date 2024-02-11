using System.Net;
using FluentValidation;
using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Core.Handlers;
using Gml.Web.Api.Core.Messages;
using Gml.Web.Api.Core.Middlewares;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Core.Validation;
using Gml.Web.Api.Data;
using Gml.Web.Api.Dto.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

WebApplication.CreateBuilder(args)
    .RegisterServices()
    .Build()
    .RegisterServices()
    .Run();
