using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace CosmicRumble.Economy.IAP
{
    /// <summary>
    /// Unity IAP v5 (StoreController API) sarmalayıcısı. Gem'i gerçek parayla satın alınabilir
    /// kılar (consumable ürünler — her satın alma o anki bakiyeye eklenir, kalıcı bir "sahip olma"
    /// durumu değil).
    ///
    /// Placeholder ürün kataloğu (<see cref="GemPacks"/>): gerçek mağaza ürünleri Play Console /
    /// App Store Connect'te oluşturulana kadar bu id'ler gerçek bir SKU'ya bağlı değil — Editor'de
    /// ve mağaza bağlantısı olmayan build'lerde Unity IAP otomatik olarak FakeStore'a düşer, satın
    /// alma akışını test etmeyi sağlar. Gerçek fiyat her zaman mağazadan gelir
    /// (<see cref="GetLocalizedPrice"/>), burada hiçbir fiyat sabitlenmez.
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        public static readonly GemPackDefinition[] GemPacks =
        {
            new GemPackDefinition("gem_pack_100",  100,  "Small Gem Pack"),
            new GemPackDefinition("gem_pack_550",  550,  "Medium Gem Pack"),
            new GemPackDefinition("gem_pack_1200", 1200, "Large Gem Pack"),
            new GemPackDefinition("gem_pack_2500", 2500, "Mega Gem Pack"),
            new GemPackDefinition("gem_pack_6000", 6000, "Ultimate Gem Pack"),
        };

        /// <summary>Bir satın alma başarıyla tamamlandığında productId ile tetiklenir.</summary>
        public event Action<string> OnPurchaseSucceeded;
        /// <summary>Bir satın alma başarısız olduğunda (productId, sebep) ile tetiklenir.</summary>
        public event Action<string, string> OnPurchaseFailedEvent;

        private StoreController _storeController;

#if IAP_RECEIPT_VALIDATION
        private CrossPlatformValidator _validator;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if IAP_RECEIPT_VALIDATION
            try
            {
                _validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[IAPManager] CrossPlatformValidator init failed: {e.Message}");
#endif
                _validator = null;
            }
#endif

            _ = InitializePurchasingAsync();
        }

        /// <summary>
        /// Makbuz doğrulaması — güvenlik denetiminde bulunan "IAP satın almaları hiçbir makbuz
        /// doğrulaması olmadan Gem veriyor" açığının düzeltmesi. IAP_RECEIPT_VALIDATION define'ı
        /// aktif olana kadar (aşağıya bak) geçici olarak her zaman true döner — STEAMWORKS_INSTALLED/
        /// GPGS_INSTALLED ile aynı "kod hazır, gerçek anahtar bekliyor" deseni:
        ///   1. Play Console → Uygulamanız → Bütünlük → Lisanslama'daki Base64 RSA public key'i kopyala.
        ///   2. Unity Editor'de Window → Unity IAP → Receipt Validation Obfuscator'ı aç, public key'i
        ///      yapıştır, "Obfuscate Secrets" çalıştır — bu Assets altına GooglePlayTangle.cs (+ Apple
        ///      App Store Connect kullanılacaksa AppleTangle.cs) üretir.
        ///   3. Player Settings → Scripting Define Symbols'a IAP_RECEIPT_VALIDATION ekle.
        /// O ana kadar bu metod bilinçli olarak no-op (true) — aksi halde Tangle sınıfları
        /// üretilmeden IAP_RECEIPT_VALIDATION tanımlanırsa derleme hatası olurdu.
        /// Editor/FakeStore'da (mağaza bağlantısı yok) her zaman true döner — gerçek mağaza
        /// receipt formatı olmadığı için CrossPlatformValidator burada zaten StoreNotSupported
        /// atar, bu da Editor'deki normal IAP test akışını kırardı.
        /// </summary>
        private bool IsReceiptValid(PendingOrder order)
        {
#if IAP_RECEIPT_VALIDATION && !UNITY_EDITOR
            if (_validator == null) return true; // validator kurulamadıysa engelleme, sessizce geç
            try
            {
                var receipts = _validator.Validate(order.Info.Receipt);
                return receipts != null && receipts.Length > 0;
            }
            catch (IAPSecurityException)
            {
                return false;
            }
#else
            return true;
#endif
        }

        private async Task InitializePurchasingAsync()
        {
            try
            {
                _storeController = UnityIAPServices.StoreController();

                _storeController.OnPurchasePending    += OnPurchasePending;
                _storeController.OnPurchaseFailed     += OnPurchaseFailed;
                _storeController.OnProductsFetched    += OnProductsFetched;
                _storeController.OnProductsFetchFailed += (failed) =>
                {
#if UNITY_EDITOR
                    Debug.LogError($"[IAPManager] Product fetch failed: {failed.FailureReason}");
#endif
                };
                // Bir satın alma (ör. bazı Google Play ödeme yöntemleri) hemen tamamlanmayıp
                // "beklemede" kalabilir — ConfirmPurchase henüz çağrılmaz, Gem de verilmez;
                // gerçek tamamlanma daha sonra ayrı bir OnPurchasePending/OnPurchaseFailed ile gelir.
                _storeController.OnPurchaseDeferred += (deferred) =>
                {
#if UNITY_EDITOR
                    Debug.Log($"[IAPManager] Purchase deferred (awaiting external confirmation, e.g. parental approval).");
#endif
                };
                _storeController.OnStoreDisconnected += (reason) =>
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[IAPManager] Store disconnected: {reason}");
#endif
                };
#if UNITY_EDITOR
                _storeController.OnStoreConnected += () => Debug.Log("[IAPManager] Store connected.");
#endif

                await _storeController.Connect();

                var toFetch = new List<ProductDefinition>();
                foreach (var pack in GemPacks)
                    toFetch.Add(new ProductDefinition(pack.productId, ProductType.Consumable));

                _storeController.FetchProductsWithNoRetries(toFetch);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"[IAPManager] Init failed: {e}");
