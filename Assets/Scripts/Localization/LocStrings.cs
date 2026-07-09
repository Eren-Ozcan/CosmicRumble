using System.Collections.Generic;

namespace CosmicRumble.Localization
{
    /// <summary>
    /// Çeviri tablosu. Anahtar = İngilizce kaynak metin (aynı zamanda İngilizce'nin kendisi).
    /// Dizi sırası SABİT ve <see cref="Language"/> enum sırasıyla eşleşir (English hariç):
    /// [0]=Turkish, [1]=ChineseSimplified, [2]=Spanish, [3]=Japanese, [4]=Korean, [5]=German.
    /// Bir dilde çeviri yoksa "" bırak — Loc.T otomatik İngilizce'ye düşer.
    /// Yeni bir UI stringi eklerken: kod tarafında Loc.T("English text") çağır, buraya bir satır ekle.
    /// </summary>
    public static class LocStrings
    {
        public static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // ── Boot / Loading ──────────────────────────────────────────────
            ["Connecting..."]         = new[] { "Bağlanılıyor...", "连接中...", "Conectando...", "接続中...", "연결 중...", "Verbindung wird hergestellt..." },
            ["Fetching cloud save..."] = new[] { "Bulut kaydı alınıyor...", "正在获取云存档...", "Obteniendo guardado en la nube...", "クラウドセーブを取得中...", "클라우드 저장 데이터 가져오는 중...", "Cloud-Speicherstand wird abgerufen..." },
            ["Loading profile..."]    = new[] { "Profil yükleniyor...", "正在加载个人资料...", "Cargando perfil...", "プロフィールを読み込み中...", "프로필 로딩 중...", "Profil wird geladen..." },

