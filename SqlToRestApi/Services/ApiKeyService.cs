using System;

namespace SqlToRestApi.Services
{
    public class ApiKey
    {
        public string User { get; set; }
        public string Key { get; set; }
    }

    public class ApiKeyService
    {
        public ApiKey ApiKey;

        /// <summary>
        /// Method to validate apikey against expiry and existence in database.
        /// </summary>
        /// <param name="apikey"></param>
        /// <returns></returns>
        public bool ValidateKey(string apikey)
        {
            if (!Config.AuthorizationRequired)
                return true;

            var user = ApiRepository.ValidateKey(apikey);

            if (string.IsNullOrEmpty(user)) return false;

            ApiKey = new ApiKey
            {
                User = user,
                Key = apikey
            };

            return true;
        }
    }
}