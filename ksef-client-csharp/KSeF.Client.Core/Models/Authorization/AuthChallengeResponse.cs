using System;

namespace KSeF.Client.Core.Models.Authorization
{
    public class AuthChallengeResponse
    {
        /// <summary>
        /// Unikatowy ciąg znaków
        /// </summary>
        public string Challenge { get; set; }

        /// <summary>
        /// Czas wygenerowania wyzwania
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

    }
}
