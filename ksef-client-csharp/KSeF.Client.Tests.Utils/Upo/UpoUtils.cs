using System.Xml.Serialization;

namespace KSeF.Client.Tests.Utils.Upo;

public static class UpoUtils
{
    /// <summary>
    /// Deserializuje XML UPO do obiektu typu T, gdzie T implementuje IUpoParsable.
    /// </summary>
    /// <param name="xml">Pełna reprezentacja XML zgodna ze schematem UPO.</param>
    /// <returns>Wynik deserializacji.</returns>
    /// <exception cref="ArgumentNullException">Gdy parametr xml jest null.</exception>
    /// <exception cref="InvalidOperationException">Gdy deserializacja się nie powiedzie (np. niezgodność elementów/namespaces).</exception>
    public static T UpoParse<T>(string xml) where T : IUpoParsable
    {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);

        return (T)serializer.Deserialize(reader)!;
    }
}