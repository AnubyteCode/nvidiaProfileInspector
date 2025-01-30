using System;
using nspector.Native.NVAPI2;

namespace nspector.Common.Import
{
    internal class ImportExportUitl
    {
        public static bool AreDrsSettingEqualToProfileSetting(NVDRS_SETTING drsSetting, ProfileSetting profileSetting)
        {
            var profileSettingCompare = ConvertDrsSettingToProfileSetting(drsSetting);
            return profileSetting.SettingValue.Equals(profileSettingCompare.SettingValue);
        }

        public static ProfileSetting ConvertDrsSettingToProfileSetting(NVDRS_SETTING setting)
        {
            return new ProfileSetting
            {
                SettingId = setting.settingId,
                SettingNameInfo = setting.settingName,
                SettingValue = ConvertSettingValueToString(setting),
                ValueType = MapValueType(setting.settingType),
            };
        }

        private static string ConvertSettingValueToString(NVDRS_SETTING setting)
        {
            var settingUnion = setting.currentValue;
            if (setting.isCurrentPredefined == 1)
            {
                settingUnion = setting.predefinedValue;
            }

            return setting.settingType switch
            {
                NVDRS_SETTING_TYPE.NVDRS_DWORD_TYPE => settingUnion.DwordValue.ToString(),
                NVDRS_SETTING_TYPE.NVDRS_STRING_TYPE => settingUnion.AnsiStringValue,
                NVDRS_SETTING_TYPE.NVDRS_WSTRING_TYPE => settingUnion.StringValue,
                NVDRS_SETTING_TYPE.NVDRS_BINARY_TYPE => Convert.ToBase64String(settingUnion.BinaryValue),
                _ => throw new Exception("invalid setting type"),
            };
        }

        private static SettingValueType MapValueType(NVDRS_SETTING_TYPE input)
        {
            return input switch
            {
                NVDRS_SETTING_TYPE.NVDRS_BINARY_TYPE => SettingValueType.Binary,
                NVDRS_SETTING_TYPE.NVDRS_STRING_TYPE => SettingValueType.AnsiString,
                NVDRS_SETTING_TYPE.NVDRS_WSTRING_TYPE => SettingValueType.String,
                _ => SettingValueType.Dword,
            };
        }

        public static NVDRS_SETTING ConvertProfileSettingToDrsSetting(ProfileSetting setting)
        {
            var newSetting = new NVDRS_SETTING()
            {
                version = NvapiDrsWrapper.NVDRS_SETTING_VER,
                settingId = setting.SettingId,
                settingType = MapValueType(setting.ValueType),
                settingLocation = NVDRS_SETTING_LOCATION.NVDRS_CURRENT_PROFILE_LOCATION,
                currentValue = ConvertStringToSettingUnion(setting.ValueType, setting.SettingValue),
            };
            return newSetting;
        }

        private static NVDRS_SETTING_UNION ConvertStringToSettingUnion(SettingValueType valueType, string valueString)
        {
            var union = new NVDRS_SETTING_UNION();
            switch (valueType)
            {
                case SettingValueType.Dword:
                    union.DwordValue = uint.Parse(valueString);
                    break;
                case SettingValueType.String:
                    union.StringValue = valueString;
                    break;
                case SettingValueType.AnsiString:
                    union.AnsiStringValue = valueString;
                    break;
                case SettingValueType.Binary:
                    union.BinaryValue = Convert.FromBase64String(valueString);
                    break;
                default:
                    throw new Exception("invalid value type");
            }
            return union;
        }

        private static NVDRS_SETTING_TYPE MapValueType(SettingValueType input)
        {
            return input switch
            {
                SettingValueType.Binary => NVDRS_SETTING_TYPE.NVDRS_BINARY_TYPE,
                SettingValueType.AnsiString => NVDRS_SETTING_TYPE.NVDRS_STRING_TYPE,
                SettingValueType.String => NVDRS_SETTING_TYPE.NVDRS_WSTRING_TYPE,
                _ => NVDRS_SETTING_TYPE.NVDRS_DWORD_TYPE,
            };
        }

    }
}