            // ── Main menu ────────────────────────────────────────────────────
            ["Turn-based planetary warfare"] = new[] { "Sıra tabanlı gezegenler arası savaş", "回合制星球战争", "Guerra planetaria por turnos", "ターン制惑星戦争", "턴제 행성 전쟁", "Rundenbasierter Planetenkrieg" },
            ["T"] = new[] { "K", "杯", "T", "T", "T", "T" }, // kupa rozeti tek harf
            ["WARDROBE"]        = new[] { "GARDIROP", "衣橱", "ARMARIO", "ワードローブ", "옷장", "GARDEROBE" },
            ["SHOP"]             = new[] { "MARKET", "商店", "TIENDA", "ショップ", "상점", "SHOP" },
            ["SOCIAL"]           = new[] { "SOSYAL", "社交", "SOCIAL", "ソーシャル", "소셜", "SOZIAL" },
            ["QUESTS"]           = new[] { "GÖREVLER", "任务", "MISIONES", "クエスト", "퀘스트", "QUESTS" },
            ["SETTINGS"]         = new[] { "AYARLAR", "设置", "AJUSTES", "設定", "설정", "EINSTELLUNGEN" },
            ["LEADERBOARD"]      = new[] { "SIRALAMA", "排行榜", "CLASIFICACIÓN", "ランキング", "리더보드", "RANGLISTE" },
            ["ACHIEVEMENTS"]     = new[] { "BAŞARIMLAR", "成就", "LOGROS", "実績", "업적", "ERFOLGE" },
            ["ACCOUNT"]          = new[] { "HESAP", "账户", "CUENTA", "アカウント", "계정", "KONTO" },
            ["BOT MATCH (DEV)"]  = new[] { "BOT MAÇI (DEV)", "机器人对战（开发）", "PARTIDA BOT (DEV)", "BOT対戦（開発用）", "봇 대전 (개발용)", "BOT-MATCH (DEV)" },
            ["QUICK MATCH"]      = new[] { "HIZLI EŞLEŞME", "快速匹配", "PARTIDA RÁPIDA", "クイックマッチ", "빠른 대전", "SCHNELLES SPIEL" },
            ["Ranked  •  Win +30 Trophies"] = new[] { "Dereceli  •  Galibiyet +30 kupa", "排位  •  胜利 +30 奖杯", "Clasificatoria  •  Victoria +30 trofeos", "ランク戦  •  勝利で+30トロフィー", "랭크전  •  승리 시 +30 트로피", "Gewertet  •  Sieg +30 Pokale" },
            ["PLAY"]             = new[] { "OYNA", "开始", "JUGAR", "プレイ", "플레이", "SPIELEN" },
            ["Quick Match  •  Ranked"] = new[] { "Hızlı Eşleşme  •  Dereceli", "快速匹配  •  排位", "Partida rápida  •  Clasificatoria", "クイックマッチ  •  ランク戦", "빠른 대전  •  랭크전", "Schnelles Spiel  •  Gewertet" },
            ["AUDIO"]            = new[] { "SES", "音频", "AUDIO", "サウンド", "오디오", "AUDIO" },
            ["GRAPHICS"]         = new[] { "GRAFİK", "画面", "GRÁFICOS", "グラフィック", "그래픽", "GRAFIK" },
            ["CONTROLS"]         = new[] { "KONTROLLER", "操作", "CONTROLES", "操作設定", "조작", "STEUERUNG" },
            ["BACK"]             = new[] { "GERİ", "返回", "ATRÁS", "戻る", "뒤로", "ZURÜCK" },
            ["Master Volume"]    = new[] { "Ana Ses", "总音量", "Volumen General", "マスター音量", "전체 음량", "Gesamtlautstärke" },
            ["Music"]            = new[] { "Müzik", "音乐", "Música", "音楽", "음악", "Musik" },
            ["Effects"]          = new[] { "Efektler", "音效", "Efectos", "効果音", "효과음", "Effekte" },
            ["Fullscreen"]       = new[] { "Tam Ekran", "全屏", "Pantalla Completa", "フルスクリーン", "전체 화면", "Vollbild" },
            ["Resolution"]       = new[] { "Çözünürlük", "分辨率", "Resolución", "解像度", "해상도", "Auflösung" },
            ["Quality"]          = new[] { "Kalite", "画质", "Calidad", "画質", "품질", "Qualität" },
            ["APPLY"]            = new[] { "UYGULA", "应用", "APLICAR", "適用", "적용", "ÜBERNEHMEN" },
            ["Move Left"]        = new[] { "Sola Git", "向左移动", "Mover Izquierda", "左移動", "왼쪽 이동", "Nach Links" },
            ["Move Right"]       = new[] { "Sağa Git", "向右移动", "Mover Derecha", "右移動", "오른쪽 이동", "Nach Rechts" },
            ["Jump"]             = new[] { "Zıpla", "跳跃", "Saltar", "ジャンプ", "점프", "Springen" },
            ["Tap a button, then press a new key (Esc to cancel)"] = new[] { "Bir butona dokun, sonra yeni tuşa bas (iptal için Esc)", "点击一个按钮，然后按下新按键（按Esc取消）", "Toca un botón y luego pulsa una nueva tecla (Esc para cancelar)", "ボタンをタップしてから新しいキーを押してください（Escでキャンセル）", "버튼을 누른 후 새 키를 입력하세요 (취소하려면 Esc)", "Tippe auf eine Schaltfläche und drücke dann eine neue Taste (Esc zum Abbrechen)" },
            ["LOG OUT"]          = new[] { "ÇIKIŞ YAP", "退出登录", "CERRAR SESIÓN", "ログアウト", "로그아웃", "ABMELDEN" },
            ["Press a key…"]     = new[] { "Bir tuşa bas…", "请按键…", "Pulsa una tecla…", "キーを押してください…", "키를 누르세요…", "Taste drücken…" },
            ["Account: {0}"]     = new[] { "Hesap: {0}", "账户：{0}", "Cuenta: {0}", "アカウント: {0}", "계정: {0}", "Konto: {0}" },
            ["{0} — no account linked.\nLink an account to protect your progress."] = new[] {
                "{0} — hesap bağlı değil.\nİlerlemeni korumak için hesabını bağla.",
                "{0} — 未绑定账户。\n绑定账户以保护你的游戏进度。",
                "{0} — sin cuenta vinculada.\nVincula una cuenta para proteger tu progreso.",
                "{0} — アカウント未連携。\n進行状況を保護するにはアカウントを連携してください。",
                "{0} — 연결된 계정 없음.\n진행 상황을 보호하려면 계정을 연결하세요.",
                "{0} — kein Konto verknüpft.\nVerknüpfe ein Konto, um deinen Fortschritt zu schützen." },
            ["Linked ({0})"]     = new[] { "Bağlı ({0})", "已绑定（{0}）", "Vinculado ({0})", "連携済み（{0}）", "연결됨 ({0})", "Verknüpft ({0})" },
            ["Not linked"]       = new[] { "Bağlı değil", "未绑定", "No vinculado", "未連携", "연결 안 됨", "Nicht verknüpft" },
            ["Connection failed."] = new[] { "Bağlanamadı.", "连接失败。", "Conexión fallida.", "接続に失敗しました。", "연결 실패.", "Verbindung fehlgeschlagen." },
            ["LINK"]             = new[] { "BAĞLA", "绑定", "VINCULAR", "連携", "연결", "VERKNÜPFEN" },

