using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    [Serializable]
    public class LocalizationText
    {
        public Dictionary<LanguageId, string> Values = new();

        public string Get(LanguageId lang, LanguageId fallback = LanguageId.EN)
        {
            if (Values.TryGetValue(lang, out var value) && !string.IsNullOrEmpty(value))
                return value;

            if (Values.TryGetValue(fallback, out var fb) && !string.IsNullOrEmpty(fb))
                return fb;

            return string.Empty;
        }

        public void Set(LanguageId lang, string text) =>
            Values[lang] = text ?? string.Empty;
    }
}