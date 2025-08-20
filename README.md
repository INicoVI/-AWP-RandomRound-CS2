# -AWP-RandomRound-CS2

Rastgele Tur (Random Round)

Bu Counter-Strike 2 eklentisi, sunucunuzda belirli aralıklarla oyuncuların rastgele özel tur modları için oylama yapmasını sağlar. Oylama sonucunda en çok oyu alan özel tur, bir sonraki turda otomatik olarak başlatılır.

✨ Özellikler

Periyodik Oylama: Ayarlanabilir tur aralıklarında (VoteInterval), otomatik olarak özel tur oylaması başlatır.

Çeşitli Tur Seçenekleri: Eklenti, AK-47, SSG08, Deagle, Nova gibi farklı silah turu seçenekleri sunar.

Ek Oynanış Değişiklikleri: Oylanan turlara sadece kafadan vuruş (Only HS) veya düşük yer çekimi (Low Gravity) gibi rastgele modlar eklenebilir.

AWP No Scope Modu: Özel olarak AWP No Scope turu seçildiğinde, oyuncuların dürbün açması engellenir.

Komutla Oylama: Sunucu yöneticileri, oylama aralığına bakılmaksızın !rastgeleround komutuyla oylamayı manuel olarak başlatabilir.

Otomatik Konfigürasyon: Oylama sonucunda seçilen tur, ilgili sunucu ayarlarını (sv_gravity, mp_damage_headshot_only vb.) otomatik olarak değiştirir ve tur sonunda eski ayarlara geri döner.

⚙️ Kurulum

RandomRound.dll dosyasını sunucunuzdaki csgo/addons/counterstrikesharp/plugins klasörüne kopyalayın.

Eklentinin konfigürasyon dosyasını (RandomRound.json) csgo/addons/counterstrikesharp/configs/plugins klasöründe düzenleyebilirsiniz.

Sunucunuzu yeniden başlatın veya konsola css_plugins reload komutunu yazarak eklentiyi yükleyin.

🛠️ Konfigürasyon

configs/plugins/RandomRound.json dosyası üzerinden eklenti ayarlarını kişiselleştirebilirsiniz:

JSON

{
  "VoteInterval": 10,
  "VoteDuration": 30,
  "GravityValue": 200
}
VoteInterval: Kaç turda bir oylama yapılacağını belirler. (Örnek: 10 turda bir)

VoteDuration: Oylamanın kaç saniye süreceğini belirler.

GravityValue: Düşük yer çekimi turlarında yer çekiminin değerini ayarlar.

🎮 Kullanım

Eklenti, VoteInterval değerine göre otomatik olarak oylama başlatacaktır.

Oylamayı manuel olarak başlatmak için, sunucu yöneticileri oyun içinde sohbet ekranına veya konsola aşağıdaki komutu yazabilir:

!rastgeleround
veya

css_rastgeleround
📝 Geliştirici Notları
Bu eklenti, CounterStrikeSharp altyapısı kullanılarak geliştirilmiştir. Oyunculara yeni ve heyecan verici bir deneyim sunmayı hedefler. Kod, genişletilebilir bir yapıya sahiptir; yeni tur türleri ve modlar kolayca eklenebilir.

Herhangi bir sorunla karşılaşırsanız veya yeni özellikler önermek isterseniz, lütfen bu projenin GitHub sayfasında bir "Issue" açmaktan çekinmeyin.
