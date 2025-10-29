using System.Runtime.Serialization;

namespace KSeF.Client.Core.Models.Invoices
{
    public enum SystemCodeEnum
    {
        [EnumMember(Value = "FA (2)")] FA2,
        [EnumMember(Value = "FA (3)")] FA3,
        [EnumMember(Value = "FA_PEF (3)")] FAPEF,
        [EnumMember(Value = "FA_KOR_PEF (3)")] FAKORPEF
    }

    public static class SystemCodeHelper
    {
        public static string GetSystemCode(SystemCodeEnum code)
        {
            switch (code)
            {
                case SystemCodeEnum.FA2:
                    return "FA (2)";
                case SystemCodeEnum.FA3:
                    return "FA (3)";
                case SystemCodeEnum.FAPEF:
                    return "FA_PEF (3)";
                case SystemCodeEnum.FAKORPEF:
                    return "FA_KOR_PEF (3)";
                default:
                    return code.ToString();
            }
        }

        public static string GetValue(SystemCodeEnum code)
        {
            switch (code)
            {
                case SystemCodeEnum.FA2:
                    return "FA";
                case SystemCodeEnum.FA3:
                    return "FA";
                case SystemCodeEnum.FAPEF:
                    return "FA_PEF";
                case SystemCodeEnum.FAKORPEF:
                    return "FA_PEF";
                default:
                    return code.ToString();
            }
        }

        public static string GetSchemaVersion(SystemCodeEnum code)
        {
            switch (code)
            {
                case SystemCodeEnum.FA2:
                    return "1-0E";
                case SystemCodeEnum.FA3:
                    return "1-0E";
                case SystemCodeEnum.FAPEF:
                    return "2-1";
                case SystemCodeEnum.FAKORPEF:
                    return "2-1";
                default:
                    return code.ToString();
            }
        }
    }
}