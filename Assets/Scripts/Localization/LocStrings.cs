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
            ["Playing offline"]       = new[] { "Çevrimdışı oynanıyor", "离线游戏中", "Jugando sin conexión", "オフラインでプレイ中", "오프라인으로 플레이 중", "Offline spielen" },

            // ── Tutorial / onboarding ───────────────────────────────────────
            ["Move with A/D"] = new[] { "A/D ile hareket et", "使用A/D移动", "Muévete con A/D", "A/Dで移動", "A/D로 이동", "Bewege dich mit A/D" },
            ["Jump with SPACE"] = new[] { "SPACE ile zıpla", "按空格键跳跃", "Salta con ESPACIO", "SPACEでジャンプ", "SPACE로 점프", "Springe mit LEERTASTE" },
            ["Pick a weapon, aim with the mouse, then fire"] = new[] { "Bir silah seç, fareyle nişan al, ateş et", "选择武器，用鼠标瞄准后开火", "Elige un arma, apunta con el ratón y dispara", "武器を選び、マウスで狙って撃つ", "무기를 선택하고 마우스로 조준한 뒤 발사", "Wähle eine Waffe, ziele mit der Maus und feuere" },

            // ── Local notifications ─────────────────────────────────────────
            ["Don't lose your streak!"] = new[] { "Serini kaybetme!", "不要失去连胜！", "¡No pierdas tu racha!", "連勝を失わないで！", "연승 기록을 잃지 마세요!", "Verliere deine Serie nicht!" },
            ["Play a match today to keep your login streak alive."] = new[] { "Serini korumak için bugün bir maç oyna.", "今天玩一局比赛以保持连胜记录。", "Juega una partida hoy para mantener tu racha.", "連勝を維持するために今日試合をプレイしよう。", "연승 기록을 유지하려면 오늘 경기를 플레이하세요.", "Spiele heute ein Match, um deine Serie zu erhalten." },
            ["Chests are waiting!"] = new[] { "Sandıklar seni bekliyor!", "宝箱在等着你！", "¡Los cofres te esperan!", "宝箱が待っています！", "상자가 기다리고 있어요!", "Truhen warten auf dich!" },
            ["You still have chests to earn today — jump into a match."] = new[] { "Bugün kazanabileceğin sandıklar var — hemen bir maça katıl.", "你今天还有宝箱可以赢取——快去参加一场比赛吧。", "Todavía puedes ganar cofres hoy — únete a una partida.", "今日獲得できる宝箱がまだあります — 試合に参加しよう。", "오늘 획득할 수 있는 상자가 남아 있어요 — 지금 경기에 참여하세요.", "Du kannst heute noch Truhen verdienen — nimm an einem Match teil." },

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
            ["SKIP"]             = new[] { "PAS", "跳过", "SALTAR", "スキップ", "건너뛰기", "PASSEN" },
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

            // ── Login screen ─────────────────────────────────────────────────
            ["Connecting with Google..."] = new[] { "Google ile bağlanılıyor...", "正在连接谷歌...", "Conectando con Google...", "Googleに接続中...", "Google에 연결 중...", "Verbindung mit Google wird hergestellt..." },
            ["Google sign-in failed."] = new[] { "Google girişi başarısız oldu.", "谷歌登录失败。", "Error al iniciar sesión con Google.", "Googleログインに失敗しました。", "Google 로그인에 실패했습니다.", "Google-Anmeldung fehlgeschlagen." },
            ["Sign in with your account to continue"] = new[] { "Devam etmek için hesabınla giriş yap", "登录你的账户以继续", "Inicia sesión con tu cuenta para continuar", "続けるにはアカウントでサインインしてください", "계속하려면 계정으로 로그인하세요", "Melde dich mit deinem Konto an, um fortzufahren" },
            ["CONTINUE WITH GOOGLE"] = new[] { "GOOGLE İLE DEVAM ET", "使用谷歌继续", "CONTINUAR CON GOOGLE", "Googleで続ける", "Google로 계속하기", "MIT GOOGLE FORTFAHREN" },
            ["SIGN IN WITH COSMIC ID"] = new[] { "COSMIC ID İLE GİRİŞ", "使用Cosmic ID登录", "INICIAR SESIÓN CON COSMIC ID", "Cosmic IDでサインイン", "Cosmic ID로 로그인", "MIT COSMIC ID ANMELDEN" },
            ["CONTINUE AS GUEST (TEST)"] = new[] { "MİSAFİR OLARAK DEVAM (TEST)", "以访客身份继续（测试）", "CONTINUAR COMO INVITADO (PRUEBA)", "ゲストとして続ける（テスト）", "게스트로 계속하기 (테스트)", "ALS GAST FORTFAHREN (TEST)" },

            // ── Login panel (Cosmic ID) ─────────────────────────────────────
            ["LINK YOUR ACCOUNT"] = new[] { "HESABINI BAĞLA", "绑定账户", "VINCULAR TU CUENTA", "アカウントを連携", "계정 연결", "KONTO VERKNÜPFEN" },
            ["SIGN IN"]          = new[] { "GİRİŞ", "登录", "INICIAR SESIÓN", "サインイン", "로그인", "ANMELDEN" },
            ["Linking an account keeps your progress saved and\nlets you continue from other devices."] = new[] {
                "Hesap bağlarsan ilerlemen hesabında saklanır ve\nbaşka cihazlardan da devam edebilirsin.",
                "绑定账户可保存你的游戏进度，\n并支持在其他设备上继续游戏。",
                "Vincular una cuenta guarda tu progreso y\nte permite continuar desde otros dispositivos.",
                "アカウントを連携すると進行状況が保存され、\n他のデバイスからも続きをプレイできます。",
                "계정을 연결하면 진행 상황이 저장되고\n다른 기기에서도 이어서 플레이할 수 있습니다.",
                "Durch die Kontoverknüpfung wird dein Fortschritt gespeichert und\ndu kannst auf anderen Geräten fortfahren." },
            ["Sign in to your account to continue\nor create a new one."] = new[] {
                "Devam etmek için hesabına gir\nveya yeni bir hesap oluştur.",
                "登录你的账户以继续，\n或创建一个新账户。",
                "Inicia sesión en tu cuenta para continuar\no crea una nueva.",
                "続けるにはアカウントにサインインするか、\n新しいアカウントを作成してください。",
                "계속하려면 계정에 로그인하거나\n새 계정을 만드세요.",
                "Melde dich an, um fortzufahren,\noder erstelle ein neues Konto." },
            ["Username"]         = new[] { "Kullanıcı adı", "用户名", "Usuario", "ユーザー名", "사용자 이름", "Benutzername" },
            ["Password"]         = new[] { "Şifre", "密码", "Contraseña", "パスワード", "비밀번호", "Passwort" },
            ["CREATE NEW ACCOUNT"] = new[] { "YENİ HESAP OLUŞTUR", "创建新账户", "CREAR NUEVA CUENTA", "新規アカウント作成", "새 계정 만들기", "NEUES KONTO ERSTELLEN" },
            ["A new account inherits your progress on this device as-is."] = new[] { "Yeni hesap, bu cihazdaki ilerlemeni olduğu gibi devralır.", "新账户将直接继承此设备上的游戏进度。", "Una cuenta nueva hereda tu progreso en este dispositivo tal cual.", "新しいアカウントは、このデバイスの進行状況をそのまま引き継ぎます。", "새 계정은 이 기기의 진행 상황을 그대로 이어받습니다.", "Ein neues Konto übernimmt deinen Fortschritt auf diesem Gerät unverändert." },
            ["FILL TEST CREDENTIALS"] = new[] { "TEST BİLGİLERİYLE DOLDUR", "填充测试信息", "RELLENAR CREDENCIALES DE PRUEBA", "テスト情報を入力", "테스트 정보 입력", "TEST-ANMELDEDATEN AUSFÜLLEN" },
            ["AuthManager not found."] = new[] { "AuthManager bulunamadı.", "未找到AuthManager。", "No se encontró AuthManager.", "AuthManagerが見つかりません。", "AuthManager를 찾을 수 없습니다.", "AuthManager nicht gefunden." },
            ["Username and password required."] = new[] { "Kullanıcı adı ve şifre gerekli.", "需要用户名和密码。", "Se requiere usuario y contraseña.", "ユーザー名とパスワードが必要です。", "사용자 이름과 비밀번호가 필요합니다.", "Benutzername und Passwort erforderlich." },
            ["Signing in..."]    = new[] { "Giriş yapılıyor...", "正在登录...", "Iniciando sesión...", "サインイン中...", "로그인 중...", "Anmeldung läuft..." },
            ["Creating account..."] = new[] { "Hesap oluşturuluyor...", "正在创建账户...", "Creando cuenta...", "アカウントを作成中...", "계정 생성 중...", "Konto wird erstellt..." },

            // ── Friend lobby (private match) ────────────────────────────────
            ["Setting up session..."] = new[] { "Oturum kuruluyor...", "正在建立会话...", "Configurando sesión...", "セッションを準備中...", "세션 설정 중...", "Sitzung wird eingerichtet..." },
            ["Couldn't set up the session, try again."] = new[] { "Oturum kurulamadı, tekrar dene.", "无法建立会话，请重试。", "No se pudo configurar la sesión, inténtalo de nuevo.", "セッションを準備できませんでした。もう一度お試しください。", "세션을 설정할 수 없습니다. 다시 시도하세요.", "Sitzung konnte nicht eingerichtet werden, versuche es erneut." },
            ["Invite sent, waiting for {0}..."] = new[] { "Davet gönderildi, {0} bekleniyor...", "邀请已发送，正在等待 {0}...", "Invitación enviada, esperando a {0}...", "招待を送信しました。{0}を待っています...", "초대를 보냈습니다. {0}님을 기다리는 중...", "Einladung gesendet, warte auf {0}..." },
            ["Couldn't send invite: {0}"] = new[] { "Davet gönderilemedi: {0}", "邀请发送失败：{0}", "No se pudo enviar la invitación: {0}", "招待を送信できませんでした: {0}", "초대를 보낼 수 없습니다: {0}", "Einladung konnte nicht gesendet werden: {0}" },
            ["Waiting for host to start..."] = new[] { "Host'un başlatması bekleniyor...", "等待房主开始...", "Esperando a que el anfitrión inicie...", "ホストの開始を待っています...", "호스트가 시작하기를 기다리는 중...", "Warte darauf, dass der Host startet..." },
            ["Waiting..."]       = new[] { "Bekleniyor...", "等待中...", "Esperando...", "待機中...", "대기 중...", "Warten..." },
            ["Opponent"]         = new[] { "Rakip", "对手", "Oponente", "対戦相手", "상대", "Gegner" },
            ["Ready! You can start the match."] = new[] { "Hazır! Maçı başlatabilirsin.", "准备就绪！你可以开始对战了。", "¡Listo! Puedes iniciar la partida.", "準備完了！対戦を開始できます。", "준비 완료! 대전을 시작할 수 있습니다.", "Bereit! Du kannst das Match starten." },
            ["Cancelling..."]    = new[] { "İptal ediliyor...", "正在取消...", "Cancelando...", "キャンセル中...", "취소 중...", "Wird abgebrochen..." },
            ["PRIVATE MATCH"]    = new[] { "ÖZEL MAÇ", "私人对战", "PARTIDA PRIVADA", "プライベートマッチ", "비공개 대전", "PRIVATES MATCH" },
            ["Friendly match — trophies unaffected"] = new[] { "Dostluk maçı — kupa değişmez", "友谊赛 — 不影响奖杯", "Partida amistosa — no afecta a los trofeos", "フレンドリーマッチ — トロフィーは変動しません", "친선 경기 — 트로피에 영향 없음", "Freundschaftsspiel — Pokale bleiben unverändert" },
            ["OPPONENT"]         = new[] { "RAKİP", "对手", "OPONENTE", "対戦相手", "상대", "GEGNER" },
            ["START"]            = new[] { "BAŞLAT", "开始", "INICIAR", "開始", "시작", "START" },
            ["CANCEL"]           = new[] { "İPTAL", "取消", "CANCELAR", "キャンセル", "취소", "ABBRECHEN" },

            // ── Online lobby (Quick Match) ──────────────────────────────────
            ["Searching for opponent..."] = new[] { "Rakip aranıyor...", "正在寻找对手...", "Buscando oponente...", "対戦相手を探しています...", "상대를 찾는 중...", "Suche nach Gegner..." },
            ["Matchmaking failed, try again."] = new[] { "Eşleşme başarısız, tekrar dene.", "匹配失败，请重试。", "Error al emparejar, inténtalo de nuevo.", "マッチングに失敗しました。もう一度お試しください。", "매칭에 실패했습니다. 다시 시도하세요.", "Matchmaking fehlgeschlagen, versuche es erneut." },
            ["Waiting for opponent..."] = new[] { "Rakip bekleniyor...", "等待对手中...", "Esperando oponente...", "対戦相手を待っています...", "상대를 기다리는 중...", "Warte auf Gegner..." },
            ["Opponent found, starting..."] = new[] { "Rakip bulundu, başlatılıyor...", "已找到对手，正在开始...", "Oponente encontrado, iniciando...", "対戦相手が見つかりました。開始しています...", "상대를 찾았습니다. 시작하는 중...", "Gegner gefunden, wird gestartet..." },
            ["QUICK MATCH — RANKED"] = new[] { "HIZLI EŞLEŞME — DERECELİ", "快速匹配 — 排位", "PARTIDA RÁPIDA — CLASIFICATORIA", "クイックマッチ — ランク戦", "빠른 대전 — 랭크전", "SCHNELLES SPIEL — GEWERTET" },
            ["Win +30 trophies  •  Loss −20 trophies"] = new[] { "Galibiyet +30 kupa  •  Mağlubiyet −20 kupa", "胜利 +30 奖杯  •  失败 −20 奖杯", "Victoria +30 trofeos  •  Derrota −20 trofeos", "勝利で+30トロフィー  •  敗北で−20トロフィー", "승리 시 +30 트로피  •  패배 시 −20 트로피", "Sieg +30 Pokale  •  Niederlage −20 Pokale" },
            ["Send an invite from the SOCIAL panel to play\nwith a friend — friendly match, trophies unaffected."] = new[] {
                "Arkadaşınla oynamak için SOSYAL panelinden\ndavet gönder — dostluk maçı, kupa değişmez.",
                "在社交面板中发送邀请即可\n与好友对战 — 友谊赛，不影响奖杯。",
                "Envía una invitación desde el panel SOCIAL para jugar\ncon un amigo — partida amistosa, no afecta a los trofeos.",
                "フレンドと遊ぶにはSOCIALパネルから\n招待を送ってください — フレンドリーマッチ、トロフィーは変動しません。",
                "친구와 플레이하려면 소셜 패널에서\n초대를 보내세요 — 친선 경기, 트로피에 영향 없음.",
                "Sende eine Einladung über das SOZIAL-Panel, um mit\neinem Freund zu spielen — Freundschaftsspiel, Pokale bleiben unverändert." },

            // ── Invite popup ─────────────────────────────────────────────────
            ["A friend"]         = new[] { "Bir arkadaşın", "一位好友", "Un amigo", "フレンド", "친구", "Ein Freund" },
            ["{0} invited you to a match!"] = new[] { "{0} seni maça davet etti!", "{0} 邀请你进行对战！", "¡{0} te ha invitado a una partida!", "{0}があなたを対戦に招待しました！", "{0}님이 대전에 초대했습니다!", "{0} hat dich zu einem Match eingeladen!" },
            ["This invite is no longer valid."] = new[] { "Davet artık geçerli değil.", "该邀请已失效。", "Esta invitación ya no es válida.", "この招待は無効になりました。", "이 초대는 더 이상 유효하지 않습니다.", "Diese Einladung ist nicht mehr gültig." },
            ["JOIN"]             = new[] { "KATIL", "加入", "UNIRSE", "参加", "참가", "BEITRETEN" },

            // ── In-game menu (ESC pause menu) ───────────────────────────────
            ["GAME MENU"]        = new[] { "OYUN MENÜSÜ", "游戏菜单", "MENÚ DEL JUEGO", "ゲームメニュー", "게임 메뉴", "SPIELMENÜ" },
            ["Resume"]           = new[] { "Devam Et", "继续", "Reanudar", "再開", "계속하기", "Fortsetzen" },
            ["Settings"]         = new[] { "Ayarlar", "设置", "Ajustes", "設定", "설정", "Einstellungen" },
            ["Return to Main Menu"] = new[] { "Ana Menüye Dön", "返回主菜单", "Volver al menú principal", "メインメニューに戻る", "메인 메뉴로 돌아가기", "Zurück zum Hauptmenü" },
            ["Audio"]            = new[] { "Ses", "音频", "Audio", "サウンド", "오디오", "Audio" },
            ["Graphics"]         = new[] { "Grafik", "画面", "Gráficos", "グラフィック", "그래픽", "Grafik" },
            ["Controls"]         = new[] { "Kontroller", "操作", "Controles", "操作設定", "조작", "Steuerung" },
            ["← Back"]           = new[] { "← Geri", "← 返回", "← Atrás", "← 戻る", "← 뒤로", "← Zurück" },
            ["SFX"]              = new[] { "Efektler", "音效", "Efectos", "効果音", "효과음", "Effekte" },
            ["Apply"]            = new[] { "Uygula", "应用", "Aplicar", "適用", "적용", "Übernehmen" },
            ["Click a button, then press the new key (Esc to cancel)"] = new[] { "Bir butona tıkla, sonra yeni tuşa bas (iptal için Esc)", "点击一个按钮，然后按下新按键（按Esc取消）", "Haz clic en un botón y luego pulsa la nueva tecla (Esc para cancelar)", "ボタンをクリックしてから新しいキーを押してください（Escでキャンセル）", "버튼을 클릭한 후 새 키를 누르세요 (취소하려면 Esc)", "Klicke auf eine Schaltfläche und drücke dann die neue Taste (Esc zum Abbrechen)" },

            // ── Reward toasts ────────────────────────────────────────────────
            ["Achievement: {0}"] = new[] { "Başarım: {0}", "成就：{0}", "Logro: {0}", "実績: {0}", "업적: {0}", "Erfolg: {0}" },
            ["Level Up!"]        = new[] { "Seviye Atladın!", "升级了！", "¡Subiste de nivel!", "レベルアップ！", "레벨 업!", "Level-Aufstieg!" },
            ["Prestige!"]        = new[] { "Prestij!", "威望提升！", "¡Prestigio!", "プレステージ！", "프레스티지!", "Prestige!" },
            ["New prestige rank: {0}"] = new[] { "Yeni prestij seviyesi: {0}", "新威望等级：{0}", "Nuevo rango de prestigio: {0}", "新しいプレステージランク: {0}", "새 프레스티지 등급: {0}", "Neuer Prestige-Rang: {0}" },
            ["+ Costume"]        = new[] { "+ Kostüm", "+ 装扮", "+ Disfraz", "+ コスチューム", "+ 코스튬", "+ Kostüm" },
            ["{0} Chest Opened"] = new[] { "{0} Sandık Açıldı", "{0}宝箱已开启", "Cofre {0} Abierto", "{0}チェストを開封", "{0} 상자 개봉", "{0}-Truhe geöffnet" },
            ["{0}-Day Login Streak!"] = new[] { "{0} Günlük Giriş Serisi!", "连续登录{0}天！", "¡Racha de inicio de sesión de {0} días!", "{0}日連続ログイン！", "{0}일 연속 로그인!", "{0}-Tage-Login-Serie!" },

            // ── Auth errors ──────────────────────────────────────────────────
            ["Not available on this platform."] = new[] { "Bu platformda kullanılamıyor.", "此平台不可用。", "No disponible en esta plataforma.", "このプラットフォームでは利用できません。", "이 플랫폼에서는 사용할 수 없습니다.", "Auf dieser Plattform nicht verfügbar." },
            ["Platform sign-in couldn't be completed."] = new[] { "Platform girişi tamamlanamadı.", "平台登录未能完成。", "No se pudo completar el inicio de sesión de la plataforma.", "プラットフォームサインインを完了できませんでした。", "플랫폼 로그인을 완료할 수 없습니다.", "Plattform-Anmeldung konnte nicht abgeschlossen werden." },
            ["Couldn't switch accounts: {0}"] = new[] { "Hesap değiştirilemedi: {0}", "无法切换账户：{0}", "No se pudo cambiar de cuenta: {0}", "アカウントを切り替えられませんでした: {0}", "계정을 전환할 수 없습니다: {0}", "Konto konnte nicht gewechselt werden: {0}" },
            ["Couldn't reach the server: {0}"] = new[] { "Sunucuya ulaşılamadı: {0}", "无法连接服务器：{0}", "No se pudo contactar con el servidor: {0}", "サーバーに接続できませんでした: {0}", "서버에 연결할 수 없습니다: {0}", "Server konnte nicht erreicht werden: {0}" },
            ["Unexpected error: {0}"] = new[] { "Beklenmeyen hata: {0}", "意外错误：{0}", "Error inesperado: {0}", "予期しないエラー: {0}", "예기치 않은 오류: {0}", "Unerwarteter Fehler: {0}" },
            ["Username must be 3-20 characters."] = new[] { "Kullanıcı adı 3-20 karakter olmalı.", "用户名长度必须为3-20个字符。", "El usuario debe tener entre 3 y 20 caracteres.", "ユーザー名は3〜20文字である必要があります。", "사용자 이름은 3~20자여야 합니다.", "Benutzername muss 3-20 Zeichen lang sein." },
            ["Password must be 8-30 characters."] = new[] { "Şifre 8-30 karakter olmalı.", "密码长度必须为8-30个字符。", "La contraseña debe tener entre 8 y 30 caracteres.", "パスワードは8〜30文字である必要があります。", "비밀번호는 8~30자여야 합니다.", "Passwort muss 8-30 Zeichen lang sein." },

            // ── Network status banner ───────────────────────────────────────
            ["Connection lost, reconnecting... (attempt {0}/{1})"] = new[] { "Bağlantı koptu, yeniden bağlanılıyor... (deneme {0}/{1})", "连接已断开，正在重新连接...（第{0}/{1}次尝试）", "Conexión perdida, reconectando... (intento {0}/{1})", "接続が切断されました。再接続中...（試行 {0}/{1}）", "연결이 끊겼습니다. 재연결 중... (시도 {0}/{1})", "Verbindung verloren, erneute Verbindung wird hergestellt... (Versuch {0}/{1})" },
            ["Connection lost completely."] = new[] { "Bağlantı tamamen kesildi.", "连接已完全中断。", "Se ha perdido la conexión por completo.", "接続が完全に切断されました。", "연결이 완전히 끊겼습니다.", "Verbindung vollständig unterbrochen." },
            ["Opponent didn't return, match is ending..."] = new[] { "Rakip geri dönmedi, maç sona eriyor...", "对手未返回，对战即将结束...", "El oponente no regresó, la partida está terminando...", "対戦相手が戻りませんでした。対戦を終了します...", "상대가 돌아오지 않아 대전을 종료합니다...", "Gegner ist nicht zurückgekehrt, Match wird beendet..." },
            ["Opponent disconnected, waiting for reconnect..."] = new[] { "Rakip bağlantısı koptu, yeniden bağlanması bekleniyor...", "对手已断线，等待重新连接...", "El oponente se desconectó, esperando reconexión...", "対戦相手の接続が切断されました。再接続を待っています...", "상대의 연결이 끊겼습니다. 재연결을 기다리는 중...", "Gegner getrennt, warte auf Wiederverbindung..." },

            // ── Friends errors ───────────────────────────────────────────────
            ["Enter an ID."]     = new[] { "Bir ID gir.", "请输入ID。", "Introduce un ID.", "IDを入力してください。", "ID를 입력하세요.", "Gib eine ID ein." },
            ["No player found with this ID."] = new[] { "Bu ID ile bir oyuncu bulunamadı.", "未找到该ID对应的玩家。", "No se encontró ningún jugador con este ID.", "このIDのプレイヤーが見つかりません。", "이 ID를 가진 플레이어를 찾을 수 없습니다.", "Kein Spieler mit dieser ID gefunden." },
            ["This player is already your friend or a request was already sent."] = new[] { "Bu oyuncu zaten arkadaşın veya istek zaten gönderilmiş.", "该玩家已是你的好友，或请求已发送。", "Este jugador ya es tu amigo o ya se envió una solicitud.", "このプレイヤーはすでにフレンドか、リクエストが送信済みです。", "이 플레이어는 이미 친구이거나 요청이 이미 전송되었습니다.", "Dieser Spieler ist bereits dein Freund oder eine Anfrage wurde bereits gesendet." },
            ["Action failed: {0}"] = new[] { "İşlem başarısız: {0}", "操作失败：{0}", "Acción fallida: {0}", "操作に失敗しました: {0}", "작업 실패: {0}", "Aktion fehlgeschlagen: {0}" },

            // ── Language selector ────────────────────────────────────────────
            ["Language"]         = new[] { "Dil", "语言", "Idioma", "言語", "언어", "Sprache" },

            // ── Training mode ────────────────────────────────────────────────
            ["TRAINING"]         = new[] { "ANTRENMAN", "训练", "ENTRENAMIENTO", "トレーニング", "훈련", "TRAINING" },

            // ── Avatar picker ────────────────────────────────────────────────
            ["CHOOSE AVATAR"]    = new[] { "AVATAR SEÇ", "选择头像", "ELEGIR AVATAR", "アバターを選択", "아바타 선택", "AVATAR WÄHLEN" },
            ["SELECTED"]         = new[] { "SEÇİLİ", "已选择", "SELECCIONADO", "選択中", "선택됨", "AUSGEWÄHLT" },

            // ── Legal links ──────────────────────────────────────────────────
            ["Privacy Policy"]    = new[] { "Gizlilik Politikası", "隐私政策", "Política de Privacidad", "プライバシーポリシー", "개인정보 처리방침", "Datenschutzrichtlinie" },
            ["Terms of Service"]  = new[] { "Kullanım Koşulları", "服务条款", "Términos de Servicio", "利用規約", "이용약관", "Nutzungsbedingungen" },
        };
    }
}
