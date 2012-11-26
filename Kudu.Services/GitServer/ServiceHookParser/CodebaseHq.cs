﻿using System;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Kudu.Services.GitServer.ServiceHookParser
{
    public class CodebaseHq : IServiceHookParser
    {
        public bool TryGetRepositoryInfo(HttpRequest request, Lazy<string> body, out RepositoryInfo repositoryInfo)
        {
            repositoryInfo = null;
            JObject payload = JObject.Parse(request.Form["payload"]);

            var repository = payload.Value<JObject>("repository");
            if (repository == null)
            {
                return false;
            }

            var info = new RepositoryInfo();

            // CodebaseHq format, see http://support.codebasehq.com/kb/howtos/repository-push-commit-notifications
            info.IsPrivate = true;

            var urls = repository.Value<JObject>("clone_urls");
            info.RepositoryUrl = urls.Value<string>("ssh");

            // work around missing 'private' property, if missing assume is private.
            JToken priv;
            if (repository.TryGetValue("private", out priv))
            {
                info.IsPrivate = priv.ToObject<bool>();
            }
            else
            {
                info.IsPrivate = true;
            }

            // The format of ref is refs/something/something else
            // For master it's normally refs/head/master
            string @ref = payload.Value<string>("ref");

            if (String.IsNullOrEmpty(@ref))
            {
                return false;
            }

            info.Deployer = "CodebaseHQ";
            info.OldRef = payload.Value<string>("before");
            info.NewRef = payload.Value<string>("after");

            // private repo, use SSH
            if (info.IsPrivate)
            {
                Uri uri = new Uri(info.RepositoryUrl);
                if (uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    info.Host = "git@" + uri.Host;
                    info.RepositoryUrl = info.Host + ":" + uri.AbsolutePath.TrimStart('/');
                    info.UseSSH = true;
                }
            }

            repositoryInfo = info;
            return true;
        }
    }
}