using System;
using Cysharp.Threading.Tasks;
using Infastructure.Common;
using Infastructure.StaticData.Product;
using Localization;
using PickupObjects;
using UnityEngine;

namespace GoogleImporter.Parsers
{
    public class ProductDescriptionLocalizationParser : IGoogleSheetParser
    {
        private readonly ProductsStaticData _tasksStaticData =
            Resources.Load<ProductsStaticData>(AssetsPath.ProductStaticDataPath);

        private ProductType _productType;
        private string _currentTaskType;

        public async UniTask Parse(string header, string token)
        {
            switch (header)
            {
                case "ID":
                    if (!string.IsNullOrEmpty(token))
                        _productType = Enum.Parse<ProductType>(token);
                    break;
                case "RU":
                    Translate(header, token);
                    break;
                case "EN":
                    Translate(header, token);
                    break;
                case "TaskType":
                    _currentTaskType = token;
                    break;
            }
        }

        private void Translate(string header, string value)
        {
            LanguageId currentLanguage = Enum.Parse<LanguageId>(header);

            switch (_currentTaskType)
            {
                case "TitleText":
                    _tasksStaticData.ProductsDictionary[_productType].ProductDescription.TitleText
                        .Set(currentLanguage, value);
                    break;
                case "HowToUseText":
                    _tasksStaticData.ProductsDictionary[_productType].ProductDescription.HowToUseText
                        .Set(currentLanguage, value);
                    break;
                case "DescriptionText":
                    _tasksStaticData.ProductsDictionary[_productType].ProductDescription.DescriptionText
                        .Set(currentLanguage, value);
                    break;
            }
        }
    }
}