            // ── Wardrobe ─────────────────────────────────────────────────────
            ["CHARACTER"]        = new[] { "KARAKTER", "角色", "PERSONAJE", "キャラクター", "캐릭터", "CHARAKTER" },
            ["WEAPON"]           = new[] { "SİLAH", "武器", "ARMA", "武器", "무기", "WAFFE" },
            ["Wardrobe is currently unavailable."] = new[] { "Gardırop şu anda kullanılamıyor.", "衣橱当前不可用。", "El armario no está disponible en este momento.", "現在ワードローブは利用できません。", "현재 옷장을 사용할 수 없습니다.", "Die Garderobe ist derzeit nicht verfügbar." },
            ["Owned: {0} / {1}"] = new[] { "Sahip olunan: {0} / {1}", "已拥有：{0} / {1}", "En posesión: {0} / {1}", "所持数: {0} / {1}", "보유: {0} / {1}", "Im Besitz: {0} / {1}" },
            ["You don't have any costumes in this category yet.\nEarn costumes from chests, achievements, and leveling up."] = new[] {
                "Bu kategoride henüz kostümün yok.\nSandıklardan, başarımlardan ve seviye atlayarak kostüm kazanabilirsin.",
                "你在此分类中还没有任何装扮。\n可通过宝箱、成就和升级获得装扮。",
                "Aún no tienes disfraces en esta categoría.\nConsigue disfraces con cofres, logros y subiendo de nivel.",
                "このカテゴリのコスチュームはまだありません。\nチェスト、実績、レベルアップでコスチュームを獲得できます。",
                "이 카테고리에는 아직 코스튬이 없습니다.\n상자, 업적, 레벨업으로 코스튬을 획득하세요.",
                "Du hast in dieser Kategorie noch keine Kostüme.\nErhalte Kostüme durch Truhen, Erfolge und Level-Aufstiege." },
            ["EQUIPPED"]         = new[] { "KUŞANILDI", "已装备", "EQUIPADO", "装備中", "장착됨", "AUSGERÜSTET" },
            ["EQUIP"]            = new[] { "KUŞAN", "装备", "EQUIPAR", "装備する", "장착", "AUSRÜSTEN" },
            ["UNCOMMON"]         = new[] { "SIRA DIŞI", "罕见", "POCO COMÚN", "アンコモン", "언커먼", "UNGEWÖHNLICH" },
            ["RARE"]             = new[] { "NADİR", "稀有", "RARO", "レア", "레어", "SELTEN" },
            ["EPIC"]             = new[] { "DESTANSI", "史诗", "ÉPICO", "エピック", "에픽", "EPISCH" },
            ["LEGENDARY"]        = new[] { "EFSANEVİ", "传说", "LEGENDARIO", "レジェンダリー", "레전더리", "LEGENDÄR" },
            ["COMMON"]           = new[] { "SIRADAN", "普通", "COMÚN", "コモン", "커먼", "GEWÖHNLICH" },

