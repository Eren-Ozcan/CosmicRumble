using System;

namespace CosmicRumble.Economy.IAP
{
    /// <summary>
    /// Bir Gem paketi tanımı. Gerçek fiyat mağazadan gelir (Product.metadata.localizedPriceString) —
    /// burada sadece ürün id'si ve verilecek Gem miktarı sabitlenir.
    ///
    /// Placeholder: productId'ler Play Console / App Store Connect'te henüz oluşturulmadı. Gerçek
    /// mağaza kurulumu yapılırken bu id'lerle BİREBİR aynı ürün ID'leri tanımlanmalı, aksi halde
    /// IAPManager ürünleri bulamaz.
    /// </summary>
    [Serializable]
    public class GemPackDefinition
    {
        public string productId;
        public int gemAmount;
        public string displayName;

        public GemPackDefinition(string productId, int gemAmount, string displayName)
        {
            this.productId = productId;
            this.gemAmount = gemAmount;
            this.displayName = displayName;
        }
    }
}