#endif
            }
        }

        /// <summary>Bir Gem paketinin satın alma akışını başlatır.</summary>
        public void BuyGemPack(string productId)
        {
            if (!IsInitialized || _storeController == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[IAPManager] Not initialized yet, cannot purchase.");
#endif
                return;
            }

            var product = _storeController.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[IAPManager] Product '{productId}' not available to purchase.");
#endif
                return;
            }

            _storeController.PurchaseProduct(productId);
        }

        /// <summary>Mağazadan gelen yerelleştirilmiş fiyat metni (örn. "$4.99"). Henüz hazır değilse "--".</summary>
        public string GetLocalizedPrice(string productId)
        {
            var product = _storeController?.GetProductById(productId);
            return product != null ? product.metadata.localizedPriceString : "--";
        }

        // ── Callbacks ─────────────────────────────────────────────────────

        private void OnProductsFetched(List<Product> products)
        {
            IsInitialized = true;
#if UNITY_EDITOR
            Debug.Log($"[IAPManager] Initialized OK, fetched {products.Count} product(s).");
#endif
        }

        private void OnPurchasePending(PendingOrder pendingOrder)
        {
            var items = pendingOrder.CartOrdered.Items();
            string productId = items.Count > 0 ? items[0].Product.definition.id : null;

            GemPackDefinition pack = productId != null
                ? Array.Find(GemPacks, p => p.productId == productId)
                : null;

            if (pack != null && IsReceiptValid(pendingOrder))
            {
                CurrencyManager.Instance?.Add(CurrencyType.Gem, pack.gemAmount);
                OnPurchaseSucceeded?.Invoke(productId);
#if UNITY_EDITOR
                Debug.Log($"[IAPManager] Purchased {productId} -> +{pack.gemAmount} Gem");
#endif
            }
            else if (pack != null)
            {
                // Makbuz doğrulaması başarısız — Gem verilmez. Yine de ConfirmPurchase çağrılır
                // (para mağazadan zaten tahsil edilmiş olabilir; onaylanmazsa sipariş sonsuza
                // kadar "pending" kalıp her açılışta tekrar denenir).
#if UNITY_EDITOR
                Debug.LogWarning($"[IAPManager] Receipt validation FAILED for '{productId}' — Gem NOT granted.");
#endif
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"[IAPManager] Purchase pending for unknown product '{productId}'.");
            }
#endif

            _storeController.ConfirmPurchase(pendingOrder);
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            var items = failedOrder.CartOrdered.Items();
            string productId = items.Count > 0 ? items[0].Product.definition.id : null;

            OnPurchaseFailedEvent?.Invoke(productId, failedOrder.FailureReason.ToString());
#if UNITY_EDITOR
            Debug.LogWarning($"[IAPManager] Purchase failed: {productId} - {failedOrder.FailureReason}: {failedOrder.Details}");
#endif
        }
    }
}