            // ── Quests ───────────────────────────────────────────────────────
            ["DAILY"]            = new[] { "GÜNLÜK", "每日", "DIARIO", "デイリー", "일일", "TÄGLICH" },
            ["WEEKLY"]           = new[] { "HAFTALIK", "每周", "SEMANAL", "ウィークリー", "주간", "WÖCHENTLICH" },
            ["MONTHLY"]          = new[] { "AYLIK", "每月", "MENSUAL", "マンスリー", "월간", "MONATLICH" },
            ["The quest system is currently unavailable."] = new[] { "Görev sistemi şu anda kullanılamıyor.", "任务系统当前不可用。", "El sistema de misiones no está disponible en este momento.", "現在クエストシステムは利用できません。", "현재 퀘스트 시스템을 사용할 수 없습니다.", "Das Questsystem ist derzeit nicht verfügbar." },
            ["Resets in {0}"]    = new[] { "Sıfırlanmasına {0} kaldı", "{0}后重置", "Se reinicia en {0}", "{0}後にリセット", "{0} 후 초기화", "Zurücksetzung in {0}" },
            ["No active quests right now."] = new[] { "Şu anda aktif görev yok.", "当前没有进行中的任务。", "No hay misiones activas en este momento.", "現在アクティブなクエストはありません。", "현재 진행 중인 퀘스트가 없습니다.", "Momentan keine aktiven Quests." },
            ["DONE"]             = new[] { "TAMAM", "已完成", "HECHO", "完了", "완료", "FERTIG" },
            ["{0}d {1}h"]        = new[] { "{0}g {1}s", "{0}天{1}小时", "{0}d {1}h", "{0}日{1}時間", "{0}일 {1}시간", "{0}T {1}Std" },
            ["{0}h {1}m"]        = new[] { "{0}s {1}dk", "{0}小时{1}分钟", "{0}h {1}m", "{0}時間{1}分", "{0}시간 {1}분", "{0}Std {1}Min" },

            // ── Achievements ─────────────────────────────────────────────────
            ["No achievements defined yet."] = new[] { "Henüz başarım tanımlanmadı.", "尚未定义任何成就。", "Aún no hay logros definidos.", "実績はまだ定義されていません。", "아직 정의된 업적이 없습니다.", "Noch keine Erfolge definiert." },
            ["{0} / {1} completed"] = new[] { "{0} / {1} tamamlandı", "{0} / {1} 已完成", "{0} / {1} completados", "{0} / {1} 達成済み", "{0} / {1} 완료", "{0} / {1} abgeschlossen" },
            ["Secret achievement"] = new[] { "Gizli başarım", "隐藏成就", "Logro secreto", "シークレット実績", "비밀 업적", "Geheimer Erfolg" },
            ["UNLOCKED"]         = new[] { "AÇILDI", "已解锁", "DESBLOQUEADO", "解除済み", "달성함", "FREIGESCHALTET" },
            ["Locked"]           = new[] { "Kilitli", "未解锁", "Bloqueado", "未解除", "잠김", "Gesperrt" },

            // ── Shop ─────────────────────────────────────────────────────────
            ["Gem packs — for costumes and chests"] = new[] { "Gem paketleri — kostüm ve sandıklar için", "宝石礼包 — 用于装扮和宝箱", "Paquetes de gemas — para disfraces y cofres", "ジェムパック — コスチュームやチェストに", "젬 패키지 — 코스튬과 상자를 위해", "Edelstein-Pakete — für Kostüme und Truhen" },
            ["POPULAR"]          = new[] { "POPÜLER", "热门", "POPULAR", "人気", "인기", "BELIEBT" },
            ["BEST VALUE"]       = new[] { "EN İYİ DEĞER", "超值", "MEJOR VALOR", "お得", "최고 가치", "BESTER WERT" },
            ["BUY"]              = new[] { "SATIN AL", "购买", "COMPRAR", "購入", "구매", "KAUFEN" },

