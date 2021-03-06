// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const aspVersion = "2.2.0";

export const pkgs = [
    // aspnet web api
    { id: "Microsoft.AspNet.WebApi.Client", version: "5.2.7" },
    { id: "Microsoft.AspNet.WebApi.Core", version: "5.2.3" },
    { id: "Microsoft.AspNet.WebApi.WebHost", version: "5.2.2" },

    // aspnet core
    { id: "Microsoft.AspNetCore.Antiforgery", version: aspVersion },
    { id: "Microsoft.AspNetCore.Authentication.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Authentication.Core", version: aspVersion },
    { id: "Microsoft.AspNetCore.Authorization.Policy", version: aspVersion },
    { id: "Microsoft.AspNetCore.Authorization", version: aspVersion },
    { id: "Microsoft.AspNetCore.Connections.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Cors", version: aspVersion },
    { id: "Microsoft.AspNetCore.Cryptography.Internal", version: aspVersion },
    { id: "Microsoft.AspNetCore.DataProtection.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.DataProtection", version: aspVersion },
    { id: "Microsoft.AspNetCore.Diagnostics.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Diagnostics", version: aspVersion },
    { id: "Microsoft.AspNetCore.HostFiltering", version: aspVersion },
    { id: "Microsoft.AspNetCore.Hosting.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Hosting.Server.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Hosting", version: aspVersion },
    { id: "Microsoft.AspNetCore.Html.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Http.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Http.Extensions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Http.Features", version: aspVersion },
    { id: "Microsoft.AspNetCore.Http", version: aspVersion },
    { id: "Microsoft.AspNetCore.HttpOverrides", version: aspVersion },
    { id: "Microsoft.AspNetCore.HttpsPolicy", version: aspVersion },
    { id: "Microsoft.AspNetCore.JsonPatch", version: aspVersion },
    { id: "Microsoft.AspNetCore.Localization", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Analyzers", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.ApiExplorer", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Core", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Cors", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.DataAnnotations", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Formatters.Json", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Localization", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Razor.Extensions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.Razor", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.RazorPages", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.TagHelpers", version: aspVersion },
    { id: "Microsoft.AspNetCore.Mvc.ViewFeatures", version: aspVersion },
    { id: "Microsoft.AspNetCore.Razor.Design", version: aspVersion },
    { id: "Microsoft.AspNetCore.Razor.Language", version: aspVersion },
    { id: "Microsoft.AspNetCore.Razor.Runtime", version: aspVersion },
    { id: "Microsoft.AspNetCore.Razor", version: aspVersion },
    { id: "Microsoft.AspNetCore.ResponseCaching.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Routing.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Routing", version: aspVersion },
    { id: "Microsoft.AspNetCore.Server.IIS", version: "2.2.6" },
    { id: "Microsoft.AspNetCore.Server.IISIntegration", version: aspVersion },
    { id: "Microsoft.AspNetCore.Server.Kestrel.Core", version: aspVersion },
    { id: "Microsoft.AspNetCore.Server.Kestrel.Https", version: aspVersion },
    { id: "Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions", version: aspVersion },
    { id: "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets", version: aspVersion },
    { id: "Microsoft.AspNetCore.Server.Kestrel", version: aspVersion },
    { id: "Microsoft.AspNetCore.WebUtilities", version: aspVersion },
    { id: "Microsoft.AspNetCore", version: aspVersion },
    { id: "Microsoft.CodeAnalysis.Razor", version: aspVersion },
    { id: "Microsoft.DiaSymReader.Native", version: "1.7.0" },
    { id: "Microsoft.DotNet.PlatformAbstractions", version: "2.1.0" },
    { id: "Microsoft.Extensions.Caching.Abstractions", version:  aspVersion },
    { id: "Microsoft.Extensions.Caching.Memory", version:  aspVersion },
    { id: "Microsoft.Extensions.Configuration.Abstractions", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration.Binder", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration.CommandLine", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration.EnvironmentVariables", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration.FileExtensions", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration.Json", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration.UserSecrets", version: aspVersion },
    { id: "Microsoft.Extensions.Configuration", version: aspVersion },
    { id: "Microsoft.Extensions.DependencyInjection.Abstractions", version: aspVersion },
    { id: "Microsoft.Extensions.DependencyInjection", version: aspVersion },
    { id: "Microsoft.Extensions.DependencyModel", version: "2.1.0" },
    { id: "Microsoft.Extensions.FileProviders.Abstractions", version: aspVersion },
    { id: "Microsoft.Extensions.FileProviders.Composite", version: aspVersion },
    { id: "Microsoft.Extensions.FileProviders.Physical", version: aspVersion },
    { id: "Microsoft.Extensions.FileSystemGlobbing", version: aspVersion },
    { id: "Microsoft.Extensions.Hosting.Abstractions", version: aspVersion },
    { id: "Microsoft.Extensions.Localization.Abstractions", version: aspVersion },
    { id: "Microsoft.Extensions.Localization", version: aspVersion },
    { id: "Microsoft.Extensions.Logging.Abstractions", version: aspVersion },
    { id: "Microsoft.Extensions.Logging.Configuration", version: aspVersion },
    { id: "Microsoft.Extensions.Logging.Console", version: aspVersion },
    { id: "Microsoft.Extensions.Logging.Debug", version: aspVersion },
    { id: "Microsoft.Extensions.Logging.EventSource", version: aspVersion },
    { id: "Microsoft.Extensions.Logging", version: aspVersion },
    { id: "Microsoft.Extensions.ObjectPool", version: aspVersion },
    { id: "Microsoft.Extensions.Options.ConfigurationExtensions", version: aspVersion },
    { id: "Microsoft.Extensions.Options", version: aspVersion },
    { id: "Microsoft.Extensions.Primitives", version: aspVersion },
    { id: "Microsoft.Extensions.WebEncoders", version: aspVersion },

    { id: "Microsoft.Net.Http", version: "2.2.29" },
    { id: "Microsoft.Net.Http.Headers", version: aspVersion },
];
