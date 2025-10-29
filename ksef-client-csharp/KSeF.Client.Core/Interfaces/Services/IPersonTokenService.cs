using KSeF.Client.Core.Models.Token;

namespace KSeF.Client.Core.Interfaces.Services
{
    public interface IPersonTokenService
    {
        /// <summary>
        /// Mapuje token JWT na obiekt PersonToken.
        /// </summary>
        /// <param name="jwt">Json Web Token</param>
        /// <returns><see cref="PersonToken"/></returns>
        PersonToken MapFromJwt(string jwt);
    }
}
