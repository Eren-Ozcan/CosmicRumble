using UnityEngine;
using CosmicRumble.Tutorial;

namespace CosmicRumble.Social
{
    /// <summary>
    /// OGRETMEN başarımının ("Guide a new player through the tutorial") iki cihaza yayılan
    /// defteri.
    ///
    /// <para><b>Neden ayrı bir parça:</b> başarım MENTORUN cihazında açılır ama koşulu ÖĞRENCİNİN
    /// cihazında gerçekleşir (eğitim orada tamamlanır). Aradaki tek köprü Friends servisinin
    /// mesajlaşması. Bu sınıf öğrenci tarafında "beni kim davet etti" bilgisini tutar, eğitim
    /// bitince mentora bir kredi mesajı yollar ve mentor o mesajı alınca başarım açılır.</para>
    ///
    /// <para><b>Kim "yeni oyuncu" sayılır:</b> daveti kabul ettiği anda eğitimi HENÜZ görmemiş
    /// olan. Zaten oynamış birini davet etmek mentorluk değildir, o yüzden bağ yalnızca o anda
    /// kurulur ve sonradan değişmez (ilk davet eden mentor kalır).</para>
    ///
    /// <para><b>Mentor çevrimdışıysa:</b> MessageAsync yalnızca online kullanıcıya ulaşır, o
    /// yüzden kredi teslim edilene kadar PlayerPrefs'te bekler ve her açılışta yeniden denenir.
    /// Teslim edilince bir daha gönderilmez.</para>
    /// </summary>
    public static class MentorLink
    {
        const string MentorIdKey   = "cr_mentor_id";
        const string CreditSentKey = "cr_mentor_credit_sent";

        /// <summary>Bu oyuncuyu davet eden mentorun PlayerId'si — yoksa null.</summary>
        public static string MentorId => PlayerPrefs.GetString(MentorIdKey, null);

        static bool CreditDelivered => PlayerPrefs.GetInt(CreditSentKey, 0) == 1;

        /// <summary>
        /// Bir maç daveti kabul edilirken çağrılır. Bağ YALNIZCA davet edilen kişi eğitimi
        /// henüz görmemişse ve daha önce bir mentoru yoksa kurulur.
        /// </summary>
        public static void RecordMentor(string mentorPlayerId)
        {
            if (string.IsNullOrEmpty(mentorPlayerId)) return;
            if (TutorialManager.HasSeenTutorial) return;      // yeni oyuncu değil
            if (!string.IsNullOrEmpty(MentorId))   return;    // ilk davet eden mentor kalır

            PlayerPrefs.SetString(MentorIdKey, mentorPlayerId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Krediyi mentora yollamayı dener. Eğitim tamamlandığında ve her açılışta (mentor o an
        /// çevrimdışıysa gönderim başarısız olur, sonraki açılışta tekrar denenir) çağrılır.
        /// </summary>
        public static async void TryDeliverCredit()
        {
            if (CreditDelivered) return;
            if (!TutorialManager.HasSeenTutorial) return;

            string mentor = MentorId;
            if (string.IsNullOrEmpty(mentor)) return;

            var friends = FriendsManager.Instance;
            if (friends == null || !friends.IsAvailable) return;

            bool sent = await friends.SendMentorCreditAsync(mentor);
            if (!sent) return;

            PlayerPrefs.SetInt(CreditSentKey, 1);
            PlayerPrefs.Save();
        }
    }
}
