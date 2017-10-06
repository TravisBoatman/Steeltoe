﻿// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    [ServiceInfoFactory]
    public abstract class ServiceInfoFactory : IServiceInfoFactory
    {
        private static List<string> _userList = new List<string>() { "user", "username", "uid" };
        private static List<string> _passwordList = new List<string>() { "password", "pw" };
        private static List<string> _hostList = new List<string>() { "hostname", "host" };

        protected Tags _tags;
        protected string[] _schemes;

        protected List<string> uriKeys = new List<string> { "uri", "url" };

        public ServiceInfoFactory(Tags tags, string scheme)
            : this(tags, new string[] { scheme })
        {
            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentNullException(nameof(scheme));
            }
        }

        public ServiceInfoFactory(Tags tags, string[] schemes)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            _tags = tags;
            _schemes = schemes;
            if (schemes != null)
            {
               foreach (string s in schemes)
                {
                    uriKeys.Add(s + "Uri");
                    uriKeys.Add(s + "uri");
                    uriKeys.Add(s + "Url");
                    uriKeys.Add(s + "url");
                }
            }
        }

        public virtual string DefaultUriScheme
        {
            get
            {
                if (_schemes != null && _schemes.Length > 0)
                {
                    return _schemes[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public virtual bool Accept(Service binding)
        {
            return TagsMatch(binding) || LabelStartsWithTag(binding) ||
                 UriMatchesScheme(binding) || UriKeyMatchesScheme(binding);
        }

        public abstract IServiceInfo Create(Service binding);

        protected internal virtual bool TagsMatch(Service binding)
        {
            var serviceTags = binding.Tags;
            return _tags.ContainsOne(serviceTags);
        }

        protected internal virtual bool LabelStartsWithTag(Service binding)
        {
            string label = binding.Label;
            return _tags.StartsWith(label);
        }

        protected internal virtual bool UriMatchesScheme(Service binding)
        {
            if (_schemes == null)
            {
                return false;
            }

            var credentials = binding.Credentials;
            if (credentials == null)
            {
                return false;
            }

            string uri = GetStringFromCredentials(binding.Credentials, uriKeys);
            if (uri != null)
            {
                foreach (string uriScheme in _schemes)
                {
                    if (uri.StartsWith(uriScheme + "://"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected internal virtual bool UriKeyMatchesScheme(Service binding)
        {
            if (_schemes == null)
            {
                return false;
            }

            var credentials = binding.Credentials;
            if (credentials == null)
            {
                return false;
            }

            foreach (string uriScheme in _schemes)
            {
                if (credentials.ContainsKey(uriScheme + "Uri") || credentials.ContainsKey(uriScheme + "uri") ||
                        credentials.ContainsKey(uriScheme + "Url") || credentials.ContainsKey(uriScheme + "url"))
                {
                    return true;
                }
            }

            return false;
        }

        protected internal virtual string GetUsernameFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _userList);
        }

        protected internal virtual string GetPasswordFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _passwordList);
        }

        protected internal virtual int GetPortFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetIntFromCredentials(credentials, "port");
        }

        protected internal virtual string GetHostFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, _hostList);
        }

        protected internal virtual string GetUriFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, uriKeys);
        }

        protected internal virtual string GetClientIdFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "client_id");
        }

        protected internal virtual string GetClientSecretFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "client_secret");
        }

        protected internal virtual string GetAccessTokenUriFromCredentials(Dictionary<string, Credential> credentials)
        {
            return GetStringFromCredentials(credentials, "access_token_uri");
        }

        protected internal virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            return GetStringFromCredentials(credentials, new List<string>() { key });
        }

        protected internal virtual string GetStringFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
        {
            if (credentials != null)
            {
                foreach (string key in keys)
                {
                    if (credentials.ContainsKey(key))
                    {
                        return credentials[key].Value;
                    }
                }
            }

            return null;
        }

        protected internal virtual bool GetBoolFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            bool result = false;
            if (credentials != null)
            {
                if (credentials.ContainsKey(key))
                {
                    bool.TryParse(credentials[key].Value, out result);
                }
            }

            return result;
        }

        protected internal virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            return GetIntFromCredentials(credentials, new List<string>() { key });
        }

        protected internal virtual int GetIntFromCredentials(Dictionary<string, Credential> credentials, List<string> keys)
        {
            int result = 0;

            if (credentials != null)
            {
                foreach (string key in keys)
                {
                    if (credentials.ContainsKey(key))
                    {
                        result = int.Parse(credentials[key].Value);
                    }
                }
            }

            return result;
        }

        protected internal virtual List<string> GetListFromCredentials(Dictionary<string, Credential> credentials, string key)
        {
            List<string> result = new List<string>();
            if (credentials != null)
            {
               if (credentials.ContainsKey(key))
                {
                    Credential keyVal = credentials[key];
                    if (keyVal.Count > 0)
                    {
                        foreach (KeyValuePair<string, Credential> kvp in keyVal)
                        {
                            if (kvp.Value.Count != 0 || string.IsNullOrEmpty(kvp.Value.Value))
                            {
                                throw new ConnectorException(string.Format("Unable to extract list from credentials: key={0}, value={1}/{2}", key, kvp.Key, kvp.Value));
                            }
                            else
                            {
                                result.Add(kvp.Value.Value);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}