            // ── Leaderboard ──────────────────────────────────────────────────
            ["REFRESH"]          = new[] { "YENİLE", "刷新", "ACTUALIZAR", "更新", "새로고침", "AKTUALISIEREN" },
            ["Loading..."]       = new[] { "Yükleniyor...", "加载中...", "Cargando...", "読み込み中...", "로딩 중...", "Wird geladen..." },
            ["Can't reach online services — check your connection."] = new[] { "Online servislere ulaşılamıyor — bağlantını kontrol et.", "无法连接在线服务 — 请检查你的网络连接。", "No se puede acceder a los servicios en línea — comprueba tu conexión.", "オンラインサービスに接続できません — 接続を確認してください。", "온라인 서비스에 연결할 수 없습니다 — 연결 상태를 확인하세요.", "Online-Dienste nicht erreichbar — überprüfe deine Verbindung." },
            ["No trophies yet — play an online match to show up here!"] = new[] { "Henüz kupa yok — burada görünmek için bir online maç oyna!", "还没有奖杯 — 打一场在线对战即可上榜！", "Aún no hay trofeos — ¡juega una partida en línea para aparecer aquí!", "まだトロフィーがありません — オンライン対戦をプレイしてここに表示させよう！", "아직 트로피가 없습니다 — 온라인 대전을 플레이하면 여기에 표시됩니다!", "Noch keine Pokale — spiele ein Online-Match, um hier zu erscheinen!" },
            ["Your rank: #{0}   •   {1} trophies   •   {2}"] = new[] { "Sıralaman: #{0}   •   {1} kupa   •   {2}", "你的排名：#{0}   •   {1} 奖杯   •   {2}", "Tu posición: #{0}   •   {1} trofeos   •   {2}", "あなたの順位: #{0}   •   {1}トロフィー   •   {2}", "내 순위: #{0}   •   {1} 트로피   •   {2}", "Dein Rang: #{0}   •   {1} Pokale   •   {2}" },
            ["No ranked trophies yet   •   Local: {0} trophies"] = new[] { "Henüz dereceli kupan yok   •   Yerel: {0} kupa", "还没有排位奖杯   •   本地：{0} 奖杯", "Aún sin trofeos clasificatorios   •   Local: {0} trofeos", "ランク戦のトロフィーはまだありません   •   ローカル: {0}トロフィー", "아직 랭크 트로피가 없습니다   •   로컬: {0} 트로피", "Noch keine gewerteten Pokale   •   Lokal: {0} Pokale" },
            ["Player"]           = new[] { "Oyuncu", "玩家", "Jugador", "プレイヤー", "플레이어", "Spieler" },

