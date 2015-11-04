// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Tracing;
using Microsoft.Azure.Mobile.Server.Cache;
using Microsoft.Azure.Mobile.Server.Properties;
using Microsoft.Azure.Mobile.Server.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Mobile.Server.Config
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "We need to derive from this and override Initialize")]
    [AttributeUsage(AttributeTargets.Class)]
    public class MobileAppControllerAttribute : ActionFilterAttribute, IControllerConfiguration
    {
        internal const string VersionHeaderName = "x-zumo-server-version";
        internal const string VersionHeaderValuePrefix = "net-";
        internal const string ApiVersionName = "ZUMO-API-VERSION";
        // Eventually we may need to capture & compare: @"(\d)[.](\d)[.](\d) instead
        internal const string ApiVersionRegex = @"^2[.]0[.]\d+$";
        internal const string ForwardLinkURL = "http://go.microsoft.com/fwlink/?LinkId=690568#2.0.0";

        /// <inheritdoc />
        public virtual void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            if (controllerSettings == null)
            {
                throw new ArgumentNullException("controllerSettings");
            }

            JsonMediaTypeFormatter jsonFormatter = new JsonMediaTypeFormatter();
            JsonSerializerSettings serializerSettings = jsonFormatter.SerializerSettings;

            // Set up date/time format to be ISO 8601 but with 3 digits and "Z" as UTC time indicator. This format
            // is the JS-valid format accepted by most JS clients.
            IsoDateTimeConverter dateTimeConverter = new IsoDateTimeConverter()
            {
                Culture = CultureInfo.InvariantCulture,
                DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFZ",
                DateTimeStyles = DateTimeStyles.AdjustToUniversal
            };

            // Ignoring default values while serializing was affecting offline scenarios as client sdk looks at first object in a batch for the properties.
            // If first row in the server response did not include columns with default values, client sdk ignores these columns for the rest of the rows
            serializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
            serializerSettings.NullValueHandling = NullValueHandling.Include;
            serializerSettings.Converters.Add(new StringEnumConverter());
            serializerSettings.Converters.Add(dateTimeConverter);
            serializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
            serializerSettings.CheckAdditionalContent = true;
            serializerSettings.ContractResolver = new ServiceContractResolver(jsonFormatter);
            controllerSettings.Formatters.Remove(controllerSettings.Formatters.JsonFormatter);
            controllerSettings.Formatters.Insert(0, jsonFormatter);
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException("actionContext");
            }
            HttpRequestMessage request = actionContext.Request;
            if (request == null)
            {
                throw new ArgumentException("ActionContext.Request");
            }

            var settingsProvider = actionContext.ControllerContext.Configuration.GetMobileAppSettingsProvider();

            if (!settingsProvider.GetMobileAppSettings().SkipVersionCheck)
            {
                string version = request.GetQueryNameValuePairs()
                                            .Where(p => p.Key.Equals(ApiVersionName, StringComparison.OrdinalIgnoreCase))
                                            .Select(p => p.Value)
                                            .FirstOrDefault();

                if (version == null)
                {
                    version = request.GetHeaderOrDefault(ApiVersionName);
                }

                if (version != null)
                {
                    var pattern = new Regex(ApiVersionRegex);
                    if (!pattern.IsMatch(version))
                    {
                        actionContext.Response = request.CreateBadRequestResponse(RResources.Version_Unsupported.FormatForUser("2.0.0", ForwardLinkURL));
                        return;
                    }
                }
                else
                {
                    actionContext.Response = request.CreateBadRequestResponse(RResources.Version_Required.FormatForUser("2.0.0", ForwardLinkURL));
                    return;
                }
            }

            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext == null)
            {
                throw new ArgumentNullException("actionExecutedContext");
            }

            HttpConfiguration config = actionExecutedContext.ActionContext.ControllerContext.Configuration;
            ICachePolicyProvider provider = config.GetCachePolicyProvider();
            HttpRequestMessage request = actionExecutedContext.Request;
            HttpResponseMessage response = actionExecutedContext.Response;

            SetCachePolicy(provider, request, response, config.Services.GetTraceWriter());
            SetVersionHeader(response);
        }

        private static void SetCachePolicy(ICachePolicyProvider provider, HttpRequestMessage request, HttpResponseMessage response, ITraceWriter tracer)
        {
            if (provider != null && response != null && IsCacheableMethod(request.Method) && !HasCachingHeaders(response))
            {
                try
                {
                    provider.SetCachePolicy(response);
                }
                catch (Exception ex)
                {
                    string msg = RResources.CachePolicy_BadProvider.FormatForUser(provider.GetType().Name, ex.Message);
                    tracer.Error(msg, ex, request, LogCategories.MessageHandlers);
                }
            }
        }

        internal static void SetVersionHeader(HttpResponseMessage response)
        {
            if (response != null)
            {
                string version = AssemblyUtils.AssemblyFileVersion;
                response.Headers.Add(VersionHeaderName, VersionHeaderValuePrefix + version);
            }
        }

        internal static bool IsCacheableMethod(HttpMethod method)
        {
            return method != null && (method == HttpMethod.Get || method == HttpMethod.Head);
        }

        /// <summary>
        /// Determines whether caching headers have already been applied. We look for "Cache-Control" and "Expires".
        /// We don't look for just "Pragma" as this is not a reliable header for control caching.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <returns><c>true</c> if either "Cache-Control" or "Expires" has been set.</returns>
        internal static bool HasCachingHeaders(HttpResponseMessage response)
        {
            IEnumerable<string> expires;
            return response != null
                && (response.Headers.CacheControl != null
                || (response.Content != null && response.Content.Headers.TryGetValues("Expires", out expires)));
        }
    }
}