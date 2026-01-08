using System;
using Cysharp.Threading.Tasks;
using Infastructure.Common;
using Infastructure.Localization;
using Infastructure.StaticData.Windows;
using Localization;
using UnityEngine;

namespace GoogleImporter.Parsers
{
    public class WindowStaticLocalizationParser : IGoogleSheetParser
    {
        private readonly WindowsLocalizationStaticData _windowsLocalizationData =
            Resources.Load<WindowsLocalizationStaticData>(AssetsPath.WindowsLocalizationStaticDataPath);

        private TextStaticId _id;

        public async UniTask Parse(string header, string token)
        {
            switch (header)
            {
                case "ID":
                    if (!string.IsNullOrEmpty(token))
                        _id = Enum.Parse<TextStaticId>(token);
                    break;
                case "RU":
                    Translate(header, token);
                    break;
                case "EN":
                    Translate(header, token);
                    break;
            }
        }

        private void Translate(string header, string value)
        {
            LanguageId currentLanguage = Enum.Parse<LanguageId>(header);

            if (!_windowsLocalizationData.Texts.TryGetValue(_id, out var localizationText) || localizationText == null)
            {
                localizationText = new LocalizationText();
                _windowsLocalizationData.Texts[_id] = localizationText;
            }

            localizationText.Set(currentLanguage, value);

            /*if (!_windowsLocalizationData.Texts.ContainsKey(_id))
            {
                LocalizationText localizationText = new LocalizationText();
                localizationText.Set(currentLanguage, value);

                _windowsLocalizationData.Texts.Add(_id, localizationText);
            }
            else
            {
                LocalizationText localizationText = _windowsLocalizationData.Texts[_id];
                if (localizationText == null)
                    localizationText = new LocalizationText();
                else
                    localizationText.Set(currentLanguage, value);

                _windowsLocalizationData.Texts[_id] = localizationText;
            }*/
        }
    }
}