            // ── Social ───────────────────────────────────────────────────────
            ["YOUR ID: ..."]     = new[] { "SENİN ID'N: ...", "你的ID：...", "TU ID: ...", "あなたのID: ...", "내 ID: ...", "DEINE ID: ..." },
            ["YOUR ID: —"]       = new[] { "SENİN ID'N: —", "你的ID：—", "TU ID: —", "あなたのID: —", "내 ID: —", "DEINE ID: —" },
            ["YOUR ID: {0}"]     = new[] { "SENİN ID'N: {0}", "你的ID：{0}", "TU ID: {0}", "あなたのID: {0}", "내 ID: {0}", "DEINE ID: {0}" },
            ["Copied!"]          = new[] { "Kopyalandı!", "已复制！", "¡Copiado!", "コピーしました！", "복사됨!", "Kopiert!" },
            ["Sending..."]       = new[] { "Gönderiliyor...", "发送中...", "Enviando...", "送信中...", "전송 중...", "Wird gesendet..." },
            ["Request sent!"]    = new[] { "İstek gönderildi!", "请求已发送！", "¡Solicitud enviada!", "リクエストを送信しました！", "요청을 보냈습니다!", "Anfrage gesendet!" },
            ["Friends system is currently unavailable."] = new[] { "Arkadaş sistemi şu an kullanılamıyor.", "好友系统当前不可用。", "El sistema de amigos no está disponible en este momento.", "現在フレンドシステムは利用できません。", "현재 친구 시스템을 사용할 수 없습니다.", "Das Freundesystem ist derzeit nicht verfügbar." },
            ["No pending requests."] = new[] { "Bekleyen davet yok.", "没有待处理的请求。", "No hay solicitudes pendientes.", "保留中のリクエストはありません。", "대기 중인 요청이 없습니다.", "Keine ausstehenden Anfragen." },
            ["You don't have any friends yet — add one by ID!"] = new[] { "Henüz arkadaşın yok — ID ile ekle!", "你还没有好友 — 通过ID添加一个吧！", "Aún no tienes amigos — ¡añade uno por ID!", "まだフレンドがいません — IDで追加しよう！", "아직 친구가 없습니다 — ID로 친구를 추가하세요!", "Du hast noch keine Freunde — füge einen per ID hinzu!" },
            ["Wants to add you as a friend"] = new[] { "Seni arkadaş olarak eklemek istiyor", "想添加你为好友", "Quiere añadirte como amigo", "あなたをフレンドに追加したがっています", "친구로 추가하고 싶어합니다", "Möchte dich als Freund hinzufügen" },
            ["In Match"]         = new[] { "Maçta", "对战中", "En partida", "対戦中", "대전 중", "Im Match" },
            ["Online"]           = new[] { "Çevrimiçi", "在线", "En línea", "オンライン", "온라인", "Online" },
            ["Away"]             = new[] { "Uzakta", "离开", "Ausente", "離席中", "자리 비움", "Abwesend" },
            ["Offline"]          = new[] { "Çevrimdışı", "离线", "Sin conexión", "オフライン", "오프라인", "Offline" },
            ["{0}  •  {1} trophies"] = new[] { "{0}  •  {1} kupa", "{0}  •  {1} 奖杯", "{0}  •  {1} trofeos", "{0}  •  {1}トロフィー", "{0}  •  {1} 트로피", "{0}  •  {1} Pokale" },
            ["ACCEPT"]           = new[] { "KABUL", "接受", "ACEPTAR", "承認", "수락", "ANNEHMEN" },
            ["DECLINE"]          = new[] { "REDDET", "拒绝", "RECHAZAR", "拒否", "거절", "ABLEHNEN" },
            ["INVITE TO GAME"]   = new[] { "OYUNA DAVET ET", "邀请对战", "INVITAR A JUGAR", "ゲームに招待", "게임에 초대", "ZUM SPIEL EINLADEN" },
            ["ARE YOU SURE?"]    = new[] { "EMİN MİSİN?", "确定吗？", "¿SEGURO?", "本当に？", "정말요?", "BIST DU SICHER?" },
            ["REMOVE"]           = new[] { "ÇIKAR", "移除", "ELIMINAR", "削除", "삭제", "ENTFERNEN" },
            ["COPY"]             = new[] { "KOPYALA", "复制", "COPIAR", "コピー", "복사", "KOPIEREN" },
            ["FRIENDS"]          = new[] { "ARKADAŞLAR", "好友", "AMIGOS", "フレンド", "친구", "FREUNDE" },
            ["REQUESTS"]         = new[] { "DAVETLER", "请求", "SOLICITUDES", "リクエスト", "요청", "ANFRAGEN" },
            ["Friend's ID (Name#1234)"] = new[] { "Arkadaşının ID'si (Ad#1234)", "好友ID（名字#1234）", "ID de tu amigo (Nombre#1234)", "フレンドのID（名前#1234）", "친구 ID (이름#1234)", "Freundes-ID (Name#1234)" },
            ["ADD"]              = new[] { "EKLE", "添加", "AÑADIR", "追加", "추가", "HINZUFÜGEN" },
        };
    }
}
