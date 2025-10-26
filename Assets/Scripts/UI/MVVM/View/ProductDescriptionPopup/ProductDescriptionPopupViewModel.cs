using Infastructure.StaticData.Product;
using UI.MVVM.Base;

namespace UI.MVVM.View.ProductDescriptionPopup
{
    public class ProductDescriptionPopupViewModel : WindowViewModel
    {
        public override string Id => "ProductDescriptionPopup";

        public readonly ProductDescription Description;

        public ProductDescriptionPopupViewModel(ProductDescription description) =>
            Description = description;
    }
}