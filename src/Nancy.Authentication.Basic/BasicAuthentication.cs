﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy.Bootstrapper;
using Nancy.Security;

namespace Nancy.Authentication.Basic
{
    /// <summary>
    /// Nancy basic authentication implementation
    /// </summary>
    public static class BasicAuthentication
    {
        private const string SCHEME = "Basic";

        /// <summary>
        /// Enables basic authentication for the application
        /// </summary>
        /// <param name="applicationPipelines">Pipelines to add handlers to (usually "this")</param>
        /// <param name="configuration">Forms authentication configuration</param>
        public static void Enable(IApplicationPipelines applicationPipelines, BasicAuthenticationConfiguration configuration)
        {
            if (applicationPipelines == null)
            {
                throw new ArgumentNullException("applicationPipelines");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            applicationPipelines.BeforeRequest.AddItemToStartOfPipeline(GetCredentialRetrievalHook(configuration));
            applicationPipelines.AfterRequest.AddItemToEndOfPipeline(GetAuthenticationPromptHook(configuration));
        }

        /// <summary>
        /// Enables basic authentication for a module
        /// </summary>
        /// <param name="module">Module to add handlers to (usually "this")</param>
        /// <param name="configuration">Forms authentication configuration</param>
        public static void Enable(NancyModule module, BasicAuthenticationConfiguration configuration)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            module.RequiresAuthentication();
            module.Before.AddItemToStartOfPipeline(GetCredentialRetrievalHook(configuration));
            module.After.AddItemToEndOfPipeline(GetAuthenticationPromptHook(configuration));
        }

        /// <summary>
        /// Gets the pre request hook for loading the authenticated user's details
        /// from the auth header.
        /// </summary>
        /// <param name="configuration">Basic authentication configuration to use</param>
        /// <returns>Pre request hook delegate</returns>
        private static Func<NancyContext, Response> GetCredentialRetrievalHook(BasicAuthenticationConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return context =>
                {
                    RetrieveCredentials(context, configuration);
                    return null;
                };
        }

        private static Action<NancyContext> GetAuthenticationPromptHook(BasicAuthenticationConfiguration configuration)
        {
            return context =>
                {
                    if (context.Response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        context.Response.Headers["WWW-Authenticate"] = String.Format("{0} realm=\"{1}\"", SCHEME, configuration.Realm);
                    }
                };
        }

        private static void RetrieveCredentials(NancyContext context, BasicAuthenticationConfiguration configuration)
        {
            var credentials = ExtractCredentialsFromHeaders(context.Request);

            if (credentials != null && credentials.Length == 2)
            {
                if (configuration.UserValidator.Validate(credentials[0], credentials[1]))
                {
                    context.Items[SecurityConventions.AuthenticatedUsernameKey] = credentials[0];
                }
            }
        }

        private static string[] ExtractCredentialsFromHeaders(Request request)
        {
            IEnumerable<string> values;

            if (!request.Headers.TryGetValue("Authorization", out values))
            {
                return null;
            }

            var authorization = values.FirstOrDefault();

            if (authorization == null || !authorization.StartsWith(SCHEME))
            {
                return null;
            }

            try
            {
                var encodedUserPass = authorization.Substring(SCHEME.Length).Trim();
                var userPass = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUserPass));

                return String.IsNullOrWhiteSpace(userPass) ? null : userPass.Split(':');
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
